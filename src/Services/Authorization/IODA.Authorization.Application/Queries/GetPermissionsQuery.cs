using MediatR;

namespace IODA.Authorization.Application.Queries;

public record GetPermissionsQuery : IRequest<IReadOnlyList<PermissionDto>>;

public record PermissionDto(Guid Id, string Code, string Description);
