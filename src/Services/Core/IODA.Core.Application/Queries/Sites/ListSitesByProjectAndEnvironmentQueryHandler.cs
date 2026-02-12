using IODA.Core.Application.DTOs;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Queries.Sites;

public class ListSitesByProjectAndEnvironmentQueryHandler : IRequestHandler<ListSitesByProjectAndEnvironmentQuery, IReadOnlyList<SiteDto>>
{
    private readonly ISiteRepository _repository;

    public ListSitesByProjectAndEnvironmentQueryHandler(ISiteRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<SiteDto>> Handle(ListSitesByProjectAndEnvironmentQuery request, CancellationToken cancellationToken)
    {
        var sites = await _repository.GetByProjectAndEnvironmentAsync(request.ProjectId, request.EnvironmentId, cancellationToken);
        return sites.Select(s => s.ToDto()).ToList();
    }
}
