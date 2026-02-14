using IODA.Core.Domain.Exceptions;
using IODA.Core.Domain.Repositories;
using MediatR;
using EnvironmentEntity = IODA.Core.Domain.Entities.Environment;

namespace IODA.Core.Application.Commands.Environments;

public class CreateEnvironmentCommandHandler : IRequestHandler<CreateEnvironmentCommand, Guid>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IEnvironmentRepository _environmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEnvironmentCommandHandler(
        IProjectRepository projectRepository,
        IEnvironmentRepository environmentRepository,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _environmentRepository = environmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var projectExists = await _projectRepository.ExistsAsync(request.ProjectId, cancellationToken);
        if (!projectExists)
        {
            throw new ProjectNotFoundException(request.ProjectId);
        }

        var environment = EnvironmentEntity.Create(
            request.ProjectId,
            request.Name,
            request.Description);

        await _environmentRepository.AddAsync(environment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return environment.Id;
    }
}
