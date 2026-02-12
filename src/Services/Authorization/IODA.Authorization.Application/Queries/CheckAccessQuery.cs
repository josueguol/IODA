using MediatR;

namespace IODA.Authorization.Application.Queries;

/// <summary>
/// Consulta: ¿el usuario tiene el permiso indicado en el contexto dado?
/// </summary>
public record CheckAccessQuery(
    Guid UserId,
    string PermissionCode,
    Guid? ProjectId = null,
    Guid? EnvironmentId = null,
    Guid? SchemaId = null,
    string? ContentStatus = null
) : IRequest<CheckAccessResult>;

public record CheckAccessResult(bool Allowed);
