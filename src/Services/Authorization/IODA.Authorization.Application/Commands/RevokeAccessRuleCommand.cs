using MediatR;

namespace IODA.Authorization.Application.Commands;

public record RevokeAccessRuleCommand(Guid AccessRuleId) : IRequest;
