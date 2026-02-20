import { useEffect, useState } from 'react'
import { coreApi } from '../api/core-api'
import type { Site } from '../types'

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

export interface SiteSelectorProps {
  projectId: string
  /** Si se pasa, solo se listan sitios de este entorno (o globales). */
  environmentId?: string | null
  value: string[]
  onChange: (siteIds: string[]) => void
  disabled?: boolean
}

export function SiteSelector({ projectId, environmentId, value, onChange, disabled }: SiteSelectorProps) {
  const [sites, setSites] = useState<Site[]>([])
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (!projectId) {
      setSites([])
      return
    }
    setLoading(true)
    coreApi
      .getSites(projectId, environmentId ?? undefined)
      .then((list) => setSites(list ?? []))
      .catch(() => setSites([]))
      .finally(() => setLoading(false))
  }, [projectId, environmentId])

  const selectedSites = sites.filter((s) => value.includes(s.id))
  const availableToAdd = sites.filter((s) => !value.includes(s.id))

  const addSite = (siteId: string) => {
    if (!value.includes(siteId)) onChange([...value, siteId])
  }

  const removeSite = (siteId: string) => {
    onChange(value.filter((id) => id !== siteId))
  }

  return (
    <div style={styles.block}>
      <label style={styles.label} htmlFor="site-select">
        Sitios (asignar contenido)
      </label>
      <select
        id="site-select"
        style={styles.select}
        value=""
        onChange={(e) => {
          const id = e.target.value
          if (id) addSite(id)
        }}
        disabled={disabled || loading}
      >
        <option value="">— Añadir sitio —</option>
        {availableToAdd.map((s) => (
          <option key={s.id} value={s.id}>
            {s.name} ({s.domain})
          </option>
        ))}
      </select>
      {selectedSites.length > 0 && (
        <div style={styles.chips}>
          {selectedSites.map((s) => (
            <span key={s.id} style={styles.chip}>
              {s.name}
              <button
                type="button"
                style={styles.chipRemove}
                onClick={() => removeSite(s.id)}
                disabled={disabled}
                aria-label={`Quitar ${s.name}`}
              >
                ×
              </button>
            </span>
          ))}
        </div>
      )}
      {loading && <span style={styles.hint}>Cargando sitios…</span>}
      {!loading && sites.length === 0 && (
        <span style={styles.hint}>
          No hay sitios en este contexto. Crea sitios en <strong>Admin → Sitios</strong>.
        </span>
      )}
    </div>
  )
}
