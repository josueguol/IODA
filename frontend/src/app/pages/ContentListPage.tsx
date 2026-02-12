import { useEffect, useState, useMemo } from 'react'
import { Link } from 'react-router-dom'
import { coreApi } from '../../modules/core/api/core-api'
import { useContextStore } from '../../modules/core/store/context-store'
import { useAuthStore } from '../../modules/auth/store/auth-store'
import { useSchemaStore } from '../../modules/schema/store/schema-store'
import { LoadingSpinner, ErrorBanner } from '../../shared/components'
import { DynamicForm, type DynamicFormProps } from '../../modules/schema/components/DynamicForm'
import type { ContentListItem, ContentSchemaListItem } from '../../modules/core/types'

/* ─── Estilos ─── */
const s: Record<string, React.CSSProperties> = {
  page: { maxWidth: 960, color: 'var(--page-text)' },
  /* Header */
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem', gap: '0.75rem', flexWrap: 'wrap' },
  title: { margin: 0, fontSize: '1.5rem', fontWeight: 700, color: 'var(--page-text)' },
  /* Schema cards */
  schemaSection: { marginBottom: '1.5rem' },
  schemaSectionTitle: { margin: '0 0 0.5rem', fontSize: '0.8125rem', fontWeight: 600, color: 'var(--page-text-muted)', textTransform: 'uppercase' as const, letterSpacing: '0.04em' },
  schemaGrid: { display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(140px, 1fr))', gap: '0.75rem' },
  schemaCard: {
    display: 'flex', flexDirection: 'column' as const, alignItems: 'center', justifyContent: 'center',
    padding: '1rem 0.75rem', borderRadius: 8, border: '1px solid var(--page-border, #ddd)',
    background: 'var(--page-bg-elevated, #fff)', cursor: 'pointer', transition: 'all 0.15s',
    gap: '0.4rem', minHeight: 90, position: 'relative' as const,
  },
  schemaCardActive: {
    border: '2px solid #0d6efd', background: 'rgba(13,110,253,0.06)', boxShadow: '0 0 0 3px rgba(13,110,253,0.12)',
  },
  schemaIcon: { fontSize: '1.5rem', lineHeight: 1 },
  schemaName: { fontSize: '0.8125rem', fontWeight: 600, color: 'var(--page-text)', textAlign: 'center' as const, lineHeight: 1.2 },
  schemaType: { fontSize: '0.6875rem', color: 'var(--page-text-muted)' },
  schemaBadge: {
    position: 'absolute' as const, top: 6, right: 6, width: 20, height: 20, borderRadius: '50%',
    background: '#0d6efd', color: '#fff', fontSize: '0.875rem', fontWeight: 700,
    display: 'flex', alignItems: 'center', justifyContent: 'center', lineHeight: 1,
  },
  /* Filter bar */
  filterBar: {
    display: 'flex', gap: '0.75rem', flexWrap: 'wrap' as const, alignItems: 'center',
    marginBottom: '1rem', padding: '0.75rem 1rem', borderRadius: 8,
    background: 'var(--page-bg-elevated, #f8f9fa)', border: '1px solid var(--page-border, #ddd)',
  },
  filterGroup: { display: 'flex', flexDirection: 'column' as const, gap: '0.15rem' },
  filterLabel: { fontSize: '0.6875rem', fontWeight: 600, color: 'var(--page-text-muted)', textTransform: 'uppercase' as const },
  filterSelect: {
    padding: '0.35rem 0.5rem', fontSize: '0.8125rem', minWidth: 130, borderRadius: 4,
    border: '1px solid var(--input-border)', background: 'var(--input-bg)', color: 'var(--input-text)',
  },
  filterReset: {
    padding: '0.35rem 0.6rem', fontSize: '0.75rem', borderRadius: 4,
    border: '1px solid var(--input-border)', background: 'transparent', color: '#0d6efd',
    cursor: 'pointer', alignSelf: 'flex-end',
  },
  /* Table */
  table: { width: '100%', borderCollapse: 'collapse' as const, fontSize: '0.875rem', color: 'var(--page-text)' },
  th: { textAlign: 'left' as const, padding: '0.5rem 0.75rem', borderBottom: '2px solid var(--page-border)', fontSize: '0.75rem', fontWeight: 600, color: 'var(--page-text-muted)', textTransform: 'uppercase' as const },
  td: { padding: '0.5rem 0.75rem', borderBottom: '1px solid var(--page-border)' },
  link: { color: '#0d6efd', textDecoration: 'none' },
  statusBadge: {
    display: 'inline-block', padding: '0.15rem 0.5rem', borderRadius: 10, fontSize: '0.75rem', fontWeight: 500,
  },
  /* Pagination */
  pagination: { marginTop: '1rem', display: 'flex', gap: '0.5rem', alignItems: 'center', justifyContent: 'center' },
  pageBtn: { padding: '0.35rem 0.75rem', fontSize: '0.8125rem', cursor: 'pointer', borderRadius: 4, border: '1px solid var(--input-border)', background: 'var(--input-bg)', color: 'var(--page-text)' },
  pageInfo: { fontSize: '0.8125rem', color: 'var(--page-text-muted)' },
  /* Create panel */
  createPanel: {
    marginBottom: '1.5rem', padding: '1.25rem', borderRadius: 8,
    border: '2px solid #0d6efd', background: 'var(--page-bg-elevated, #fff)',
  },
  createHeader: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' },
  createTitle: { margin: 0, fontSize: '1.1rem', fontWeight: 600, color: 'var(--page-text)' },
  cancelBtn: {
    padding: '0.35rem 0.75rem', fontSize: '0.8125rem', borderRadius: 4,
    border: '1px solid var(--input-border)', background: 'transparent', color: 'var(--page-text)',
    cursor: 'pointer',
  },
  input: {
    width: '100%', maxWidth: 400, padding: '0.5rem', fontSize: '0.875rem', borderRadius: 4,
    border: '1px solid var(--input-border)', color: 'var(--input-text)', background: 'var(--input-bg)',
    marginBottom: '0.75rem',
  },
  error: { color: '#dc3545', fontSize: '0.875rem', marginBottom: '0.5rem' },
  hint: { color: 'var(--page-text-muted)', fontSize: '0.875rem' },
  emptyState: {
    textAlign: 'center' as const, padding: '3rem 1rem', color: 'var(--page-text-muted)',
    border: '1px dashed var(--page-border, #ddd)', borderRadius: 8, marginTop: '0.5rem',
  },
}

/* ─── Iconos por tipo de schema ─── */
const SCHEMA_ICONS: Record<string, string> = {
  article: '📄', page: '📃', blog: '📝', post: '📝', landing: '🚀',
  video: '🎬', image: '🖼️', gallery: '🎨', product: '🛍️', event: '📅',
  faq: '❓', news: '📰', review: '⭐', testimonial: '💬', category: '📁',
}
function schemaIcon(schemaType: string): string {
  const key = schemaType.toLowerCase().replace(/[-_]/g, '')
  for (const [k, icon] of Object.entries(SCHEMA_ICONS)) {
    if (key.includes(k)) return icon
  }
  return '📋'
}

function statusBadgeStyle(status: string): React.CSSProperties {
  switch (status) {
    case 'Published': return { ...s.statusBadge, background: '#198754', color: '#fff' }
    case 'Draft': return { ...s.statusBadge, background: '#6c757d', color: '#fff' }
    case 'InReview': return { ...s.statusBadge, background: '#0d6efd', color: '#fff' }
    case 'Archived': return { ...s.statusBadge, background: '#ffc107', color: '#000' }
    default: return { ...s.statusBadge, background: '#e9ecef', color: '#333' }
  }
}

const PAGE_SIZE = 20

/* ═══════════════ Componente principal ═══════════════ */

export function ContentListPage() {
  const { currentProjectId, currentEnvironmentId, currentSiteId, sites } = useContextStore()
  const user = useAuthStore((st) => st.user)
  const { schemaList, loadSchemas, listLoading } = useSchemaStore()

  /* ── Estado: lista ── */
  const [items, setItems] = useState<ContentListItem[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [contentType, setContentType] = useState('')
  const [status, setStatus] = useState('')
  const [siteIdFilter, setSiteIdFilter] = useState(currentSiteId ?? '')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  /* ── Estado: creación inline ── */
  const [createSchema, setCreateSchema] = useState<ContentSchemaListItem | null>(null)
  const [contentTitle, setContentTitle] = useState('')
  const [submitError, setSubmitError] = useState<string | null>(null)

  /* ── Cargar schemas ── */
  useEffect(() => {
    if (currentProjectId) loadSchemas(currentProjectId).catch(() => {})
  }, [currentProjectId, loadSchemas])

  /* ── Cargar contenido ── */
  useEffect(() => {
    if (!currentProjectId) return
    let cancelled = false
    const fetchContent = async () => {
      setLoading(true)
      setError(null)
      try {
        const result = await coreApi.getContentList(currentProjectId, {
          page,
          pageSize: PAGE_SIZE,
          contentType: contentType || undefined,
          status: status || undefined,
          siteId: siteIdFilter || undefined,
        })
        if (!cancelled && result) {
          setItems(result.items ?? [])
          setTotalCount(result.totalCount ?? 0)
        }
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Error al cargar')
      } finally {
        if (!cancelled) setLoading(false)
      }
    }
    fetchContent()
    return () => { cancelled = true }
  }, [currentProjectId, page, contentType, status, siteIdFilter])

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))
  const hasFilters = Boolean(contentType || status || siteIdFilter)

  /* ── Contadores por tipo ── */
  const countByType = useMemo(() => {
    const map: Record<string, number> = {}
    for (const it of items) {
      map[it.contentType] = (map[it.contentType] ?? 0) + 1
    }
    return map
  }, [items])

  /* ── Handlers ── */
  const handleSelectSchema = (sc: ContentSchemaListItem) => {
    if (createSchema?.id === sc.id) {
      setCreateSchema(null)
      return
    }
    setCreateSchema(sc)
    setContentTitle('')
    setSubmitError(null)
  }

  const handleCancelCreate = () => {
    setCreateSchema(null)
    setContentTitle('')
    setSubmitError(null)
  }

  const handleCreate: DynamicFormProps['onSubmit'] = async (values) => {
    if (!currentProjectId || !currentEnvironmentId || !createSchema || !contentTitle.trim()) {
      setSubmitError('Proyecto, entorno, schema y título son obligatorios.')
      return
    }
    const createdBy = user?.userId
    if (!createdBy) { setSubmitError('Usuario no identificado.'); return }
    setSubmitError(null)
    try {
      await coreApi.createContent(currentProjectId, {
        environmentId: currentEnvironmentId,
        siteId: currentSiteId ?? undefined,
        schemaId: createSchema.id,
        title: contentTitle.trim(),
        contentType: createSchema.schemaType,
        fields: values as Record<string, unknown>,
        createdBy,
      })
      handleCancelCreate()
      // Reload list
      setPage(1)
      const result = await coreApi.getContentList(currentProjectId, { page: 1, pageSize: PAGE_SIZE, contentType: contentType || undefined, status: status || undefined, siteId: siteIdFilter || undefined })
      if (result) { setItems(result.items ?? []); setTotalCount(result.totalCount ?? 0) }
    } catch (e) {
      setSubmitError(e instanceof Error ? e.message : 'Error al crear contenido')
    }
  }

  const resetFilters = () => { setContentType(''); setStatus(''); setSiteIdFilter(''); setPage(1) }

  /* ── Guard ── */
  if (!currentProjectId) {
    return (
      <div style={s.page}>
        <h1 style={s.title}>Contenido</h1>
        <p style={s.hint}>Selecciona un proyecto en la barra superior.</p>
      </div>
    )
  }

  return (
    <div style={s.page}>
      {/* ═══ Header ═══ */}
      <div style={s.header}>
        <h1 style={s.title}>Contenido</h1>
        <span style={s.pageInfo}>{totalCount} elemento{totalCount !== 1 ? 's' : ''}</span>
      </div>

      {/* ═══ Schema cards — Nuevo contenido ═══ */}
      <div style={s.schemaSection}>
        <p style={s.schemaSectionTitle}>Nuevo contenido</p>
        {listLoading && <p style={s.hint}>Cargando tipos…</p>}
        {!listLoading && schemaList.length === 0 && (
          <p style={s.hint}>No hay schemas. <Link to="/admin/schemas" style={s.link}>Crear schema</Link>.</p>
        )}
        {!listLoading && schemaList.length > 0 && (
          <div style={s.schemaGrid}>
            {schemaList.map((sc) => (
              <button
                key={sc.id}
                type="button"
                style={{
                  ...s.schemaCard,
                  ...(createSchema?.id === sc.id ? s.schemaCardActive : {}),
                }}
                onClick={() => handleSelectSchema(sc)}
                onMouseEnter={(e) => { if (createSchema?.id !== sc.id) (e.currentTarget.style.borderColor = '#0d6efd') }}
                onMouseLeave={(e) => { if (createSchema?.id !== sc.id) (e.currentTarget.style.borderColor = '') }}
                title={`Crear ${sc.schemaName}`}
              >
                <span style={s.schemaBadge}>+</span>
                <span style={s.schemaIcon}>{schemaIcon(sc.schemaType)}</span>
                <span style={s.schemaName}>{sc.schemaName}</span>
                <span style={s.schemaType}>{sc.schemaType}</span>
              </button>
            ))}
          </div>
        )}
      </div>

      {/* ═══ Create panel (inline) ═══ */}
      {createSchema && (
        <div style={s.createPanel}>
          <div style={s.createHeader}>
            <h2 style={s.createTitle}>
              {schemaIcon(createSchema.schemaType)} Nuevo {createSchema.schemaName}
            </h2>
            <button type="button" style={s.cancelBtn} onClick={handleCancelCreate}>
              ✕ Cancelar
            </button>
          </div>

          <div>
            <label htmlFor="content-title" style={{ fontSize: '0.8125rem', fontWeight: 600, color: 'var(--page-text)' }}>
              Título del contenido *
            </label>
            <br />
            <input
              id="content-title"
              type="text"
              style={s.input}
              value={contentTitle}
              onChange={(e) => setContentTitle(e.target.value)}
              placeholder="Ej. Mi primer artículo"
              autoFocus
            />
          </div>

          {submitError && <p style={s.error}>{submitError}</p>}

          {currentProjectId && (
            <DynamicForm
              projectId={currentProjectId}
              schemaId={createSchema.id}
              onSubmit={handleCreate}
              submitLabel={`Crear ${createSchema.schemaName}`}
            />
          )}
        </div>
      )}

      {/* ═══ Filter bar ═══ */}
      <div style={s.filterBar}>
        <div style={s.filterGroup}>
          <span style={s.filterLabel}>Tipo</span>
          <select style={s.filterSelect} value={contentType} onChange={(e) => { setContentType(e.target.value); setPage(1) }}>
            <option value="">Todos</option>
            {schemaList.map((sc) => (
              <option key={sc.id} value={sc.schemaType}>
                {sc.schemaName}{countByType[sc.schemaType] ? ` (${countByType[sc.schemaType]})` : ''}
              </option>
            ))}
          </select>
        </div>
        <div style={s.filterGroup}>
          <span style={s.filterLabel}>Estado</span>
          <select style={s.filterSelect} value={status} onChange={(e) => { setStatus(e.target.value); setPage(1) }}>
            <option value="">Todos</option>
            <option value="Draft">Draft</option>
            <option value="Published">Published</option>
            <option value="InReview">En revisión</option>
            <option value="Archived">Archivado</option>
          </select>
        </div>
        {sites.length > 0 && (
          <div style={s.filterGroup}>
            <span style={s.filterLabel}>Sitio</span>
            <select style={s.filterSelect} value={siteIdFilter} onChange={(e) => { setSiteIdFilter(e.target.value); setPage(1) }}>
              <option value="">Todos</option>
              {sites.map((st) => (
                <option key={st.id} value={st.id}>{st.name}</option>
              ))}
            </select>
          </div>
        )}
        {hasFilters && (
          <button type="button" style={s.filterReset} onClick={resetFilters}>
            Limpiar filtros
          </button>
        )}
      </div>

      {/* ═══ Content table ═══ */}
      {error && <ErrorBanner message={error} />}
      {loading && <LoadingSpinner text="Cargando contenido…" />}

      {!loading && items.length === 0 && (
        <div style={s.emptyState}>
          <p style={{ fontSize: '1.1rem', marginBottom: '0.5rem' }}>Sin contenido todavía</p>
          <p style={{ margin: 0 }}>Selecciona un tipo de contenido arriba para crear tu primer elemento.</p>
        </div>
      )}

      {!loading && items.length > 0 && (
        <>
          <table style={s.table}>
            <thead>
              <tr>
                <th style={s.th}>Título</th>
                <th style={s.th}>Tipo</th>
                <th style={s.th}>Estado</th>
                <th style={s.th}>Creado</th>
                <th style={{ ...s.th, textAlign: 'right' }}>Acción</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.id}>
                  <td style={s.td}>
                    <Link to={`/content/${item.id}/edit`} style={{ ...s.link, fontWeight: 500 }}>
                      {item.title}
                    </Link>
                    <br />
                    <span style={{ fontSize: '0.75rem', color: 'var(--page-text-muted)' }}>{item.slug}</span>
                  </td>
                  <td style={s.td}>
                    <span style={{ fontSize: '0.8125rem' }}>{schemaIcon(item.contentType)} {item.contentType}</span>
                  </td>
                  <td style={s.td}>
                    <span style={statusBadgeStyle(item.status)}>{item.status}</span>
                  </td>
                  <td style={s.td}>
                    <span style={{ fontSize: '0.8125rem' }}>
                      {item.createdAt ? new Date(item.createdAt).toLocaleDateString() : '—'}
                    </span>
                  </td>
                  <td style={{ ...s.td, textAlign: 'right' }}>
                    <Link to={`/content/${item.id}/edit`} style={{ ...s.link, fontSize: '0.8125rem' }}>
                      Editar
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {/* Pagination */}
          {totalPages > 1 && (
            <div style={s.pagination}>
              <button type="button" style={s.pageBtn} disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
                ← Anterior
              </button>
              <span style={s.pageInfo}>
                Pág. {page} de {totalPages}
              </span>
              <button type="button" style={s.pageBtn} disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
                Siguiente →
              </button>
            </div>
          )}
        </>
      )}
    </div>
  )
}
