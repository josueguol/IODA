using IODA.Core.Domain.Exceptions;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Commands.Sites;

public class DeleteSiteCommandHandler : IRequestHandler<DeleteSiteCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSiteCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteSiteCommand request, CancellationToken cancellationToken)
    {
        var site = await _unitOfWork.Sites.GetByIdAsync(request.SiteId, cancellationToken);
        if (site == null)
            throw new SiteNotFoundException(request.SiteId);

        await _unitOfWork.Sites.DeleteAsync(site, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
