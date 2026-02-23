using IODA.Core.Application.Commands.Environments;
using IODA.Core.Application.Commands.Projects;
using IODA.Core.Application.DTOs;
using IODA.Core.Application.Queries.Environments;
using IODA.Core.Application.Queries.Projects;
using IODA.Core.API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IODA.Core.API.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    private const string PERMISSION_CLAIM_TYPE = "permission";
    private const string PROJECT_EDIT_PERMISSION = "project.edit";
    private const string PROJECT_CREATE_PERMISSION = "project.create";
    /// <summary>Rol con todos los privilegios; si el JWT no trae permisos aún (ej. primer usuario), se permite por rol.</summary>
    private const string SUPER_ADMIN_ROLE_NAME = "SuperAdmin";

    public ProjectsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Listar proyectos (paginado). Requiere project.create o project.edit; si no hay proyectos, permite bootstrap; si hay uno y es del usuario, también.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResultDto<ProjectDto>>> List(CancellationToken cancellationToken = default)
    {
        var query = Request.Query;
        var page = 1;
        var pageSize = 20;
        if (query.TryGetValue("page", out var pageVal) && int.TryParse(pageVal, out var p) && p >= 1)
            page = p;
        if (query.TryGetValue("pageSize", out var sizeVal) && int.TryParse(sizeVal, out var s) && s >= 1 && s <= 100)
            pageSize = s;

        var canList = User.IsInRole(SUPER_ADMIN_ROLE_NAME) ||
                      User.HasClaim(PERMISSION_CLAIM_TYPE, PROJECT_CREATE_PERMISSION) ||
                      User.HasClaim(PERMISSION_CLAIM_TYPE, PROJECT_EDIT_PERMISSION);
        if (!canList)
        {
            var bootstrapCheck = await _mediator.Send(new GetProjectsPagedQuery(1, 1), cancellationToken);
            var userId = User.GetUserId();
            var isSingleProjectOwnedByCurrentUser =
                userId.HasValue &&
                bootstrapCheck.TotalCount == 1 &&
                bootstrapCheck.Items.Count == 1 &&
                bootstrapCheck.Items[0].CreatedBy == userId.Value;

            if (bootstrapCheck.TotalCount > 0 && !isSingleProjectOwnedByCurrentUser)
                return Forbid();
        }

        var result = await _mediator.Send(new GetProjectsPagedQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>Crear un nuevo proyecto. Requiere project.create o project.edit; si no hay proyectos, permite bootstrap al primer usuario.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var canCreate = User.IsInRole(SUPER_ADMIN_ROLE_NAME) ||
                        User.HasClaim(PERMISSION_CLAIM_TYPE, PROJECT_CREATE_PERMISSION) ||
                        User.HasClaim(PERMISSION_CLAIM_TYPE, PROJECT_EDIT_PERMISSION);
        if (!canCreate)
        {
            var bootstrapCheck = await _mediator.Send(new GetProjectsPagedQuery(1, 1), cancellationToken);
            if (bootstrapCheck.TotalCount > 0)
                return Forbid();
        }

        var command = new CreateProjectCommand(
            request.Name,
            request.Description,
            request.CreatedBy);
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { projectId = id }, id);
    }

    /// <summary>Obtener un proyecto por ID. Requiere project.access (project.edit o project.create).</summary>
    [HttpGet("{projectId:guid}")]
    [Authorize(Policy = "project.access")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> GetById(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProjectByIdQuery(projectId), cancellationToken);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>Listar entornos del proyecto. Requiere project.access.</summary>
    [HttpGet("{projectId:guid}/environments")]
    [Authorize(Policy = "project.access")]
    [ProducesResponseType(typeof(IReadOnlyList<EnvironmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EnvironmentDto>>> ListEnvironments(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEnvironmentsByProjectQuery(projectId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Obtener un entorno por ID. Requiere project.access.</summary>
    [HttpGet("{projectId:guid}/environments/{environmentId:guid}")]
    [Authorize(Policy = "project.access")]
    [ProducesResponseType(typeof(EnvironmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnvironmentDto>> GetEnvironmentById(Guid projectId, Guid environmentId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEnvironmentByIdQuery(projectId, environmentId), cancellationToken);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>Crear un entorno en el proyecto. Requiere project.access.</summary>
    [HttpPost("{projectId:guid}/environments")]
    [Authorize(Policy = "project.access")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> CreateEnvironment(Guid projectId, [FromBody] CreateEnvironmentRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateEnvironmentCommand(
            projectId,
            request.Name,
            request.Description,
            request.CreatedBy);
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetEnvironmentById), new { projectId, environmentId = id }, id);
    }
}

public record CreateProjectRequest(string Name, string? Description, Guid CreatedBy);

public record CreateEnvironmentRequest(string Name, string? Description, Guid CreatedBy);
