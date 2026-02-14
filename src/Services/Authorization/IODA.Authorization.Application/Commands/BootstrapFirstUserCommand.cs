using MediatR;

namespace IODA.Authorization.Application.Commands;

/// <summary>
/// 2.5: Asigna el rol SuperAdmin al primer usuario del sistema (solo permitido cuando aún no hay reglas de acceso).
/// Identity llama a este endpoint tras registrar al primer usuario.
/// </summary>
public record BootstrapFirstUserCommand(Guid UserId) : IRequest<BootstrapFirstUserResult>;

public record BootstrapFirstUserResult(bool Success, string? ErrorMessage);
