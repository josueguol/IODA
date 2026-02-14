namespace IODA.Publishing.API.Contracts;

/// <summary>
/// Contratos HTTP (request/response) del API de Publishing.
/// </summary>
public record RequestPublicationRequest(
    Guid ContentId,
    Guid ProjectId,
    Guid EnvironmentId,
    Guid RequestedBy);

public record ApprovePublicationRequest(Guid ApprovedBy);

public record RejectPublicationRequest(Guid RejectedBy, string? Reason = null);
