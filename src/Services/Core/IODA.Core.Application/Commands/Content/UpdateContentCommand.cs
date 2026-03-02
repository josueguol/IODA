using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Commands.Content;

public record UpdateContentCommand(
    Guid ContentId,
    string Title,
    string? Slug,
    Dictionary<string, object> Fields,
    Guid UpdatedBy,
    Guid? ParentContentId = null,
    int? Order = null,
    IReadOnlyList<Guid>? TagIds = null,
    IReadOnlyList<Guid>? HierarchyIds = null,
    Guid? PrimaryHierarchyId = null,
    IReadOnlyList<Guid>? SiteIds = null,
    IReadOnlyList<ContentSiteUrlInput>? SiteUrls = null) : IRequest<ContentDto>;
