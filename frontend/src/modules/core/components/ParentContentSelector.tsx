import { useEffect, useState } from 'react'
import { coreApi } from '../api/core-api'
import type { ContentListItem } from '../types'

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
  hint: { fontSize: '0.75rem', color: 'var(--page-text-muted)', marginTop: '0.25rem' },
}

export interface ParentContentSelectorProps {
  projectId: string
  value: string | null
  onChange: (parentContentId: string | null) => void
  /** Excluir este contenido del listado (ej. al editar, no permitir ser padre de sí mismo). */
  excludeContentId?: string | null
  disabled?: boolean
}

export function ParentContentSelector({
  projectId,
  value,
  onChange,
  excludeContentId,
  disabled,
}: ParentContentSelectorProps) {
  const [options, setOptions] = useState<ContentListItem[]>([])
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (!projectId) {
      setOptions([])
      return
    }
    setLoading(true)
    coreApi
      .getContentList(projectId, { pageSize: 500 })
      .then((res) => {
        const list = res?.items ?? []
        setOptions(excludeContentId ? list.filter((c) => c.id !== excludeContentId) : list)
      })
      .catch(() => setOptions([]))
      .finally(() => setLoading(false))
  }, [projectId, excludeContentId])

  return (
    <div style={styles.block}>
      <label style={styles.label} htmlFor="parent-content-select">
        Contenido padre
      </label>
      <select
        id="parent-content-select"
        style={styles.select}
        value={value ?? ''}
        onChange={(e) => onChange(e.target.value === '' ? null : e.target.value)}
        disabled={disabled || loading}
      >
        <option value="">— Sin padre (raíz) —</option>
        {options.map((c) => (
          <option key={c.id} value={c.id}>
            {c.title} ({c.contentType})
          </option>
        ))}
      </select>
      {loading && <span style={styles.hint}>Cargando…</span>}
    </div>
  )
}
