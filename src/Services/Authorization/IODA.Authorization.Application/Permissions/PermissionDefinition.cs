namespace IODA.Authorization.Application.Permissions;

/// <summary>Definición de un permiso del sistema (código + descripción). Fuente única de verdad en código.</summary>
public record PermissionDefinition(string Code, string Description);
