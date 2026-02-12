namespace IODA.Indexing.Infrastructure.Elasticsearch;

/// <summary>
/// Documento almacenado en Elasticsearch (solo contenido publicado).
/// </summary>
internal sealed class IndexedContentDoc
{
    public Guid ContentId { get; set; }
    public Guid VersionId { get; set; }
    public string Title { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public DateTime PublishedAt { get; set; }
    public Dictionary<string, object>? Fields { get; set; }
}
