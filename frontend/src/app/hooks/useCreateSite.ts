import { useState, useCallback } from 'react'
import { coreApi } from '../../modules/core/api/core-api'
import { useAuthStore } from '../../modules/auth/store/auth-store'
import type { ContextState } from '../../modules/core/store/context-store'

export interface UseCreateSiteOptions {
  currentProjectId: string | null
  currentEnvironmentId: string | null
  loadSites: ContextState['loadSites']
  setSite: ContextState['setSite']
}

export interface UseCreateSiteReturn {
  showCreate: boolean
  saving: boolean
  name: string
  domain: string
  subdomain: string
  subpath: string
  themeId: string
  urlTemplate: string
  error: string | null
  setName: (value: string) => void
  setDomain: (value: string) => void
  setSubdomain: (value: string) => void
  setSubpath: (value: string) => void
  setThemeId: (value: string) => void
  setUrlTemplate: (value: string) => void
  toggleCreate: () => void
  createSite: () => Promise<void>
}

export function useCreateSite({
  currentProjectId,
  currentEnvironmentId,
  loadSites,
  setSite,
}: UseCreateSiteOptions): UseCreateSiteReturn {
  const user = useAuthStore((s) => s.user)
  const [showCreate, setShowCreate] = useState(false)
  const [saving, setSaving] = useState(false)
  const [name, setName] = useState('')
  const [domain, setDomain] = useState('')
  const [subdomain, setSubdomain] = useState('')
  const [subpath, setSubpath] = useState('')
  const [themeId, setThemeId] = useState('')
  const [urlTemplate, setUrlTemplate] = useState('')
  const [error, setError] = useState<string | null>(null)

  const toggleCreate = useCallback(() => {
    setError(null)
    setShowCreate((x) => !x)
  }, [])

  const createSite = useCallback(async () => {
    setError(null)
    if (!currentProjectId) {
      setError('Selecciona un proyecto primero.')
      return
    }
    if (!currentEnvironmentId) {
      setError('Selecciona un entorno primero.')
      return
    }
    if (!user?.userId) {
      setError('No se pudo identificar tu usuario. Cierra sesión y vuelve a entrar.')
      return
    }
    if (!name.trim()) {
      setError('Escribe un nombre para el sitio.')
      return
    }
    if (!domain.trim()) {
      setError('Escribe un dominio para el sitio.')
      return
    }
    setSaving(true)
    try {
      const id = await coreApi.createSite(currentProjectId, {
        environmentId: currentEnvironmentId,
        name: name.trim(),
        domain: domain.trim(),
        subdomain: subdomain.trim() || null,
        subpath: subpath.trim() || null,
        themeId: themeId.trim() || null,
        urlTemplate: urlTemplate.trim() || null,
        createdBy: user.userId,
      })
      if (id) {
        setName('')
        setDomain('')
        setSubdomain('')
        setSubpath('')
        setThemeId('')
        setUrlTemplate('')
        setShowCreate(false)
        await loadSites(currentProjectId, currentEnvironmentId ?? undefined)
        setSite(id)
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al crear sitio')
    } finally {
      setSaving(false)
    }
  }, [
    currentProjectId,
    currentEnvironmentId,
    user?.userId,
    name,
    domain,
    subdomain,
    subpath,
    themeId,
    urlTemplate,
    loadSites,
    setSite,
  ])

  return {
    showCreate,
    saving,
    name,
    domain,
    subdomain,
    subpath,
    themeId,
    urlTemplate,
    error,
    setName,
    setDomain,
    setSubdomain,
    setSubpath,
    setThemeId,
    setUrlTemplate,
    toggleCreate,
    createSite,
  }
}
