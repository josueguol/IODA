import { useEffect, useState } from 'react'
import { coreApi } from '../../core/api/core-api'
import type { MediaItem } from '../../core/types'

const styles: Record<string, React.CSSProperties> = {
  container: { marginTop: '0.25rem' },
  select: { width: '100%', maxWidth: 400, padding: '0.5rem', fontSize: '0.875rem', borderRadius: 4, border: '1px solid #ccc' },
  list: { display: 'flex', flexWrap: 'wrap', gap: '0.5rem', marginTop: '0.5rem' },
  thumb: {
    width: 300,
    height: 200,
    objectFit: 'cover',
    borderRadius: 4,
    border: '1px solid #eee',
    cursor: 'pointer',
  },
  thumbSelected: { border: '2px solid #0d6efd', boxShadow: '0 0 0 2px rgba(13,110,253,0.25)' },
  upload: { marginTop: '0.5rem', fontSize: '0.875rem' },
  loading: { color: '#666', fontSize: '0.875rem' },
  error: { color: '#dc3545', fontSize: '0.875rem', marginTop: '0.25rem' },
  hint: { marginTop: '0.35rem', fontSize: '0.75rem', color: '#666' },
}

export interface MediaPickerProps {
  projectId: string
  value: string | null | undefined
  onChange: (mediaId: string | null) => void
  disabled?: boolean
  validationRules?: Record<string, unknown> | null
  /** Si true, muestra botón de subir y permite elegir de la galería. */
  allowUpload?: boolean
}

interface MediaRules {
  allowedCategories: string[]
  allowedMimeTypes: string[]
  allowedExtensions: string[]
  maxSizeBytes?: number
}

const CATEGORY_MIME_PREFIX: Record<string, string> = {
  image: 'image/',
  video: 'video/',
  audio: 'audio/',
}

function normalizeList(value: unknown): string[] {
  if (!Array.isArray(value)) return []
  return value
    .map((x) => String(x).trim().toLowerCase())
    .filter(Boolean)
}

function parseMediaRules(validationRules?: Record<string, unknown> | null): MediaRules | null {
  if (!validationRules || typeof validationRules !== 'object') return null
  const mediaRaw = validationRules['media']
  if (!mediaRaw || typeof mediaRaw !== 'object') return null

  const mediaObj = mediaRaw as Record<string, unknown>
  const allowedCategories = normalizeList(mediaObj['allowedCategories'])
  const allowedMimeTypes = normalizeList(mediaObj['allowedMimeTypes'])
  const allowedExtensions = normalizeList(mediaObj['allowedExtensions']).map((ext) => ext.replace(/^\./, ''))
  const maxSizeRaw = mediaObj['maxSizeBytes']
  const maxSizeParsed =
    typeof maxSizeRaw === 'number' && Number.isFinite(maxSizeRaw)
      ? maxSizeRaw
      : typeof maxSizeRaw === 'string'
        ? Number(maxSizeRaw)
        : undefined

  if (allowedCategories.length === 0 && allowedMimeTypes.length === 0 && allowedExtensions.length === 0 && !maxSizeParsed) {
    return null
  }

  return {
    allowedCategories,
    allowedMimeTypes,
    allowedExtensions,
    maxSizeBytes: maxSizeParsed && maxSizeParsed > 0 ? maxSizeParsed : undefined,
  }
}

function getFileExtension(fileName: string): string {
  const idx = fileName.lastIndexOf('.')
  if (idx < 0) return ''
  return fileName.slice(idx + 1).trim().toLowerCase()
}

function isMimeAllowedByCategories(contentType: string, categories: string[]): boolean {
  if (categories.length === 0) return true
  const lower = contentType.toLowerCase()
  return categories.some((cat) => {
    const prefix = CATEGORY_MIME_PREFIX[cat]
    return prefix ? lower.startsWith(prefix) : false
  })
}

function isMediaItemAllowed(item: MediaItem, rules: MediaRules | null): boolean {
  if (!rules) return true
  const contentType = (item.contentType ?? '').toLowerCase()
  const ext = getFileExtension(item.fileName)

  if (!isMimeAllowedByCategories(contentType, rules.allowedCategories)) return false
  if (rules.allowedMimeTypes.length > 0 && !rules.allowedMimeTypes.includes(contentType)) return false
  if (rules.allowedExtensions.length > 0 && !rules.allowedExtensions.includes(ext)) return false
  if (rules.maxSizeBytes && item.sizeBytes > rules.maxSizeBytes) return false
  return true
}

function isSelectedFileAllowed(file: File, rules: MediaRules | null): string | null {
  if (!rules) return null
  const contentType = (file.type ?? '').toLowerCase()
  const ext = getFileExtension(file.name)

  if (!isMimeAllowedByCategories(contentType, rules.allowedCategories)) {
    return 'Categoría de archivo no permitida por el campo.'
  }
  if (rules.allowedMimeTypes.length > 0 && !rules.allowedMimeTypes.includes(contentType)) {
    return `Tipo MIME no permitido: ${file.type || '(vacío)'}.`
  }
  if (rules.allowedExtensions.length > 0 && !rules.allowedExtensions.includes(ext)) {
    return `Extensión no permitida: .${ext || '(sin extensión)'}.`
  }
  if (rules.maxSizeBytes && file.size > rules.maxSizeBytes) {
    return `El archivo excede el máximo permitido (${rules.maxSizeBytes} bytes).`
  }
  return null
}

function buildAccept(rules: MediaRules | null): string {
  if (!rules) return 'image/*,.pdf,.doc,.docx'

  const tokens = new Set<string>()
  for (const mime of rules.allowedMimeTypes) tokens.add(mime)
  for (const ext of rules.allowedExtensions) tokens.add(`.${ext}`)
  for (const cat of rules.allowedCategories) {
    if (cat === 'image' || cat === 'video' || cat === 'audio') tokens.add(`${cat}/*`)
  }

  return tokens.size > 0 ? Array.from(tokens).join(',') : 'image/*,.pdf,.doc,.docx'
}

export function MediaPicker({
  projectId,
  value,
  onChange,
  disabled = false,
  validationRules = null,
  allowUpload = true,
}: MediaPickerProps) {
  const [items, setItems] = useState<MediaItem[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [uploading, setUploading] = useState(false)
  const [showGallery, setShowGallery] = useState(false)
  const mediaRules = parseMediaRules(validationRules)
  const accept = buildAccept(mediaRules)
  const filteredItems = items.filter((m) => isMediaItemAllowed(m, mediaRules))

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
    if (!file || !projectId) return

    const localValidationError = isSelectedFileAllowed(file, mediaRules)
    if (localValidationError) {
      setError(localValidationError)
      e.target.value = ''
      return
    }

    setUploading(true)
    setError(null)
    coreApi
      .uploadMedia(projectId, file)
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
                createdBy: '',
              } as MediaItem
          onChange(item.id)
          setItems((prev) => [...prev, item])
          setShowGallery(false)
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
  const shouldShowGallery = !disabled && (showGallery || !selectedItem)

  return (
    <div style={styles.container}>
      {loading && <p style={styles.loading}>Cargando galería…</p>}
      {error && <p style={styles.error}>{error}</p>}
      {!loading && selectedItem && (
        <div style={styles.list}>
          <button
            type="button"
            disabled={disabled}
            style={{ padding: 0, border: 'none', background: 'none', cursor: disabled ? 'default' : 'pointer' }}
            title={selectedItem.displayName ?? selectedItem.fileName}
            onClick={() => {
              if (!disabled) setShowGallery((prev) => !prev)
            }}
          >
            {selectedItem.contentType.startsWith('image/') ? (
              <img
                src={fileUrl(selectedItem.id)}
                alt={selectedItem.displayName ?? selectedItem.fileName}
                style={{
                  ...styles.thumb,
                  ...styles.thumbSelected,
                }}
              />
            ) : (
              <div
                style={{
                  ...styles.thumb,
                  ...styles.thumbSelected,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: '0.7rem',
                  color: '#666',
                }}
              >
                {selectedItem.fileName}
              </div>
            )}
          </button>
        </div>
      )}
      {!disabled && selectedItem && (
        <div style={{ marginTop: '0.35rem' }}>
          <button
            type="button"
            onClick={() => setShowGallery((prev) => !prev)}
            style={{ fontSize: '0.75rem', padding: '0.25rem 0.5rem' }}
          >
            {showGallery ? 'Ocultar librería' : 'Cambiar archivo'}
          </button>
          <button
            type="button"
            onClick={() => {
              onChange(null)
              setShowGallery(true)
            }}
            style={{ fontSize: '0.75rem', padding: '0.25rem 0.5rem', marginLeft: '0.5rem' }}
          >
            Quitar
          </button>
        </div>
      )}
      {!loading && shouldShowGallery && filteredItems.length > 0 && (
        <div style={styles.list}>
          {filteredItems.map((m) => (
            <button
              key={m.id}
              type="button"
              disabled={disabled}
              onClick={() => {
                onChange(value === m.id ? null : m.id)
                setShowGallery(false)
              }}
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
            accept={accept}
            onChange={handleFileChange}
            disabled={disabled || uploading || !projectId}
          />
          {uploading && <span style={styles.loading}> Subiendo…</span>}
        </div>
      )}
      <p style={styles.hint}>Este campo admite un solo archivo. Seleccionar/subir uno nuevo reemplaza el actual.</p>
      {selectedItem && (
        <p style={{ marginTop: '0.25rem', fontSize: '0.75rem', color: '#666' }}>
          Seleccionado: {selectedItem.displayName ?? selectedItem.fileName}
        </p>
      )}
    </div>
  )
}
