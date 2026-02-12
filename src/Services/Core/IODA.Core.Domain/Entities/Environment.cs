using IODA.Core.Domain.ValueObjects;
using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Core.Domain.Entities;

/// <summary>
/// Represents an environment within a project (dev, staging, production)
/// </summary>
public class Environment : Entity<Guid>
{
    public Guid ProjectId { get; private set; }
    public Identifier PublicId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Slug Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    public Project Project { get; private set; } = null!;

    // EF Core constructor
    private Environment() { }

    private Environment(
        Guid id,
        Guid projectId,
        Identifier publicId,
        string name,
        Slug slug,
        string? description)
    {
        Id = id;
        ProjectId = projectId;
        PublicId = publicId;
        Name = name;
        Slug = slug;
        Description = description;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Environment Create(
        Guid projectId,
        string name,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Environment name cannot be empty", nameof(name));
        }

        var id = Guid.NewGuid();
        var publicId = Identifier.Create("env");
        var slug = Slug.FromTitle(name);

        return new Environment(id, projectId, publicId, name, slug, description);
    }

    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Environment name cannot be empty", nameof(name));
        }

        Name = name;
        Description = description;
        Slug = Slug.FromTitle(name);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
