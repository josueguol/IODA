using IODA.Core.Application.Commands.Media;
using IODA.Core.Application.DTOs;
using IODA.Core.Application.Interfaces;
using IODA.Core.Application.Queries.Media;
using IODA.Core.API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IODA.Core.API.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}")]
[Authorize(Policy = "content.edit")]
public class MediaController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMediaStorage _storage;
    private readonly IMediaLifecycleService _mediaLifecycle;

    public MediaController(IMediator mediator, IMediaStorage storage, IMediaLifecycleService mediaLifecycle)
    {
        _mediator = mediator;
        _storage = storage;
        _mediaLifecycle = mediaLifecycle;
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
        [FromForm] string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest("No file or empty file.");

        if (!TryParseMetadata(metadata, out var parsedMetadata))
            return BadRequest("metadata must be a valid JSON object.");

        await using var stream = file.OpenReadStream();
        var command = new UploadMediaCommand(
            projectId,
            stream,
            file.FileName ?? "file",
            file.ContentType ?? "application/octet-stream",
            file.Length,
            userId.Value,
            displayName,
            parsedMetadata);

        var id = await _mediator.Send(command, cancellationToken);
        var item = await _mediator.Send(new GetMediaByIdQuery(id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { projectId, mediaId = id }, item);
    }

    /// <summary>Actualizar metadatos y/o nombre visible del media.</summary>
    [HttpPatch("media/{mediaId:guid}")]
    [ProducesResponseType(typeof(MediaItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaItemDto>> UpdateMetadata(
        Guid projectId,
        Guid mediaId,
        [FromBody] UpdateMediaMetadataRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        var command = new UpdateMediaMetadataCommand(
            projectId,
            mediaId,
            userId.Value,
            request.DisplayName,
            request.Metadata);

        var updated = await _mediator.Send(command, cancellationToken);
        return Ok(updated);
    }

    /// <summary>Reemplazar archivo de un media (incrementa version).</summary>
    [HttpPost("media/{mediaId:guid}/replace")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB
    [ProducesResponseType(typeof(MediaItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaItemDto>> ReplaceFile(
        Guid projectId,
        Guid mediaId,
        IFormFile file,
        [FromForm] string? displayName = null,
        [FromForm] string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest("No file or empty file.");

        if (!TryParseMetadata(metadata, out var parsedMetadata))
            return BadRequest("metadata must be a valid JSON object.");

        await using var stream = file.OpenReadStream();
        var command = new ReplaceMediaFileCommand(
            projectId,
            mediaId,
            stream,
            file.FileName ?? "file",
            file.ContentType ?? "application/octet-stream",
            file.Length,
            userId.Value,
            displayName,
            parsedMetadata);

        var updated = await _mediator.Send(command, cancellationToken);
        return Ok(updated);
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
        [FromQuery] string? variant = null,
        [FromQuery] bool download = false,
        CancellationToken cancellationToken = default)
    {
        var item = await _mediator.Send(new GetMediaByIdQuery(mediaId), cancellationToken);
        if (item == null || item.ProjectId != projectId)
            return NotFound();

        var storageKey = ResolveStorageKey(item, variant);
        var stream = await _storage.OpenReadAsync(storageKey, cancellationToken);
        if (download)
            return File(stream, item.ContentType, item.FileName);
        return File(stream, item.ContentType);
    }

    /// <summary>
    /// Limpia archivos huérfanos en storage que no están referenciados por ningún MediaItem del proyecto.
    /// Dry-run por defecto para ejecución segura.
    /// </summary>
    [HttpPost("media/cleanup-orphans")]
    [ProducesResponseType(typeof(MediaLifecycleCleanupReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MediaLifecycleCleanupReport>> CleanupOrphans(
        Guid projectId,
        [FromBody] CleanupOrphanMediaRequest? request,
        CancellationToken cancellationToken = default)
    {
        var dryRun = request?.DryRun ?? true;
        var maxDeletes = request?.MaxDeletes ?? 1000;
        if (maxDeletes <= 0)
            return BadRequest("maxDeletes must be greater than zero.");

        var report = await _mediaLifecycle.CleanupOrphanedFilesAsync(projectId, dryRun, maxDeletes, cancellationToken);
        return Ok(report);
    }

    private static string ResolveStorageKey(MediaItemDto item, string? variant)
    {
        if (string.IsNullOrWhiteSpace(variant) || item.Metadata is null)
            return item.StorageKey;

        if (!item.Metadata.TryGetValue("variants", out var variantsRaw) || variantsRaw is null)
            return item.StorageKey;

        var element = ToJsonElement(variantsRaw);
        if (element.ValueKind != JsonValueKind.Array)
            return item.StorageKey;

        foreach (var entry in element.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
                continue;

            var name = entry.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
            if (!string.Equals(name, variant, StringComparison.OrdinalIgnoreCase))
                continue;

            var storageKey = entry.TryGetProperty("storageKey", out var storageEl) ? storageEl.GetString() : null;
            if (!string.IsNullOrWhiteSpace(storageKey))
                return storageKey!;
        }

        return item.StorageKey;
    }

    private static bool TryParseMetadata(string? metadata, out Dictionary<string, object>? parsedMetadata)
    {
        parsedMetadata = null;

        if (string.IsNullOrWhiteSpace(metadata))
            return true;

        try
        {
            parsedMetadata = JsonSerializer.Deserialize<Dictionary<string, object>>(metadata);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static JsonElement ToJsonElement(object raw)
    {
        if (raw is JsonElement je) return je;
        if (raw is JsonNode node)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(node);
            using var doc = JsonDocument.Parse(bytes);
            return doc.RootElement.Clone();
        }

        var serialized = JsonSerializer.SerializeToUtf8Bytes(raw);
        using var document = JsonDocument.Parse(serialized);
        return document.RootElement.Clone();
    }
}

public record UpdateMediaMetadataRequest(
    string? DisplayName,
    Dictionary<string, object>? Metadata);

public record CleanupOrphanMediaRequest(
    bool DryRun = true,
    int MaxDeletes = 1000);
