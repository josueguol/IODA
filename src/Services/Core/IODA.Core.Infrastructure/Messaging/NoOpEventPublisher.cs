using IODA.Core.Application.Interfaces;
using IODA.Shared.Contracts.Events;

namespace IODA.Core.Infrastructure.Messaging;

/// <summary>
/// Event publisher que no hace nada. Se usa cuando RabbitMQ no está configurado o está deshabilitado
/// para que la API arranque y se pueda probar el CRUD sin cola de mensajes.
/// </summary>
public class NoOpEventPublisher : IEventPublisher
{
    public Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        return Task.CompletedTask;
    }
}
