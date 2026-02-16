import { create } from 'zustand'
import type { ApiError } from '../../../shared/api'
import { coreApi } from '../api/core-api'
import type { Environment, Project, Site } from '../types'

function getProjectsErrorMessage(err: unknown): string {
  if (err && typeof err === 'object' && 'status' in err) {
    const apiErr = err as ApiError
    if (apiErr.status === 403) return 'No tienes permiso para ver proyectos.'
    if (apiErr.status === 400) {
      const detail = apiErr.body && typeof apiErr.body === 'object' && 'detail' in apiErr.body ? String((apiErr.body as { detail?: string }).detail) : ''
      if (detail && /authenticationScheme|DefaultChallengeScheme/i.test(detail))
        return 'La Core API no tiene autenticación JWT configurada. Configura Jwt:SecretKey (mismo valor que Identity) y reinicia la Core API.'
      return 'Parámetros de búsqueda no válidos.'
    }
  }
  return err instanceof Error ? err.message : 'Error al cargar proyectos'
}

const STORAGE_KEY_PROJECT = 'ioda_context_project_id'
const STORAGE_KEY_ENVIRONMENT = 'ioda_context_environment_id'
const STORAGE_KEY_SITE = 'ioda_context_site_id'

function getStoredProjectId(): string | null {
  try {
    return sessionStorage.getItem(STORAGE_KEY_PROJECT)
  } catch {
    return null
  }
}

function getStoredEnvironmentId(): string | null {
  try {
    return sessionStorage.getItem(STORAGE_KEY_ENVIRONMENT)
  } catch {
    return null
  }
}

function getStoredSiteId(): string | null {
  try {
    return sessionStorage.getItem(STORAGE_KEY_SITE)
  } catch {
    return null
  }
}

function setStoredProjectId(id: string | null): void {
  try {
    if (id == null) sessionStorage.removeItem(STORAGE_KEY_PROJECT)
    else sessionStorage.setItem(STORAGE_KEY_PROJECT, id)
  } catch {
    // ignore
  }
}

function setStoredEnvironmentId(id: string | null): void {
  try {
    if (id == null) sessionStorage.removeItem(STORAGE_KEY_ENVIRONMENT)
    else sessionStorage.setItem(STORAGE_KEY_ENVIRONMENT, id)
  } catch {
    // ignore
  }
}

function setStoredSiteId(id: string | null): void {
  try {
    if (id == null) sessionStorage.removeItem(STORAGE_KEY_SITE)
    else sessionStorage.setItem(STORAGE_KEY_SITE, id)
  } catch {
    // ignore
  }
}

export interface ContextState {
  /** Proyecto seleccionado (id). */
  currentProjectId: string | null
  /** Entorno seleccionado (id). */
  currentEnvironmentId: string | null
  /** Sitio seleccionado (id). */
  currentSiteId: string | null
  /** Lista de proyectos cargados (cache en memoria). */
  projects: Project[]
  /** Lista de entornos del proyecto actual (cache en memoria). */
  environments: Environment[]
  /** Lista de sitios del proyecto actual (cache en memoria). */
  sites: Site[]
  /** Cargando proyectos. */
  projectsLoading: boolean
  /** Cargando entornos. */
  environmentsLoading: boolean
  /** Cargando sitios. */
  sitesLoading: boolean
  /** Error al cargar proyectos. */
  projectsError: string | null
  /** Error al cargar entornos. */
  environmentsError: string | null
  /** Error al cargar sitios. */
  sitesError: string | null

  setProject: (projectId: string | null) => void
  setEnvironment: (environmentId: string | null) => void
  setSite: (siteId: string | null) => void
  /** Carga proyectos desde Core y opcionalmente rehidrata proyecto/entorno/sitio guardados. */
  loadProjects: () => Promise<void>
  /** Carga entornos del proyecto actual (requiere currentProjectId). */
  loadEnvironments: (projectId: string) => Promise<void>
  /** Carga sitios del proyecto actual (requiere currentProjectId). Opcionalmente por entorno. */
  loadSites: (projectId: string, environmentId?: string) => Promise<void>
  /** Restaura proyecto/entorno/sitio desde sessionStorage (sin llamadas API). */
  rehydrate: () => void
  /** Limpia listas en memoria (útil al cambiar de usuario). */
  clearLists: () => void
}

export const useContextStore = create<ContextState>((set, get) => ({
  currentProjectId: null,
  currentEnvironmentId: null,
  currentSiteId: null,
  projects: [],
  environments: [],
  sites: [],
  projectsLoading: false,
  environmentsLoading: false,
  sitesLoading: false,
  projectsError: null,
  environmentsError: null,
  sitesError: null,

  setProject: (projectId) => {
    setStoredProjectId(projectId)
    setStoredEnvironmentId(null)
    setStoredSiteId(null)
    set({
      currentProjectId: projectId,
      currentEnvironmentId: null,
      currentSiteId: null,
      environments: [],
      sites: [],
    })
    if (projectId) {
      get().loadEnvironments(projectId).catch(() => {})
      get().loadSites(projectId).catch(() => {})
    }
  },

  setEnvironment: (environmentId) => {
    setStoredEnvironmentId(environmentId)
    // Limpiar sitio al cambiar entorno (evita estado inválido)
    setStoredSiteId(null)
    set({
      currentEnvironmentId: environmentId,
      currentSiteId: null,
      sites: [],
    })
    const projectId = get().currentProjectId
    if (projectId) {
      get().loadSites(projectId, environmentId ?? undefined).catch(() => {})
    }
  },

  setSite: (siteId) => {
    setStoredSiteId(siteId)
    set({ currentSiteId: siteId })
  },

  loadProjects: async () => {
    set({ projectsLoading: true, projectsError: null })
    try {
      const response = await coreApi.getProjects({ page: 1, pageSize: 50 })
      const projects = response?.items ?? []
      set({ projects })
      const storedProjectId = getStoredProjectId()
      const storedEnvId = getStoredEnvironmentId()
      if (storedProjectId && projects.some((p) => p.id === storedProjectId)) {
        set({ currentProjectId: storedProjectId })

        // --- Validar entorno ---
        let validEnvId: string | null = null
        if (storedEnvId) {
          const envs = await coreApi.getEnvironments(storedProjectId).catch(() => [])
          const envList = envs ?? []
          if (envList.some((e) => e.id === storedEnvId)) {
            validEnvId = storedEnvId
            set({ environments: envList, currentEnvironmentId: validEnvId })
          } else {
            // Entorno inválido → limpiar entorno Y sitio (cascada)
            set({ environments: envList, currentEnvironmentId: null, currentSiteId: null })
            setStoredEnvironmentId(null)
            setStoredSiteId(null)
          }
        } else {
          get().loadEnvironments(storedProjectId).catch(() => {})
        }

        // --- Validar sitio (solo si hay entorno válido) ---
        const storedSiteId = getStoredSiteId()
        if (validEnvId) {
          // Filtrar sitios por entorno para evitar restaurar un sitio de otro entorno
          const sitesList = await coreApi.getSites(storedProjectId, validEnvId).catch(() => [])
          const siteList = sitesList ?? []
          set({ sites: siteList })
          if (storedSiteId && siteList.some((s) => s.id === storedSiteId)) {
            set({ currentSiteId: storedSiteId })
          } else {
            set({ currentSiteId: null })
            setStoredSiteId(null)
          }
        } else {
          // Sin entorno válido → no hay sitio posible
          set({ sites: [], currentSiteId: null })
          setStoredSiteId(null)
        }
      }
    } catch (e) {
      set({
        projectsError: getProjectsErrorMessage(e),
      })
    } finally {
      set({ projectsLoading: false })
    }
  },

  loadEnvironments: async (projectId) => {
    set({ environmentsLoading: true, environmentsError: null })
    try {
      const list = await coreApi.getEnvironments(projectId)
      set({ environments: list ?? [] })
    } catch (e) {
      set({
        environmentsError:
          e instanceof Error ? e.message : 'Error al cargar entornos',
      })
    } finally {
      set({ environmentsLoading: false })
    }
  },

  loadSites: async (projectId, environmentId) => {
    set({ sitesLoading: true, sitesError: null })
    try {
      const list = await coreApi.getSites(projectId, environmentId)
      set({ sites: list ?? [] })
    } catch (e) {
      set({
        sitesError: e instanceof Error ? e.message : 'Error al cargar sitios',
      })
    } finally {
      set({ sitesLoading: false })
    }
  },

  rehydrate: () => {
    const projectId = getStoredProjectId()
    const environmentId = getStoredEnvironmentId()
    const siteId = getStoredSiteId()

    // Validar cascada: sin proyecto no hay entorno, sin entorno no hay sitio.
    // Esto evita estados intermedios inválidos al restaurar desde sessionStorage.
    const validProject = projectId ?? null
    const validEnvironment = validProject && environmentId ? environmentId : null
    const validSite = validProject && validEnvironment && siteId ? siteId : null

    // Limpiar valores huérfanos de sessionStorage
    if (!validEnvironment && environmentId) setStoredEnvironmentId(null)
    if (!validSite && siteId) setStoredSiteId(null)

    set({
      currentProjectId: validProject,
      currentEnvironmentId: validEnvironment,
      currentSiteId: validSite,
    })
  },

  clearLists: () => {
    setStoredProjectId(null)
    setStoredEnvironmentId(null)
    setStoredSiteId(null)
    set({
      currentProjectId: null,
      currentEnvironmentId: null,
      currentSiteId: null,
      projects: [],
      environments: [],
      sites: [],
      projectsError: null,
      environmentsError: null,
      sitesError: null,
    })
  },
}))
