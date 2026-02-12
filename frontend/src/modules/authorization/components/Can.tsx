import { usePermission } from '../hooks/usePermission'
import type { PermissionContext } from '../types'

export interface CanProps {
  /** Código del permiso (ej. content.edit, content.publish). */
  permission: string
  /** Contexto opcional (projectId, environmentId, schemaId, contentStatus). */
  context?: PermissionContext | null
  /** Contenido a mostrar si tiene permiso. */
  children: React.ReactNode
  /** Opcional: contenido mientras se está cargando (por defecto no muestra nada). */
  fallback?: React.ReactNode
}

/**
 * Renderiza children solo si el usuario tiene el permiso en el contexto dado.
 * Si no está autenticado o no tiene permiso, no renderiza nada (o fallback mientras loading).
 */
export function Can({ permission, context, children, fallback = null }: CanProps) {
  const { allowed, loading } = usePermission(permission, context)

  if (loading && fallback) return <>{fallback}</>
  if (loading) return null
  if (!allowed) return null

  return <>{children}</>
}
