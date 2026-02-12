using IODA.Core.Application.DTOs;
using MediatR;

namespace IODA.Core.Application.Queries.Media;

public record ListMediaByProjectQuery(Guid ProjectId, int Page = 1, int PageSize = 20) : IRequest<PagedResultDto<MediaItemDto>>;
