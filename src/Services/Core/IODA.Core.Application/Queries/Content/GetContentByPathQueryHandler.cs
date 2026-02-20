using IODA.Core.Application.DTOs;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Queries.Content;

public class GetContentByPathQueryHandler : IRequestHandler<GetContentByPathQuery, ContentDto?>
{
    private readonly IContentRepository _contentRepository;
    private readonly IContentTagRepository _contentTagRepository;
    private readonly IContentHierarchyRepository _contentHierarchyRepository;
    private readonly IContentSiteRepository _contentSiteRepository;

    public GetContentByPathQueryHandler(
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

    public async Task<ContentDto?> Handle(GetContentByPathQuery request, CancellationToken cancellationToken)
    {
        var slug = request.Path?.Trim().Trim('/');
        if (string.IsNullOrEmpty(slug))
            return null;

        var content = await _contentRepository.GetPublishedBySiteAndSlugAsync(request.SiteId, slug, cancellationToken);
        if (content == null)
            return null;

        var tagIds = await _contentTagRepository.GetTagIdsByContentIdAsync(content.Id, cancellationToken);
        var hierarchyIds = await _contentHierarchyRepository.GetHierarchyIdsByContentIdAsync(content.Id, cancellationToken);
        var siteIds = await _contentSiteRepository.GetSiteIdsByContentIdAsync(content.Id, cancellationToken);
        return content.ToDto(tagIds, hierarchyIds, siteIds);
    }
}
