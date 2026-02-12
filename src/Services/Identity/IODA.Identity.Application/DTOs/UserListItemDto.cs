namespace IODA.Identity.Application.DTOs;

/// <summary>
/// DTO para listar usuarios en el panel de administración (sin datos sensibles).
/// </summary>
public record UserListItemDto(
    Guid Id,
    string Email,
    string? DisplayName,
    bool IsActive,
    DateTime CreatedAt);
