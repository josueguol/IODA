using IODA.Identity.Application.Interfaces;

namespace IODA.Identity.Infrastructure.Clients;

/// <summary>
/// No-op cuando no está configurada la URL del servicio Authorization (no se llama al bootstrap).
/// </summary>
public class NoOpFirstUserBootstrapClient : IFirstUserBootstrapClient
{
    public Task BootstrapFirstUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
