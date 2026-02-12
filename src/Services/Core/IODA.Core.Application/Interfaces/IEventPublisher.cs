using IODA.Shared.Contracts.Events;

namespace IODA.Core.Application.Interfaces;

/// <summary>
/// Publishes integration events to the message bus (RabbitMQ).
/// Implemented by Infrastructure layer.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IEvent;
}
