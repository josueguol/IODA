using IODA.Core.Application.DTOs;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Queries.Hierarchies;

public class ListHierarchiesByProjectQueryHandler : IRequestHandler<ListHierarchiesByProjectQuery, IReadOnlyList<HierarchyDto>>
{
    private readonly IHierarchyRepository _hierarchyRepository;

    public ListHierarchiesByProjectQueryHandler(IHierarchyRepository hierarchyRepository)
    {
        _hierarchyRepository = hierarchyRepository;
    }

    public async Task<IReadOnlyList<HierarchyDto>> Handle(ListHierarchiesByProjectQuery request, CancellationToken cancellationToken)
    {
        var list = await _hierarchyRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        return list.Select(h => new HierarchyDto(h.Id, h.ProjectId, h.Name, h.Slug, h.Description, h.ImageUrl, h.ParentHierarchyId)).ToList();
    }
}
