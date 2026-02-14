namespace IODA.Identity.Application.Interfaces;

/// <summary>
/// Cliente para obtener los permisos efectivos de un usuario desde el servicio Authorization.
/// Usado en Login y Refresh para enriquecer el JWT (Fase 2.2).
/// </summary>
public interface IEffectivePermissionsClient
{
    Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
}
