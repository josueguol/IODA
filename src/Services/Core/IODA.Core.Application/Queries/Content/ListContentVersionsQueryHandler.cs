using IODA.Core.Application.DTOs;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Queries.Content;

public class ListContentVersionsQueryHandler : IRequestHandler<ListContentVersionsQuery, IReadOnlyList<ContentVersionDto>>
{
    private readonly IContentRepository _contentRepository;

    public ListContentVersionsQueryHandler(IContentRepository contentRepository)
    {
        _contentRepository = contentRepository;
    }

    public async Task<IReadOnlyList<ContentVersionDto>> Handle(ListContentVersionsQuery request, CancellationToken cancellationToken)
    {
        var content = await _contentRepository.GetByIdAsync(request.ContentId, cancellationToken);
        if (content == null)
        {
            return Array.Empty<ContentVersionDto>();
        }

        return content.Versions
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => v.ToDto())
            .ToList();
    }
}
