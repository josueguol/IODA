using IODA.Authorization.Domain.Entities;
using IODA.Authorization.Domain.Exceptions;
using IODA.Authorization.Domain.Repositories;
using MediatR;

namespace IODA.Authorization.Application.Commands;

public class CreateAccessRuleCommandHandler : IRequestHandler<CreateAccessRuleCommand, Guid>
{
    private readonly IAccessRuleRepository _accessRuleRepository;
    private readonly IRoleRepository _roleRepository;

    public CreateAccessRuleCommandHandler(IAccessRuleRepository accessRuleRepository, IRoleRepository roleRepository)
    {
        _accessRuleRepository = accessRuleRepository;
        _roleRepository = roleRepository;
    }

    public async Task<Guid> Handle(CreateAccessRuleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw new RoleNotFoundException(request.RoleId);
        var rule = AccessRule.Create(request.UserId, request.RoleId, request.ProjectId, request.EnvironmentId, request.SchemaId, request.ContentStatus);
        await _accessRuleRepository.AddAsync(rule, cancellationToken);
        return rule.Id;
    }
}
