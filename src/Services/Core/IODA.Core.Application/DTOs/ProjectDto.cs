namespace IODA.Core.Application.DTOs;

public record ProjectDto(
    Guid Id,
    string PublicId,
    string Name,
    string Slug,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Guid CreatedBy);
