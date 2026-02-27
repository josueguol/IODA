import { useEffect, useState } from 'react'
import { coreApi } from '../../modules/core/api/core-api'
import { useContextStore } from '../../modules/core/store/context-store'
import type { Hierarchy } from '../../modules/core/types'
import './HierarchiesPage.css'

function buildTree(items: Hierarchy[]): { item: Hierarchy; depth: number }[] {
  const result: { item: Hierarchy; depth: number }[] = []
  const byParent = new Map<string | null, Hierarchy[]>()
  items.forEach((h) => {
    const key = h.parentHierarchyId ?? null
    if (!byParent.has(key)) byParent.set(key, [])
    byParent.get(key)!.push(h)
  })
  function add(pid: string | null, depth: number) {
    const children = byParent.get(pid) ?? []
    children.sort((a, b) => a.name.localeCompare(b.name))
    children.forEach((h) => {
      result.push({ item: h, depth })
      add(h.id, depth + 1)
    })
  }
  add(null, 0)
  return result
}

export function HierarchiesPage() {
  const { currentProjectId } = useContextStore()
  const [list, setList] = useState<Hierarchy[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [formOpen, setFormOpen] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [name, setName] = useState('')
  const [slug, setSlug] = useState('')
  const [description, setDescription] = useState('')
  const [imageUrl, setImageUrl] = useState('')
  const [parentHierarchyId, setParentHierarchyId] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)

  const load = async () => {
    if (!currentProjectId) return
    setLoading(true)
    setError(null)
    try {
      const data = await coreApi.getHierarchies(currentProjectId)
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
    setEditingId(null)
    setName('')
    setSlug('')
    setDescription('')
    setImageUrl('')
    setParentHierarchyId(null)
    setSubmitError(null)
    setFormOpen(true)
  }

  const openEdit = (h: Hierarchy) => {
    setEditingId(h.id)
    setName(h.name)
    setSlug(h.slug)
    setDescription(h.description ?? '')
    setImageUrl(h.imageUrl ?? '')
    setParentHierarchyId(h.parentHierarchyId)
    setSubmitError(null)
    setFormOpen(true)
  }

  const closeForm = () => {
    setFormOpen(false)
    setEditingId(null)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!currentProjectId) return
    setSubmitError(null)
    try {
      if (editingId) {
        await coreApi.updateHierarchy(currentProjectId, editingId, {
          name: name.trim(),
          slug: slug.trim() || undefined,
          description: description.trim() || undefined,
          imageUrl: imageUrl.trim() || undefined,
          parentHierarchyId: parentHierarchyId ?? undefined,
        })
      } else {
        await coreApi.createHierarchy(currentProjectId, {
          name: name.trim(),
          slug: slug.trim() || undefined,
          description: description.trim() || undefined,
          imageUrl: imageUrl.trim() || undefined,
          parentHierarchyId: parentHierarchyId ?? undefined,
        })
      }
      closeForm()
      await load()
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : 'Error al guardar')
    }
  }

  const handleDelete = async (id: string) => {
    if (!currentProjectId || !window.confirm('¿Eliminar esta jerarquía? Debe no tener hijos.')) return
    try {
      await coreApi.deleteHierarchy(currentProjectId, id)
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al eliminar')
    }
  }

  const tree = buildTree(list)

  if (!currentProjectId) {
    return (
      <div className="hierarchies-page">
        <h1 className="hierarchies-page__title">Jerarquías</h1>
        <p className="hierarchies-page__hint">Selecciona un proyecto en la barra superior.</p>
      </div>
    )
  }

  return (
    <div className="hierarchies-page">
      <h1 className="hierarchies-page__title">Jerarquías</h1>
      <p className="hierarchies-page__hint">
        Categorías para agrupar contenido. Puedes crear niveles padre e hijos (ej. Sección → Subsección).
      </p>

      {error && <p className="hierarchies-page__error">{error}</p>}

      <div className="hierarchies-page__toolbar">
        <button type="button" className="hierarchies-page__btn hierarchies-page__btn--primary" onClick={openCreate}>
          + Nueva jerarquía
        </button>
      </div>

      {formOpen && (
        <div className="hierarchies-page__form">
          <h2 className="hierarchies-page__form-title">{editingId ? 'Editar' : 'Nueva'} jerarquía</h2>
          <form onSubmit={handleSubmit}>
            <div>
              <label className="hierarchies-page__label" htmlFor="h-name">Nombre *</label>
              <input
                id="h-name"
                className="hierarchies-page__input"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Ej. Noticias"
                required
              />
            </div>
            <div>
              <label className="hierarchies-page__label" htmlFor="h-slug">Slug (URL)</label>
              <input
                id="h-slug"
                className="hierarchies-page__input"
                value={slug}
                onChange={(e) => setSlug(e.target.value)}
                placeholder="noticias (opcional, se deriva del nombre)"
              />
            </div>
            <div>
              <label className="hierarchies-page__label" htmlFor="h-desc">Descripción</label>
              <input
                id="h-desc"
                className="hierarchies-page__input"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Opcional"
              />
            </div>
            <div>
              <label className="hierarchies-page__label" htmlFor="h-image">Imagen (URL)</label>
              <input
                id="h-image"
                className="hierarchies-page__input"
                value={imageUrl}
                onChange={(e) => setImageUrl(e.target.value)}
                placeholder="Opcional"
              />
            </div>
            <div>
              <label className="hierarchies-page__label" htmlFor="h-parent">Padre</label>
              <select
                id="h-parent"
                className="hierarchies-page__input"
                value={parentHierarchyId ?? ''}
                onChange={(e) => setParentHierarchyId(e.target.value === '' ? null : e.target.value)}
              >
                <option value="">— Raíz (sin padre) —</option>
                {list
                  .filter((h) => !editingId || h.id !== editingId)
                  .map((h) => (
                    <option key={h.id} value={h.id}>
                      {h.name}
                    </option>
                  ))}
              </select>
            </div>
            {submitError && <p className="hierarchies-page__error">{submitError}</p>}
            <div className="hierarchies-page__form-actions">
              <button type="submit" className="hierarchies-page__btn hierarchies-page__btn--primary">
                {editingId ? 'Guardar' : 'Crear'}
              </button>
              <button type="button" className="hierarchies-page__btn" onClick={closeForm}>
                Cancelar
              </button>
            </div>
          </form>
        </div>
      )}

      {loading && <p className="hierarchies-page__hint">Cargando…</p>}
      {!loading && list.length === 0 && !formOpen && (
        <p className="hierarchies-page__hint">No hay jerarquías. Crea una para agrupar contenido por categorías.</p>
      )}
      {!loading && (list.length > 0 || formOpen) && (
        <ul className="hierarchies-page__list">
          {tree.map(({ item, depth }) => (
            <li key={item.id} className="hierarchies-page__row" style={{ paddingLeft: `${0.5 + depth * 1.5}rem` }}>
              <span className="hierarchies-page__row-content">
                <span className="hierarchies-page__name">{item.name}</span>{' '}
                <span className="hierarchies-page__slug">/{item.slug}</span>
              </span>
              <button type="button" className="hierarchies-page__btn" onClick={() => openEdit(item)}>
                Editar
              </button>
              <button
                type="button"
                className="hierarchies-page__btn hierarchies-page__btn--danger"
                onClick={() => handleDelete(item.id)}
              >
                Eliminar
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
