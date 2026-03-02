namespace IODA.Core.Application.DTOs;

public record ContentSiteUrlDto(
    Guid SiteId,
    string Path,
    bool IsOwner);

public record ContentDto(
    Guid Id,
    string PublicId,
    Guid ProjectId,
    Guid EnvironmentId,
    Guid? SiteId,
    Guid? ParentContentId,
    int Order,
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
    Guid? PrimaryHierarchyId,
    IReadOnlyList<Guid> TagIds,
    IReadOnlyList<Guid> HierarchyIds,
    IReadOnlyList<Guid> SiteIds,
    IReadOnlyList<ContentSiteUrlDto> SiteUrls,
    IReadOnlyList<ContentBlockDto> Blocks);

public record ContentListItemDto(
    Guid Id,
    string PublicId,
    string Title,
    string Slug,
    string Status,
    string ContentType,
    Guid? SiteId,
    Guid? ParentContentId,
    int Order,
    DateTime CreatedAt,
    DateTime? PublishedAt);
