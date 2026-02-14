namespace IODA.Authorization.API.Contracts;

/// <summary>
/// Contratos HTTP (request/response) del API de Authorization.
/// </summary>
public record CheckAccessRequest(
    Guid UserId,
    string PermissionCode,
    Guid? ProjectId = null,
    Guid? EnvironmentId = null,
    Guid? SchemaId = null,
    string? ContentStatus = null);

public record CreateRoleRequest(string Name, string Description = "");

public record AssignPermissionsRequest(IReadOnlyList<Guid> PermissionIds);

public record CreatePermissionRequest(string Code, string Description = "");

public record CreateAccessRuleRequest(
    Guid UserId,
    Guid RoleId,
    Guid? ProjectId = null,
    Guid? EnvironmentId = null,
    Guid? SchemaId = null,
    string? ContentStatus = null);
