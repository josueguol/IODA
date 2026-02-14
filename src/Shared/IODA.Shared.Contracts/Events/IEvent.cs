namespace IODA.Shared.Contracts.Events;

/// <summary>
/// Base interface for all integration events
/// Integration events are used for communication between services
/// </summary>
public interface IEvent
{
    /// <summary>
    /// Unique identifier of the event
    /// </summary>
    public Guid EventId { get; }

    /// <summary>
    /// Timestamp when the event occurred (UTC)
    /// </summary>
    public DateTime OccurredAt { get; }

    /// <summary>
    /// Version of the event schema
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Type name of the event
    /// </summary>
    public string EventType { get; }
}
