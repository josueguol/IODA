using System.Collections.Concurrent;
using IODA.Core.Application.Interfaces;
using IODA.Core.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IODA.Core.Infrastructure.Processing;

public class MediaProcessingBackgroundService : BackgroundService, IMediaProcessingQueue
{
    private readonly ConcurrentQueue<Guid> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MediaProcessingBackgroundService> _logger;

    public MediaProcessingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<MediaProcessingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Enqueue(Guid mediaItemId)
    {
        _queue.Enqueue(mediaItemId);
        _signal.Release();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Media processing background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await _signal.WaitAsync(stoppingToken);

            while (_queue.TryDequeue(out var mediaItemId))
            {
                try
                {
                    await ProcessMediaAsync(mediaItemId, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Media processing failed for {MediaItemId}", mediaItemId);
                }
            }
        }
    }

    private async Task ProcessMediaAsync(Guid mediaItemId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var mediaItem = await unitOfWork.MediaItems.GetByIdAsync(mediaItemId, cancellationToken);
        if (mediaItem == null)
        {
            _logger.LogWarning("Media item not found for processing: {MediaItemId}", mediaItemId);
            return;
        }

        var metadata = mediaItem.Metadata != null
            ? new Dictionary<string, object>(mediaItem.Metadata)
            : new Dictionary<string, object>();

        metadata["processingStatus"] = "processing";
        mediaItem.UpdateMetadata(metadata);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var variants = BuildLogicalVariants(mediaItem.ContentType, mediaItem.StorageKey);

            metadata = mediaItem.Metadata != null
                ? new Dictionary<string, object>(mediaItem.Metadata)
                : new Dictionary<string, object>();

            metadata["processingStatus"] = "ready";
            metadata["processedAt"] = DateTime.UtcNow;
            metadata["variants"] = variants;
            metadata.Remove("processingError");

            mediaItem.UpdateMetadata(metadata);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            metadata = mediaItem.Metadata != null
                ? new Dictionary<string, object>(mediaItem.Metadata)
                : new Dictionary<string, object>();

            metadata["processingStatus"] = "failed";
            metadata["processingError"] = ex.Message;
            mediaItem.UpdateMetadata(metadata);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            throw;
        }
    }

    private static List<Dictionary<string, object>> BuildLogicalVariants(string contentType, string storageKey)
    {
        var safeContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
        var lower = safeContentType.ToLowerInvariant();

        if (lower.StartsWith("image/"))
        {
            return new List<Dictionary<string, object>>
            {
                Variant("original", storageKey, safeContentType),
                Variant("thumbnail", storageKey, safeContentType),
                Variant("small", storageKey, safeContentType),
                Variant("medium", storageKey, safeContentType),
                Variant("large", storageKey, safeContentType),
            };
        }

        if (lower.StartsWith("video/"))
        {
            return new List<Dictionary<string, object>>
            {
                Variant("original", storageKey, safeContentType),
                Variant("poster", storageKey, "image/jpeg"),
            };
        }

        if (lower.StartsWith("audio/"))
        {
            return new List<Dictionary<string, object>>
            {
                Variant("original", storageKey, safeContentType),
                Variant("cover", storageKey, "image/jpeg"),
            };
        }

        return new List<Dictionary<string, object>>
        {
            Variant("original", storageKey, safeContentType),
        };
    }

    private static Dictionary<string, object> Variant(string name, string storageKey, string contentType)
    {
        return new Dictionary<string, object>
        {
            ["name"] = name,
            ["storageKey"] = storageKey,
            ["contentType"] = contentType,
        };
    }
}
