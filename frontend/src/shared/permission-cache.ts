/**
 * Caché de resultados de checkAccess (por userId, permissionCode, context).
 * Compartido entre usePermission y auth-store para poder invalidar tras refresh sin dependencia circular.
 */

export interface PermissionCacheEntry {
  allowed: boolean
  at: number
}

export const permissionCache = new Map<string, PermissionCacheEntry>()

/** Invalida la caché de permisos. Llamar tras refresh de token o cambio de roles/reglas. */
export function invalidatePermissionCache(): void {
  permissionCache.clear()
}
