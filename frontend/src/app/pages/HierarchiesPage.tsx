import { useEffect, useState } from 'react'
import { coreApi } from '../../modules/core/api/core-api'
import { useContextStore } from '../../modules/core/store/context-store'
import type { Hierarchy } from '../../modules/core/types'

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
  btnDanger: { background: '#dc3545', color: '#fff', borderColor: '#dc3545' },
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
      <div style={styles.page}>
        <h1 style={styles.title}>Jerarquías</h1>
        <p style={styles.hint}>Selecciona un proyecto en la barra superior.</p>
      </div>
    )
  }

  return (
    <div style={styles.page}>
      <h1 style={styles.title}>Jerarquías</h1>
      <p style={styles.hint}>
        Categorías para agrupar contenido. Puedes crear niveles padre e hijos (ej. Sección → Subsección).
      </p>

      {error && <p style={styles.error}>{error}</p>}

      <div style={styles.toolbar}>
        <button type="button" style={{ ...styles.btn, ...styles.btnPrimary }} onClick={openCreate}>
          + Nueva jerarquía
        </button>
      </div>

      {formOpen && (
        <div style={styles.form}>
          <h2 style={{ margin: '0 0 0.75rem', fontSize: '1rem' }}>{editingId ? 'Editar' : 'Nueva'} jerarquía</h2>
          <form onSubmit={handleSubmit}>
            <div>
              <label style={styles.label} htmlFor="h-name">Nombre *</label>
              <input
                id="h-name"
                style={styles.input}
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Ej. Noticias"
                required
              />
            </div>
            <div>
              <label style={styles.label} htmlFor="h-slug">Slug (URL)</label>
              <input
                id="h-slug"
                style={styles.input}
                value={slug}
                onChange={(e) => setSlug(e.target.value)}
                placeholder="noticias (opcional, se deriva del nombre)"
              />
            </div>
            <div>
              <label style={styles.label} htmlFor="h-desc">Descripción</label>
              <input
                id="h-desc"
                style={styles.input}
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Opcional"
              />
            </div>
            <div>
              <label style={styles.label} htmlFor="h-image">Imagen (URL)</label>
              <input
                id="h-image"
                style={styles.input}
                value={imageUrl}
                onChange={(e) => setImageUrl(e.target.value)}
                placeholder="Opcional"
              />
            </div>
            <div>
              <label style={styles.label} htmlFor="h-parent">Padre</label>
              <select
                id="h-parent"
                style={styles.input}
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
            {submitError && <p style={styles.error}>{submitError}</p>}
            <div style={{ display: 'flex', gap: '0.5rem', marginTop: '0.75rem' }}>
              <button type="submit" style={{ ...styles.btn, ...styles.btnPrimary }}>
                {editingId ? 'Guardar' : 'Crear'}
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
        <p style={styles.hint}>No hay jerarquías. Crea una para agrupar contenido por categorías.</p>
      )}
      {!loading && (list.length > 0 || formOpen) && (
        <ul style={styles.list}>
          {tree.map(({ item, depth }) => (
            <li key={item.id} style={{ ...styles.row, paddingLeft: `${0.5 + depth * 1.5}rem` }}>
              <span style={{ flex: 1 }}>
                <span style={styles.name}>{item.name}</span>{' '}
                <span style={styles.slug}>/{item.slug}</span>
              </span>
              <button type="button" style={styles.btn} onClick={() => openEdit(item)}>
                Editar
              </button>
              <button
                type="button"
                style={{ ...styles.btn, ...styles.btnDanger }}
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
