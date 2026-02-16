import { config } from '../../../config/env'
import { createAuthAwareHttpClient } from '../../../shared/api'
import { buildLoginRedirect } from '../../../shared/auth-redirect'
import { useAuthStore } from '../../auth/store/auth-store'
import type {
  Content,
  ContentListItem,
  ContentSchema,
  ContentSchemaListItem,
  ContentVersion,
  CreateSchemaRequest,
  Environment,
  MediaItem,
  PagedResult,
  Project,
  Site,
} from '../types'

const coreClient = createAuthAwareHttpClient({
  baseUrl: config.coreApiUrl,
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

/** Cliente de la Core API (proyectos, entornos, schemas). Requiere JWT. */
export const coreApi = {
  /** Lista proyectos (paginado). GET /api/projects */
  getProjects: (params?: { page?: number; pageSize?: number }) => {
    const search = new URLSearchParams()
    if (params?.page != null) search.set('page', String(params.page))
    if (params?.pageSize != null) search.set('pageSize', String(params.pageSize))
    const q = search.toString()
    return coreClient.get<PagedResult<Project>>(`api/projects${q ? `?${q}` : ''}`)
  },

  /** Obtiene un proyecto por id. GET /api/projects/{id} */
  getProject: (projectId: string) =>
    coreClient.get<Project>(`api/projects/${projectId}`),

  /** Crea un proyecto. POST /api/projects */
  createProject: (body: { name: string; description?: string | null; createdBy: string }) =>
    coreClient.post<string>('api/projects', body),

  /** Lista entornos de un proyecto. GET /api/projects/{projectId}/environments */
  getEnvironments: (projectId: string) =>
    coreClient.get<Environment[]>(`api/projects/${projectId}/environments`),

  /** Crea un entorno en el proyecto. POST /api/projects/{projectId}/environments */
  createEnvironment: (
    projectId: string,
    body: { name: string; description?: string | null; createdBy: string }
  ) => coreClient.post<string>(`api/projects/${projectId}/environments`, body),

  /** Lista sitios del proyecto. GET /api/projects/{projectId}/sites */
  getSites: (projectId: string, environmentId?: string) => {
    const q = environmentId ? `?environmentId=${encodeURIComponent(environmentId)}` : ''
    return coreClient.get<Site[]>(`api/projects/${projectId}/sites${q}`)
  },

  /** Obtiene un sitio por id. GET /api/projects/{projectId}/sites/{siteId} */
  getSite: (projectId: string, siteId: string) =>
    coreClient.get<Site>(`api/projects/${projectId}/sites/${siteId}`),

  /** Crea un sitio. POST /api/projects/{projectId}/sites */
  createSite: (
    projectId: string,
    body: {
      environmentId?: string | null
      name: string
      domain: string
      subdomain?: string | null
      subpath?: string | null
      themeId?: string | null
      createdBy: string
    }
  ) => coreClient.post<string>(`api/projects/${projectId}/sites`, body),

  /** Actualiza un sitio. PUT /api/projects/{projectId}/sites/{siteId} */
  updateSite: (
    projectId: string,
    siteId: string,
    body: {
      name: string
      domain: string
      subdomain?: string | null
      subpath?: string | null
      themeId?: string | null
    }
  ) => coreClient.put<Site>(`api/projects/${projectId}/sites/${siteId}`, body),

  /** Activa un sitio. POST /api/projects/{projectId}/sites/{siteId}/activate */
  activateSite: (projectId: string, siteId: string) =>
    coreClient.post<void>(`api/projects/${projectId}/sites/${siteId}/activate`, undefined),

  /** Desactiva un sitio. POST /api/projects/{projectId}/sites/{siteId}/deactivate */
  deactivateSite: (projectId: string, siteId: string) =>
    coreClient.post<void>(`api/projects/${projectId}/sites/${siteId}/deactivate`, undefined),

  /** Elimina un sitio. DELETE /api/projects/{projectId}/sites/{siteId} */
  deleteSite: (projectId: string, siteId: string) =>
    coreClient.delete<void>(`api/projects/${projectId}/sites/${siteId}`),

  /** Lista schemas del proyecto. GET /api/projects/{projectId}/schemas */
  getSchemas: (projectId: string, activeOnly = true) =>
    coreClient.get<ContentSchemaListItem[]>(
      `api/projects/${projectId}/schemas?activeOnly=${activeOnly}`
    ),

  /** Obtiene un schema por id (incluye fields). GET /api/projects/{projectId}/schemas/{schemaId} */
  getSchema: (projectId: string, schemaId: string) =>
    coreClient.get<ContentSchema>(
      `api/projects/${projectId}/schemas/${schemaId}`
    ),

  /** Crea un schema de contenido. POST /api/projects/{projectId}/schemas → devuelve schemaId */
  createSchema: (projectId: string, body: CreateSchemaRequest) =>
    coreClient.post<string>(`api/projects/${projectId}/schemas`, body),

  // Content
  /** Lista contenido del proyecto (paginado). GET /api/projects/{projectId}/content */
  getContentList: (
    projectId: string,
    params?: { page?: number; pageSize?: number; contentType?: string; status?: string; siteId?: string }
  ) => {
    const search = new URLSearchParams()
    if (params?.page != null) search.set('page', String(params.page))
    if (params?.pageSize != null) search.set('pageSize', String(params.pageSize))
    if (params?.contentType) search.set('contentType', params.contentType)
    if (params?.status) search.set('status', params.status)
    if (params?.siteId) search.set('siteId', params.siteId)
    const q = search.toString()
    return coreClient.get<PagedResult<ContentListItem>>(
      `api/projects/${projectId}/content${q ? `?${q}` : ''}`
    )
  },

  /** Obtiene contenido por id. GET /api/projects/{projectId}/content/{contentId} */
  getContent: (projectId: string, contentId: string) =>
    coreClient.get<Content>(`api/projects/${projectId}/content/${contentId}`),

  /** Crea contenido. POST /api/projects/{projectId}/content */
  createContent: (
    projectId: string,
    body: {
      environmentId: string
      siteId?: string | null
      schemaId: string
      title: string
      contentType: string
      fields: Record<string, unknown>
      createdBy: string
    }
  ) => coreClient.post<string>(`api/projects/${projectId}/content`, body),

  /** Actualiza contenido. PUT /api/projects/{projectId}/content/{contentId} */
  updateContent: (
    projectId: string,
    contentId: string,
    body: { title: string; fields: Record<string, unknown>; updatedBy: string }
  ) =>
    coreClient.put<Content>(`api/projects/${projectId}/content/${contentId}`, body),

  /** Elimina contenido. DELETE /api/projects/{projectId}/content/{contentId} */
  deleteContent: (projectId: string, contentId: string) =>
    coreClient.delete<void>(`api/projects/${projectId}/content/${contentId}`),

  /** Lista todas las versiones del contenido (historial). GET /api/projects/{projectId}/content/{contentId}/versions */
  getContentVersions: (projectId: string, contentId: string) =>
    coreClient.get<ContentVersion[]>(
      `api/projects/${projectId}/content/${contentId}/versions`
    ),

  /** Obtiene una versión específica del contenido. GET /api/projects/{projectId}/content/{contentId}/versions/{versionNumber} */
  getContentVersion: (projectId: string, contentId: string, versionNumber: number) =>
    coreClient.get<ContentVersion>(
      `api/projects/${projectId}/content/${contentId}/versions/${versionNumber}`
    ),

  // Media
  /** Lista media del proyecto. GET /api/projects/{projectId}/media */
  getMediaList: (projectId: string, params?: { page?: number; pageSize?: number }) => {
    const search = new URLSearchParams()
    if (params?.page != null) search.set('page', String(params.page))
    if (params?.pageSize != null) search.set('pageSize', String(params.pageSize))
    const q = search.toString()
    return coreClient.get<PagedResult<MediaItem>>(
      `api/projects/${projectId}/media${q ? `?${q}` : ''}`
    )
  },

  /** Obtiene un media por id. GET /api/projects/{projectId}/media/{mediaId} */
  getMedia: (projectId: string, mediaId: string) =>
    coreClient.get<MediaItem>(`api/projects/${projectId}/media/${mediaId}`),

  /** URL para descargar/ver archivo. GET /api/projects/{projectId}/media/{mediaId}/file */
  getMediaFileUrl: (projectId: string, mediaId: string): string =>
    `${config.coreApiUrl}/api/projects/${projectId}/media/${mediaId}/file`,

  /** Sube un archivo. POST /api/projects/{projectId}/media (multipart). Devuelve el media creado (el backend responde con MediaItemDto). */
  uploadMedia: (
    projectId: string,
    file: File,
    options?: { displayName?: string; createdBy: string; metadata?: Record<string, unknown> }
  ) => {
    const form = new FormData()
    form.append('file', file)
    if (options?.displayName) form.append('displayName', options.displayName)
    if (options?.createdBy) form.append('createdBy', options.createdBy)
    if (options?.metadata) form.append('metadata', JSON.stringify(options.metadata))
    return coreClient.post<MediaItem>(`api/projects/${projectId}/media`, form)
  },
}
