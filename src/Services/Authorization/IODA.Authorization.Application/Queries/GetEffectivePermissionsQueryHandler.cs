using IODA.Authorization.Domain.Repositories;
using MediatR;

namespace IODA.Authorization.Application.Queries;

public class GetEffectivePermissionsQueryHandler : IRequestHandler<GetEffectivePermissionsQuery, IReadOnlyList<string>>
{
    private readonly IAccessRuleRepository _accessRuleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;

    public GetEffectivePermissionsQueryHandler(
        IAccessRuleRepository accessRuleRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository)
    {
        _accessRuleRepository = accessRuleRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
    }

    public async Task<IReadOnlyList<string>> Handle(GetEffectivePermissionsQuery request, CancellationToken cancellationToken)
    {
        var rules = await _accessRuleRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var permissionIds = new HashSet<Guid>();

        foreach (var rule in rules)
        {
            var role = await _roleRepository.GetByIdWithPermissionsAsync(rule.RoleId, cancellationToken);
            if (role is null)
                continue;
            foreach (var rp in role.RolePermissions)
                permissionIds.Add(rp.PermissionId);
        }

        if (permissionIds.Count == 0)
            return Array.Empty<string>();

        var permissions = await _permissionRepository.GetByIdsAsync(permissionIds, cancellationToken);
        return permissions.Select(p => p.Code).Distinct().OrderBy(c => c).ToList();
    }
}
