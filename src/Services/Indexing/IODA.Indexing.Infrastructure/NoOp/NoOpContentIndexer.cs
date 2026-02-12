using IODA.Indexing.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace IODA.Indexing.Infrastructure.NoOp;

/// <summary>
/// Indexador que no hace nada (cuando Elasticsearch está deshabilitado).
/// </summary>
public class NoOpContentIndexer : IContentIndexer
{
    private readonly ILogger<NoOpContentIndexer> _logger;

    public NoOpContentIndexer(ILogger<NoOpContentIndexer> logger)
    {
        _logger = logger;
    }

    public Task IndexAsync(IndexContentRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("NoOp: IndexAsync for ContentId {ContentId} (Elasticsearch disabled)", request.ContentId);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid contentId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("NoOp: RemoveAsync for ContentId {ContentId} (Elasticsearch disabled)", contentId);
        return Task.CompletedTask;
    }

    public Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("NoOp: SearchAsync (Elasticsearch disabled), returning empty result");
        return Task.FromResult(new SearchResult(0, []));
    }
}
