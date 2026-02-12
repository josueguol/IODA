import { config } from '../../../config/env'
import { createAuthAwareHttpClient } from '../../../shared/api'
import { useAuthStore } from '../../auth/store/auth-store'
import type { SearchResult } from '../types'

const indexingClient = createAuthAwareHttpClient({
  baseUrl: config.indexingApiUrl,
  getAccessToken: () => useAuthStore.getState().accessToken,
  refreshSession: () => useAuthStore.getState().refreshSession(),
  onUnauthorized: () => {
    useAuthStore.getState().logout()
    window.location.href = config.routerType === 'hash' ? '/#/login' : '/login'
  },
})

/** Cliente de la Indexing API (búsqueda de contenido publicado). Requiere JWT. */
export const indexingApi = {
  /** Buscar contenido publicado. GET /api/indexing/search */
  search: (params: {
    q?: string
    page?: number
    pageSize?: number
    contentType?: string
  }) => {
    const search = new URLSearchParams()
    if (params.q) search.set('q', params.q)
    if (params.page != null) search.set('page', String(params.page))
    if (params.pageSize != null) search.set('pageSize', String(params.pageSize))
    if (params.contentType) search.set('contentType', params.contentType)
    const q = search.toString()
    return indexingClient.get<SearchResult>(`api/indexing/search${q ? `?${q}` : ''}`)
  },

  /** Reindexar contenido publicado manualmente. POST /api/indexing/index */
  reindexContent: (body: {
    contentId: string
    versionId: string
    title: string
    contentType: string
    publishedAt: string
    fields?: Record<string, unknown>
  }) => indexingClient.post<void>('api/indexing/index', body),
}
