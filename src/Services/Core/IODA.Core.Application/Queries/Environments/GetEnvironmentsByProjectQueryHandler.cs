using IODA.Core.Application.DTOs;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Queries.Environments;

public class GetEnvironmentsByProjectQueryHandler : IRequestHandler<GetEnvironmentsByProjectQuery, IReadOnlyList<EnvironmentDto>>
{
    private readonly IEnvironmentRepository _environmentRepository;

    public GetEnvironmentsByProjectQueryHandler(IEnvironmentRepository environmentRepository)
    {
        _environmentRepository = environmentRepository;
    }

    public async Task<IReadOnlyList<EnvironmentDto>> Handle(GetEnvironmentsByProjectQuery request, CancellationToken cancellationToken)
    {
        var environments = await _environmentRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        return environments.Select(e => e.ToDto()).ToList();
    }
}
