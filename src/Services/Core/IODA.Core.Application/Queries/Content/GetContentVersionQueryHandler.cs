using IODA.Core.Application.DTOs;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Queries.Content;

public class GetContentVersionQueryHandler : IRequestHandler<GetContentVersionQuery, ContentVersionDto?>
{
    private readonly IContentRepository _contentRepository;

    public GetContentVersionQueryHandler(IContentRepository contentRepository)
    {
        _contentRepository = contentRepository;
    }

    public async Task<ContentVersionDto?> Handle(GetContentVersionQuery request, CancellationToken cancellationToken)
    {
        var content = await _contentRepository.GetByIdAsync(request.ContentId, cancellationToken);
        if (content == null)
        {
            return null;
        }

        var version = content.GetVersion(request.VersionNumber);
        return version?.ToDto();
    }
}
