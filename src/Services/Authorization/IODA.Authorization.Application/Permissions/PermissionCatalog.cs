namespace IODA.Authorization.Application.Permissions;

/// <summary>
/// Catálogo único de permisos del sistema. No exponer como API; uso interno (seeder, validación de asignación).
/// Cualquier permiso asignable a roles debe estar aquí.
/// </summary>
public static class PermissionCatalog
{
    /// <summary>Lista completa de permisos con código y descripción.</summary>
    public static IReadOnlyList<PermissionDefinition> All { get; } = new[]
    {
        new PermissionDefinition("content.create", "Create content"),
        new PermissionDefinition("content.edit", "Edit content"),
        new PermissionDefinition("content.delete", "Delete content"),
        new PermissionDefinition("content.publish", "Publish content"),
        new PermissionDefinition("project.create", "Create project"),
        new PermissionDefinition("project.edit", "Edit project"),
        new PermissionDefinition("project.delete", "Delete project"),
        new PermissionDefinition("environment.create", "Create environment"),
        new PermissionDefinition("environment.edit", "Edit environment"),
        new PermissionDefinition("environment.delete", "Delete environment"),
        new PermissionDefinition("site.create", "Create site"),
        new PermissionDefinition("site.edit", "Edit site"),
        new PermissionDefinition("site.delete", "Delete site"),
        new PermissionDefinition("schema.create", "Create schema"),
        new PermissionDefinition("schema.edit", "Edit schema"),
        new PermissionDefinition("schema.delete", "Delete schema"),
        new PermissionDefinition("user.list", "List users"),
        new PermissionDefinition("user.create", "Create user"),
        new PermissionDefinition("role.manage", "Manage roles and permissions"),
    };

    /// <summary>Códigos de permiso del catálogo (para validación rápida por código).</summary>
    public static IReadOnlySet<string> AllCodes { get; } =
        new HashSet<string>(All.Select(p => p.Code), StringComparer.OrdinalIgnoreCase);

    /// <summary>Indica si el código dado pertenece al catálogo.</summary>
    public static bool IsInCatalog(string code) =>
        !string.IsNullOrWhiteSpace(code) && AllCodes.Contains(code);
}
