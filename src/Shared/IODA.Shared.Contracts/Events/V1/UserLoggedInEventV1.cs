namespace IODA.Shared.Contracts.Events.V1;

using IODA.Shared.Contracts.Events;

/// <summary>
/// Event fired when a user successfully logs in.
/// </summary>
public record UserLoggedInEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(UserLoggedInEventV1);

    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required DateTime LoggedInAt { get; init; }
    public EventMetadata? Metadata { get; init; }
}
