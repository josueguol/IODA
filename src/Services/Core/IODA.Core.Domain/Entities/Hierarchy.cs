namespace IODA.Core.Domain.Entities;

/// <summary>
/// Categoría/jerarquía por proyecto para agrupar contenido. Admite jerarquía padre-hijos.
/// Módulo: Jerarquías.
/// </summary>
public class Hierarchy
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    /// <summary>Nombre visible de la categoría.</summary>
    public string Name { get; private set; } = null!;
    /// <summary>Slug para URL, único en el proyecto.</summary>
    public string Slug { get; private set; } = null!;
    /// <summary>Descripción opcional.</summary>
    public string? Description { get; private set; }
    /// <summary>URL de imagen o referencia a media (uso futuro).</summary>
    public string? ImageUrl { get; private set; }
    /// <summary>Padre en la jerarquía; null = raíz.</summary>
    public Guid? ParentHierarchyId { get; private set; }

    public Project Project { get; private set; } = null!;
    public Hierarchy? Parent { get; private set; }
    private readonly List<Hierarchy> _children = new();
    public IReadOnlyList<Hierarchy> Children => _children;

    private Hierarchy() { }

    internal Hierarchy(
        Guid id,
        Guid projectId,
        string name,
        string slug,
        string? description,
        string? imageUrl,
        Guid? parentHierarchyId)
    {
        Id = id;
        ProjectId = projectId;
        Name = name;
        Slug = slug;
        Description = description;
        ImageUrl = imageUrl;
        ParentHierarchyId = parentHierarchyId;
    }

    public static Hierarchy Create(
        Guid projectId,
        string name,
        string slug,
        string? description = null,
        string? imageUrl = null,
        Guid? parentHierarchyId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Hierarchy name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Hierarchy slug cannot be empty", nameof(slug));
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return new Hierarchy(
            Guid.NewGuid(),
            projectId,
            name.Trim(),
            normalizedSlug,
            string.IsNullOrWhiteSpace(description) ? null : description!.Trim(),
            string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl!.Trim(),
            parentHierarchyId);
    }

    public void SetParent(Guid? parentHierarchyId)
    {
        ParentHierarchyId = parentHierarchyId;
    }

    public void Update(string name, string slug, string? description = null, string? imageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Hierarchy name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Hierarchy slug cannot be empty", nameof(slug));
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = string.IsNullOrWhiteSpace(description) ? null : description!.Trim();
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl!.Trim();
    }
}
