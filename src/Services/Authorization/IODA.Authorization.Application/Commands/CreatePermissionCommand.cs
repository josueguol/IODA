using MediatR;

namespace IODA.Authorization.Application.Commands;

public record CreatePermissionCommand(string Code, string Description) : IRequest<Guid>;
