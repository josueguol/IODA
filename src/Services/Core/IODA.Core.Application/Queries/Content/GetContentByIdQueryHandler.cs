using IODA.Core.Application.DTOs;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Queries.Content;

public class GetContentByIdQueryHandler : IRequestHandler<GetContentByIdQuery, ContentDto?>
{
    private readonly IContentRepository _contentRepository;

    public GetContentByIdQueryHandler(IContentRepository contentRepository)
    {
        _contentRepository = contentRepository;
    }

    public async Task<ContentDto?> Handle(GetContentByIdQuery request, CancellationToken cancellationToken)
    {
        var content = await _contentRepository.GetByIdAsync(request.ContentId, cancellationToken);
        return content?.ToDto();
    }
}
