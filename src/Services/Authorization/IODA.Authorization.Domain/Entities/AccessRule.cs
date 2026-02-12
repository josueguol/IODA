using IODA.Shared.BuildingBlocks.Domain;

namespace IODA.Authorization.Domain.Entities;

/// <summary>
/// Regla de acceso: asigna un rol a un usuario (UserId de Identity) en un ámbito opcional.
/// Ámbito: por proyecto, entorno, tipo de contenido (schema), estado de contenido.
/// Si todos los campos de ámbito son null = rol global para ese usuario.
/// </summary>
public class AccessRule : AggregateRoot<Guid>
{
    /// <summary>Usuario (Identity UserId).</summary>
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    /// <summary>Opcional: rol solo en este proyecto.</summary>
    public Guid? ProjectId { get; private set; }
    /// <summary>Opcional: rol solo en este entorno.</summary>
    public Guid? EnvironmentId { get; private set; }
    /// <summary>Opcional: rol solo para este tipo de contenido (schema).</summary>
    public Guid? SchemaId { get; private set; }
    /// <summary>Opcional: rol solo para contenido en este estado (ej. Draft, Published).</summary>
    public string? ContentStatus { get; private set; }

    private AccessRule() { }

    private AccessRule(Guid id, Guid userId, Guid roleId, Guid? projectId, Guid? environmentId, Guid? schemaId, string? contentStatus)
    {
        Id = id;
        UserId = userId;
        RoleId = roleId;
        ProjectId = projectId;
        EnvironmentId = environmentId;
        SchemaId = schemaId;
        ContentStatus = contentStatus;
    }

    public static AccessRule Create(Guid userId, Guid roleId, Guid? projectId = null, Guid? environmentId = null, Guid? schemaId = null, string? contentStatus = null)
    {
        return new AccessRule(Guid.NewGuid(), userId, roleId, projectId, environmentId, schemaId, contentStatus?.Trim());
    }

    /// <summary>
    /// Indica si esta regla aplica al contexto dado (todos los valores no null del contexto deben coincidir).
    /// </summary>
    public bool AppliesTo(Guid? projectId, Guid? environmentId, Guid? schemaId, string? contentStatus)
    {
        if (ProjectId.HasValue && ProjectId != projectId) return false;
        if (EnvironmentId.HasValue && EnvironmentId != environmentId) return false;
        if (SchemaId.HasValue && SchemaId != schemaId) return false;
        if (!string.IsNullOrEmpty(ContentStatus) && !string.Equals(ContentStatus, contentStatus, StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }
}
