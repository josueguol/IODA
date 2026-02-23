import { useState, useCallback } from 'react'
import { coreApi } from '../../modules/core/api/core-api'
import { useAuthStore } from '../../modules/auth/store/auth-store'
import type { ContextState } from '../../modules/core/store/context-store'

export interface UseCreateEnvironmentOptions {
  currentProjectId: string | null
  loadEnvironments: ContextState['loadEnvironments']
  setEnvironment: ContextState['setEnvironment']
}

export interface UseCreateEnvironmentReturn {
  showCreate: boolean
  saving: boolean
  name: string
  description: string
  error: string | null
  setName: (value: string) => void
  setDescription: (value: string) => void
  toggleCreate: () => void
  createEnvironment: () => Promise<void>
}

export function useCreateEnvironment({
  currentProjectId,
  loadEnvironments,
  setEnvironment,
}: UseCreateEnvironmentOptions): UseCreateEnvironmentReturn {
  const user = useAuthStore((s) => s.user)
  const [showCreate, setShowCreate] = useState(false)
  const [saving, setSaving] = useState(false)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [error, setError] = useState<string | null>(null)

  const toggleCreate = useCallback(() => {
    setError(null)
    setShowCreate((x) => !x)
  }, [])

  const createEnvironment = useCallback(async () => {
    setError(null)
    if (!currentProjectId) {
      setError('Selecciona un proyecto primero.')
      return
    }
    if (!user?.userId) {
      setError('No se pudo identificar tu usuario. Cierra sesión y vuelve a entrar.')
      return
    }
    if (!name.trim()) {
      setError('Escribe un nombre para el entorno.')
      return
    }
    setSaving(true)
    try {
      const id = await coreApi.createEnvironment(currentProjectId, {
        name: name.trim(),
        description: description.trim() || null,
        createdBy: user.userId,
      })
      if (id) {
        setName('')
        setDescription('')
        setShowCreate(false)
        await loadEnvironments(currentProjectId)
        setEnvironment(id)
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al crear entorno')
    } finally {
      setSaving(false)
    }
  }, [currentProjectId, user?.userId, name, description, loadEnvironments, setEnvironment])

  return {
    showCreate,
    saving,
    name,
    description,
    error,
    setName,
    setDescription,
    toggleCreate,
    createEnvironment,
  }
}
