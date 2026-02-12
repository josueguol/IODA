namespace IODA.Shared.Contracts.Events.V1;

using IODA.Shared.Contracts.Events;

/// <summary>
/// Event fired when content is deleted
/// </summary>
public record ContentDeletedEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(ContentDeletedEventV1);

    public required Guid ContentId { get; init; }
    public required string ContentType { get; init; }
    public required Guid DeletedBy { get; init; }
    public required string Reason { get; init; }
    public bool IsSoftDelete { get; init; }
    public EventMetadata? Metadata { get; init; }
}
