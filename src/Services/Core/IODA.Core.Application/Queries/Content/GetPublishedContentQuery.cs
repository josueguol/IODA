using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Queries.Content;

public record GetPublishedContentQuery(
    Guid ProjectId,
    Guid EnvironmentId,
    int Page = 1,
    int PageSize = 20,
    Guid? SiteId = null) : IRequest<PagedResultDto<ContentListItemDto>>;
