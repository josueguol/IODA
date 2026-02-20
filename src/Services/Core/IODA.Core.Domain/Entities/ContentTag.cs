namespace IODA.Core.Domain.Entities;

/// <summary>
/// Relación muchos a muchos entre Content y Tag (Req 3).
/// </summary>
public class ContentTag
{
    public Guid ContentId { get; private set; }
    public Guid TagId { get; private set; }

    public Content Content { get; private set; } = null!;
    public Tag Tag { get; private set; } = null!;

    private ContentTag() { }

    internal ContentTag(Guid contentId, Guid tagId)
    {
        ContentId = contentId;
        TagId = tagId;
    }

    public static ContentTag Create(Guid contentId, Guid tagId)
    {
        return new ContentTag(contentId, tagId);
    }
}
