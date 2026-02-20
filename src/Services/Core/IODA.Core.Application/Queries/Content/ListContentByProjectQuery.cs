using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Queries.Content;

public record ListContentByProjectQuery(
    Guid ProjectId,
    int Page = 1,
    int PageSize = 20,
    string? ContentType = null,
    string? Status = null,
    Guid? SiteId = null,
    Guid? ParentContentId = null) : IRequest<PagedResultDto<ContentListItemDto>>;
