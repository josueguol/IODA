using IODA.Core.Application.DTOs;
using IODA.Core.Application.Interfaces;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Exceptions;
using IODA.Core.Domain.Repositories;
using IODA.Shared.Contracts.Events.V1;
using MediatR;

namespace IODA.Core.Application.Commands.Content;

public class PublishContentCommandHandler : IRequestHandler<PublishContentCommand, ContentDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public PublishContentCommandHandler(IUnitOfWork unitOfWork, IEventPublisher eventPublisher)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<ContentDto> Handle(PublishContentCommand request, CancellationToken cancellationToken)
    {
        var content = await _unitOfWork.Contents.GetByIdAsync(request.ContentId, cancellationToken);
        if (content == null)
        {
            throw new ContentNotFoundException(request.ContentId);
        }

        content.Publish(request.PublishedBy);

        await _unitOfWork.Contents.UpdateAsync(content, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var latestVersion = content.GetLatestVersion();
        var integrationEvent = new ContentPublishedEventV1
        {
            ContentId = content.Id,
            VersionId = latestVersion.Id,
            Title = content.Title,
            ContentType = content.ContentType,
            PublishedBy = request.PublishedBy,
            PublishedAt = content.PublishedAt!.Value
        };

        await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        var tagIds = await _unitOfWork.ContentTags.GetTagIdsByContentIdAsync(content.Id, cancellationToken);
        var hierarchyIds = await _unitOfWork.ContentHierarchies.GetHierarchyIdsByContentIdAsync(content.Id, cancellationToken);
        var siteIds = await _unitOfWork.ContentSites.GetSiteIdsByContentIdAsync(content.Id, cancellationToken);
        return content.ToDto(tagIds, hierarchyIds, siteIds);
    }
}
