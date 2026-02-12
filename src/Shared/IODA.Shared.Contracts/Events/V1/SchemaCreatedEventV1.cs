namespace IODA.Shared.Contracts.Events.V1;

using IODA.Shared.Contracts.Events;

/// <summary>
/// DTO for field definitions in schemas
/// </summary>
public record FieldDefinitionDto(
    string Name,
    string Type,
    bool IsRequired,
    object? DefaultValue,
    Dictionary<string, object>? ValidationRules);

/// <summary>
/// Event fired when a new content schema is created
/// </summary>
public record SchemaCreatedEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(SchemaCreatedEventV1);

    public required Guid SchemaId { get; init; }
    public required string SchemaName { get; init; }
    public required string SchemaType { get; init; }
    public required Guid ProjectId { get; init; }
    public required Guid CreatedBy { get; init; }
    public List<FieldDefinitionDto>? Fields { get; init; }
    public EventMetadata? Metadata { get; init; }
}
