namespace IODA.Indexing.Domain.ValueObjects;

/// <summary>
/// Documento a indexar (solo contenido publicado).
/// </summary>
public record IndexedContentDocument(
    Guid ContentId,
    Guid VersionId,
    string Title,
    string ContentType,
    DateTime PublishedAt,
    IReadOnlyDictionary<string, object>? Fields = null
);
