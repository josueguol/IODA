import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useContextStore } from '../../modules/core/store/context-store'

/**
 * Bloquea el acceso a rutas que requieren contexto completo (Proyecto + Entorno).
 * Si falta proyecto o entorno, redirige a "/" (Home) donde se guía al usuario paso a paso.
 */
export function RequireContext({ children }: { children: ReactNode }) {
  const location = useLocation()
  const currentProjectId = useContextStore((s) => s.currentProjectId)
  const currentEnvironmentId = useContextStore((s) => s.currentEnvironmentId)

  const hasContext = Boolean(currentProjectId && currentEnvironmentId)

  if (!hasContext) {
    return <Navigate to="/" state={{ from: location }} replace />
  }

  return <>{children}</>
}
