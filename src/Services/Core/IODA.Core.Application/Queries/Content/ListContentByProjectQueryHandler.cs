using IODA.Core.Application.DTOs;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Queries.Content;

public class ListContentByProjectQueryHandler : IRequestHandler<ListContentByProjectQuery, PagedResultDto<ContentListItemDto>>
{
    private readonly IContentRepository _contentRepository;

    public ListContentByProjectQueryHandler(IContentRepository contentRepository)
    {
        _contentRepository = contentRepository;
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

        var totalCount = query.Count();
        var items = query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => c.ToListItemDto())
            .ToList();

        return new PagedResultDto<ContentListItemDto>(items, totalCount, request.Page, request.PageSize);
    }
}
