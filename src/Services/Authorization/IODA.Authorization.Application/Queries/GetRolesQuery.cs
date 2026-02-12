using MediatR;

namespace IODA.Authorization.Application.Queries;

public record GetRolesQuery : IRequest<IReadOnlyList<RoleDto>>;

public record RoleDto(Guid Id, string Name, string Description);
