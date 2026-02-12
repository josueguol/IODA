namespace IODA.Core.Application.DTOs;

public record ContentVersionDto(
    Guid Id,
    Guid ContentId,
    int VersionNumber,
    string Title,
    Dictionary<string, object> Fields,
    string Status,
    DateTime CreatedAt,
    Guid CreatedBy,
    string? Comment);
