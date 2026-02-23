namespace IODA.Identity.Application.Interfaces;

/// <summary>
/// Cliente para obtener los nombres de roles efectivos de un usuario desde Authorization.
/// Usado en Login y Refresh para incluir roles en el JWT (ej. SuperAdmin → UI trata como todos los permisos).
/// </summary>
public interface IUserRolesClient
{
    Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, CancellationToken cancellationToken = default);
}
