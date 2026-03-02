using IODA.Core.Application.DTOs;
using IODA.Core.Domain.Entities;

namespace IODA.Core.Application.Mappings;

public static class ContentMappings
{
    public static ContentDto ToDto(
        this Content content,
        Guid? primaryHierarchyId = null,
        IReadOnlyList<Guid>? tagIds = null,
        IReadOnlyList<Guid>? hierarchyIds = null,
        IReadOnlyList<Guid>? siteIds = null,
        IReadOnlyList<ContentSiteUrlDto>? siteUrls = null)
    {
        var blocks = content.Blocks
            .OrderBy(b => b.Order)
            .Select(b => new ContentBlockDto(b.Id, b.ContentId, b.BlockType, b.Order, new Dictionary<string, object>(b.Payload)))
            .ToList();
        return new ContentDto(
            content.Id,
            content.PublicId.FullId,
            content.ProjectId,
            content.EnvironmentId,
            content.SiteId,
            content.ParentContentId,
            content.Order,
            content.SchemaId,
            content.Title,
            content.Slug.Value,
            content.Status.Value,
            content.ContentType,
            new Dictionary<string, object>(content.Fields),
            content.CurrentVersion,
            content.CreatedAt,
            content.UpdatedAt,
            content.PublishedAt,
            content.CreatedBy,
            content.UpdatedBy,
            content.PublishedBy,
            primaryHierarchyId,
            tagIds ?? Array.Empty<Guid>(),
            hierarchyIds ?? Array.Empty<Guid>(),
            siteIds ?? Array.Empty<Guid>(),
            siteUrls ?? Array.Empty<ContentSiteUrlDto>(),
            blocks);
    }

    public static ContentListItemDto ToListItemDto(this Content content)
    {
        return new ContentListItemDto(
            content.Id,
            content.PublicId.FullId,
            content.Title,
            content.Slug.Value,
            content.Status.Value,
            content.ContentType,
            content.SiteId,
            content.ParentContentId,
            content.Order,
            content.CreatedAt,
            content.PublishedAt);
    }

    public static ContentVersionDto ToDto(this ContentVersion version)
    {
        return new ContentVersionDto(
            version.Id,
            version.ContentId,
            version.VersionNumber,
            version.Title,
            new Dictionary<string, object>(version.Fields),
            version.Status,
            version.CreatedAt,
            version.CreatedBy,
            version.Comment);
    }
}
