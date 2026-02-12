using IODA.Core.Application.Interfaces;
using IODA.Shared.Contracts.Events;
using MassTransit;

namespace IODA.Core.Infrastructure.Messaging;

/// <summary>
/// Publishes integration events to RabbitMQ via MassTransit
/// </summary>
public class MassTransitEventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        await _publishEndpoint.Publish(integrationEvent, cancellationToken);
    }
}
