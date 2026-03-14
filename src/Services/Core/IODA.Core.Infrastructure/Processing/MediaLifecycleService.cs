using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Text.Json.Nodes;
using IODA.Core.Application.DTOs;
using IODA.Core.Application.Interfaces;
using IODA.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IODA.Core.Infrastructure.Processing;

public sealed class MediaLifecycleService : IMediaLifecycleService
{
    private static readonly Meter Meter = new("IODA.Core.MediaLifecycle", "1.0.0");
    private static readonly Counter<long> CleanupRuns = Meter.CreateCounter<long>("media_lifecycle_cleanup_runs_total");
    private static readonly Counter<long> CleanupOrphansFound = Meter.CreateCounter<long>("media_lifecycle_orphans_found_total");
    private static readonly Counter<long> CleanupDeletes = Meter.CreateCounter<long>("media_lifecycle_deleted_total");

    private readonly CoreDbContext _dbContext;
    private readonly IMediaStorage _storage;
    private readonly ILogger<MediaLifecycleService> _logger;

    public MediaLifecycleService(CoreDbContext dbContext, IMediaStorage storage, ILogger<MediaLifecycleService> logger)
    {
        _dbContext = dbContext;
        _storage = storage;
        _logger = logger;
    }

    public async Task<MediaLifecycleCleanupReport> CleanupOrphanedFilesAsync(
        Guid projectId,
        bool dryRun,
        int maxDeletes,
        CancellationToken cancellationToken = default)
    {
        if (maxDeletes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDeletes), "maxDeletes must be greater than zero.");

        var projectPrefix = $"{projectId:N}/";
        var referencedKeys = await LoadReferencedStorageKeys(projectId, cancellationToken);
        var storageKeys = await _storage.ListKeysAsync(projectPrefix, cancellationToken);
        var orphanKeys = storageKeys
            .Where(key => !referencedKeys.Contains(NormalizeKey(key)))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var keysToDelete = orphanKeys.Take(maxDeletes).ToList();
        var deleted = new List<string>();

        if (!dryRun)
        {
            foreach (var key in keysToDelete)
            {
                await _storage.DeleteAsync(key, cancellationToken);
                deleted.Add(key);
            }
        }

        CleanupRuns.Add(1, new KeyValuePair<string, object?>("project_id", projectId));
        CleanupOrphansFound.Add(orphanKeys.Count, new KeyValuePair<string, object?>("project_id", projectId));
        CleanupDeletes.Add(deleted.Count, new KeyValuePair<string, object?>("project_id", projectId));

        _logger.LogInformation(
            "Media lifecycle cleanup completed. ProjectId={ProjectId}, DryRun={DryRun}, ReferencedKeys={ReferencedKeys}, StorageKeys={StorageKeys}, OrphanKeys={OrphanKeys}, DeletedKeys={DeletedKeys}",
            projectId,
            dryRun,
            referencedKeys.Count,
            storageKeys.Count,
            orphanKeys.Count,
            deleted.Count);

        return new MediaLifecycleCleanupReport(
            projectId,
            dryRun,
            ReferencedKeys: referencedKeys.Count,
            StorageKeys: storageKeys.Count,
            OrphanKeys: orphanKeys.Count,
            DeletedKeys: deleted.Count,
            SampleOrphanKeys: orphanKeys.Take(20).ToList(),
            SampleDeletedKeys: deleted.Take(20).ToList());
    }

    private async Task<HashSet<string>> LoadReferencedStorageKeys(Guid projectId, CancellationToken cancellationToken)
    {
        var rows = await _dbContext.MediaItems
            .AsNoTracking()
            .Where(m => m.ProjectId == projectId)
            .Select(m => new { m.StorageKey, m.Metadata })
            .ToListAsync(cancellationToken);

        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.StorageKey))
                keys.Add(NormalizeKey(row.StorageKey));

            foreach (var variantKey in ExtractVariantStorageKeys(row.Metadata))
                keys.Add(NormalizeKey(variantKey));
        }

        return keys;
    }

    private static IEnumerable<string> ExtractVariantStorageKeys(Dictionary<string, object>? metadata)
    {
        if (metadata is null)
            yield break;
        if (!metadata.TryGetValue("variants", out var variantsRaw) || variantsRaw is null)
            yield break;

        var element = ToJsonElement(variantsRaw);
        if (element.ValueKind != JsonValueKind.Array)
            yield break;

        foreach (var entry in element.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
                continue;
            if (!entry.TryGetProperty("storageKey", out var storageEl) || storageEl.ValueKind != JsonValueKind.String)
                continue;

            var storageKey = storageEl.GetString();
            if (!string.IsNullOrWhiteSpace(storageKey))
                yield return storageKey!;
        }
    }

    private static JsonElement ToJsonElement(object raw)
    {
        if (raw is JsonElement je)
            return je;
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

    private static string NormalizeKey(string key) =>
        key.Trim().TrimStart('/').Replace('\\', '/');
}
