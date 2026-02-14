using IODA.Identity.Application.Interfaces;

namespace IODA.Identity.Infrastructure.Clients;

/// <summary>
/// Implementación que devuelve lista vacía cuando no está configurada la URL del servicio Authorization.
/// </summary>
public class NoOpEffectivePermissionsClient : IEffectivePermissionsClient
{
    public Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
}
