namespace IODA.Indexing.API.Contracts;

/// <summary>
/// Contratos HTTP (request/response) del API de Indexing.
/// </summary>
public record SearchResultDto(long Total, IReadOnlyList<IndexedContentHitDto> Items);

public record IndexedContentHitDto(Guid ContentId, Guid VersionId, string Title, string ContentType, DateTime PublishedAt);

public record IndexContentRequestDto(
    Guid ContentId,
    Guid VersionId,
    string Title,
    string ContentType,
    DateTime PublishedAt,
    IReadOnlyDictionary<string, object>? Fields = null);
