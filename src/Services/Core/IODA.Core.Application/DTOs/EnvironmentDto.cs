namespace IODA.Core.Application.DTOs;

public record EnvironmentDto(
    Guid Id,
    string PublicId,
    string Name,
    string Slug,
    string? Description,
    bool IsActive,
    Guid ProjectId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
