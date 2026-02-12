import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useContextStore } from '../../modules/core/store/context-store'

/**
 * Bloquea el acceso a rutas que requieren contexto completo (Proyecto + Entorno + Sitio).
 * Si falta algo, redirige a "/" (Home) donde el wizard guía al usuario paso a paso.
 */
export function RequireFullContext({ children }: { children: ReactNode }) {
  const location = useLocation()
  const currentProjectId = useContextStore((s) => s.currentProjectId)
  const currentEnvironmentId = useContextStore((s) => s.currentEnvironmentId)
  const currentSiteId = useContextStore((s) => s.currentSiteId)

  const hasFullContext = Boolean(currentProjectId && currentEnvironmentId && currentSiteId)

  if (!hasFullContext) {
    return <Navigate to="/" state={{ from: location }} replace />
  }

  return <>{children}</>
}

