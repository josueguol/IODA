using IODA.Core.Application.Interfaces;
using IODA.Core.Domain.Entities;
using IODA.Core.Domain.Exceptions;
using IODA.Core.Domain.Repositories;
using IODA.Shared.Contracts.Events;
using IODA.Shared.Contracts.Events.V1;
using MediatR;

namespace IODA.Core.Application.Commands.Content;

public class CreateContentCommandHandler : IRequestHandler<CreateContentCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ISchemaValidationService _schemaValidation;

    public CreateContentCommandHandler(IUnitOfWork unitOfWork, IEventPublisher eventPublisher, ISchemaValidationService schemaValidation)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _schemaValidation = schemaValidation;
    }

    public async Task<Guid> Handle(CreateContentCommand request, CancellationToken cancellationToken)
    {
        var schema = await _unitOfWork.Schemas.GetByIdAsync(request.SchemaId, cancellationToken);
        if (schema == null)
        {
            throw new SchemaNotFoundException(request.SchemaId);
        }

        var validationResult = _schemaValidation.Validate(schema, request.Fields ?? new Dictionary<string, object>());
        if (!validationResult.IsValid)
        {
            throw new SchemaValidationException(validationResult.Errors
                .Select(e => new SchemaValidationErrorEntry(e.Field, e.Message))
                .ToList());
        }

        await MediaFieldContentValidator.ValidateAsync(
            schema,
            request.Fields ?? new Dictionary<string, object>(),
            request.ProjectId,
            _unitOfWork.MediaItems,
            cancellationToken);

        if (request.SiteId.HasValue)
        {
            var site = await _unitOfWork.Sites.GetByIdAsync(request.SiteId.Value, cancellationToken);
            if (site == null || site.ProjectId != request.ProjectId)
            {
                throw new SiteNotFoundException(request.SiteId.Value);
            }
        }

        var order = request.Order ?? await _unitOfWork.Contents.GetNextOrderForParentAsync(null, cancellationToken);
        var fields = request.Fields ?? new Dictionary<string, object>();
        var content = Domain.Entities.Content.Create(
            request.ProjectId,
            request.EnvironmentId,
            request.SiteId,
            null,
            order,
            request.SchemaId,
            request.Title,
            request.Slug,
            request.ContentType,
            fields,
            request.CreatedBy);

        await _unitOfWork.Contents.AddAsync(content, cancellationToken);

        if (request.TagIds is { Count: > 0 })
        {
            var tags = await _unitOfWork.Tags.GetByIdsAsync(request.TagIds, cancellationToken);
            if (tags.Count != request.TagIds.Count)
                throw new ArgumentException("One or more tag IDs are invalid or do not belong to the project.", nameof(request.TagIds));
            if (tags.Any(t => t.ProjectId != request.ProjectId))
                throw new ArgumentException("All tags must belong to the project.", nameof(request.TagIds));
            await _unitOfWork.ContentTags.ReplaceTagsForContentAsync(content.Id, request.TagIds, cancellationToken);
        }

        if (request.PrimaryHierarchyId.HasValue && (request.HierarchyIds == null || request.HierarchyIds.Count == 0))
            throw new ArgumentException("HierarchyIds are required when PrimaryHierarchyId is provided.", nameof(request.PrimaryHierarchyId));

        if (request.HierarchyIds is { Count: > 0 })
        {
            var hierarchies = await _unitOfWork.Hierarchies.GetByIdsAsync(request.HierarchyIds, cancellationToken);
            if (hierarchies.Count != request.HierarchyIds.Count)
                throw new ArgumentException("One or more hierarchy IDs are invalid or do not belong to the project.", nameof(request.HierarchyIds));
            if (hierarchies.Any(h => h.ProjectId != request.ProjectId))
                throw new ArgumentException("All hierarchies must belong to the project.", nameof(request.HierarchyIds));

            if (request.PrimaryHierarchyId.HasValue && !request.HierarchyIds.Contains(request.PrimaryHierarchyId.Value))
                throw new ArgumentException("PrimaryHierarchyId must be included in HierarchyIds.", nameof(request.PrimaryHierarchyId));

            await _unitOfWork.ContentHierarchies.ReplaceHierarchiesForContentAsync(
                content.Id,
                request.HierarchyIds,
                request.PrimaryHierarchyId,
                cancellationToken);
        }

        var targetSiteIds = new HashSet<Guid>();
        if (request.SiteId.HasValue)
            targetSiteIds.Add(request.SiteId.Value);

        if (request.SiteIds is { Count: > 0 })
        {
            foreach (var siteId in request.SiteIds)
            {
                var site = await _unitOfWork.Sites.GetByIdAsync(siteId, cancellationToken);
                if (site == null)
                    throw new ArgumentException($"Site {siteId} not found.", nameof(request.SiteIds));
                if (site.ProjectId != request.ProjectId)
                    throw new ArgumentException("All sites must belong to the same project.", nameof(request.SiteIds));
                if (site.EnvironmentId.HasValue && site.EnvironmentId != request.EnvironmentId)
                    throw new ArgumentException("All sites must belong to the same environment or be global to the project.", nameof(request.SiteIds));
                targetSiteIds.Add(siteId);
            }
            await _unitOfWork.ContentSites.ReplaceSitesForContentAsync(content.Id, request.SiteIds, cancellationToken);
        }

        if (targetSiteIds.Count > 0)
        {
            var requestedPathBySite = (request.SiteUrls ?? Array.Empty<ContentSiteUrlInput>())
                .GroupBy(x => x.SiteId)
                .ToDictionary(g => g.Key, g => g.Last().Path);
            var unknownSiteIds = requestedPathBySite.Keys.Where(siteId => !targetSiteIds.Contains(siteId)).ToList();
            if (unknownSiteIds.Count > 0)
                throw new ArgumentException("SiteUrls contains site IDs that are not assigned to the content.", nameof(request.SiteUrls));

            var siteUrls = new List<Domain.Entities.ContentSiteUrl>();
            foreach (var siteId in targetSiteIds)
            {
                var rawPath = requestedPathBySite.TryGetValue(siteId, out var customPath)
                    ? customPath
                    : content.Slug.Value;
                var normalizedPath = Domain.Entities.ContentSiteUrl.NormalizePath(rawPath);

                var existing = await _unitOfWork.ContentSiteUrls.GetBySiteAndPathAsync(siteId, normalizedPath, cancellationToken);
                if (existing != null && existing.ContentId != content.Id)
                    throw new ArgumentException($"Path '{normalizedPath}' is already in use for site '{siteId}'.", nameof(request.SiteUrls));

                siteUrls.Add(Domain.Entities.ContentSiteUrl.Create(content.Id, siteId, normalizedPath));
            }

            await _unitOfWork.ContentSiteUrls.ReplaceForContentAsync(content.Id, siteUrls, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var integrationEvent = new ContentCreatedEventV1
        {
            ContentId = content.Id,
            Title = content.Title,
            ContentType = content.ContentType,
            Status = content.Status.Value,
            ProjectId = content.ProjectId,
            EnvironmentId = content.EnvironmentId,
            CreatedBy = request.CreatedBy,
            Fields = content.Fields
        };

        await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        return content.Id;
    }
}
