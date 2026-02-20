using MediatR;

namespace IODA.Core.Application.Commands.Content;

public record CreateContentCommand(
    Guid ProjectId,
    Guid EnvironmentId,
    Guid? SiteId,
    Guid? ParentContentId,
    Guid SchemaId,
    string Title,
    string ContentType,
    Dictionary<string, object> Fields,
    IReadOnlyList<Guid>? TagIds,
    IReadOnlyList<Guid>? HierarchyIds,
    IReadOnlyList<Guid>? SiteIds,
    Guid CreatedBy) : IRequest<Guid>;
