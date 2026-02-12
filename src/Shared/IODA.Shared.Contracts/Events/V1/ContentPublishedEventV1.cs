namespace IODA.Shared.Contracts.Events.V1;

using IODA.Shared.Contracts.Events;

/// <summary>
/// Event fired when content is published
/// </summary>
public record ContentPublishedEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(ContentPublishedEventV1);

    public required Guid ContentId { get; init; }
    public required Guid VersionId { get; init; }
    public required string Title { get; init; }
    public required string ContentType { get; init; }
    public required Guid PublishedBy { get; init; }
    public required DateTime PublishedAt { get; init; }
    public Guid? ScheduledPublishId { get; init; }
    public EventMetadata? Metadata { get; init; }
}
