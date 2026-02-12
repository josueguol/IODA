namespace IODA.Shared.Contracts.Events.V1;

using IODA.Shared.Contracts.Events;

/// <summary>
/// Event fired when content is unpublished
/// </summary>
public record ContentUnpublishedEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(ContentUnpublishedEventV1);

    public required Guid ContentId { get; init; }
    public required Guid UnpublishedBy { get; init; }
    public required string Reason { get; init; }
    public required DateTime UnpublishedAt { get; init; }
    public EventMetadata? Metadata { get; init; }
}
