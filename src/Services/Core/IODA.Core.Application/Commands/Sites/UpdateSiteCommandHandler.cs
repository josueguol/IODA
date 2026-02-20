using IODA.Core.Application.DTOs;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Exceptions;
using IODA.Core.Domain.Repositories;
using MediatR;

namespace IODA.Core.Application.Commands.Sites;

public class UpdateSiteCommandHandler : IRequestHandler<UpdateSiteCommand, SiteDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSiteCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SiteDto> Handle(UpdateSiteCommand request, CancellationToken cancellationToken)
    {
        var site = await _unitOfWork.Sites.GetByIdAsync(request.SiteId, cancellationToken);
        if (site == null)
            throw new SiteNotFoundException(request.SiteId);

        site.Update(
            request.Name,
            request.Domain,
            request.Subdomain,
            request.Subpath,
            request.ThemeId,
            request.UrlTemplate);

        await _unitOfWork.Sites.UpdateAsync(site, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return site.ToDto();
    }
}
