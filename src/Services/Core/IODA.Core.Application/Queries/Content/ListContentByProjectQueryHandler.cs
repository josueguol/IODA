using IODA.Core.Application.DTOs;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Queries.Content;

public class ListContentByProjectQueryHandler : IRequestHandler<ListContentByProjectQuery, PagedResultDto<ContentListItemDto>>
{
    private readonly IContentRepository _contentRepository;
    private readonly IHierarchyRepository _hierarchyRepository;
    private readonly IContentHierarchyRepository _contentHierarchyRepository;

    public ListContentByProjectQueryHandler(
        IContentRepository contentRepository,
        IHierarchyRepository hierarchyRepository,
        IContentHierarchyRepository contentHierarchyRepository)
    {
        _contentRepository = contentRepository;
        _hierarchyRepository = hierarchyRepository;
        _contentHierarchyRepository = contentHierarchyRepository;
    }

    public async Task<PagedResultDto<ContentListItemDto>> Handle(
        ListContentByProjectQuery request,
        CancellationToken cancellationToken)
    {
        var allContent = await _contentRepository.GetByProjectAsync(request.ProjectId, request.SiteId, cancellationToken);
        var query = allContent.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.ContentType))
        {
            query = query.Where(c => c.ContentType == request.ContentType);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(c => c.Status.Value == request.Status);
        }

        if (request.ParentContentId.HasValue)
        {
            query = query.Where(c => c.ParentContentId == request.ParentContentId.Value);
        }

        if (request.SectionId.HasValue)
        {
            var descendantIds = await _hierarchyRepository.GetDescendantIdsAsync(request.SectionId.Value, maxDepth: 50, cancellationToken);
            var scopeHierarchyIds = new List<Guid> { request.SectionId.Value };
            scopeHierarchyIds.AddRange(descendantIds);

            var contentIds = await _contentHierarchyRepository.GetContentIdsByHierarchyIdsAsync(scopeHierarchyIds, cancellationToken);
            var contentIdSet = contentIds.ToHashSet();
            query = query.Where(c => contentIdSet.Contains(c.Id));
        }

        var totalCount = query.Count();
        var items = query
            .OrderBy(c => c.Order)
            .ThenBy(c => c.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => c.ToListItemDto())
            .ToList();

        return new PagedResultDto<ContentListItemDto>(items, totalCount, request.Page, request.PageSize);
    }
}
