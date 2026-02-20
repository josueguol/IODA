import { useEffect, useState } from 'react'
import { coreApi } from '../api/core-api'
import type { Tag } from '../types'

const styles: Record<string, React.CSSProperties> = {
  block: { marginBottom: '1rem' },
  label: { fontSize: '0.8125rem', fontWeight: 600, color: 'var(--page-text)', display: 'block', marginBottom: '0.35rem' },
  select: {
    padding: '0.5rem',
    fontSize: '0.875rem',
    minWidth: 220,
    maxWidth: '100%',
    borderRadius: 4,
    border: '1px solid var(--input-border)',
    color: 'var(--input-text)',
    background: 'var(--input-bg)',
  },
  chips: { display: 'flex', flexWrap: 'wrap', gap: '0.35rem', marginTop: '0.5rem' },
  chip: {
    display: 'inline-flex',
    alignItems: 'center',
    gap: '0.35rem',
    padding: '0.25rem 0.5rem',
    fontSize: '0.8125rem',
    borderRadius: 6,
    background: 'var(--page-bg-elevated, #f0f0f0)',
    border: '1px solid var(--page-border, #ddd)',
    color: 'var(--page-text)',
  },
  chipRemove: {
    cursor: 'pointer',
    padding: 0,
    border: 'none',
    background: 'transparent',
    color: 'var(--page-text-muted)',
    fontSize: '1rem',
    lineHeight: 1,
  },
  hint: { fontSize: '0.75rem', color: 'var(--page-text-muted)', marginTop: '0.25rem' },
}

export interface TagsSelectorProps {
  projectId: string
  value: string[]
  onChange: (tagIds: string[]) => void
  disabled?: boolean
}

export function TagsSelector({ projectId, value, onChange, disabled }: TagsSelectorProps) {
  const [tags, setTags] = useState<Tag[]>([])
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (!projectId) {
      setTags([])
      return
    }
    setLoading(true)
    coreApi
      .getTags(projectId)
      .then((list) => setTags(list ?? []))
      .catch(() => setTags([]))
      .finally(() => setLoading(false))
  }, [projectId])

  const selectedTags = tags.filter((t) => value.includes(t.id))
  const availableToAdd = tags.filter((t) => !value.includes(t.id))

  const addTag = (tagId: string) => {
    if (!value.includes(tagId)) onChange([...value, tagId])
  }

  const removeTag = (tagId: string) => {
    onChange(value.filter((id) => id !== tagId))
  }

  return (
    <div style={styles.block}>
      <label style={styles.label} htmlFor="tags-select">
        Etiquetas
      </label>
      <select
        id="tags-select"
        style={styles.select}
        value=""
        onChange={(e) => {
          const id = e.target.value
          if (id) addTag(id)
        }}
        disabled={disabled || loading}
      >
        <option value="">— Añadir etiqueta —</option>
        {availableToAdd.map((t) => (
          <option key={t.id} value={t.id}>
            {t.name}
          </option>
        ))}
      </select>
      {selectedTags.length > 0 && (
        <div style={styles.chips}>
          {selectedTags.map((t) => (
            <span key={t.id} style={styles.chip}>
              {t.name}
              <button
                type="button"
                style={styles.chipRemove}
                onClick={() => removeTag(t.id)}
                disabled={disabled}
                aria-label={`Quitar ${t.name}`}
              >
                ×
              </button>
            </span>
          ))}
        </div>
      )}
      {loading && <span style={styles.hint}>Cargando etiquetas…</span>}
      {!loading && tags.length === 0 && (
        <span style={styles.hint}>
          No hay etiquetas. Créalas en <strong>Admin → Etiquetas</strong> para asignarlas al contenido.
        </span>
      )}
    </div>
  )
}
