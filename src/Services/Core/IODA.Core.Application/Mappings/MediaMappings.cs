using IODA.Core.Application.DTOs;
using IODA.Core.Domain.Entities;

namespace IODA.Core.Application.Mappings;

public static class MediaMappings
{
    public static MediaItemDto ToDto(this MediaItem item)
    {
        return new MediaItemDto(
            item.Id,
            item.PublicId.FullId,
            item.ProjectId,
            item.FileName,
            item.DisplayName,
            item.ContentType,
            item.SizeBytes,
            item.StorageKey,
            item.Version,
            item.Metadata != null ? new Dictionary<string, object>(item.Metadata) : null,
            item.CreatedAt,
            item.CreatedBy);
    }
}
