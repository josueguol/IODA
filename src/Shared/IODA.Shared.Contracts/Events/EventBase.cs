namespace IODA.Shared.Contracts.Events;

/// <summary>
/// Base abstract record for all integration events
/// </summary>
public abstract record EventBase : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public abstract int Version { get; }
    public abstract string EventType { get; }
}
