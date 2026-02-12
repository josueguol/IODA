import { useState, useEffect, useRef } from 'react'
import { useAuthStore } from '../../auth/store/auth-store'
import { authorizationApi } from '../api/authorization-api'
import type { PermissionContext } from '../types'

/** TTL del cache en ms (misma sesión). */
const CACHE_TTL_MS = 60_000

interface CacheEntry {
  allowed: boolean
  at: number
}

const cache = new Map<string, CacheEntry>()

function cacheKey(userId: string, permissionCode: string, context: PermissionContext | undefined): string {
  const parts = [
    userId,
    permissionCode,
    context?.projectId ?? '',
    context?.environmentId ?? '',
    context?.schemaId ?? '',
    context?.contentStatus ?? '',
  ]
  return parts.join('|')
}

export interface UsePermissionResult {
  allowed: boolean
  loading: boolean
  error: string | null
}

/**
 * Comprueba si el usuario actual tiene el permiso en el contexto dado.
 * Si no está autenticado, devuelve allowed: false sin llamar a la API.
 * Opcionalmente cachea el resultado por (userId, permissionCode, context) para evitar llamadas repetidas.
 */
export function usePermission(
  permissionCode: string,
  context?: PermissionContext | null
): UsePermissionResult {
  const userId = useAuthStore((s) => s.user?.userId ?? null)
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const [allowed, setAllowed] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const mounted = useRef(true)

  useEffect(() => {
    mounted.current = true

    if (!isAuthenticated || !userId) {
      setAllowed(false)
      setLoading(false)
      setError(null)
      return
    }

    const key = cacheKey(userId, permissionCode, context ?? undefined)
    const cached = cache.get(key)
    if (cached && Date.now() - cached.at < CACHE_TTL_MS) {
      setAllowed(cached.allowed)
      setLoading(false)
      setError(null)
      return
    }

    setLoading(true)
    setError(null)
    authorizationApi
      .checkAccess({
        userId,
        permissionCode,
        projectId: context?.projectId ?? undefined,
        environmentId: context?.environmentId ?? undefined,
        schemaId: context?.schemaId ?? undefined,
        contentStatus: context?.contentStatus ?? undefined,
      })
      .then((result) => {
        if (mounted.current) {
          setAllowed(result.allowed)
          setError(null)
          cache.set(key, { allowed: result.allowed, at: Date.now() })
        }
      })
      .catch((err) => {
        if (mounted.current) {
          setAllowed(false)
          setError(err?.message ?? 'Error al comprobar permiso')
        }
      })
      .finally(() => {
        if (mounted.current) setLoading(false)
      })

    return () => {
      mounted.current = false
    }
  }, [isAuthenticated, userId, permissionCode, context?.projectId, context?.environmentId, context?.schemaId, context?.contentStatus])

  return { allowed, loading, error }
}

/** Invalida el cache de permisos (útil tras cambiar roles/reglas). */
export function invalidatePermissionCache(): void {
  cache.clear()
}
