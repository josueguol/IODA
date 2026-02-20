import { useEffect, useState, useMemo } from 'react'
import { coreApi } from '../api/core-api'
import type { Hierarchy } from '../types'

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

function buildOptionLabel(h: Hierarchy, list: Hierarchy[]): string {
  if (!h.parentHierarchyId) return h.name
  const parent = list.find((x) => x.id === h.parentHierarchyId)
  return parent ? `${parent.name} → ${h.name}` : h.name
}

export interface HierarchySelectorProps {
  projectId: string
  value: string[]
  onChange: (hierarchyIds: string[]) => void
  disabled?: boolean
}

export function HierarchySelector({ projectId, value, onChange, disabled }: HierarchySelectorProps) {
  const [hierarchies, setHierarchies] = useState<Hierarchy[]>([])
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (!projectId) {
      setHierarchies([])
      return
    }
    setLoading(true)
    coreApi
      .getHierarchies(projectId)
      .then((list) => setHierarchies(list ?? []))
      .catch(() => setHierarchies([]))
      .finally(() => setLoading(false))
  }, [projectId])

  const selectedHierarchies = useMemo(
    () => hierarchies.filter((h) => value.includes(h.id)),
    [hierarchies, value]
  )
  const availableToAdd = hierarchies.filter((h) => !value.includes(h.id))

  const addHierarchy = (hierarchyId: string) => {
    if (!value.includes(hierarchyId)) onChange([...value, hierarchyId])
  }

  const removeHierarchy = (hierarchyId: string) => {
    onChange(value.filter((id) => id !== hierarchyId))
  }

  return (
    <div style={styles.block}>
      <label style={styles.label} htmlFor="hierarchy-select">
        Jerarquías (categorías)
      </label>
      <select
        id="hierarchy-select"
        style={styles.select}
        value=""
        onChange={(e) => {
          const id = e.target.value
          if (id) addHierarchy(id)
        }}
        disabled={disabled || loading}
      >
        <option value="">— Añadir categoría —</option>
        {availableToAdd.map((h) => (
          <option key={h.id} value={h.id}>
            {buildOptionLabel(h, hierarchies)}
          </option>
        ))}
      </select>
      {selectedHierarchies.length > 0 && (
        <div style={styles.chips}>
          {selectedHierarchies.map((h) => (
            <span key={h.id} style={styles.chip}>
              {h.name}
              <button
                type="button"
                style={styles.chipRemove}
                onClick={() => removeHierarchy(h.id)}
                disabled={disabled}
                aria-label={`Quitar ${h.name}`}
              >
                ×
              </button>
            </span>
          ))}
        </div>
      )}
      {loading && <span style={styles.hint}>Cargando jerarquías…</span>}
      {!loading && hierarchies.length === 0 && (
        <span style={styles.hint}>
          No hay jerarquías. Créalas en <strong>Jerarquías</strong> para agrupar contenido.
        </span>
      )}
    </div>
  )
}
