import { useEffect, useState } from 'react'
import { coreApi } from '../../core/api/core-api'
import { useAuthStore } from '../../auth/store/auth-store'
import type { MediaItem } from '../../core/types'

const styles: Record<string, React.CSSProperties> = {
  container: { marginTop: '0.25rem' },
  select: { width: '100%', maxWidth: 400, padding: '0.5rem', fontSize: '0.875rem', borderRadius: 4, border: '1px solid #ccc' },
  list: { display: 'flex', flexWrap: 'wrap', gap: '0.5rem', marginTop: '0.5rem' },
  thumb: {
    width: 80,
    height: 80,
    objectFit: 'cover',
    borderRadius: 4,
    border: '1px solid #eee',
    cursor: 'pointer',
  },
  thumbSelected: { border: '2px solid #0d6efd', boxShadow: '0 0 0 2px rgba(13,110,253,0.25)' },
  upload: { marginTop: '0.5rem', fontSize: '0.875rem' },
  loading: { color: '#666', fontSize: '0.875rem' },
  error: { color: '#dc3545', fontSize: '0.875rem', marginTop: '0.25rem' },
}

export interface MediaPickerProps {
  projectId: string
  value: string | null | undefined
  onChange: (mediaId: string | null) => void
  disabled?: boolean
  /** Si true, muestra botón de subir y permite elegir de la galería. */
  allowUpload?: boolean
}

export function MediaPicker({
  projectId,
  value,
  onChange,
  disabled = false,
  allowUpload = true,
}: MediaPickerProps) {
  const user = useAuthStore((s) => s.user)
  const [items, setItems] = useState<MediaItem[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [uploading, setUploading] = useState(false)

  useEffect(() => {
    if (!projectId) return
    setLoading(true)
    setError(null)
    coreApi
      .getMediaList(projectId, { pageSize: 50 })
      .then((res) => setItems(res?.items ?? []))
      .catch((e) => setError(e instanceof Error ? e.message : 'Error al cargar media'))
      .finally(() => setLoading(false))
  }, [projectId])

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file || !projectId || !user?.userId) return
    setUploading(true)
    setError(null)
    coreApi
      .uploadMedia(projectId, file, { createdBy: user.userId })
      .then((created) => {
        if (created) {
          const item = typeof created === 'object' && 'id' in created
            ? (created as MediaItem)
            : {
                id: String(created),
                publicId: String(created),
                projectId,
                fileName: file.name,
                displayName: file.name,
                contentType: file.type,
                sizeBytes: file.size,
                storageKey: '',
                version: 1,
                metadata: null,
                createdAt: new Date().toISOString(),
                createdBy: user.userId,
              } as MediaItem
          onChange(item.id)
          setItems((prev) => [...prev, item])
        }
      })
      .catch((e) => setError(e instanceof Error ? e.message : 'Error al subir'))
      .finally(() => {
        setUploading(false)
        e.target.value = ''
      })
  }

  const fileUrl = (mediaId: string) =>
    coreApi.getMediaFileUrl(projectId, mediaId)

  const selectedItem = items.find((m) => m.id === value)

  return (
    <div style={styles.container}>
      {loading && <p style={styles.loading}>Cargando galería…</p>}
      {error && <p style={styles.error}>{error}</p>}
      {!loading && items.length > 0 && (
        <div style={styles.list}>
          {items.map((m) => (
            <button
              key={m.id}
              type="button"
              disabled={disabled}
              onClick={() => onChange(value === m.id ? null : m.id)}
              style={{ padding: 0, border: 'none', background: 'none', cursor: disabled ? 'default' : 'pointer' }}
              title={m.displayName ?? m.fileName}
            >
              {m.contentType.startsWith('image/') ? (
                <img
                  src={fileUrl(m.id)}
                  alt={m.displayName ?? m.fileName}
                  style={{
                    ...styles.thumb,
                    ...(value === m.id ? styles.thumbSelected : {}),
                  }}
                />
              ) : (
                <div
                  style={{
                    ...styles.thumb,
                    ...(value === m.id ? styles.thumbSelected : {}),
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    fontSize: '0.7rem',
                    color: '#666',
                  }}
                >
                  {m.fileName}
                </div>
              )}
            </button>
          ))}
        </div>
      )}
      {allowUpload && (
        <div style={styles.upload}>
          <input
            type="file"
            accept="image/*,.pdf,.doc,.docx"
            onChange={handleFileChange}
            disabled={disabled || uploading || !projectId}
          />
          {uploading && <span style={styles.loading}> Subiendo…</span>}
        </div>
      )}
      {selectedItem && (
        <p style={{ marginTop: '0.25rem', fontSize: '0.75rem', color: '#666' }}>
          Seleccionado: {selectedItem.displayName ?? selectedItem.fileName}
        </p>
      )}
    </div>
  )
}
