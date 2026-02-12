using MediatR;

namespace IODA.Authorization.Application.Queries;

public record GetUserAccessRulesQuery(Guid UserId) : IRequest<IReadOnlyList<AccessRuleDto>>;

public record AccessRuleDto(
    Guid Id,
    Guid UserId,
    Guid RoleId,
    Guid? ProjectId,
    Guid? EnvironmentId,
    Guid? SchemaId,
    string? ContentStatus
);
