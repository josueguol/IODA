using IODA.Core.Application.Commands.Sites;
using IODA.Core.Application.DTOs;
using IODA.Core.Application.Queries.Sites;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IODA.Core.API.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}")]
[Authorize(Policy = "site.edit")]
public class SitesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SitesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Crear un sitio en el proyecto.</summary>
    [HttpPost("sites")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> Create(
        Guid projectId,
        [FromBody] CreateSiteRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateSiteCommand(
            projectId,
            request.EnvironmentId,
            request.Name,
            request.Domain,
            request.Subdomain,
            request.Subpath,
            request.ThemeId,
            request.UrlTemplate,
            request.CreatedBy);
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { projectId, siteId = id }, id);
    }

    /// <summary>Listar sitios del proyecto.</summary>
    [HttpGet("sites")]
    [ProducesResponseType(typeof(IReadOnlyList<SiteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SiteDto>>> List(
        Guid projectId,
        [FromQuery] Guid? environmentId = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SiteDto> result;
        if (environmentId.HasValue)
        {
            result = await _mediator.Send(new ListSitesByProjectAndEnvironmentQuery(projectId, environmentId.Value), cancellationToken);
        }
        else
        {
            result = await _mediator.Send(new ListSitesByProjectQuery(projectId), cancellationToken);
        }
        return Ok(result);
    }

    /// <summary>Obtener un sitio por ID.</summary>
    [HttpGet("sites/{siteId:guid}")]
    [ProducesResponseType(typeof(SiteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SiteDto>> GetById(
        Guid projectId,
        Guid siteId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSiteByIdQuery(siteId), cancellationToken);
        if (result == null)
            return NotFound();
        if (result.ProjectId != projectId)
            return NotFound();
        return Ok(result);
    }

    /// <summary>Actualizar un sitio.</summary>
    [HttpPut("sites/{siteId:guid}")]
    [ProducesResponseType(typeof(SiteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SiteDto>> Update(
        Guid projectId,
        Guid siteId,
        [FromBody] UpdateSiteRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateSiteCommand(
            siteId,
            request.Name,
            request.Domain,
            request.Subdomain,
            request.Subpath,
            request.ThemeId,
            request.UrlTemplate);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.ProjectId != projectId)
            return NotFound();
        return Ok(result);
    }

    /// <summary>Activar un sitio.</summary>
    [HttpPost("sites/{siteId:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(
        Guid projectId,
        Guid siteId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ActivateSiteCommand(siteId), cancellationToken);
        return NoContent();
    }

    /// <summary>Desactivar un sitio.</summary>
    [HttpPost("sites/{siteId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(
        Guid projectId,
        Guid siteId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivateSiteCommand(siteId), cancellationToken);
        return NoContent();
    }

    /// <summary>Eliminar un sitio.</summary>
    [HttpDelete("sites/{siteId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid projectId,
        Guid siteId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteSiteCommand(siteId), cancellationToken);
        return NoContent();
    }
}

public record CreateSiteRequest(
    Guid? EnvironmentId,
    string Name,
    string Domain,
    Guid CreatedBy,
    string? Subdomain = null,
    string? Subpath = null,
    string? ThemeId = null,
    string? UrlTemplate = null);

public record UpdateSiteRequest(
    string Name,
    string Domain,
    string? Subdomain = null,
    string? Subpath = null,
    string? ThemeId = null,
    string? UrlTemplate = null);
