using IODA.Authorization.Domain.Exceptions;
using IODA.Authorization.Domain.Repositories;
using MediatR;

namespace IODA.Authorization.Application.Commands;

public class RevokeAccessRuleCommandHandler : IRequestHandler<RevokeAccessRuleCommand>
{
    private readonly IAccessRuleRepository _accessRuleRepository;

    public RevokeAccessRuleCommandHandler(IAccessRuleRepository accessRuleRepository)
    {
        _accessRuleRepository = accessRuleRepository;
    }

    public async Task Handle(RevokeAccessRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _accessRuleRepository.GetByIdAsync(request.AccessRuleId, cancellationToken)
            ?? throw new AccessRuleNotFoundException(request.AccessRuleId);
        await _accessRuleRepository.DeleteAsync(rule, cancellationToken);
    }
}
