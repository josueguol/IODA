using MediatR;

namespace IODA.Authorization.Application.Queries;

/// <summary>
/// Obtiene los códigos de permiso efectivos de un usuario (unión de todos los permisos de sus roles).
/// </summary>
public record GetEffectivePermissionsQuery(Guid UserId) : IRequest<IReadOnlyList<string>>;
