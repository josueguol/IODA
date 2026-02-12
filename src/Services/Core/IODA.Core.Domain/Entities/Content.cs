using IODA.Core.Domain.ValueObjects;
using IODA.Shared.BuildingBlocks.Domain;
using System.Text.Json;

namespace IODA.Core.Domain.Entities;

/// <summary>
/// Represents content with dynamic fields defined by a schema
/// The core of the schema-driven CMS
/// </summary>
public class Content : AggregateRoot<Guid>
{
    private readonly List<ContentVersion> _versions = [];

    public Identifier PublicId { get; private set; } = null!;
    public Guid ProjectId { get; private set; }
    public Guid EnvironmentId { get; private set; }
    public Guid? SiteId { get; private set; }
    public Guid SchemaId { get; private set; }
    public string Title { get; private set; } = null!;
    public Slug Slug { get; private set; } = null!;
    public ContentStatus Status { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    
    /// <summary>
    /// Dynamic fields stored as JSON (will be mapped to JSONB in PostgreSQL)
    /// </summary>
    public Dictionary<string, object> Fields { get; private set; } = [];
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public Guid? PublishedBy { get; private set; }
    
    public int CurrentVersion { get; private set; }

    // Navigation
    public Project Project { get; private set; } = null!;
    public Environment Environment { get; private set; } = null!;
    public Site? Site { get; private set; }
    public ContentSchema Schema { get; private set; } = null!;
    public IReadOnlyCollection<ContentVersion> Versions => _versions.AsReadOnly();

    // EF Core constructor
    private Content() { }

    private Content(
        Guid id,
        Identifier publicId,
        Guid projectId,
        Guid environmentId,
        Guid? siteId,
        Guid schemaId,
        string title,
        Slug slug,
        string contentType,
        Dictionary<string, object> fields,
        Guid createdBy)
    {
        Id = id;
        PublicId = publicId;
        ProjectId = projectId;
        EnvironmentId = environmentId;
        SiteId = siteId;
        SchemaId = schemaId;
        Title = title;
        Slug = slug;
        ContentType = contentType;
        Fields = fields;
        Status = ContentStatus.Draft;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
        CurrentVersion = 1;

        // Create initial version
        var initialVersion = ContentVersion.Create(
            Id,
            CurrentVersion,
            title,
            fields,
            ContentStatus.Draft,
            createdBy);
        
        _versions.Add(initialVersion);

        // Domain event
        RaiseDomainEvent(new ContentCreatedDomainEvent(
            Id,
            title,
            contentType,
            projectId,
            environmentId));
    }

    public static Content Create(
        Guid projectId,
        Guid environmentId,
        Guid? siteId,
        Guid schemaId,
        string title,
        string contentType,
        Dictionary<string, object> fields,
        Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Content title cannot be empty", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type cannot be empty", nameof(contentType));
        }

        var id = Guid.NewGuid();
        var publicId = Identifier.Create("cnt");
        var slug = Slug.FromTitle(title);

        return new Content(
            id,
            publicId,
            projectId,
            environmentId,
            siteId,
            schemaId,
            title,
            slug,
            contentType,
            fields,
            createdBy);
    }

    /// <summary>Asigna o cambia el sitio asociado al contenido (opcional).</summary>
    public void AssignToSite(Guid? siteId)
    {
        SiteId = siteId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(
        string title,
        Dictionary<string, object> fields,
        Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Content title cannot be empty", nameof(title));
        }

        if (!Status.CanBeEdited)
        {
            throw new InvalidOperationException($"Cannot update content in {Status} status");
        }

        Title = title;
        Fields = fields;
        Slug = Slug.FromTitle(title);
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        CurrentVersion++;

        // Create new version
        var newVersion = ContentVersion.Create(
            Id,
            CurrentVersion,
            title,
            fields,
            Status,
            updatedBy);
        
        _versions.Add(newVersion);

        // Domain event
        RaiseDomainEvent(new ContentUpdatedDomainEvent(
            Id,
            newVersion.Id,
            CurrentVersion,
            title));
    }

    public void ChangeStatus(ContentStatus newStatus, Guid changedBy)
    {
        if (Status == newStatus)
        {
            return;
        }

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = changedBy;

        if (newStatus.IsPublished)
        {
            PublishedAt = DateTime.UtcNow;
            PublishedBy = changedBy;
        }

        RaiseDomainEvent(new ContentStatusChangedDomainEvent(
            Id,
            Status.Value,
            newStatus.Value));
    }

    public void Publish(Guid publishedBy)
    {
        if (!Status.CanBePublished && !Status.IsPublished)
        {
            throw new InvalidOperationException($"Cannot publish content in {Status} status");
        }

        Status = ContentStatus.Published;
        PublishedAt = DateTime.UtcNow;
        PublishedBy = publishedBy;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ContentPublishedDomainEvent(
            Id,
            _versions.Last().Id,
            Title,
            ContentType));
    }

    public void Unpublish(string reason, Guid unpublishedBy)
    {
        if (!Status.IsPublished)
        {
            throw new InvalidOperationException("Content is not published");
        }

        Status = ContentStatus.Draft;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = unpublishedBy;

        RaiseDomainEvent(new ContentUnpublishedDomainEvent(
            Id,
            reason,
            unpublishedBy));
    }

    public void Archive(Guid archivedBy)
    {
        Status = ContentStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = archivedBy;
    }

    public ContentVersion? GetVersion(int versionNumber)
    {
        return _versions.FirstOrDefault(v => v.VersionNumber == versionNumber);
    }

    public ContentVersion GetLatestVersion()
    {
        return _versions.OrderByDescending(v => v.VersionNumber).First();
    }
}

/// <summary>
/// Domain event raised when content is created
/// </summary>
public record ContentCreatedDomainEvent(
    Guid ContentId,
    string Title,
    string ContentType,
    Guid ProjectId,
    Guid EnvironmentId) : DomainEvent;

/// <summary>
/// Domain event raised when content is updated
/// </summary>
public record ContentUpdatedDomainEvent(
    Guid ContentId,
    Guid VersionId,
    int VersionNumber,
    string Title) : DomainEvent;

/// <summary>
/// Domain event raised when content status changes
/// </summary>
public record ContentStatusChangedDomainEvent(
    Guid ContentId,
    string OldStatus,
    string NewStatus) : DomainEvent;

/// <summary>
/// Domain event raised when content is published
/// </summary>
public record ContentPublishedDomainEvent(
    Guid ContentId,
    Guid VersionId,
    string Title,
    string ContentType) : DomainEvent;

/// <summary>
/// Domain event raised when content is unpublished
/// </summary>
public record ContentUnpublishedDomainEvent(
    Guid ContentId,
    string Reason,
    Guid UnpublishedBy) : DomainEvent;
