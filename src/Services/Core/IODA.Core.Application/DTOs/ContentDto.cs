namespace IODA.Core.Application.DTOs;

public record ContentDto(
    Guid Id,
    string PublicId,
    Guid ProjectId,
    Guid EnvironmentId,
    Guid? SiteId,
    Guid? ParentContentId,
    Guid SchemaId,
    string Title,
    string Slug,
    string Status,
    string ContentType,
    Dictionary<string, object> Fields,
    int CurrentVersion,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? PublishedAt,
    Guid CreatedBy,
    Guid? UpdatedBy,
    Guid? PublishedBy,
    IReadOnlyList<Guid> TagIds,
    IReadOnlyList<Guid> HierarchyIds,
    IReadOnlyList<Guid> SiteIds);

public record ContentListItemDto(
    Guid Id,
    string PublicId,
    string Title,
    string Slug,
    string Status,
    string ContentType,
    Guid? SiteId,
    Guid? ParentContentId,
    DateTime CreatedAt,
    DateTime? PublishedAt);
