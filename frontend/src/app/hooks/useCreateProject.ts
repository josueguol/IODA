import { useState, useCallback } from 'react'
import { coreApi } from '../../modules/core/api/core-api'
import { useAuthStore } from '../../modules/auth/store/auth-store'
import type { ContextState } from '../../modules/core/store/context-store'

export interface UseCreateProjectOptions {
  loadProjects: ContextState['loadProjects']
  setProject: ContextState['setProject']
}

export interface UseCreateProjectReturn {
  showCreate: boolean
  saving: boolean
  name: string
  description: string
  error: string | null
  setName: (value: string) => void
  setDescription: (value: string) => void
  toggleCreate: () => void
  createProject: () => Promise<void>
}

export function useCreateProject({
  loadProjects,
  setProject,
}: UseCreateProjectOptions): UseCreateProjectReturn {
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

  const createProject = useCallback(async () => {
    setError(null)
    if (!user?.userId) {
      setError('No se pudo identificar tu usuario. Cierra sesión y vuelve a entrar.')
      return
    }
    if (!name.trim()) {
      setError('Escribe un nombre para el proyecto.')
      return
    }
    setSaving(true)
    try {
      const id = await coreApi.createProject({
        name: name.trim(),
        description: description.trim() || null,
        createdBy: user.userId,
      })
      if (id) {
        setName('')
        setDescription('')
        setShowCreate(false)
        await loadProjects()
        setProject(id)
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al crear proyecto')
    } finally {
      setSaving(false)
    }
  }, [user?.userId, name, description, loadProjects, setProject])

  return {
    showCreate,
    saving,
    name,
    description,
    error,
    setName,
    setDescription,
    toggleCreate,
    createProject,
  }
}
