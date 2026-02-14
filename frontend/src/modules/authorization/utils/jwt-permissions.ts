/**
 * Lectura de permisos desde el access token (JWT).
 * El backend Identity emite claims de tipo "permission" con valor = código (ej. content.edit).
 * Varios permisos se serializan como array en el payload: "permission": ["code1", "code2"].
 */

/** Tipo de claim de permiso en el JWT (alineado con backend Identity). */
export const JWT_PERMISSION_CLAIM_TYPE = 'permission'

/**
 * Decodifica el payload del JWT sin verificar firma (solo para lectura en cliente).
 * La autorización real la hace el backend en cada petición.
 */
function decodeJwtPayload(token: string): Record<string, unknown> | null {
  try {
    const parts = token.split('.')
    if (parts.length !== 3) return null
    const payload = parts[1]
    if (!payload) return null
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/')
    const json = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
        .join('')
    )
    return JSON.parse(json) as Record<string, unknown>
  } catch {
    return null
  }
}

/**
 * Extrae los códigos de permiso del access token.
 * Compatible con payload que tenga "permission" como string (un permiso) o string[] (varios).
 */
export function parsePermissionsFromAccessToken(accessToken: string | null): string[] {
  if (!accessToken?.trim()) return []
  const payload = decodeJwtPayload(accessToken)
  if (!payload || typeof payload !== 'object') return []
  const raw = payload[JWT_PERMISSION_CLAIM_TYPE]
  if (raw == null) return []
  if (typeof raw === 'string') return raw ? [raw] : []
  if (Array.isArray(raw)) {
    return raw.filter((c): c is string => typeof c === 'string' && c.length > 0)
  }
  return []
}
