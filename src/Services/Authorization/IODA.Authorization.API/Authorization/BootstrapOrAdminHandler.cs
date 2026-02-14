using IODA.Authorization.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace IODA.Authorization.API.Authorization;

/// <summary>
/// Allows access to Admin-protected endpoints when there are no access rules (bootstrap mode)
/// or when the user has the "role.manage" permission claim.
/// </summary>
public class BootstrapOrAdminHandler : AuthorizationHandler<BootstrapOrAdminRequirement>
{
    private readonly IAccessRuleRepository _accessRuleRepository;

    public BootstrapOrAdminHandler(IAccessRuleRepository accessRuleRepository)
    {
        _accessRuleRepository = accessRuleRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BootstrapOrAdminRequirement requirement)
    {
        var count = await _accessRuleRepository.CountAsync();
        if (count == 0)
        {
            context.Succeed(requirement);
            return;
        }

        if (context.User.HasClaim(BootstrapOrAdminRequirement.PermissionClaimType, BootstrapOrAdminRequirement.RoleManagePermission))
        {
            context.Succeed(requirement);
        }
    }
}
