namespace IODA.Core.Application.DTOs;

public record MediaItemDto(
    Guid Id,
    string PublicId,
    Guid ProjectId,
    string FileName,
    string DisplayName,
    string ContentType,
    long SizeBytes,
    string StorageKey,
    int Version,
    Dictionary<string, object>? Metadata,
    DateTime CreatedAt,
    Guid CreatedBy);
