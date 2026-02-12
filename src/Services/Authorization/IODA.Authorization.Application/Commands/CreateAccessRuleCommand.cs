using MediatR;

namespace IODA.Authorization.Application.Commands;

public record CreateAccessRuleCommand(
    Guid UserId,
    Guid RoleId,
    Guid? ProjectId = null,
    Guid? EnvironmentId = null,
    Guid? SchemaId = null,
    string? ContentStatus = null
) : IRequest<Guid>;
