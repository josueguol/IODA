using IODA.Core.Application.DTOs;
using IODA.Core.Application.Interfaces;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Exceptions;
using IODA.Core.Domain.Repositories;
using IODA.Shared.Contracts.Events.V1;
using MediatR;

namespace IODA.Core.Application.Commands.Content;

public class UnpublishContentCommandHandler : IRequestHandler<UnpublishContentCommand, ContentDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public UnpublishContentCommandHandler(IUnitOfWork unitOfWork, IEventPublisher eventPublisher)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<ContentDto> Handle(UnpublishContentCommand request, CancellationToken cancellationToken)
    {
        var content = await _unitOfWork.Contents.GetByIdAsync(request.ContentId, cancellationToken);
        if (content == null)
        {
            throw new ContentNotFoundException(request.ContentId);
        }

        content.Unpublish(request.Reason, request.UnpublishedBy);

        await _unitOfWork.Contents.UpdateAsync(content, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var integrationEvent = new ContentUnpublishedEventV1
        {
            ContentId = content.Id,
            UnpublishedBy = request.UnpublishedBy,
            Reason = request.Reason,
            UnpublishedAt = DateTime.UtcNow
        };

        await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        var tagIds = await _unitOfWork.ContentTags.GetTagIdsByContentIdAsync(content.Id, cancellationToken);
        var hierarchyIds = await _unitOfWork.ContentHierarchies.GetHierarchyIdsByContentIdAsync(content.Id, cancellationToken);
        var primaryHierarchyId = await _unitOfWork.ContentHierarchies.GetPrimaryHierarchyIdByContentIdAsync(content.Id, cancellationToken);
        var siteIds = await _unitOfWork.ContentSites.GetSiteIdsByContentIdAsync(content.Id, cancellationToken);
        var siteUrls = await _unitOfWork.ContentSiteUrls.GetByContentIdAsync(content.Id, cancellationToken);
        var siteUrlDtos = siteUrls
            .Select(x => new ContentSiteUrlDto(x.SiteId, x.Path, content.SiteId.HasValue && x.SiteId == content.SiteId.Value))
            .ToList();
        return content.ToDto(primaryHierarchyId, tagIds, hierarchyIds, siteIds, siteUrlDtos);
    }
}
