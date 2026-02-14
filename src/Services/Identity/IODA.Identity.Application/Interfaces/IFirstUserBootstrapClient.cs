namespace IODA.Identity.Application.Interfaces;

/// <summary>
/// Cliente para notificar a Authorization que se ha registrado el primer usuario y asignarle el rol SuperAdmin (2.5).
/// </summary>
public interface IFirstUserBootstrapClient
{
    /// <summary>
    /// Llama a Authorization para crear la regla de acceso SuperAdmin para el usuario. Solo tiene efecto si aún no existe ninguna regla (primer usuario).
    /// </summary>
    Task BootstrapFirstUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
