using IODA.Core.Application.DTOs;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Queries.Sites;

public class GetSiteByIdQueryHandler : IRequestHandler<GetSiteByIdQuery, SiteDto?>
{
    private readonly ISiteRepository _repository;

    public GetSiteByIdQueryHandler(ISiteRepository repository)
    {
        _repository = repository;
    }

    public async Task<SiteDto?> Handle(GetSiteByIdQuery request, CancellationToken cancellationToken)
    {
        var site = await _repository.GetByIdAsync(request.SiteId, cancellationToken);
        return site?.ToDto();
    }
}
