using Elastic.Clients.Elasticsearch;
using IODA.Indexing.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IODA.Indexing.Infrastructure.Elasticsearch;

public class ElasticsearchContentIndexer : IContentIndexer
{
    private readonly ElasticsearchClient _client;
    private readonly string _indexName;
    private readonly ILogger<ElasticsearchContentIndexer> _logger;

    public ElasticsearchContentIndexer(
        ElasticsearchClient client,
        IConfiguration configuration,
        ILogger<ElasticsearchContentIndexer> logger)
    {
        _client = client;
        _indexName = configuration["Elasticsearch:IndexName"] ?? "ioda-published-content";
        _logger = logger;
    }

    public async Task IndexAsync(IndexContentRequest request, CancellationToken cancellationToken = default)
    {
        var doc = new IndexedContentDoc
        {
            ContentId = request.ContentId,
            VersionId = request.VersionId,
            Title = request.Title,
            ContentType = request.ContentType,
            PublishedAt = request.PublishedAt,
            Fields = request.Fields != null ? new Dictionary<string, object>(request.Fields) : null
        };

        var response = await _client.IndexAsync(
            doc,
            x => x.Index(_indexName).Id(new Id(request.ContentId.ToString())),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            _logger.LogError("Elasticsearch index failed for ContentId {ContentId}: {Error}", request.ContentId, response.DebugInformation);
            throw new InvalidOperationException($"Failed to index content {request.ContentId}: {response.DebugInformation}");
        }

        _logger.LogInformation("Indexed content {ContentId} in {Index}", request.ContentId, _indexName);
    }

    public async Task RemoveAsync(Guid contentId, CancellationToken cancellationToken = default)
    {
        var response = await _client.DeleteAsync(
            _indexName,
            new Id(contentId.ToString()),
            cancellationToken);

        if (!response.IsValidResponse && response.Result != Result.NotFound)
        {
            _logger.LogError("Elasticsearch delete failed for ContentId {ContentId}: {Error}", contentId, response.DebugInformation);
            throw new InvalidOperationException($"Failed to remove content {contentId} from index: {response.DebugInformation}");
        }

        _logger.LogInformation("Removed content {ContentId} from index {Index}", contentId, _indexName);
    }

    public async Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        var response = await _client.SearchAsync<IndexedContentDoc>(s =>
        {
            s.Index(_indexName)
                .From((query.Page - 1) * query.PageSize)
                .Size(query.PageSize);

            if (!string.IsNullOrWhiteSpace(query.Query))
            {
                s.Query(q => q
                    .Bool(b => b
                        .Should(
                            sh => sh.Match(m => m.Field(f => f.Title).Query(query.Query)),
                            sh => sh.Match(m => m.Field(f => f.ContentType).Query(query.Query))
                        )
                    )
                );
            }

            if (!string.IsNullOrWhiteSpace(query.ContentType))
            {
                s.Query(q => q.Term(t => t.Field(f => f.ContentType).Value(query.ContentType)));
            }
        }, cancellationToken);

        if (!response.IsValidResponse)
        {
            _logger.LogWarning("Elasticsearch search failed: {Error}", response.DebugInformation);
            return new SearchResult(0, []);
        }

        var hits = (response.Documents ?? []).Select(d =>
            new IndexedContentHit(d.ContentId, d.VersionId, d.Title, d.ContentType, d.PublishedAt)
        ).ToList();

        var total = response.Total;
        return new SearchResult(total, hits);
    }
}
