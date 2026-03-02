using IODA.Core.API.Extensions;
using IODA.Core.Application.Commands.Content;
using IODA.Core.Application.DTOs;
using IODA.Core.Application.Queries.Content;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IODA.Core.API.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}")]
[Authorize(Policy = "content.edit")]
public class ContentController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Crear contenido en el proyecto. CreatedBy se toma del JWT (ADR-011).</summary>
    [HttpPost("content")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> Create(
        Guid projectId,
        [FromBody] CreateContentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var command = new CreateContentCommand(
            projectId,
            request.EnvironmentId,
            request.SiteId,
            request.ParentContentId,
            request.SchemaId,
            request.Title,
            request.Slug,
            request.ContentType,
            request.Fields,
            request.TagIds,
            request.HierarchyIds,
            request.PrimaryHierarchyId,
            request.SiteIds,
            request.SiteUrls?.Select(x => new ContentSiteUrlInput(x.SiteId, x.Path)).ToList(),
            userId.Value,
            request.Order);
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
        [FromQuery] Guid? parentContentId = null,
        [FromQuery] Guid? sectionId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ListContentByProjectQuery(projectId, page, pageSize, contentType, status, siteId, parentContentId, sectionId),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Actualizar contenido. UpdatedBy se toma del JWT (ADR-011).</summary>
    [HttpPut("content/{contentId:guid}")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentDto>> Update(
        Guid projectId,
        Guid contentId,
        [FromBody] UpdateContentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var command = new UpdateContentCommand(
            contentId,
            request.Title,
            request.Slug,
            request.Fields,
            userId.Value,
            request.ParentContentId,
            request.Order,
            request.TagIds,
            request.HierarchyIds,
            request.PrimaryHierarchyId,
            request.SiteIds,
            request.SiteUrls?.Select(x => new ContentSiteUrlInput(x.SiteId, x.Path)).ToList());
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Publicar contenido. PublishedBy se toma del JWT (ADR-011).</summary>
    [HttpPost("content/{contentId:guid}/publish")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentDto>> Publish(
        Guid projectId,
        Guid contentId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var command = new PublishContentCommand(contentId, userId.Value);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Despublicar contenido. UnpublishedBy se toma del JWT (ADR-011).</summary>
    [HttpPost("content/{contentId:guid}/unpublish")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentDto>> Unpublish(
        Guid projectId,
        Guid contentId,
        [FromBody] UnpublishContentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var command = new UnpublishContentCommand(contentId, request.Reason, userId.Value);
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

    /// <summary>Añade un bloque al contenido.</summary>
    [HttpPost("content/{contentId:guid}/blocks")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> AddBlock(
        Guid projectId,
        Guid contentId,
        [FromBody] AddContentBlockRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddContentBlockCommand(contentId, request.BlockType, request.Order, request.Payload);
        var blockId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { projectId, contentId }, blockId);
    }

    /// <summary>Actualiza un bloque (payload y/o orden).</summary>
    [HttpPut("content/{contentId:guid}/blocks/{blockId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateBlock(
        Guid projectId,
        Guid contentId,
        Guid blockId,
        [FromBody] UpdateContentBlockRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateContentBlockCommand(blockId, request.Payload, request.Order);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Elimina un bloque.</summary>
    [HttpDelete("content/{contentId:guid}/blocks/{blockId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteBlock(
        Guid projectId,
        Guid contentId,
        Guid blockId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveContentBlockCommand(blockId), cancellationToken);
        return NoContent();
    }

    /// <summary>Reordena los bloques del contenido según la lista de ids.</summary>
    [HttpPost("content/{contentId:guid}/blocks/reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ReorderBlocks(
        Guid projectId,
        Guid contentId,
        [FromBody] ReorderContentBlocksRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ReorderContentBlocksCommand(contentId, request.BlockIds);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}

public record CreateContentRequest(
    Guid EnvironmentId,
    Guid? SiteId,
    Guid? ParentContentId,
    Guid SchemaId,
    string Title,
    string? Slug,
    string ContentType,
    Dictionary<string, object> Fields,
    IReadOnlyList<Guid>? TagIds,
    IReadOnlyList<Guid>? HierarchyIds,
    Guid? PrimaryHierarchyId,
    IReadOnlyList<Guid>? SiteIds,
    IReadOnlyList<ContentSiteUrlRequest>? SiteUrls = null,
    int? Order = null);

public record UpdateContentRequest(
    string Title,
    string? Slug,
    Dictionary<string, object> Fields,
    Guid? ParentContentId,
    int? Order = null,
    IReadOnlyList<Guid>? TagIds = null,
    IReadOnlyList<Guid>? HierarchyIds = null,
    Guid? PrimaryHierarchyId = null,
    IReadOnlyList<Guid>? SiteIds = null,
    IReadOnlyList<ContentSiteUrlRequest>? SiteUrls = null);

public record ContentSiteUrlRequest(Guid SiteId, string Path);

public record UnpublishContentRequest(string Reason);

public record AddContentBlockRequest(string BlockType, int Order, Dictionary<string, object>? Payload = null);
public record UpdateContentBlockRequest(Dictionary<string, object>? Payload = null, int? Order = null);
public record ReorderContentBlocksRequest(IReadOnlyList<Guid> BlockIds);
