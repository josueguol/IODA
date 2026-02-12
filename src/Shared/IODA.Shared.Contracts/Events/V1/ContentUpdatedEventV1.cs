namespace IODA.Shared.Contracts.Events.V1;

using IODA.Shared.Contracts.Events;

/// <summary>
/// Event fired when content is updated
/// </summary>
public record ContentUpdatedEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(ContentUpdatedEventV1);

    public required Guid ContentId { get; init; }
    public required Guid VersionId { get; init; }
    public required int VersionNumber { get; init; }
    public required string Title { get; init; }
    public required string Status { get; init; }
    public required Guid UpdatedBy { get; init; }
    public Dictionary<string, object>? Fields { get; init; }
    public Dictionary<string, object>? ChangedFields { get; init; }
    public EventMetadata? Metadata { get; init; }
}
