/**
 * Tipos alineados a la Authorization API.
 * La API devuelve camelCase por defecto (ASP.NET Core).
 */

// ---------------------------------------------------------------------------
// Check access
// ---------------------------------------------------------------------------

/** Request para POST /api/authorization/check */
export interface CheckAccessRequest {
  userId: string
  permissionCode: string
  projectId?: string | null
  environmentId?: string | null
  schemaId?: string | null
  contentStatus?: string | null
}

/** Response de check (CheckAccessResult). */
export interface CheckAccessResult {
  allowed: boolean
}

/** Contexto opcional para comprobar permiso (ámbito). */
export interface PermissionContext {
  projectId?: string | null
  environmentId?: string | null
  schemaId?: string | null
  contentStatus?: string | null
}

// ---------------------------------------------------------------------------
// Roles
// ---------------------------------------------------------------------------

/** DTO de rol (GET /api/authorization/roles). */
export interface RoleDto {
  id: string
  name: string
  description: string
}

/** Request para POST /api/authorization/roles. */
export interface CreateRoleRequest {
  name: string
  description?: string
}

/** Request para POST /api/authorization/roles/{roleId}/permissions. */
export interface AssignPermissionsRequest {
  permissionIds: string[]
}

// ---------------------------------------------------------------------------
// Permissions
// ---------------------------------------------------------------------------

/** DTO de permiso (GET /api/authorization/permissions). */
export interface PermissionDto {
  id: string
  code: string
  description: string
}

// ---------------------------------------------------------------------------
// Access rules
// ---------------------------------------------------------------------------

/** DTO de regla de acceso (GET /api/authorization/users/{userId}/rules). */
export interface AccessRuleDto {
  id: string
  userId: string
  roleId: string
  projectId?: string | null
  environmentId?: string | null
  schemaId?: string | null
  contentStatus?: string | null
}

/** Request para POST /api/authorization/rules. */
export interface CreateAccessRuleRequest {
  userId: string
  roleId: string
  projectId?: string | null
  environmentId?: string | null
  schemaId?: string | null
  contentStatus?: string | null
}
