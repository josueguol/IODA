using MediatR;

namespace IODA.Core.Application.Commands.Content;

public record CreateContentCommand(
    Guid ProjectId,
    Guid EnvironmentId,
    Guid? SiteId,
    Guid SchemaId,
    string Title,
    string? Slug,
    string ContentType,
    Dictionary<string, object> Fields,
    IReadOnlyList<Guid>? TagIds,
    IReadOnlyList<Guid>? HierarchyIds,
    Guid? PrimaryHierarchyId,
    IReadOnlyList<Guid>? SiteIds,
    IReadOnlyList<ContentSiteUrlInput>? SiteUrls,
    Guid CreatedBy,
    int? Order = null) : IRequest<Guid>;

public record ContentSiteUrlInput(Guid SiteId, string Path);
