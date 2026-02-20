using IODA.Core.Application.DTOs;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Queries.Content;

public class GetContentByIdQueryHandler : IRequestHandler<GetContentByIdQuery, ContentDto?>
{
    private readonly IContentRepository _contentRepository;
    private readonly IContentTagRepository _contentTagRepository;
    private readonly IContentHierarchyRepository _contentHierarchyRepository;
    private readonly IContentSiteRepository _contentSiteRepository;

    public GetContentByIdQueryHandler(
        IContentRepository contentRepository,
        IContentTagRepository contentTagRepository,
        IContentHierarchyRepository contentHierarchyRepository,
        IContentSiteRepository contentSiteRepository)
    {
        _contentRepository = contentRepository;
        _contentTagRepository = contentTagRepository;
        _contentHierarchyRepository = contentHierarchyRepository;
        _contentSiteRepository = contentSiteRepository;
    }

    public async Task<ContentDto?> Handle(GetContentByIdQuery request, CancellationToken cancellationToken)
    {
        var content = await _contentRepository.GetByIdAsync(request.ContentId, cancellationToken);
        if (content == null)
            return null;
        var tagIds = await _contentTagRepository.GetTagIdsByContentIdAsync(content.Id, cancellationToken);
        var hierarchyIds = await _contentHierarchyRepository.GetHierarchyIdsByContentIdAsync(content.Id, cancellationToken);
        var siteIds = await _contentSiteRepository.GetSiteIdsByContentIdAsync(content.Id, cancellationToken);
        return content.ToDto(tagIds, hierarchyIds, siteIds);
    }
}
