using IODA.Core.Application.DTOs;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Queries.Hierarchies;

public class GetHierarchyByIdQueryHandler : IRequestHandler<GetHierarchyByIdQuery, HierarchyDto?>
{
    private readonly IHierarchyRepository _hierarchyRepository;

    public GetHierarchyByIdQueryHandler(IHierarchyRepository hierarchyRepository)
    {
        _hierarchyRepository = hierarchyRepository;
    }

    public async Task<HierarchyDto?> Handle(GetHierarchyByIdQuery request, CancellationToken cancellationToken)
    {
        var hierarchy = await _hierarchyRepository.GetByIdAsync(request.HierarchyId, cancellationToken);
        return hierarchy == null
            ? null
            : new HierarchyDto(hierarchy.Id, hierarchy.ProjectId, hierarchy.Name, hierarchy.Slug, hierarchy.Description, hierarchy.ImageUrl, hierarchy.ParentHierarchyId);
    }
}
