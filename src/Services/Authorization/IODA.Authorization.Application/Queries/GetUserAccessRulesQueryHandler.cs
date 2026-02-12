using IODA.Authorization.Domain.Repositories;
using MediatR;

namespace IODA.Authorization.Application.Queries;

public class GetUserAccessRulesQueryHandler : IRequestHandler<GetUserAccessRulesQuery, IReadOnlyList<AccessRuleDto>>
{
    private readonly IAccessRuleRepository _accessRuleRepository;

    public GetUserAccessRulesQueryHandler(IAccessRuleRepository accessRuleRepository)
    {
        _accessRuleRepository = accessRuleRepository;
    }

    public async Task<IReadOnlyList<AccessRuleDto>> Handle(GetUserAccessRulesQuery request, CancellationToken cancellationToken)
    {
        var rules = await _accessRuleRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        return rules.Select(r => new AccessRuleDto(r.Id, r.UserId, r.RoleId, r.ProjectId, r.EnvironmentId, r.SchemaId, r.ContentStatus)).ToList();
    }
}
