import { Navigate, useLocation } from 'react-router-dom'
import { useAuthStore } from '../../auth/store/auth-store'
import { usePermission } from '../hooks/usePermission'
import type { PermissionContext } from '../types'

interface ProtectedRouteByPermissionProps {
  /** Código del permiso requerido (ej. content.publish). */
  permission: string
  /** Contexto opcional para el check. */
  context?: PermissionContext | null
  /** Contenido a mostrar si tiene permiso. */
  children: React.ReactNode
  /** Ruta a la que redirigir si no tiene permiso (por defecto /). */
  redirectTo?: string
  /** Si true, redirige a /forbidden en lugar de redirectTo (página "Sin permiso"). */
  showForbidden?: boolean
}

/**
 * Protege una ruta por permiso: solo muestra children si el usuario tiene el permiso.
 * Si no está autenticado, redirige a /login. Si está autenticado pero no tiene permiso, redirige a redirectTo o /forbidden.
 */
export function ProtectedRouteByPermission({
  permission,
  context,
  children,
  redirectTo = '/',
  showForbidden = false,
}: ProtectedRouteByPermissionProps) {
  const location = useLocation()
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const isRehydrating = useAuthStore((s) => s.isRehydrating)
  const { allowed, loading } = usePermission(permission, context)

  if (isRehydrating || loading) {
    return (
      <div style={{ padding: '2rem', fontFamily: 'system-ui', textAlign: 'center' }}>
        Comprobando permisos…
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  if (!allowed) {
    const to = showForbidden ? '/forbidden' : redirectTo
    return <Navigate to={to} state={{ from: location, permission }} replace />
  }

  return <>{children}</>
}
