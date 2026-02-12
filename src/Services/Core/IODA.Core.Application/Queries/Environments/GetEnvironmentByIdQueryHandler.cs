using IODA.Core.Application.DTOs;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Queries.Environments;

public class GetEnvironmentByIdQueryHandler : IRequestHandler<GetEnvironmentByIdQuery, EnvironmentDto?>
{
    private readonly IEnvironmentRepository _environmentRepository;

    public GetEnvironmentByIdQueryHandler(IEnvironmentRepository environmentRepository)
    {
        _environmentRepository = environmentRepository;
    }

    public async Task<EnvironmentDto?> Handle(GetEnvironmentByIdQuery request, CancellationToken cancellationToken)
    {
        var environment = await _environmentRepository.GetByIdAsync(request.EnvironmentId, cancellationToken);
        if (environment is null || environment.ProjectId != request.ProjectId)
            return null;
        return environment.ToDto();
    }
}
