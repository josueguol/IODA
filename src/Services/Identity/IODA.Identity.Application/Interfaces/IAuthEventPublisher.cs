namespace IODA.Identity.Application.Interfaces;

/// <summary>
/// Publica eventos de autenticación (login, logout, etc.) al bus de mensajes.
/// </summary>
public interface IAuthEventPublisher
{
    Task PublishUserLoggedInAsync(Guid userId, string email, DateTime occurredAt, CancellationToken cancellationToken = default);
}
