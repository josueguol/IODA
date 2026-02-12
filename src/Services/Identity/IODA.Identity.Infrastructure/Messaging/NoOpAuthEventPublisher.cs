using IODA.Identity.Application.Interfaces;

namespace IODA.Identity.Infrastructure.Messaging;

/// <summary>
/// Publicador de eventos de autenticación que no hace nada.
/// Sustituir por implementación con MassTransit/RabbitMQ cuando se configure el bus.
/// </summary>
public class NoOpAuthEventPublisher : IAuthEventPublisher
{
    public Task PublishUserLoggedInAsync(Guid userId, string email, DateTime occurredAt, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
