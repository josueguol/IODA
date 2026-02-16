using IODA.Core.Application.Commands.Environments;
using IODA.Core.Application.Commands.Projects;
using IODA.Core.Application.DTOs;
using IODA.Core.Application.Queries.Environments;
using IODA.Core.Application.Queries.Projects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IODA.Core.API.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize(Policy = "project.edit")]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProjectsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Listar proyectos (paginado). Requiere permiso project.edit. Sin permiso → 403 Forbidden.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResultDto<ProjectDto>>> List(CancellationToken cancellationToken = default)
    {
        // Leer query a mano para no devolver 400 por model binding (ej. page= vacío o no numérico); primer usuario / clientes antiguos pueden no enviar params.
        var query = Request.Query;
        var page = 1;
        var pageSize = 20;
        if (query.TryGetValue("page", out var pageVal) && int.TryParse(pageVal, out var p) && p >= 1)
            page = p;
        if (query.TryGetValue("pageSize", out var sizeVal) && int.TryParse(sizeVal, out var s) && s >= 1 && s <= 100)
            pageSize = s;
        var result = await _mediator.Send(new GetProjectsPagedQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>Crear un nuevo proyecto.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateProjectCommand(
            request.Name,
            request.Description,
            request.CreatedBy);
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { projectId = id }, id);
    }

    /// <summary>Obtener un proyecto por ID.</summary>
    [HttpGet("{projectId:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> GetById(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProjectByIdQuery(projectId), cancellationToken);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>Listar entornos del proyecto.</summary>
    [HttpGet("{projectId:guid}/environments")]
    [ProducesResponseType(typeof(IReadOnlyList<EnvironmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EnvironmentDto>>> ListEnvironments(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEnvironmentsByProjectQuery(projectId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Obtener un entorno por ID.</summary>
    [HttpGet("{projectId:guid}/environments/{environmentId:guid}")]
    [ProducesResponseType(typeof(EnvironmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnvironmentDto>> GetEnvironmentById(Guid projectId, Guid environmentId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEnvironmentByIdQuery(projectId, environmentId), cancellationToken);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>Crear un entorno en el proyecto.</summary>
    [HttpPost("{projectId:guid}/environments")]
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
