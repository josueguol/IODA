import { useMemo } from 'react'
import { useAuthStore } from '../../auth/store/auth-store'
import { parsePermissionsFromAccessToken } from '../utils/jwt-permissions'

/**
 * Devuelve la lista de códigos de permiso incluidos en el JWT del usuario actual.
 * Útil para ocultar/mostrar UI sin llamar a checkAccess cuando no hay contexto.
 * Si el token no incluye claims de permiso (p. ej. backend aún no los emite), devuelve [].
 */
export function useJwtPermissions(): string[] {
  const accessToken = useAuthStore((s) => s.accessToken)
  return useMemo(() => parsePermissionsFromAccessToken(accessToken ?? null), [accessToken])
}
