using IODA.Core.Application.DTOs;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Queries.Content;

public class GetPublishedContentQueryHandler : IRequestHandler<GetPublishedContentQuery, PagedResultDto<ContentListItemDto>>
{
    private readonly IContentRepository _contentRepository;

    public GetPublishedContentQueryHandler(IContentRepository contentRepository)
    {
        _contentRepository = contentRepository;
    }

    public async Task<PagedResultDto<ContentListItemDto>> Handle(
        GetPublishedContentQuery request,
        CancellationToken cancellationToken)
    {
        var published = await _contentRepository.GetPublishedAsync(
            request.ProjectId,
            request.EnvironmentId,
            request.SiteId,
            cancellationToken);

        var totalCount = published.Count();
        var items = published
            .OrderByDescending(c => c.PublishedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => c.ToListItemDto())
            .ToList();

        return new PagedResultDto<ContentListItemDto>(items, totalCount, request.Page, request.PageSize);
    }
}
