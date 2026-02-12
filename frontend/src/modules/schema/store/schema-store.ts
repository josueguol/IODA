import { create } from 'zustand'
import { coreApi } from '../../core/api/core-api'
import type { ContentSchema, ContentSchemaListItem } from '../../core/types'

/** Cache por projectId: lista de ítems. */
const listCache = new Map<string, ContentSchemaListItem[]>()
/** Cache por schemaId (global): schema completo con fields. */
const schemaCache = new Map<string, ContentSchema>()

export interface SchemaState {
  /** Lista de schemas del proyecto actual (por projectId en loadSchemas). */
  schemaList: ContentSchemaListItem[]
  /** Schema completo actual (el que se pidió por getSchema). */
  currentSchema: ContentSchema | null
  listLoading: boolean
  schemaLoading: boolean
  listError: string | null
  schemaError: string | null

  /** Carga la lista de schemas del proyecto y la guarda en cache. */
  loadSchemas: (projectId: string, activeOnly?: boolean) => Promise<void>
  /** Carga un schema por id; si ya está en cache lo devuelve sin llamar API. */
  loadSchema: (projectId: string, schemaId: string) => Promise<ContentSchema | null>
  /** Obtiene el schema de forma síncrona si ya está cargado (en currentSchema o en cache). */
  getSchemaSync: (projectId: string, schemaId: string) => ContentSchema | null
  /** Obtiene la lista de schemas de forma síncrona si ya está cargada para ese proyecto. */
  getSchemaListSync: (projectId: string) => ContentSchemaListItem[]
  clearError: () => void
  clearCache: () => void
}

export const useSchemaStore = create<SchemaState>((set, get) => ({
  schemaList: [],
  currentSchema: null,
  listLoading: false,
  schemaLoading: false,
  listError: null,
  schemaError: null,

  loadSchemas: async (projectId, activeOnly = true) => {
    set({ listLoading: true, listError: null })
    try {
      const list = await coreApi.getSchemas(projectId, activeOnly)
      const items = list ?? []
      listCache.set(projectId, items)
      set({ schemaList: items })
    } catch (e) {
      const message = e instanceof Error ? e.message : 'Error al cargar schemas'
      set({ listError: message })
    } finally {
      set({ listLoading: false })
    }
  },

  loadSchema: async (projectId, schemaId) => {
    const cached = schemaCache.get(schemaId)
    if (cached) {
      set({ currentSchema: cached, schemaError: null })
      return cached
    }
    set({ schemaLoading: true, schemaError: null })
    try {
      const schema = await coreApi.getSchema(projectId, schemaId)
      if (schema) {
        schemaCache.set(schemaId, schema)
        set({ currentSchema: schema })
        return schema
      }
      set({ currentSchema: null })
      return null
    } catch (e) {
      const message = e instanceof Error ? e.message : 'Error al cargar schema'
      set({ schemaError: message, currentSchema: null })
      return null
    } finally {
      set({ schemaLoading: false })
    }
  },

  getSchemaSync: (_projectId, schemaId) => {
    const cached = schemaCache.get(schemaId)
    if (cached) return cached
    const { currentSchema } = get()
    if (currentSchema?.id === schemaId) return currentSchema
    return null
  },

  getSchemaListSync: (projectId) => {
    return listCache.get(projectId) ?? get().schemaList
  },

  clearError: () => set({ listError: null, schemaError: null }),

  clearCache: () => {
    listCache.clear()
    schemaCache.clear()
    set({
      schemaList: [],
      currentSchema: null,
      listError: null,
      schemaError: null,
    })
  },
}))
