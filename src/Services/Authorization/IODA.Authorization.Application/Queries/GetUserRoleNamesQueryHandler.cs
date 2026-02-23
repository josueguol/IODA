using IODA.Authorization.Domain.Repositories;
using MediatR;

namespace IODA.Authorization.Application.Queries;

public class GetUserRoleNamesQueryHandler : IRequestHandler<GetUserRoleNamesQuery, IReadOnlyList<string>>
{
    private readonly IAccessRuleRepository _accessRuleRepository;
    private readonly IRoleRepository _roleRepository;

    public GetUserRoleNamesQueryHandler(
        IAccessRuleRepository accessRuleRepository,
        IRoleRepository roleRepository)
    {
        _accessRuleRepository = accessRuleRepository;
        _roleRepository = roleRepository;
    }

    public async Task<IReadOnlyList<string>> Handle(GetUserRoleNamesQuery request, CancellationToken cancellationToken)
    {
        var rules = await _accessRuleRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in rules)
        {
            var role = await _roleRepository.GetByIdAsync(rule.RoleId, cancellationToken);
            if (role != null && !string.IsNullOrEmpty(role.Name))
                names.Add(role.Name);
        }

        return names.OrderBy(n => n).ToList();
    }
}
