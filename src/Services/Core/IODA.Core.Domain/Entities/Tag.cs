namespace IODA.Core.Domain.Entities;

/// <summary>
/// Etiqueta asociada a un proyecto. Los contenidos se relacionan con tags vía ContentTag (Req 3).
/// </summary>
public class Tag
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    /// <summary>Nombre visible (ej. "Noticias").</summary>
    public string Name { get; private set; } = null!;
    /// <summary>Slug único en el proyecto (ej. "noticias") para búsquedas.</summary>
    public string Slug { get; private set; } = null!;

    public Project Project { get; private set; } = null!;

    private Tag() { }

    internal Tag(Guid id, Guid projectId, string name, string slug)
    {
        Id = id;
        ProjectId = projectId;
        Name = name;
        Slug = slug;
    }

    public static Tag Create(Guid projectId, string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Tag slug cannot be empty", nameof(slug));
        return new Tag(Guid.NewGuid(), projectId, name.Trim(), slug.Trim().ToLowerInvariant());
    }
}
