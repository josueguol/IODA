using IODA.Core.Application.Commands.Media;
using IODA.Core.Application.DTOs;
using IODA.Core.Application.Interfaces;
using IODA.Core.Application.Queries.Media;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IODA.Core.API.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}")]
[Authorize(Policy = "content.edit")]
public class MediaController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMediaStorage _storage;

    public MediaController(IMediator mediator, IMediaStorage storage)
    {
        _mediator = mediator;
        _storage = storage;
    }

    /// <summary>Subir un archivo de media al proyecto.</summary>
    [HttpPost("media")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB
    [ProducesResponseType(typeof(MediaItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaItemDto>> Upload(
        Guid projectId,
        IFormFile file,
        [FromForm] string? displayName = null,
        [FromForm] Guid? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file or empty file.");

        if (createdBy == null || createdBy == Guid.Empty)
            return BadRequest("createdBy is required.");

        await using var stream = file.OpenReadStream();
        var command = new UploadMediaCommand(
            projectId,
            stream,
            file.FileName ?? "file",
            file.ContentType ?? "application/octet-stream",
            file.Length,
            createdBy.Value,
            displayName,
            null);

        var id = await _mediator.Send(command, cancellationToken);
        var item = await _mediator.Send(new GetMediaByIdQuery(id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { projectId, mediaId = id }, item);
    }

    /// <summary>Listar media del proyecto (paginado).</summary>
    [HttpGet("media")]
    [ProducesResponseType(typeof(PagedResultDto<MediaItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<MediaItemDto>>> List(
        Guid projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ListMediaByProjectQuery(projectId, page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>Obtener metadatos de un media por ID.</summary>
    [HttpGet("media/{mediaId:guid}")]
    [ProducesResponseType(typeof(MediaItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaItemDto>> GetById(
        Guid projectId,
        Guid mediaId,
        CancellationToken cancellationToken = default)
    {
        var item = await _mediator.Send(new GetMediaByIdQuery(mediaId), cancellationToken);
        if (item == null)
            return NotFound();
        if (item.ProjectId != projectId)
            return NotFound();
        return Ok(item);
    }

    /// <summary>
    /// Descargar o previsualizar el archivo (stream).
    /// Acceso público intencional ([AllowAnonymous]) para permitir referencias directas en &lt;img&gt;/&lt;video&gt; y entrega vía CDN
    /// sin pasar token en la URL. Si en el futuro el contenido debe ser privado, implementar signed URLs o token de acceso corto
    /// y eliminar [AllowAnonymous]. Ver docs/FASE_DE_SEGUIMIENTO/BACKEND.md (Fase 3.3).
    /// </summary>
    [HttpGet("media/{mediaId:guid}/file")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(
        Guid projectId,
        Guid mediaId,
        [FromQuery] bool download = false,
        CancellationToken cancellationToken = default)
    {
        var item = await _mediator.Send(new GetMediaByIdQuery(mediaId), cancellationToken);
        if (item == null || item.ProjectId != projectId)
            return NotFound();

        var stream = await _storage.OpenReadAsync(item.StorageKey, cancellationToken);
        if (download)
            return File(stream, item.ContentType, item.FileName);
        return File(stream, item.ContentType);
    }
}
