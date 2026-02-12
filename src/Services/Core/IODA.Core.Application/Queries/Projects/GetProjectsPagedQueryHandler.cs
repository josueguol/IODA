using IODA.Core.Application.DTOs;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Queries.Projects;

public class GetProjectsPagedQueryHandler : IRequestHandler<GetProjectsPagedQuery, PagedResultDto<ProjectDto>>
{
    private readonly IProjectRepository _projectRepository;

    public GetProjectsPagedQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<PagedResultDto<ProjectDto>> Handle(GetProjectsPagedQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var (items, totalCount) = await _projectRepository.GetPagedAsync(page, pageSize, cancellationToken);
        var dtos = items.Select(p => p.ToDto()).ToList();
        return new PagedResultDto<ProjectDto>(dtos, totalCount, page, pageSize);
    }
}
