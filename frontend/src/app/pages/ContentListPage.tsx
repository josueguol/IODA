import { useEffect, useState, useMemo } from 'react'
import { Link } from 'react-router-dom'
import { coreApi } from '../../modules/core/api/core-api'
import { useContextStore } from '../../modules/core/store/context-store'
import { useSchemaStore } from '../../modules/schema/store/schema-store'
import { LoadingSpinner, ErrorBanner } from '../../shared/components'
import { DynamicForm, type DynamicFormProps } from '../../modules/schema/components/DynamicForm'
import { ParentContentSelector } from '../../modules/core/components/ParentContentSelector'
import { TagsSelector } from '../../modules/core/components/TagsSelector'
import { HierarchySelector } from '../../modules/core/components/HierarchySelector'
import { SiteSelector } from '../../modules/core/components/SiteSelector'
import type { ContentListItem, ContentSchemaListItem, Hierarchy, Site } from '../../modules/core/types'
import './ContentListPage.css'

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

function statusBadgeClass(status: string): string {
  const base = 'content-list-page__status-badge'
  switch (status) {
    case 'Published': return `${base} content-list-page__status-badge--published`
    case 'Draft': return `${base} content-list-page__status-badge--draft`
    case 'InReview': return `${base} content-list-page__status-badge--inreview`
    case 'Archived': return `${base} content-list-page__status-badge--archived`
    default: return `${base} content-list-page__status-badge--default`
  }
}

const PAGE_SIZE = 20

function normalizeSlugInput(value: string): string {
  return value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .trim()
    .replace(/\s+/g, '-')
    .replace(/[^a-z0-9_-]/g, '')
}

export function ContentListPage() {
  const { currentProjectId, currentEnvironmentId, currentSiteId } = useContextStore()
  const { schemaList, loadSchemas, listLoading } = useSchemaStore()

  const [items, setItems] = useState<ContentListItem[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [contentType, setContentType] = useState('')
  const [status, setStatus] = useState('')
  const [sectionId, setSectionId] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [hierarchies, setHierarchies] = useState<Hierarchy[]>([])
  const [sites, setSites] = useState<Site[]>([])

  const [createSchema, setCreateSchema] = useState<ContentSchemaListItem | null>(null)
  const [contentTitle, setContentTitle] = useState('')
  const [contentSlug, setContentSlug] = useState('')
  const [createParentContentId, setCreateParentContentId] = useState<string | null>(null)
  const [createTagIds, setCreateTagIds] = useState<string[]>([])
  const [createHierarchyIds, setCreateHierarchyIds] = useState<string[]>([])
  const [createPrimaryHierarchyId, setCreatePrimaryHierarchyId] = useState<string>('')
  const [createSiteIds, setCreateSiteIds] = useState<string[]>([])
  const [createSiteUrlMap, setCreateSiteUrlMap] = useState<Record<string, string>>({})
  const [submitError, setSubmitError] = useState<string | null>(null)

  useEffect(() => {
    if (currentProjectId) loadSchemas(currentProjectId).catch(() => {})
  }, [currentProjectId, loadSchemas])

  useEffect(() => {
    if (!currentProjectId) return
    coreApi.getHierarchies(currentProjectId).then(setHierarchies).catch(() => setHierarchies([]))
  }, [currentProjectId])

  useEffect(() => {
    if (!currentProjectId || !currentEnvironmentId) return
    coreApi.getSites(currentProjectId, currentEnvironmentId).then(setSites).catch(() => setSites([]))
  }, [currentProjectId, currentEnvironmentId])

  useEffect(() => {
    if (createPrimaryHierarchyId && !createHierarchyIds.includes(createPrimaryHierarchyId)) {
      setCreatePrimaryHierarchyId('')
    }
  }, [createHierarchyIds, createPrimaryHierarchyId])

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
          sectionId: sectionId || undefined,
          siteId: currentSiteId ?? undefined,
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
  }, [currentProjectId, currentSiteId, page, contentType, status, sectionId])

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))
  const hasFilters = Boolean(contentType || status || sectionId)

  const countByType = useMemo(() => {
    const map: Record<string, number> = {}
    for (const it of items) {
      map[it.contentType] = (map[it.contentType] ?? 0) + 1
    }
    return map
  }, [items])

  const createTargetSiteIds = useMemo(() => {
    const ids = new Set<string>()
    if (currentSiteId) ids.add(currentSiteId)
    for (const id of createSiteIds) ids.add(id)
    return Array.from(ids)
  }, [currentSiteId, createSiteIds])

  const handleSelectSchema = (sc: ContentSchemaListItem) => {
    if (createSchema?.id === sc.id) {
      setCreateSchema(null)
      return
    }
    setCreateSchema(sc)
    setContentTitle('')
    setContentSlug('')
    setSubmitError(null)
  }

  const handleCancelCreate = () => {
    setCreateSchema(null)
    setContentTitle('')
    setContentSlug('')
    setCreateParentContentId(null)
    setCreateTagIds([])
    setCreateHierarchyIds([])
    setCreatePrimaryHierarchyId('')
    setCreateSiteIds([])
    setCreateSiteUrlMap({})
    setSubmitError(null)
  }

  const handleCreate: DynamicFormProps['onSubmit'] = async (values) => {
    if (!currentProjectId || !currentEnvironmentId || !createSchema || !contentTitle.trim() || !contentSlug.trim()) {
      setSubmitError('Proyecto, entorno, schema, título y slug son obligatorios.')
      return
    }
    setSubmitError(null)
    try {
      await coreApi.createContent(currentProjectId, {
        environmentId: currentEnvironmentId,
        siteId: currentSiteId ?? undefined,
        parentContentId: createParentContentId ?? undefined,
        schemaId: createSchema.id,
        title: contentTitle.trim(),
        slug: contentSlug.trim() ? normalizeSlugInput(contentSlug) : undefined,
        contentType: createSchema.schemaType,
        fields: values as Record<string, unknown>,
        tagIds: createTagIds.length > 0 ? createTagIds : undefined,
        hierarchyIds: createHierarchyIds.length > 0 ? createHierarchyIds : undefined,
        primaryHierarchyId: createPrimaryHierarchyId || undefined,
        siteIds: createSiteIds.length > 0 ? createSiteIds : undefined,
        siteUrls:
          createTargetSiteIds.length > 0
            ? createTargetSiteIds
                .map((siteId) => ({
                  siteId,
                  path: normalizeSlugInput(createSiteUrlMap[siteId] || contentSlug || contentTitle),
                }))
                .filter((x) => x.path.length > 0)
            : undefined,
      })
      handleCancelCreate()
      setPage(1)
      const result = await coreApi.getContentList(currentProjectId, {
        page: 1,
        pageSize: PAGE_SIZE,
        contentType: contentType || undefined,
        status: status || undefined,
        sectionId: sectionId || undefined,
        siteId: currentSiteId ?? undefined,
      })
      if (result) {
        setItems(result.items ?? [])
        setTotalCount(result.totalCount ?? 0)
      }
    } catch (e) {
      setSubmitError(e instanceof Error ? e.message : 'Error al crear contenido')
    }
  }

  const resetFilters = () => {
    setContentType('')
    setStatus('')
    setSectionId('')
    setPage(1)
  }

  return (
    <div className="content-list-page">
      <div className="content-list-page__schema-section">
        {listLoading && <p className="content-list-page__hint">Cargando tipos…</p>}
        {!listLoading && schemaList.length === 0 && (
          <p className="content-list-page__hint">
            No hay schemas. <Link to="/admin/schemas" className="content-list-page__link">Crear schema</Link>.
          </p>
        )}
        {!listLoading && schemaList.length > 0 && (
          <div className="content-list-page__schema-grid">
            {schemaList.map((sc) => (
              <button
                key={sc.id}
                type="button"
                className={`content-list-page__schema-card ${createSchema?.id === sc.id ? 'content-list-page__schema-card--active' : ''}`}
                onClick={() => handleSelectSchema(sc)}
                title={`Crear ${sc.schemaName}`}
              >
                <span className="content-list-page__schema-badge" aria-hidden>+</span>
                <span className="content-list-page__schema-icon">{schemaIcon(sc.schemaType)}</span>
                <span className="content-list-page__schema-name">{sc.schemaName}</span>
                <span className="content-list-page__schema-type">{sc.schemaType}</span>
              </button>
            ))}
          </div>
        )}
      </div>

      {createSchema && (
        <div className="content-list-page__create-panel">
          <div className="content-list-page__create-header">
            <h2 className="content-list-page__create-title">
              {schemaIcon(createSchema.schemaType)} Nuevo {createSchema.schemaName}
            </h2>
            <button type="button" className="content-list-page__cancel-btn" onClick={handleCancelCreate}>
              ✕ Cancelar
            </button>
          </div>

          <div>
            <label htmlFor="content-title" className="content-list-page__create-label">
              Título del contenido *
            </label>
            <input
              id="content-title"
              type="text"
              className="content-list-page__create-input"
              value={contentTitle}
              onChange={(e) => {
                const nextTitle = e.target.value
                setContentTitle(nextTitle)
                if (!contentSlug.trim()) {
                  setContentSlug(normalizeSlugInput(nextTitle))
                }
              }}
              placeholder="Ej. Mi primer artículo"
              autoFocus
            />
          </div>

          <div>
            <label htmlFor="content-slug" className="content-list-page__create-label">
              Slug (URL) *
            </label>
            <input
              id="content-slug"
              type="text"
              className="content-list-page__create-input"
              value={contentSlug}
              onChange={(e) => setContentSlug(normalizeSlugInput(e.target.value))}
              placeholder="ej-mi-contenido"
            />
          </div>

          {currentProjectId && (
            <>
              <ParentContentSelector
                projectId={currentProjectId}
                value={createParentContentId}
                onChange={setCreateParentContentId}
              />
              <TagsSelector projectId={currentProjectId} value={createTagIds} onChange={setCreateTagIds} />
              <HierarchySelector projectId={currentProjectId} value={createHierarchyIds} onChange={setCreateHierarchyIds} />
              {createHierarchyIds.length > 0 && (
                <div>
                  <label htmlFor="content-primary-section" className="content-list-page__create-label">
                    Sección principal (opcional)
                  </label>
                  <select
                    id="content-primary-section"
                    className="content-list-page__create-input"
                    value={createPrimaryHierarchyId}
                    onChange={(e) => setCreatePrimaryHierarchyId(e.target.value)}
                  >
                    <option value="">Sin sección principal</option>
                    {createHierarchyIds.map((id) => {
                      const hierarchy = hierarchies.find((h) => h.id === id)
                      return (
                        <option key={id} value={id}>
                          {hierarchy?.name ?? id}
                        </option>
                      )
                    })}
                  </select>
                </div>
              )}
              <SiteSelector
                projectId={currentProjectId}
                environmentId={currentEnvironmentId}
                value={createSiteIds}
                onChange={setCreateSiteIds}
              />
              {createTargetSiteIds.length > 0 && (
                <div>
                  <p className="content-list-page__create-label">URLs por sitio (opcional)</p>
                  {createTargetSiteIds.map((siteId) => {
                    const site = sites.find((s) => s.id === siteId)
                    return (
                      <div key={siteId}>
                        <label htmlFor={`site-url-${siteId}`} className="content-list-page__create-label">
                          {site?.name ?? siteId} {siteId === currentSiteId ? '(owner)' : '(shared)'}
                        </label>
                        <input
                          id={`site-url-${siteId}`}
                          type="text"
                          className="content-list-page__create-input"
                          value={createSiteUrlMap[siteId] ?? ''}
                          onChange={(e) =>
                            setCreateSiteUrlMap((prev) => ({ ...prev, [siteId]: normalizeSlugInput(e.target.value) }))
                          }
                          placeholder={normalizeSlugInput(contentSlug || contentTitle || 'contenido')}
                        />
                      </div>
                    )
                  })}
                </div>
              )}
            </>
          )}

          {submitError && <p className="content-list-page__error">{submitError}</p>}

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

      <div className="content-list-page__filter-bar">
        <div className="content-list-page__filter-group">
          <span className="content-list-page__filter-label">Tipo</span>
          <select
            className="content-list-page__filter-select"
            value={contentType}
            onChange={(e) => { setContentType(e.target.value); setPage(1) }}
          >
            <option value="">Todos</option>
            {schemaList.map((sc) => (
              <option key={sc.id} value={sc.schemaType}>
                {sc.schemaName}{countByType[sc.schemaType] ? ` (${countByType[sc.schemaType]})` : ''}
              </option>
            ))}
          </select>
        </div>
        <div className="content-list-page__filter-group">
          <span className="content-list-page__filter-label">Estado</span>
          <select
            className="content-list-page__filter-select"
            value={status}
            onChange={(e) => { setStatus(e.target.value); setPage(1) }}
          >
            <option value="">Todos</option>
            <option value="Draft">Draft</option>
            <option value="Published">Published</option>
            <option value="InReview">En revisión</option>
            <option value="Archived">Archivado</option>
          </select>
        </div>
        <div className="content-list-page__filter-group">
          <span className="content-list-page__filter-label">Sección</span>
          <select
            className="content-list-page__filter-select"
            value={sectionId}
            onChange={(e) => { setSectionId(e.target.value); setPage(1) }}
          >
            <option value="">Todas</option>
            {hierarchies.map((h) => (
              <option key={h.id} value={h.id}>
                {h.name}
              </option>
            ))}
          </select>
        </div>
        {hasFilters && (
          <button type="button" className="content-list-page__filter-reset" onClick={resetFilters}>
            Limpiar filtros
          </button>
        )}
      </div>

      {error && <ErrorBanner message={error} />}
      {loading && <LoadingSpinner text="Cargando contenido…" />}

      {!loading && items.length === 0 && (
        <div className="content-list-page__empty-state">
          <p className="content-list-page__empty-state-title">Sin contenido todavía</p>
          <p className="content-list-page__empty-state-desc">Selecciona un tipo de contenido arriba para crear tu primer elemento.</p>
        </div>
      )}

      {!loading && items.length > 0 && (
        <>
          <table className="content-list-page__table">
            <thead>
              <tr>
                <th className="content-list-page__th">Título</th>
                <th className="content-list-page__th">Tipo</th>
                <th className="content-list-page__th">Orden</th>
                <th className="content-list-page__th">Estado</th>
                <th className="content-list-page__th">Creado</th>
                <th className="content-list-page__th content-list-page__th--right">Acción</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.id}>
                  <td className="content-list-page__td">
                    <Link to={`/content/${item.id}/edit`} className="content-list-page__link content-list-page__link--bold">
                      {item.title}
                    </Link>
                    <br />
                    <span className="content-list-page__slug">{item.slug}</span>
                  </td>
                  <td className="content-list-page__td">
                    <span>{schemaIcon(item.contentType)} {item.contentType}</span>
                  </td>
                  <td className="content-list-page__td">{item.order}</td>
                  <td className="content-list-page__td">
                    <span className={statusBadgeClass(item.status)}>{item.status}</span>
                  </td>
                  <td className="content-list-page__td">
                    {item.createdAt ? new Date(item.createdAt).toLocaleDateString() : '—'}
                  </td>
                  <td className="content-list-page__td content-list-page__td--right">
                    <Link to={`/content/${item.id}/edit`} className="content-list-page__link">
                      Editar
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {totalPages > 1 && (
            <div className="content-list-page__pagination">
              <button
                type="button"
                className="content-list-page__page-btn"
                disabled={page <= 1}
                onClick={() => setPage((p) => p - 1)}
              >
                ← Anterior
              </button>
              <span className="content-list-page__page-info">
                Pág. {page} de {totalPages}
              </span>
              <button
                type="button"
                className="content-list-page__page-btn"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => p + 1)}
              >
                Siguiente →
              </button>
            </div>
          )}
        </>
      )}
    </div>
  )
}
