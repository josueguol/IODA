import { useEffect, useState } from 'react'
import { coreApi } from '../../modules/core/api/core-api'
import { useContextStore } from '../../modules/core/store/context-store'
import type { Tag } from '../../modules/core/types'
import './TagsPage.css'

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
      <div className="tags-page">
        <h1 className="tags-page__title">Etiquetas</h1>
        <p className="tags-page__hint">Selecciona un proyecto en la barra superior.</p>
      </div>
    )
  }

  return (
    <div className="tags-page">
      <h1 className="tags-page__title">Etiquetas</h1>
      <p className="tags-page__hint">
        Crea etiquetas para clasificar el contenido. Luego asígnalas al crear o editar contenido.
      </p>

      {error && <p className="tags-page__error">{error}</p>}

      <div className="tags-page__toolbar">
        <button type="button" className="tags-page__btn tags-page__btn--primary" onClick={openCreate}>
          + Nueva etiqueta
        </button>
      </div>

      {formOpen && (
        <div className="tags-page__form">
          <h2 className="tags-page__form-title">Nueva etiqueta</h2>
          <form onSubmit={handleSubmit}>
            <div>
              <label className="tags-page__label" htmlFor="tag-name">
                Nombre *
              </label>
              <input
                id="tag-name"
                className="tags-page__input"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Ej. Noticias"
                required
              />
            </div>
            <div>
              <label className="tags-page__label" htmlFor="tag-slug">
                Slug (opcional)
              </label>
              <input
                id="tag-slug"
                className="tags-page__input"
                value={slug}
                onChange={(e) => setSlug(e.target.value)}
                placeholder="Se deriva del nombre si se deja vacío"
              />
            </div>
            {submitError && <p className="tags-page__error">{submitError}</p>}
            <div className="tags-page__form-actions">
              <button type="submit" className="tags-page__btn tags-page__btn--primary">
                Crear
              </button>
              <button type="button" className="tags-page__btn" onClick={closeForm}>
                Cancelar
              </button>
            </div>
          </form>
        </div>
      )}

      {loading && <p className="tags-page__hint">Cargando…</p>}
      {!loading && list.length === 0 && !formOpen && (
        <p className="tags-page__hint">No hay etiquetas. Crea una para usarlas en el contenido.</p>
      )}
      {!loading && list.length > 0 && (
        <ul className="tags-page__list">
          {list.map((tag) => (
            <li key={tag.id} className="tags-page__row">
              <span className="tags-page__name">{tag.name}</span>
              <span className="tags-page__slug">/{tag.slug}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
