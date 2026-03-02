using IODA.Core.Application.DTOs;
using IODA.Core.Application.Interfaces;
using IODA.Core.Application.Mappings;
using IODA.Core.Domain.Exceptions;
using IODA.Core.Domain.Repositories;
using IODA.Shared.Contracts.Events.V1;
using MediatR;

namespace IODA.Core.Application.Commands.Content;

public class UpdateContentCommandHandler : IRequestHandler<UpdateContentCommand, ContentDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ISchemaValidationService _schemaValidation;

    public UpdateContentCommandHandler(IUnitOfWork unitOfWork, IEventPublisher eventPublisher, ISchemaValidationService schemaValidation)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _schemaValidation = schemaValidation;
    }

    public async Task<ContentDto> Handle(UpdateContentCommand request, CancellationToken cancellationToken)
    {
        var content = await _unitOfWork.Contents.GetByIdAsync(request.ContentId, cancellationToken);
        if (content == null)
        {
            throw new ContentNotFoundException(request.ContentId);
        }

        var schema = await _unitOfWork.Schemas.GetByIdAsync(content.SchemaId, cancellationToken);
        if (schema == null)
        {
            throw new SchemaNotFoundException(content.SchemaId);
        }

        var validationResult = _schemaValidation.Validate(schema, request.Fields ?? new Dictionary<string, object>());
        if (!validationResult.IsValid)
        {
            throw new SchemaValidationException(validationResult.Errors
                .Select(e => new SchemaValidationErrorEntry(e.Field, e.Message))
                .ToList());
        }

        if (request.ParentContentId.HasValue)
        {
            var parent = await _unitOfWork.Contents.GetByIdAsync(request.ParentContentId.Value, cancellationToken);
            if (parent == null)
                throw new ContentNotFoundException(request.ParentContentId.Value);
            if (parent.ProjectId != content.ProjectId || parent.EnvironmentId != content.EnvironmentId)
                throw new InvalidOperationException("Parent content must belong to the same project and environment.");
            var ancestorIds = await _unitOfWork.Contents.GetAncestorIdsAsync(request.ParentContentId.Value, maxDepth: 50, cancellationToken);
            if (ancestorIds.Contains(content.Id))
                throw new InvalidOperationException("Setting this parent would create a circular reference.");
            content.SetParent(request.ParentContentId);
        }

        if (request.Order.HasValue)
        {
            content.SetOrder(request.Order.Value);
        }

        if (request.TagIds != null)
        {
            if (request.TagIds.Count > 0)
            {
                var tags = await _unitOfWork.Tags.GetByIdsAsync(request.TagIds.ToList(), cancellationToken);
                if (tags.Count != request.TagIds.Count)
                    throw new InvalidOperationException("One or more tag IDs are invalid.");
                if (tags.Any(t => t.ProjectId != content.ProjectId))
                    throw new InvalidOperationException("All tags must belong to the same project.");
            }
            await _unitOfWork.ContentTags.ReplaceTagsForContentAsync(content.Id, request.TagIds, cancellationToken);
        }

        if (request.PrimaryHierarchyId.HasValue && request.HierarchyIds == null)
            throw new InvalidOperationException("HierarchyIds are required when PrimaryHierarchyId is provided.");

        if (request.HierarchyIds != null)
        {
            if (request.HierarchyIds.Count > 0)
            {
                var hierarchies = await _unitOfWork.Hierarchies.GetByIdsAsync(request.HierarchyIds, cancellationToken);
                if (hierarchies.Count != request.HierarchyIds.Count)
                    throw new InvalidOperationException("One or more hierarchy IDs are invalid.");
                if (hierarchies.Any(h => h.ProjectId != content.ProjectId))
                    throw new InvalidOperationException("All hierarchies must belong to the same project.");
            }

            if (request.PrimaryHierarchyId.HasValue && (request.HierarchyIds.Count == 0 || !request.HierarchyIds.Contains(request.PrimaryHierarchyId.Value)))
                throw new InvalidOperationException("PrimaryHierarchyId must be included in HierarchyIds.");

            await _unitOfWork.ContentHierarchies.ReplaceHierarchiesForContentAsync(
                content.Id,
                request.HierarchyIds,
                request.PrimaryHierarchyId,
                cancellationToken);
        }

        var targetSiteIds = new HashSet<Guid>();
        if (content.SiteId.HasValue)
            targetSiteIds.Add(content.SiteId.Value);
        if (request.SiteIds == null)
        {
            var existingSharedSiteIds = await _unitOfWork.ContentSites.GetSiteIdsByContentIdAsync(content.Id, cancellationToken);
            foreach (var siteId in existingSharedSiteIds)
                targetSiteIds.Add(siteId);
        }

        if (request.SiteIds != null)
        {
            if (request.SiteIds.Count > 0)
            {
                foreach (var siteId in request.SiteIds)
                {
                    var site = await _unitOfWork.Sites.GetByIdAsync(siteId, cancellationToken);
                    if (site == null)
                        throw new InvalidOperationException($"Site {siteId} not found.");
                    if (site.ProjectId != content.ProjectId)
                        throw new InvalidOperationException("All sites must belong to the same project.");
                    if (site.EnvironmentId.HasValue && site.EnvironmentId != content.EnvironmentId)
                        throw new InvalidOperationException("All sites must belong to the same environment or be global to the project.");
                    targetSiteIds.Add(siteId);
                }
            }
            await _unitOfWork.ContentSites.ReplaceSitesForContentAsync(content.Id, request.SiteIds, cancellationToken);
        }

        content.Update(request.Title, request.Slug, request.Fields ?? new Dictionary<string, object>(), request.UpdatedBy);

        if (targetSiteIds.Count > 0)
        {
            var requestedPathBySite = (request.SiteUrls ?? Array.Empty<ContentSiteUrlInput>())
                .GroupBy(x => x.SiteId)
                .ToDictionary(g => g.Key, g => g.Last().Path);
            var unknownSiteIds = requestedPathBySite.Keys.Where(siteId => !targetSiteIds.Contains(siteId)).ToList();
            if (unknownSiteIds.Count > 0)
                throw new InvalidOperationException("SiteUrls contains site IDs that are not assigned to the content.");

            var siteUrlEntities = new List<Domain.Entities.ContentSiteUrl>();
            foreach (var siteId in targetSiteIds)
            {
                var rawPath = requestedPathBySite.TryGetValue(siteId, out var customPath)
                    ? customPath
                    : content.Slug.Value;
                var normalizedPath = Domain.Entities.ContentSiteUrl.NormalizePath(rawPath);
                var existing = await _unitOfWork.ContentSiteUrls.GetBySiteAndPathAsync(siteId, normalizedPath, cancellationToken);
                if (existing != null && existing.ContentId != content.Id)
                    throw new InvalidOperationException($"Path '{normalizedPath}' is already in use for site '{siteId}'.");
                siteUrlEntities.Add(Domain.Entities.ContentSiteUrl.Create(content.Id, siteId, normalizedPath));
            }
            await _unitOfWork.ContentSiteUrls.ReplaceForContentAsync(content.Id, siteUrlEntities, cancellationToken);
        }

        await _unitOfWork.Contents.UpdateAsync(content, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var latestVersion = content.GetLatestVersion();
        var integrationEvent = new ContentUpdatedEventV1
        {
            ContentId = content.Id,
            VersionId = latestVersion.Id,
            VersionNumber = content.CurrentVersion,
            Title = content.Title,
            Status = content.Status.Value,
            UpdatedBy = request.UpdatedBy,
            Fields = content.Fields
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
