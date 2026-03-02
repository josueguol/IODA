namespace IODA.Core.Domain.Entities;

/// <summary>
/// Relación muchos a muchos entre Content y Hierarchy (categorías/jerarquías).
/// </summary>
public class ContentHierarchy
{
    public Guid ContentId { get; private set; }
    public Guid HierarchyId { get; private set; }
    /// <summary>Marca la sección principal del contenido (máximo una por contenido).</summary>
    public bool IsPrimary { get; private set; }

    public Content Content { get; private set; } = null!;
    public Hierarchy Hierarchy { get; private set; } = null!;

    private ContentHierarchy() { }

    internal ContentHierarchy(Guid contentId, Guid hierarchyId, bool isPrimary)
    {
        ContentId = contentId;
        HierarchyId = hierarchyId;
        IsPrimary = isPrimary;
    }

    public static ContentHierarchy Create(Guid contentId, Guid hierarchyId, bool isPrimary = false)
    {
        return new ContentHierarchy(contentId, hierarchyId, isPrimary);
    }
}
