namespace IODA.Shared.BuildingBlocks.Domain;

/// <summary>
/// Marker interface for domain events
/// Domain events represent something that happened in the domain
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Unique identifier of the event
    /// </summary>
    public Guid EventId { get; }

    /// <summary>
    /// Timestamp when the event occurred (UTC)
    /// </summary>
    public DateTime OccurredAt { get; }
}
