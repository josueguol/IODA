using IODA.Core.Domain.ValueObjects;
using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Core.Domain.Entities;

/// <summary>
/// Represents a project that contains environments, sites, and content
/// </summary>
public class Project : AggregateRoot<Guid>
{
    private readonly List<Environment> _environments = [];

    public Identifier PublicId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Slug Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    public IReadOnlyCollection<Environment> Environments => _environments.AsReadOnly();

    // EF Core constructor
    private Project() { }

    private Project(
        Guid id,
        Identifier publicId,
        string name,
        Slug slug,
        string? description,
        Guid createdBy)
    {
        Id = id;
        PublicId = publicId;
        Name = name;
        Slug = slug;
        Description = description;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;

        // Domain event
        RaiseDomainEvent(new ProjectCreatedDomainEvent(Id, name));
    }

    public static Project Create(
        string name,
        string? description,
        Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name cannot be empty", nameof(name));
        }

        var id = Guid.NewGuid();
        var publicId = Identifier.Create("prj");
        var slug = Slug.FromTitle(name);

        return new Project(id, publicId, name, slug, description, createdBy);
    }

    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name cannot be empty", nameof(name));
        }

        Name = name;
        Description = description;
        Slug = Slug.FromTitle(name);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ProjectUpdatedDomainEvent(Id, name));
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

    public void AddEnvironment(Environment environment)
    {
        _environments.Add(environment);
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Domain event raised when a project is created
/// </summary>
public record ProjectCreatedDomainEvent(Guid ProjectId, string ProjectName) : DomainEvent;

/// <summary>
/// Domain event raised when a project is updated
/// </summary>
public record ProjectUpdatedDomainEvent(Guid ProjectId, string ProjectName) : DomainEvent;
