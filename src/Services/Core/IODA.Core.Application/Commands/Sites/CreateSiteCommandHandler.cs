using IODA.Core.Domain.Entities;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Commands.Sites;

public class CreateSiteCommandHandler : IRequestHandler<CreateSiteCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEnvironmentRepository _environmentRepository;

    public CreateSiteCommandHandler(IUnitOfWork unitOfWork, IEnvironmentRepository environmentRepository)
    {
        _unitOfWork = unitOfWork;
        _environmentRepository = environmentRepository;
    }

    public async Task<Guid> Handle(CreateSiteCommand request, CancellationToken cancellationToken)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
            throw new InvalidOperationException($"Project '{request.ProjectId}' not found.");

        if (request.EnvironmentId.HasValue)
        {
            var environment = await _environmentRepository.GetByIdAsync(request.EnvironmentId.Value, cancellationToken);
            if (environment == null)
                throw new InvalidOperationException($"Environment '{request.EnvironmentId.Value}' not found.");
        }

        var site = Site.Create(
            request.ProjectId,
            request.EnvironmentId,
            request.Name,
            request.Domain,
            request.Subdomain,
            request.Subpath,
            request.ThemeId,
            request.CreatedBy);

        await _unitOfWork.Sites.AddAsync(site, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return site.Id;
    }
}
