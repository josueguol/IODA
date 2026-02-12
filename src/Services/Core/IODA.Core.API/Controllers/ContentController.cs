using IODA.Core.Application.Commands.Content;
using IODA.Core.Application.DTOs;
using IODA.Core.Application.Queries.Content;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IODA.Core.API.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}")]
[Authorize]
public class ContentController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Crear contenido en el proyecto.</summary>
    [HttpPost("content")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> Create(
        Guid projectId,
        [FromBody] CreateContentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateContentCommand(
            projectId,
            request.EnvironmentId,
            request.SiteId,
            request.SchemaId,
            request.Title,
            request.ContentType,
            request.Fields,
            request.CreatedBy);
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { projectId, contentId = id }, id);
    }

    /// <summary>Obtener contenido por ID.</summary>
    [HttpGet("content/{contentId:guid}")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentDto>> GetById(
        Guid projectId,
        Guid contentId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetContentByIdQuery(contentId), cancellationToken);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>Listar contenido del proyecto (paginado).</summary>
    [HttpGet("content")]
    [ProducesResponseType(typeof(PagedResultDto<ContentListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ContentListItemDto>>> List(
        Guid projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? contentType = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? siteId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ListContentByProjectQuery(projectId, page, pageSize, contentType, status, siteId),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Actualizar contenido.</summary>
    [HttpPut("content/{contentId:guid}")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentDto>> Update(
        Guid projectId,
        Guid contentId,
        [FromBody] UpdateContentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateContentCommand(
            contentId,
            request.Title,
            request.Fields,
            request.UpdatedBy);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Publicar contenido.</summary>
    [HttpPost("content/{contentId:guid}/publish")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentDto>> Publish(
        Guid projectId,
        Guid contentId,
        [FromBody] PublishContentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new PublishContentCommand(contentId, request.PublishedBy);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Despublicar contenido.</summary>
    [HttpPost("content/{contentId:guid}/unpublish")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentDto>> Unpublish(
        Guid projectId,
        Guid contentId,
        [FromBody] UnpublishContentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UnpublishContentCommand(contentId, request.Reason, request.UnpublishedBy);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Eliminar contenido.</summary>
    [HttpDelete("content/{contentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(
        Guid projectId,
        Guid contentId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteContentCommand(contentId), cancellationToken);
        return NoContent();
    }

    /// <summary>Listar todas las versiones del contenido (historial de cambios).</summary>
    [HttpGet("content/{contentId:guid}/versions")]
    [ProducesResponseType(typeof(IReadOnlyList<ContentVersionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ContentVersionDto>>> ListVersions(
        Guid projectId,
        Guid contentId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ListContentVersionsQuery(contentId),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Obtener una versión concreta del contenido.</summary>
    [HttpGet("content/{contentId:guid}/versions/{versionNumber:int}")]
    [ProducesResponseType(typeof(ContentVersionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentVersionDto>> GetVersion(
        Guid projectId,
        Guid contentId,
        int versionNumber,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetContentVersionQuery(contentId, versionNumber),
            cancellationToken);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>Listar contenido publicado en un entorno (paginado).</summary>
    [HttpGet("environments/{environmentId:guid}/content/published")]
    [ProducesResponseType(typeof(PagedResultDto<ContentListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ContentListItemDto>>> GetPublished(
        Guid projectId,
        Guid environmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? siteId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetPublishedContentQuery(projectId, environmentId, page, pageSize, siteId),
            cancellationToken);
        return Ok(result);
    }
}

public record CreateContentRequest(
    Guid EnvironmentId,
    Guid? SiteId,
    Guid SchemaId,
    string Title,
    string ContentType,
    Dictionary<string, object> Fields,
    Guid CreatedBy);

public record UpdateContentRequest(string Title, Dictionary<string, object> Fields, Guid UpdatedBy);

public record PublishContentRequest(Guid PublishedBy);

public record UnpublishContentRequest(string Reason, Guid UnpublishedBy);
