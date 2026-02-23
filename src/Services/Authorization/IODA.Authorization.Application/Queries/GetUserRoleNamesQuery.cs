using MediatR;

namespace IODA.Authorization.Application.Queries;

/// <summary>Obtiene los nombres de los roles efectivos de un usuario (desde sus reglas de acceso).</summary>
public record GetUserRoleNamesQuery(Guid UserId) : IRequest<IReadOnlyList<string>>;
