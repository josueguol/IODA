import { useEffect, useState } from 'react'
import { coreApi } from '../../core/api/core-api'
import type { ContentListItem, ValidationRules } from '../../core/types'

const styles: Record<string, React.CSSProperties> = {
  container: { marginTop: '0.25rem', color: 'var(--page-text)' },
  select: {
    width: '100%',
    maxWidth: 400,
    padding: '0.5rem',
    fontSize: '0.875rem',
    borderRadius: 4,
    border: '1px solid var(--input-border)',
    color: 'var(--input-text)',
    background: 'var(--input-bg)',
  },
  loading: { color: 'var(--page-text-muted)', fontSize: '0.875rem', marginTop: '0.25rem' },
  error: { color: '#dc3545', fontSize: '0.875rem', marginTop: '0.25rem' },
  hint: { fontSize: '0.75rem', color: 'var(--page-text-muted)', marginTop: '0.25rem' },
}

export interface ReferenceSelectorProps {
  projectId: string
  value: string | null | undefined
  onChange: (contentId: string | null) => void
  disabled?: boolean
  /**
   * Filtro opcional por tipo de contenido (contentType / schemaType).
   * Puede venir de validationRules.referenceContentType en el schema.
   */
  contentTypeFilter?: string | null
  /** Reglas del campo para leer convenciones (ej. referenceContentType, referenceSchemaId). */
  validationRules?: ValidationRules | null
}

export function ReferenceSelector({
  projectId,
  value,
  onChange,
  disabled = false,
  contentTypeFilter,
  validationRules,
}: ReferenceSelectorProps) {
  const [items, setItems] = useState<ContentListItem[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const contentType =
    contentTypeFilter ??
    (validationRules && (validationRules['referenceContentType'] as string | undefined)) ??
    (validationRules && (validationRules['referenceSchemaId'] as string | undefined)) ??
    null

  useEffect(() => {
    if (!projectId) return
    setLoading(true)
    setError(null)
    coreApi
      .getContentList(projectId, {
        pageSize: 200,
        ...(contentType ? { contentType } : {}),
      })
      .then((res) => setItems(res?.items ?? []))
      .catch((e) => setError(e instanceof Error ? e.message : 'Error al cargar referencias'))
      .finally(() => setLoading(false))
  }, [projectId, contentType])

  const selected = items.find((c) => c.id === value)

  return (
    <div style={styles.container}>
      {loading && <p style={styles.loading}>Cargando contenido…</p>}
      {error && <p style={styles.error}>{error}</p>}
      {!loading && (
        <select
          style={styles.select}
          value={value ?? ''}
          onChange={(e) => onChange(e.target.value || null)}
          disabled={disabled}
          title="Seleccionar contenido referenciado"
        >
          <option value="">— Sin referencia —</option>
          {items.map((c) => (
            <option key={c.id} value={c.id}>
              {c.title} ({c.contentType})
            </option>
          ))}
        </select>
      )}
      {selected && !loading && (
        <p style={styles.hint}>
          Referencia: {selected.title} · {selected.contentType}
        </p>
      )}
    </div>
  )
}
