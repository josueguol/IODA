using IODA.Identity.Application.DTOs;
using MediatR;

namespace IODA.Identity.Application.Queries;

/// <summary>
/// Lista todos los usuarios (admin). Requiere JWT.
/// </summary>
public record ListUsersQuery : IRequest<IReadOnlyList<UserListItemDto>>;
