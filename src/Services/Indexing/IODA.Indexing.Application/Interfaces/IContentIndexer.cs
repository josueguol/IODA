namespace IODA.Indexing.Application.Interfaces;

/// <summary>
/// Indexador de contenido publicado (Elasticsearch u otra implementación).
/// </summary>
public interface IContentIndexer
{
    Task IndexAsync(IndexContentRequest request, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid contentId, CancellationToken cancellationToken = default);
    Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default);
}

public record IndexContentRequest(
    Guid ContentId,
    Guid VersionId,
    string Title,
    string ContentType,
    DateTime PublishedAt,
    IReadOnlyDictionary<string, object>? Fields = null
);

public record SearchQuery(string Query, int Page = 1, int PageSize = 20, string? ContentType = null);

public record SearchResult(long Total, IReadOnlyList<IndexedContentHit> Items);

public record IndexedContentHit(
    Guid ContentId,
    Guid VersionId,
    string Title,
    string ContentType,
    DateTime PublishedAt
);
