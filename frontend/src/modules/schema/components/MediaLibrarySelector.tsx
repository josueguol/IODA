import { useEffect, useMemo, useState } from 'react'
import { coreApi } from '../../core/api/core-api'
import type { MediaItem } from '../../core/types'

interface MediaLibrarySelectorProps {
  projectId: string
  onSelect: (item: MediaItem) => void
  onClose: () => void
}

function inferKind(contentType: string): 'image' | 'video' | 'audio' | 'other' {
  const lower = contentType.toLowerCase()
  if (lower.startsWith('image/')) return 'image'
  if (lower.startsWith('video/')) return 'video'
  if (lower.startsWith('audio/')) return 'audio'
  return 'other'
}

const styles: Record<string, React.CSSProperties> = {
  backdrop: {
    position: 'fixed',
    inset: 0,
    background: 'rgba(0,0,0,0.35)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    zIndex: 1000,
    padding: '1rem',
  },
  panel: {
    width: 'min(880px, 100%)',
    maxHeight: '80vh',
    overflow: 'hidden',
    borderRadius: 12,
    border: '1px solid var(--page-border)',
    background: 'var(--page-bg-elevated)',
    color: 'var(--page-text)',
    display: 'grid',
    gridTemplateRows: 'auto auto 1fr auto',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: '0.75rem 1rem',
    borderBottom: '1px solid var(--page-border)',
  },
  title: { margin: 0, fontSize: '1rem', fontWeight: 700 },
  filters: {
    display: 'flex',
    gap: '0.5rem',
    padding: '0.75rem 1rem',
    borderBottom: '1px solid var(--page-border)',
    flexWrap: 'wrap',
  },
  input: {
    flex: 1,
    minWidth: 220,
    border: '1px solid var(--input-border)',
    borderRadius: 6,
    padding: '0.45rem 0.6rem',
    background: 'var(--input-bg)',
    color: 'var(--input-text)',
    fontSize: '0.875rem',
  },
  select: {
    border: '1px solid var(--input-border)',
    borderRadius: 6,
    padding: '0.45rem 0.6rem',
    background: 'var(--input-bg)',
    color: 'var(--input-text)',
    fontSize: '0.875rem',
  },
  list: {
    overflow: 'auto',
    padding: '0.75rem 1rem',
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))',
    gap: '0.6rem',
    alignContent: 'start',
  },
  card: {
    border: '1px solid var(--page-border)',
    borderRadius: 8,
    background: 'var(--page-bg)',
    display: 'grid',
    gridTemplateColumns: '72px 1fr',
    gap: '0.5rem',
    padding: '0.5rem',
    textAlign: 'left',
    cursor: 'pointer',
  },
  thumb: {
    width: 72,
    height: 72,
    borderRadius: 6,
    objectFit: 'cover',
    border: '1px solid var(--page-border)',
    background: 'var(--page-bg-elevated)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    color: 'var(--page-text-muted)',
    fontSize: '0.7rem',
  },
  name: {
    margin: 0,
    fontSize: '0.82rem',
    fontWeight: 600,
    whiteSpace: 'nowrap',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
  },
  meta: {
    margin: '0.1rem 0 0',
    fontSize: '0.72rem',
    color: 'var(--page-text-muted)',
  },
  footer: {
    display: 'flex',
    justifyContent: 'flex-end',
    padding: '0.75rem 1rem',
    borderTop: '1px solid var(--page-border)',
  },
  button: {
    border: '1px solid var(--input-border)',
    borderRadius: 6,
    padding: '0.4rem 0.7rem',
    background: 'transparent',
    color: 'var(--page-text)',
    fontSize: '0.82rem',
    cursor: 'pointer',
  },
  state: {
    padding: '1rem',
    fontSize: '0.85rem',
    color: 'var(--page-text-muted)',
  },
}

export function MediaLibrarySelector({ projectId, onSelect, onClose }: MediaLibrarySelectorProps) {
  const [items, setItems] = useState<MediaItem[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [query, setQuery] = useState('')
  const [kind, setKind] = useState<'all' | 'image' | 'video' | 'audio' | 'other'>('all')

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError(null)

    coreApi
      .getMediaList(projectId, { page: 1, pageSize: 200 })
      .then((result) => {
        if (!cancelled) setItems(result?.items ?? [])
      })
      .catch((e) => {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Error al cargar multimedia')
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [projectId])

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase()
    return items.filter((item) => {
      const itemKind = inferKind(item.contentType)
      if (kind !== 'all' && itemKind !== kind) return false
      if (!q) return true
      const haystack = `${item.displayName ?? ''} ${item.fileName} ${item.contentType}`.toLowerCase()
      return haystack.includes(q)
    })
  }, [items, kind, query])

  return (
    <div style={styles.backdrop} role="dialog" aria-modal="true" aria-label="Seleccionar media">
      <div style={styles.panel}>
        <div style={styles.header}>
          <h3 style={styles.title}>Seleccionar desde Multimedia</h3>
          <button type="button" style={styles.button} onClick={onClose}>Cerrar</button>
        </div>

        <div style={styles.filters}>
          <input
            style={styles.input}
            placeholder="Buscar por nombre o tipo..."
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
          <select style={styles.select} value={kind} onChange={(e) => setKind(e.target.value as typeof kind)}>
            <option value="all">Todos</option>
            <option value="image">Imagenes</option>
            <option value="video">Videos</option>
            <option value="audio">Audios</option>
            <option value="other">Otros</option>
          </select>
        </div>

        {loading && <div style={styles.state}>Cargando multimedia...</div>}
        {error && <div style={styles.state}>{error}</div>}

        {!loading && !error && (
          <div style={styles.list}>
            {filtered.length === 0 && <div style={styles.state}>No hay resultados.</div>}

            {filtered.map((item) => {
              const url = coreApi.getMediaFileUrl(projectId, item.id)
              const itemKind = inferKind(item.contentType)

              return (
                <button key={item.id} type="button" style={styles.card} onClick={() => onSelect(item)}>
                  {itemKind === 'image' ? (
                    <img style={styles.thumb} src={url} alt={item.displayName ?? item.fileName} />
                  ) : (
                    <div style={styles.thumb}>{itemKind.toUpperCase()}</div>
                  )}

                  <div style={{ minWidth: 0 }}>
                    <p style={styles.name} title={item.displayName ?? item.fileName}>{item.displayName ?? item.fileName}</p>
                    <p style={styles.meta}>{item.contentType}</p>
                    <p style={styles.meta}>v{item.version}</p>
                  </div>
                </button>
              )
            })}
          </div>
        )}

        <div style={styles.footer}>
          <button type="button" style={styles.button} onClick={onClose}>Cancelar</button>
        </div>
      </div>
    </div>
  )
}
