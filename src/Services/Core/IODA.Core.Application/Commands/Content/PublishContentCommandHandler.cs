using IODA.Core.Application.DTOs;
using IODA.Core.Application.Interfaces;
using IODA.Core.Application.Mappings;
using IODA.Core.Application.Schemas;
using IODA.Core.Domain.Exceptions;
using IODA.Core.Domain.Repositories;
using IODA.Shared.Contracts.Events.V1;
using MediatR;
using System.Text.Json;

namespace IODA.Core.Application.Commands.Content;

public class PublishContentCommandHandler : IRequestHandler<PublishContentCommand, ContentDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly IMediaPublicUrlBuilder _mediaPublicUrlBuilder;

    public PublishContentCommandHandler(
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        IMediaPublicUrlBuilder mediaPublicUrlBuilder)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _mediaPublicUrlBuilder = mediaPublicUrlBuilder;
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
        var projectedFields = await BuildPublishedFieldsProjectionAsync(content.Id, content.SchemaId, content.ProjectId, content.Fields, cancellationToken);
        var integrationEvent = new ContentPublishedEventV1
        {
            ContentId = content.Id,
            VersionId = latestVersion.Id,
            Title = content.Title,
            ContentType = content.ContentType,
            PublishedBy = request.PublishedBy,
            PublishedAt = content.PublishedAt!.Value,
            Fields = projectedFields
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

    private async Task<Dictionary<string, object>> BuildPublishedFieldsProjectionAsync(
        Guid contentId,
        Guid schemaId,
        Guid projectId,
        Dictionary<string, object> fields,
        CancellationToken cancellationToken)
    {
        var projected = new Dictionary<string, object>(fields);
        var schema = await _unitOfWork.Schemas.GetByIdAsync(schemaId, cancellationToken);
        if (schema is null)
            return projected;

        foreach (var field in schema.Fields)
        {
            if (!string.Equals(field.FieldType, "media", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!projected.TryGetValue(field.Slug, out var rawValue) || rawValue is null)
                continue;

            if (!MediaFieldRulesParser.TryGetMediaId(rawValue, out var mediaId))
                continue;

            var mediaItem = await _unitOfWork.MediaItems.GetByIdAsync(mediaId, cancellationToken);
            if (mediaItem is null || mediaItem.ProjectId != projectId)
                continue;

            projected[field.Slug] = BuildMediaProjection(mediaItem.Id, mediaItem.ProjectId, mediaItem.ContentType, mediaItem.DisplayName, mediaItem.Version, mediaItem.Metadata);
        }

        return projected;
    }

    private Dictionary<string, object> BuildMediaProjection(
        Guid mediaId,
        Guid projectId,
        string contentType,
        string displayName,
        int version,
        Dictionary<string, object>? metadata)
    {
        var mediaProjection = new Dictionary<string, object>
        {
            ["id"] = mediaId,
            ["url"] = _mediaPublicUrlBuilder.BuildFileUrl(projectId, mediaId),
            ["contentType"] = contentType,
            ["displayName"] = displayName,
            ["version"] = version,
        };

        if (metadata == null || !metadata.TryGetValue("variants", out var variantsRaw))
            return mediaProjection;

        var variants = ParseVariantNames(variantsRaw)
            .Select(name => (object)new Dictionary<string, object>
            {
                ["name"] = name,
                ["url"] = _mediaPublicUrlBuilder.BuildFileUrl(projectId, mediaId, name),
            })
            .ToList();

        if (variants.Count > 0)
            mediaProjection["variants"] = variants;

        return mediaProjection;
    }

    private static List<string> ParseVariantNames(object rawVariants)
    {
        var list = new List<string>();
        var element = ToJsonElement(rawVariants);
        if (element.ValueKind != JsonValueKind.Array)
            return list;

        foreach (var entry in element.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
                continue;

            if (entry.TryGetProperty("name", out var nameEl))
            {
                var name = nameEl.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                    list.Add(name.Trim());
            }
        }

        return list;
    }

    private static JsonElement ToJsonElement(object raw)
    {
        if (raw is JsonElement je) return je;
        var bytes = JsonSerializer.SerializeToUtf8Bytes(raw);
        using var doc = JsonDocument.Parse(bytes);
        return doc.RootElement.Clone();
    }
}
