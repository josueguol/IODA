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
    Guid EventId { get; }

    /// <summary>
    /// Timestamp when the event occurred (UTC)
    /// </summary>
    DateTime OccurredAt { get; }

    /// <summary>
    /// Version of the event schema
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Type name of the event
    /// </summary>
    string EventType { get; }
}
