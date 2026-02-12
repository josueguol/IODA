using IODA.Core.Application.DTOs;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Queries.Media;

public class ListMediaByProjectQueryHandler : IRequestHandler<ListMediaByProjectQuery, PagedResultDto<MediaItemDto>>
{
    private readonly IMediaItemRepository _repository;

    public ListMediaByProjectQueryHandler(IMediaItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<MediaItemDto>> Handle(ListMediaByProjectQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetByProjectAsync(request.ProjectId, request.Page, request.PageSize, cancellationToken);
        var total = await _repository.CountByProjectAsync(request.ProjectId, cancellationToken);
        var dtos = items.Select(MediaMappings.ToDto).ToList();
        return new PagedResultDto<MediaItemDto>(dtos, total, request.Page, request.PageSize);
    }
}
