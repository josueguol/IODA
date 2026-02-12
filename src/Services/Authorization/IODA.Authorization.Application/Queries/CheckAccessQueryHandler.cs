using IODA.Authorization.Domain.Entities;
using IODA.Authorization.Domain.Repositories;
using MediatR;

namespace IODA.Authorization.Application.Queries;

public class CheckAccessQueryHandler : IRequestHandler<CheckAccessQuery, CheckAccessResult>
{
    private readonly IAccessRuleRepository _accessRuleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;

    public CheckAccessQueryHandler(
        IAccessRuleRepository accessRuleRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository)
    {
        _accessRuleRepository = accessRuleRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
    }

    public async Task<CheckAccessResult> Handle(CheckAccessQuery request, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByCodeAsync(request.PermissionCode, cancellationToken);
        if (permission == null)
            return new CheckAccessResult(false);

        var userRules = await _accessRuleRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var applicableRules = userRules
            .Where(r => r.AppliesTo(request.ProjectId, request.EnvironmentId, request.SchemaId, request.ContentStatus))
            .ToList();

        foreach (var rule in applicableRules)
        {
            var role = await _roleRepository.GetByIdWithPermissionsAsync(rule.RoleId, cancellationToken);
            if (role?.HasPermission(permission.Id) == true)
                return new CheckAccessResult(true);
        }

        return new CheckAccessResult(false);
    }
}
