namespace IODA.Core.Application.DTOs;

public record SiteDto(
    Guid Id,
    string PublicId,
    Guid ProjectId,
    Guid? EnvironmentId,
    string Name,
    string Domain,
    string? Subdomain,
    string? Subpath,
    string? ThemeId,
    string? UrlTemplate,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Guid CreatedBy);

public record SiteListItemDto(
    Guid Id,
    string PublicId,
    string Name,
    string Domain,
    string? Subdomain,
    bool IsActive);
