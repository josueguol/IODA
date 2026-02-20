namespace IODA.Core.Domain.Entities;

/// <summary>
/// Relación muchos a muchos entre Content y Site (Req 4 - multi-sitio).
/// Un contenido puede estar asignado a uno o más sitios del mismo proyecto/entorno.
/// </summary>
public class ContentSite
{
    public Guid ContentId { get; private set; }
    public Guid SiteId { get; private set; }

    public Content Content { get; private set; } = null!;
    public Site Site { get; private set; } = null!;

    private ContentSite() { }

    internal ContentSite(Guid contentId, Guid siteId)
    {
        ContentId = contentId;
        SiteId = siteId;
    }

    public static ContentSite Create(Guid contentId, Guid siteId)
    {
        return new ContentSite(contentId, siteId);
    }
}
