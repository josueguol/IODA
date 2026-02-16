import { config } from '../../../config/env'
import { createAuthAwareHttpClient } from '../../../shared/api'
import { buildLoginRedirect } from '../../../shared/auth-redirect'
import { useAuthStore } from '../../auth/store/auth-store'
import type { PublicationRequest, PublicationRequestStatus, RequestPublicationResponse } from '../types'

const publishingClient = createAuthAwareHttpClient({
  baseUrl: config.publishingApiUrl,
  getAccessToken: () => useAuthStore.getState().accessToken,
  refreshSession: () => useAuthStore.getState().refreshSession(),
  onUnauthorized: (reason) => {
    if (reason === '401') {
      useAuthStore.getState().logout()
      window.location.href = buildLoginRedirect(config.routerType, reason)
    }
    // 403: no desloguear; el error se propaga y la UI muestra mensaje de falta de permiso
  },
})

/** Cliente de la Publishing API (requiere JWT). */
export const publishingApi = {
  /** Solicitar publicación. POST /api/publishing/requests */
  requestPublication: (body: {
    contentId: string
    projectId: string
    environmentId: string
    requestedBy: string
  }) =>
    publishingClient.post<RequestPublicationResponse>('api/publishing/requests', body),

  /** Aprobar solicitud. POST /api/publishing/requests/{requestId}/approve */
  approvePublication: (requestId: string, body: { approvedBy: string }) =>
    publishingClient.post<void>(
      `api/publishing/requests/${requestId}/approve`,
      body
    ),

  /** Rechazar solicitud. POST /api/publishing/requests/{requestId}/reject */
  rejectPublication: (
    requestId: string,
    body: { rejectedBy: string; reason?: string | null }
  ) =>
    publishingClient.post<void>(
      `api/publishing/requests/${requestId}/reject`,
      body
    ),

  /** Listar solicitudes. GET /api/publishing/requests?contentId=&status= */
  getPublicationRequests: (params?: {
    contentId?: string
    status?: PublicationRequestStatus
  }) => {
    const search = new URLSearchParams()
    if (params?.contentId) search.set('contentId', params.contentId)
    if (params?.status) search.set('status', params.status)
    const q = search.toString()
    return publishingClient.get<PublicationRequest[]>(
      `api/publishing/requests${q ? `?${q}` : ''}`
    )
  },
}
