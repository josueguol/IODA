using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Commands.Content;

public record UpdateContentCommand(
    Guid ContentId,
    string Title,
    Dictionary<string, object> Fields,
    Guid UpdatedBy,
    Guid? ParentContentId = null,
    IReadOnlyList<Guid>? TagIds = null,
    IReadOnlyList<Guid>? HierarchyIds = null,
    IReadOnlyList<Guid>? SiteIds = null) : IRequest<ContentDto>;
