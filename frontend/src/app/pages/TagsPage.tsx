import { useEffect, useState } from 'react'
import { coreApi } from '../../modules/core/api/core-api'
import { useContextStore } from '../../modules/core/store/context-store'
import type { Tag } from '../../modules/core/types'

const styles: Record<string, React.CSSProperties> = {
  page: { maxWidth: 720, color: 'var(--page-text)' },
  title: { margin: '0 0 1rem', fontSize: '1.5rem', fontWeight: 700 },
  hint: { color: 'var(--page-text-muted)', fontSize: '0.875rem', marginBottom: '1rem' },
  toolbar: { display: 'flex', gap: '0.5rem', marginBottom: '1rem', flexWrap: 'wrap' },
  btn: {
    padding: '0.5rem 1rem',
    fontSize: '0.875rem',
    borderRadius: 6,
    border: '1px solid var(--input-border)',
    background: 'var(--input-bg)',
    color: 'var(--page-text)',
    cursor: 'pointer',
  },
  btnPrimary: { background: '#0d6efd', color: '#fff', borderColor: '#0d6efd' },
  list: { listStyle: 'none', padding: 0, margin: 0 },
  row: {
    display: 'flex',
    alignItems: 'center',
    gap: '0.75rem',
    padding: '0.5rem 0.75rem',
    borderBottom: '1px solid var(--page-border)',
  },
  name: { fontWeight: 600 },
  slug: { fontSize: '0.8125rem', color: 'var(--page-text-muted)' },
  form: {
    marginBottom: '1.5rem',
    padding: '1rem',
    border: '1px solid var(--page-border)',
    borderRadius: 8,
    background: 'var(--page-bg-elevated)',
  },
  input: {
    width: '100%',
    maxWidth: 320,
    padding: '0.5rem',
    fontSize: '0.875rem',
    borderRadius: 4,
    border: '1px solid var(--input-border)',
    marginBottom: '0.5rem',
  },
  label: { fontSize: '0.8125rem', fontWeight: 600, display: 'block', marginBottom: '0.25rem' },
  error: { color: '#dc3545', fontSize: '0.875rem', marginBottom: '0.5rem' },
}

export function TagsPage() {
  const { currentProjectId } = useContextStore()
  const [list, setList] = useState<Tag[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [formOpen, setFormOpen] = useState(false)
  const [name, setName] = useState('')
  const [slug, setSlug] = useState('')
  const [submitError, setSubmitError] = useState<string | null>(null)

  const load = async () => {
    if (!currentProjectId) return
    setLoading(true)
    setError(null)
    try {
      const data = await coreApi.getTags(currentProjectId)
      setList(data ?? [])
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al cargar')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
  }, [currentProjectId])

  const openCreate = () => {
    setName('')
    setSlug('')
    setSubmitError(null)
    setFormOpen(true)
  }

  const closeForm = () => {
    setFormOpen(false)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!currentProjectId) return
    setSubmitError(null)
    try {
      await coreApi.createTag(currentProjectId, {
        name: name.trim(),
        slug: slug.trim() || undefined,
      })
      closeForm()
      await load()
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : 'Error al crear etiqueta')
    }
  }

  if (!currentProjectId) {
    return (
      <div style={styles.page}>
        <h1 style={styles.title}>Etiquetas</h1>
        <p style={styles.hint}>Selecciona un proyecto en la barra superior.</p>
      </div>
    )
  }

  return (
    <div style={styles.page}>
      <h1 style={styles.title}>Etiquetas</h1>
      <p style={styles.hint}>
        Crea etiquetas para clasificar el contenido. Luego asígnalas al crear o editar contenido.
      </p>

      {error && <p style={styles.error}>{error}</p>}

      <div style={styles.toolbar}>
        <button type="button" style={{ ...styles.btn, ...styles.btnPrimary }} onClick={openCreate}>
          + Nueva etiqueta
        </button>
      </div>

      {formOpen && (
        <div style={styles.form}>
          <h2 style={{ margin: '0 0 0.75rem', fontSize: '1rem' }}>Nueva etiqueta</h2>
          <form onSubmit={handleSubmit}>
            <div>
              <label style={styles.label} htmlFor="tag-name">
                Nombre *
              </label>
              <input
                id="tag-name"
                style={styles.input}
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Ej. Noticias"
                required
              />
            </div>
            <div>
              <label style={styles.label} htmlFor="tag-slug">
                Slug (opcional)
              </label>
              <input
                id="tag-slug"
                style={styles.input}
                value={slug}
                onChange={(e) => setSlug(e.target.value)}
                placeholder="Se deriva del nombre si se deja vacío"
              />
            </div>
            {submitError && <p style={styles.error}>{submitError}</p>}
            <div style={{ display: 'flex', gap: '0.5rem', marginTop: '0.75rem' }}>
              <button type="submit" style={{ ...styles.btn, ...styles.btnPrimary }}>
                Crear
              </button>
              <button type="button" style={styles.btn} onClick={closeForm}>
                Cancelar
              </button>
            </div>
          </form>
        </div>
      )}

      {loading && <p style={styles.hint}>Cargando…</p>}
      {!loading && list.length === 0 && !formOpen && (
        <p style={styles.hint}>No hay etiquetas. Crea una para usarlas en el contenido.</p>
      )}
      {!loading && list.length > 0 && (
        <ul style={styles.list}>
          {list.map((tag) => (
            <li key={tag.id} style={styles.row}>
              <span style={styles.name}>{tag.name}</span>
              <span style={styles.slug}>/{tag.slug}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
