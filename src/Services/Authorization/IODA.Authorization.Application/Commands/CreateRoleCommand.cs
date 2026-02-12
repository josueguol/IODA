using MediatR;

namespace IODA.Authorization.Application.Commands;

public record CreateRoleCommand(string Name, string Description) : IRequest<Guid>;
