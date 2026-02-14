using IODA.Authorization.Domain.Entities;
using IODA.Authorization.Domain.Repositories;
using MediatR;

namespace IODA.Authorization.Application.Commands;

public class BootstrapFirstUserCommandHandler : IRequestHandler<BootstrapFirstUserCommand, BootstrapFirstUserResult>
{
    private readonly IAccessRuleRepository _accessRuleRepository;
    private readonly IRoleRepository _roleRepository;

    public BootstrapFirstUserCommandHandler(
        IAccessRuleRepository accessRuleRepository,
        IRoleRepository roleRepository)
    {
        _accessRuleRepository = accessRuleRepository;
        _roleRepository = roleRepository;
    }

    public async Task<BootstrapFirstUserResult> Handle(BootstrapFirstUserCommand request, CancellationToken cancellationToken)
    {
        var count = await _accessRuleRepository.CountAsync(cancellationToken);
        if (count > 0)
            return new BootstrapFirstUserResult(false, "Bootstrap already done. Only the first user can be assigned SuperAdmin.");

        var superAdmin = await _roleRepository.GetByNameAsync(AuthorizationConstants.SuperAdminRoleName, cancellationToken);
        if (superAdmin is null)
            return new BootstrapFirstUserResult(false, "SuperAdmin role not found. Ensure seeders have run.");

        var rule = AccessRule.Create(request.UserId, superAdmin.Id);
        await _accessRuleRepository.AddAsync(rule, cancellationToken);
        return new BootstrapFirstUserResult(true, null);
    }
}
