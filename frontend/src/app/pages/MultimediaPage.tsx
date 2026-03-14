import { useEffect, useMemo, useState } from 'react'
import { coreApi } from '../../modules/core/api/core-api'
import { useContextStore } from '../../modules/core/store/context-store'
import type { MediaItem } from '../../modules/core/types'
import { ErrorBanner, LoadingSpinner } from '../../shared/components'
import './MultimediaPage.css'

type MediaKind = 'all' | 'image' | 'video' | 'audio' | 'other'

function inferKind(contentType: string): Exclude<MediaKind, 'all'> {
  const lower = contentType.toLowerCase()
  if (lower.startsWith('image/')) return 'image'
  if (lower.startsWith('video/')) return 'video'
  if (lower.startsWith('audio/')) return 'audio'
  return 'other'
}

function getMetadataText(item: MediaItem, key: string): string {
  const value = item.metadata?.[key]
  return typeof value === 'string' ? value : ''
}

function getProcessingStatus(item: MediaItem): string {
  const value = item.metadata?.processingStatus
  if (typeof value === 'string' && value.trim().length > 0) return value
  return 'ready'
}

function getVariants(item: MediaItem): Array<{ name: string; contentType: string }> {
  const raw = item.metadata?.variants
  if (!Array.isArray(raw)) return []

  return raw
    .map((entry) => {
      if (!entry || typeof entry !== 'object') return null
      const obj = entry as Record<string, unknown>
      const name = typeof obj.name === 'string' ? obj.name : ''
      const contentType = typeof obj.contentType === 'string' ? obj.contentType : ''
      if (!name) return null
      return { name, contentType }
    })
    .filter((x): x is { name: string; contentType: string } => x !== null)
}

export function MultimediaPage() {
  const { currentProjectId } = useContextStore()

  const [items, setItems] = useState<MediaItem[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const [query, setQuery] = useState('')
  const [kind, setKind] = useState<MediaKind>('all')

  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [uploading, setUploading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [replacing, setReplacing] = useState(false)

  const [displayName, setDisplayName] = useState('')
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [message, setMessage] = useState<string | null>(null)

  useEffect(() => {
    if (!currentProjectId) return
    let cancelled = false

    const load = async () => {
      setLoading(true)
      setError(null)
      try {
        const result = await coreApi.getMediaList(currentProjectId, { page: 1, pageSize: 200 })
        if (!cancelled) {
          setItems(result?.items ?? [])
          if (!selectedId && result?.items?.length) {
            setSelectedId(result.items[0].id)
          }
        }
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Error al cargar multimedia')
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    load()
    return () => {
      cancelled = true
    }
  }, [currentProjectId])

  const filteredItems = useMemo(() => {
    const q = query.trim().toLowerCase()
    return items.filter((item) => {
      const itemKind = inferKind(item.contentType)
      if (kind !== 'all' && itemKind !== kind) return false
      if (!q) return true

      const haystack = [
        item.displayName ?? '',
        item.fileName,
        item.contentType,
        getMetadataText(item, 'title'),
        getMetadataText(item, 'description'),
      ]
        .join(' ')
        .toLowerCase()

      return haystack.includes(q)
    })
  }, [items, kind, query])

  const selected = useMemo(() => items.find((m) => m.id === selectedId) ?? null, [items, selectedId])
  const selectedVariants = useMemo(() => (selected ? getVariants(selected) : []), [selected])

  useEffect(() => {
    if (!selected) {
      setDisplayName('')
      setTitle('')
      setDescription('')
      return
    }
    setDisplayName(selected.displayName ?? '')
    setTitle(getMetadataText(selected, 'title'))
    setDescription(getMetadataText(selected, 'description'))
  }, [selected])

  const refreshItem = (updated: MediaItem) => {
    setItems((prev) => {
      const idx = prev.findIndex((m) => m.id === updated.id)
      if (idx < 0) return [updated, ...prev]
      const next = [...prev]
      next[idx] = updated
      return next
    })
  }

  const handleUpload = async (file: File) => {
    if (!currentProjectId) return
    setUploading(true)
    setError(null)
    setMessage(null)
    try {
      const created = await coreApi.uploadMedia(currentProjectId, file, {
        displayName: file.name,
        metadata: { title: file.name },
      })
      refreshItem(created)
      setSelectedId(created.id)
      setMessage('Archivo cargado correctamente.')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al subir archivo')
    } finally {
      setUploading(false)
    }
  }

  const handleSaveMetadata = async () => {
    if (!currentProjectId || !selected) return
    setSaving(true)
    setError(null)
    setMessage(null)
    try {
      const updated = await coreApi.updateMediaMetadata(currentProjectId, selected.id, {
        displayName: displayName.trim() || selected.fileName,
        metadata: {
          ...(selected.metadata ?? {}),
          title: title.trim(),
          description: description.trim(),
          processingStatus: getProcessingStatus(selected),
        },
      })
      refreshItem(updated)
      setMessage('Metadatos actualizados.')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al actualizar metadatos')
    } finally {
      setSaving(false)
    }
  }

  const handleReplaceFile = async (file: File) => {
    if (!currentProjectId || !selected) return
    setReplacing(true)
    setError(null)
    setMessage(null)
    try {
      const updated = await coreApi.replaceMedia(currentProjectId, selected.id, file, {
        displayName: displayName.trim() || selected.fileName,
        metadata: {
          ...(selected.metadata ?? {}),
          title: title.trim(),
          description: description.trim(),
        },
      })
      refreshItem(updated)
      setSelectedId(updated.id)
      setMessage('Archivo reemplazado correctamente.')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al reemplazar archivo')
    } finally {
      setReplacing(false)
    }
  }

  const renderPreview = () => {
    if (!selected || !currentProjectId) {
      return <p className="multimedia-page__empty-msg">Selecciona un archivo para ver su detalle.</p>
    }

    const url = coreApi.getMediaFileUrl(currentProjectId, selected.id)
    const ctype = selected.contentType.toLowerCase()

    if (ctype.startsWith('image/')) {
      return <img className="multimedia-page__preview-image" src={url} alt={selected.displayName ?? selected.fileName} />
    }
    if (ctype.startsWith('video/')) {
      return <video className="multimedia-page__preview-video" src={url} controls preload="metadata" />
    }
    if (ctype.startsWith('audio/')) {
      return <audio className="multimedia-page__preview-audio" src={url} controls preload="metadata" />
    }

    return (
      <a className="multimedia-page__file-link" href={url} target="_blank" rel="noreferrer">
        Abrir archivo
      </a>
    )
  }

  return (
    <div className="multimedia-page">
      <header className="multimedia-page__header">
        <h1 className="multimedia-page__title">Multimedia</h1>
        <label className="multimedia-page__upload-btn">
          {uploading ? 'Subiendo...' : 'Subir archivo'}
          <input
            type="file"
            className="multimedia-page__hidden-input"
            disabled={!currentProjectId || uploading}
            onChange={(e) => {
              const file = e.target.files?.[0]
              if (file) void handleUpload(file)
              e.currentTarget.value = ''
            }}
          />
        </label>
      </header>

      {error && <ErrorBanner message={error} />}
      {message && <p className="multimedia-page__success">{message}</p>}

      <div className="multimedia-page__filters">
        <input
          className="multimedia-page__search"
          placeholder="Buscar por nombre, titulo o tipo..."
          value={query}
          onChange={(e) => setQuery(e.target.value)}
        />
        <select className="multimedia-page__select" value={kind} onChange={(e) => setKind(e.target.value as MediaKind)}>
          <option value="all">Todos</option>
          <option value="image">Imagenes</option>
          <option value="video">Videos</option>
          <option value="audio">Audios</option>
          <option value="other">Otros</option>
        </select>
      </div>

      {loading && <LoadingSpinner text="Cargando multimedia..." />}

      {!loading && (
        <div className="multimedia-page__layout">
          <section className="multimedia-page__list" aria-label="Listado de multimedia">
            {filteredItems.length === 0 && <p className="multimedia-page__empty-msg">No hay archivos para mostrar.</p>}

            {filteredItems.map((item) => {
              const isSelected = item.id === selectedId
              const url = currentProjectId ? coreApi.getMediaFileUrl(currentProjectId, item.id) : '#'
              const itemKind = inferKind(item.contentType)

              return (
                <button
                  key={item.id}
                  type="button"
                  className={`multimedia-page__card ${isSelected ? 'multimedia-page__card--selected' : ''}`}
                  onClick={() => setSelectedId(item.id)}
                >
                  <div className="multimedia-page__thumb-wrap">
                    {itemKind === 'image' ? (
                      <img className="multimedia-page__thumb" src={url} alt={item.displayName ?? item.fileName} />
                    ) : (
                      <div className="multimedia-page__thumb multimedia-page__thumb--fallback">{itemKind.toUpperCase()}</div>
                    )}
                  </div>
                  <div className="multimedia-page__card-body">
                    <p className="multimedia-page__name" title={item.displayName ?? item.fileName}>
                      {item.displayName ?? item.fileName}
                    </p>
                    <p className="multimedia-page__meta">{item.contentType}</p>
                    <p className="multimedia-page__meta">v{item.version} - {Math.ceil(item.sizeBytes / 1024)} KB</p>
                    <p className="multimedia-page__status">Estado: {getProcessingStatus(item)}</p>
                  </div>
                </button>
              )
            })}
          </section>

          <section className="multimedia-page__detail" aria-label="Detalle multimedia">
            <div className="multimedia-page__preview">{renderPreview()}</div>

            {selected && (
              <>
                <div className="multimedia-page__form">
                  <label className="multimedia-page__label">
                    Nombre visible
                    <input
                      className="multimedia-page__input"
                      value={displayName}
                      onChange={(e) => setDisplayName(e.target.value)}
                    />
                  </label>

                  <label className="multimedia-page__label">
                    Titulo
                    <input className="multimedia-page__input" value={title} onChange={(e) => setTitle(e.target.value)} />
                  </label>

                  <label className="multimedia-page__label">
                    Descripcion
                    <textarea
                      className="multimedia-page__textarea"
                      value={description}
                      onChange={(e) => setDescription(e.target.value)}
                      rows={4}
                    />
                  </label>

                  <button
                    type="button"
                    className="multimedia-page__primary-btn"
                    disabled={saving}
                    onClick={() => void handleSaveMetadata()}
                  >
                    {saving ? 'Guardando...' : 'Guardar metadatos'}
                  </button>
                </div>

                <div className="multimedia-page__replace-block">
                  <p className="multimedia-page__replace-title">Reemplazar archivo (conserva el registro y sube version)</p>
                  <label className="multimedia-page__secondary-btn">
                    {replacing ? 'Reemplazando...' : 'Seleccionar nuevo archivo'}
                    <input
                      type="file"
                      className="multimedia-page__hidden-input"
                      disabled={replacing}
                      onChange={(e) => {
                        const file = e.target.files?.[0]
                        if (file) void handleReplaceFile(file)
                        e.currentTarget.value = ''
                      }}
                    />
                  </label>
                </div>

                <div className="multimedia-page__replace-block">
                  <p className="multimedia-page__replace-title">Variantes generadas</p>
                  {selectedVariants.length === 0 ? (
                    <p className="multimedia-page__empty-msg">Sin variantes registradas.</p>
                  ) : (
                    <ul className="multimedia-page__variants">
                      {selectedVariants.map((variant) => (
                        <li key={variant.name} className="multimedia-page__variant-item">
                          <span>{variant.name}</span>
                          <span>{variant.contentType || '-'}</span>
                        </li>
                      ))}
                    </ul>
                  )}
                </div>
              </>
            )}
          </section>
        </div>
      )}
    </div>
  )
}
