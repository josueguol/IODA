namespace IODA.Shared.Contracts.Events.V1;

using IODA.Shared.Contracts.Events;

/// <summary>
/// Event fired when new content is created
/// </summary>
public record ContentCreatedEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(ContentCreatedEventV1);

    public required Guid ContentId { get; init; }
    public required string Title { get; init; }
    public required string ContentType { get; init; }
    public required string Status { get; init; }
    public required Guid ProjectId { get; init; }
    public required Guid EnvironmentId { get; init; }
    public required Guid CreatedBy { get; init; }
    public Dictionary<string, object>? Fields { get; init; }
    public EventMetadata? Metadata { get; init; }
}
