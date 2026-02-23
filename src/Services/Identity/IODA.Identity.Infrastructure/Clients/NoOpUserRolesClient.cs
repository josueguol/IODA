using IODA.Identity.Application.Interfaces;

namespace IODA.Identity.Infrastructure.Clients;

/// <summary>No-op cuando no está configurada la URL del servicio Authorization (no se obtienen roles).</summary>
public class NoOpUserRolesClient : IUserRolesClient
{
    public Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
}
