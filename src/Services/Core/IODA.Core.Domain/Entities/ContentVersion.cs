using IODA.Core.Domain.ValueObjects;
using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Core.Domain.Entities;

/// <summary>
/// Represents a historical version of content
/// Every update creates a new version for full audit trail
/// </summary>
public class ContentVersion : Entity<Guid>
{
    public Guid ContentId { get; private set; }
    public int VersionNumber { get; private set; }
    public string Title { get; private set; } = null!;
    
    /// <summary>
    /// Snapshot of fields at this version (stored as JSONB)
    /// </summary>
    public Dictionary<string, object> Fields { get; private set; } = [];
    
    public string Status { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    
    /// <summary>
    /// Optional comment about this version
    /// </summary>
    public string? Comment { get; private set; }

    // Navigation
    public Content Content { get; private set; } = null!;

    // EF Core constructor
    private ContentVersion() { }

    private ContentVersion(
        Guid id,
        Guid contentId,
        int versionNumber,
        string title,
        Dictionary<string, object> fields,
        string status,
        Guid createdBy,
        string? comment = null)
    {
        Id = id;
        ContentId = contentId;
        VersionNumber = versionNumber;
        Title = title;
        Fields = fields;
        Status = status;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
        Comment = comment;
    }

    public static ContentVersion Create(
        Guid contentId,
        int versionNumber,
        string title,
        Dictionary<string, object> fields,
        ContentStatus status,
        Guid createdBy,
        string? comment = null)
    {
        var id = Guid.NewGuid();
        
        // Deep copy fields to avoid reference issues
        var fieldsCopy = new Dictionary<string, object>(fields);

        return new ContentVersion(
            id,
            contentId,
            versionNumber,
            title,
            fieldsCopy,
            status.Value,
            createdBy,
            comment);
    }
}
