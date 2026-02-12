using IODA.Core.Domain.Exceptions;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Commands.Sites;

public class DeactivateSiteCommandHandler : IRequestHandler<DeactivateSiteCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateSiteCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeactivateSiteCommand request, CancellationToken cancellationToken)
    {
        var site = await _unitOfWork.Sites.GetByIdAsync(request.SiteId, cancellationToken);
        if (site == null)
            throw new SiteNotFoundException(request.SiteId);

        site.Deactivate();
        await _unitOfWork.Sites.UpdateAsync(site, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
