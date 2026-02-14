using Microsoft.AspNetCore.Authorization;

namespace IODA.Authorization.API.Authorization;

/// <summary>
/// Requirement that allows access when the system is in bootstrap mode (no access rules yet)
/// or when the user has the permission "role.manage".
/// </summary>
public class BootstrapOrAdminRequirement : IAuthorizationRequirement
{
    public const string PermissionClaimType = "permission";
    public const string RoleManagePermission = "role.manage";
}
