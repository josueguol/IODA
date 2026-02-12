using IODA.Indexing.Application.Commands;
using IODA.Indexing.Application.Interfaces;
using IODA.Indexing.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IODA.Indexing.API.Controllers;

[ApiController]
[Route("api/indexing")]
public class IndexingController : ControllerBase
{
    private readonly IMediator _mediator;

    public IndexingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Buscar contenido indexado (publicado).</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(SearchResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResultDto>> Search(
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new SearchContentQuery(q ?? string.Empty, page, pageSize, contentType), cancellationToken);
        var dto = new SearchResultDto(
            result.Total,
            result.Items.Select(i => new IndexedContentHitDto(i.ContentId, i.VersionId, i.Title, i.ContentType, i.PublishedAt)).ToList());
        return Ok(dto);
    }

    /// <summary>Indexar manualmente un contenido publicado (normalmente se hace vía evento ContentPublished).</summary>
    [HttpPost("index")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IndexContent([FromBody] IndexContentRequestDto request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new IndexContentCommand(
            request.ContentId,
            request.VersionId,
            request.Title,
            request.ContentType,
            request.PublishedAt,
            request.Fields), cancellationToken);
        return NoContent();
    }

    /// <summary>Eliminar contenido del índice (normalmente se hace vía evento ContentUnpublished).</summary>
    [HttpDelete("index/{contentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveFromIndex(Guid contentId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveFromIndexCommand(contentId), cancellationToken);
        return NoContent();
    }
}

public record SearchResultDto(long Total, IReadOnlyList<IndexedContentHitDto> Items);

public record IndexedContentHitDto(Guid ContentId, Guid VersionId, string Title, string ContentType, DateTime PublishedAt);

public record IndexContentRequestDto(
    Guid ContentId,
    Guid VersionId,
    string Title,
    string ContentType,
    DateTime PublishedAt,
    IReadOnlyDictionary<string, object>? Fields = null);
