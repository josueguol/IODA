namespace IODA.Shared.Contracts.Events.V1;

using IODA.Shared.Contracts.Events;

/// <summary>
/// Event fired when a content schema is updated
/// </summary>
public record SchemaUpdatedEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(SchemaUpdatedEventV1);

    public required Guid SchemaId { get; init; }
    public required string SchemaName { get; init; }
    public required int SchemaVersion { get; init; }
    public required Guid UpdatedBy { get; init; }
    public List<FieldDefinitionDto>? AddedFields { get; init; }
    public List<string>? RemovedFields { get; init; }
    public List<FieldDefinitionDto>? ModifiedFields { get; init; }
    public EventMetadata? Metadata { get; init; }
}
