using IODA.Core.Application.DTOs;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Queries.Sites;

public class ListSitesByProjectQueryHandler : IRequestHandler<ListSitesByProjectQuery, IReadOnlyList<SiteDto>>
{
    private readonly ISiteRepository _repository;

    public ListSitesByProjectQueryHandler(ISiteRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<SiteDto>> Handle(ListSitesByProjectQuery request, CancellationToken cancellationToken)
    {
        var sites = await _repository.GetByProjectAsync(request.ProjectId, cancellationToken);
        return sites.Select(s => s.ToDto()).ToList();
    }
}
