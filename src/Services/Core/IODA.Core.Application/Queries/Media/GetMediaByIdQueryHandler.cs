using IODA.Core.Application.DTOs;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Queries.Media;

public class GetMediaByIdQueryHandler : IRequestHandler<GetMediaByIdQuery, MediaItemDto?>
{
    private readonly IMediaItemRepository _repository;

    public GetMediaByIdQueryHandler(IMediaItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<MediaItemDto?> Handle(GetMediaByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(request.MediaItemId, cancellationToken);
        return item?.ToDto();
    }
}
