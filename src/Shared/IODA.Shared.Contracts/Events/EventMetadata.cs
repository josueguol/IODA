namespace IODA.Shared.Contracts.Events;

/// <summary>
/// Optional metadata for events
/// </summary>
public record EventMetadata(
    string? CorrelationId = null,
    string? CausationId = null,
    string? UserId = null,
    string? Source = null,
    Dictionary<string, string>? CustomProperties = null);
