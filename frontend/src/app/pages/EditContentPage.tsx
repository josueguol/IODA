import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams, Link } from 'react-router-dom'
import { coreApi } from '../../modules/core/api/core-api'
import { useContextStore } from '../../modules/core/store/context-store'
import { useAuthStore } from '../../modules/auth/store/auth-store'
import { useSchemaStore } from '../../modules/schema/store/schema-store'
import { Can } from '../../modules/authorization/components/Can'
import { publishingApi } from '../../modules/publishing/api/publishing-api'
import { indexingApi } from '../../modules/indexing/api/indexing-api'
import { DynamicForm, type DynamicFormProps } from '../../modules/schema/components/DynamicForm'
import type { DynamicFormValues } from '../../modules/schema/utils/field-validation'
import type { Content, ContentVersion } from '../../modules/core/types'
import { ParentContentSelector } from '../../modules/core/components/ParentContentSelector'
import { TagsSelector } from '../../modules/core/components/TagsSelector'
import { HierarchySelector } from '../../modules/core/components/HierarchySelector'
import { SiteSelector } from '../../modules/core/components/SiteSelector'
import { ContentBlocksSection } from '../components/ContentBlocksSection'
import './EditContentPage.css'
import type { Hierarchy, Site } from '../../modules/core/types'

function normalizeSlugInput(value: string): string {
  return value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .trim()
    .replace(/\s+/g, '-')
    .replace(/[^a-z0-9_-]/g, '')
}

export function EditContentPage() {
  const { contentId } = useParams<{ contentId: string }>()
  const navigate = useNavigate()
  const { currentProjectId } = useContextStore()
  const user = useAuthStore((s) => s.user)
  const { loadSchema, getSchemaSync } = useSchemaStore()
  const [content, setContent] = useState<Content | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [deleteConfirm, setDeleteConfirm] = useState(false)
  const [editTitle, setEditTitle] = useState('')
  const [editSlug, setEditSlug] = useState('')
  const [editParentContentId, setEditParentContentId] = useState<string | null>(null)
  const [editOrder, setEditOrder] = useState<string>('')
  const [editTagIds, setEditTagIds] = useState<string[]>([])
  const [editHierarchyIds, setEditHierarchyIds] = useState<string[]>([])
  const [editPrimaryHierarchyId, setEditPrimaryHierarchyId] = useState<string>('')
  const [editSiteIds, setEditSiteIds] = useState<string[]>([])
  const [editSiteUrlMap, setEditSiteUrlMap] = useState<Record<string, string>>({})
  const [hierarchies, setHierarchies] = useState<Hierarchy[]>([])
  const [sites, setSites] = useState<Site[]>([])
  const [requestingPublication, setRequestingPublication] = useState(false)
  const [requestPublicationMessage, setRequestPublicationMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null)
  const [reindexing, setReindexing] = useState(false)
  const [reindexMessage, setReindexMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null)
  const [versions, setVersions] = useState<ContentVersion[]>([])
  const [versionsLoading, setVersionsLoading] = useState(false)
  const [showHistory, setShowHistory] = useState(false)
  const [expandedVersion, setExpandedVersion] = useState<number | null>(null)

  const { currentEnvironmentId } = useContextStore()

  const refreshContent = () => {
    if (!currentProjectId || !contentId) return
    coreApi
      .getContent(currentProjectId, contentId)
      .then((data) => {
        if (data) {
          setContent({ ...data, blocks: data.blocks ?? [] })
        }
      })
      .catch(() => {})
  }

  useEffect(() => {
    if (!currentProjectId) return
    coreApi.getHierarchies(currentProjectId).then(setHierarchies).catch(() => setHierarchies([]))
  }, [currentProjectId])

  useEffect(() => {
    if (!currentProjectId || !content?.environmentId) return
    coreApi.getSites(currentProjectId, content.environmentId).then(setSites).catch(() => setSites([]))
  }, [currentProjectId, content?.environmentId])

  useEffect(() => {
    if (!currentProjectId || !contentId) {
      setLoading(false)
      return
    }
    setLoading(true)
    coreApi
      .getContent(currentProjectId, contentId)
      .then((data) => {
        setContent(data ? { ...data, blocks: data.blocks ?? [] } : null)
        setEditTitle(data?.title ?? '')
        setEditSlug(data?.slug ?? '')
        setEditParentContentId(data?.parentContentId ?? null)
        setEditOrder(data?.order != null ? String(data.order) : '')
        setEditTagIds(data?.tagIds ?? [])
        setEditHierarchyIds(data?.hierarchyIds ?? [])
        setEditPrimaryHierarchyId(data?.primaryHierarchyId ?? '')
        setEditSiteIds(data?.siteIds ?? [])
        const nextSiteUrlMap: Record<string, string> = {}
        for (const siteUrl of data?.siteUrls ?? []) {
          nextSiteUrlMap[siteUrl.siteId] = siteUrl.path
        }
        setEditSiteUrlMap(nextSiteUrlMap)
        if (data?.schemaId) {
          loadSchema(currentProjectId, data.schemaId).catch(() => {})
        }
      })
      .catch(() => setError('Contenido no encontrado'))
      .finally(() => setLoading(false))
  }, [currentProjectId, contentId, loadSchema])

  useEffect(() => {
    if (editPrimaryHierarchyId && !editHierarchyIds.includes(editPrimaryHierarchyId)) {
      setEditPrimaryHierarchyId('')
    }
  }, [editPrimaryHierarchyId, editHierarchyIds])

  const schema = content && currentProjectId ? getSchemaSync(currentProjectId, content.schemaId) : null

  const defaultValues: DynamicFormValues | undefined = content?.fields
    ? Object.fromEntries(
        Object.entries(content.fields).map(([k, v]) => [k, v as string | number | boolean | null | undefined])
      )
    : undefined

  const editTargetSiteIds = useMemo(() => {
    const ids = new Set<string>()
    if (content?.siteId) ids.add(content.siteId)
    for (const id of editSiteIds) ids.add(id)
    return Array.from(ids)
  }, [content?.siteId, editSiteIds])

  const handleSubmit: DynamicFormProps['onSubmit'] = async (values) => {
    if (!currentProjectId || !contentId || !content) return
    setSubmitError(null)
    try {
      const orderNum = editOrder === '' ? undefined : parseInt(editOrder, 10)
      await coreApi.updateContent(currentProjectId, contentId, {
        title: editTitle.trim() || content.title,
        slug: editSlug.trim() ? normalizeSlugInput(editSlug) : undefined,
        fields: values as Record<string, unknown>,
        parentContentId: editParentContentId ?? undefined,
        order: orderNum !== undefined && !Number.isNaN(orderNum) ? orderNum : undefined,
        tagIds: editTagIds,
        hierarchyIds: editHierarchyIds,
        primaryHierarchyId: editPrimaryHierarchyId || undefined,
        siteIds: editSiteIds,
        siteUrls:
          editTargetSiteIds.length > 0
            ? editTargetSiteIds
                .map((siteId) => ({
                  siteId,
                  path: normalizeSlugInput(editSiteUrlMap[siteId] || editSlug || editTitle),
                }))
                .filter((x) => x.path.length > 0)
            : undefined,
      })
      const updated = await coreApi.getContent(currentProjectId, contentId)
      if (updated) {
        setContent(updated)
        setEditTitle(updated.title)
        setEditSlug(updated.slug)
        setEditParentContentId(updated.parentContentId ?? null)
        setEditOrder(updated.order != null ? String(updated.order) : '')
        setEditTagIds(updated.tagIds ?? [])
        setEditHierarchyIds(updated.hierarchyIds ?? [])
        setEditPrimaryHierarchyId(updated.primaryHierarchyId ?? '')
        setEditSiteIds(updated.siteIds ?? [])
        const nextSiteUrlMap: Record<string, string> = {}
        for (const siteUrl of updated.siteUrls ?? []) {
          nextSiteUrlMap[siteUrl.siteId] = siteUrl.path
        }
        setEditSiteUrlMap(nextSiteUrlMap)
      }
      if (showHistory) await loadVersions()
    } catch (e) {
      setSubmitError(e instanceof Error ? e.message : 'Error al guardar')
    }
  }

  const handleDelete = async () => {
    if (!currentProjectId || !contentId) return
    try {
      await coreApi.deleteContent(currentProjectId, contentId)
      navigate('/content', { replace: true })
    } catch (e) {
      setSubmitError(e instanceof Error ? e.message : 'Error al eliminar')
    }
  }

  const handleRequestPublication = async () => {
    if (!currentProjectId || !currentEnvironmentId || !contentId || !user?.userId) return
    setRequestPublicationMessage(null)
    setRequestingPublication(true)
    try {
      await publishingApi.requestPublication({
        contentId,
        projectId: currentProjectId,
        environmentId: currentEnvironmentId,
        requestedBy: user.userId,
      })
      setRequestPublicationMessage({ type: 'success', text: 'Solicitud de publicación enviada. Puedes verla en Solicitudes de publicación.' })
    } catch (e) {
      setRequestPublicationMessage({ type: 'error', text: e instanceof Error ? e.message : 'Error al solicitar publicación' })
    } finally {
      setRequestingPublication(false)
    }
  }

  const loadVersions = async () => {
    if (!currentProjectId || !contentId) return
    setVersionsLoading(true)
    try {
      const list = await coreApi.getContentVersions(currentProjectId, contentId)
      setVersions(list ?? [])
    } catch {
      setVersions([])
    } finally {
      setVersionsLoading(false)
    }
  }

  const handleToggleHistory = () => {
    const next = !showHistory
    setShowHistory(next)
    if (next && versions.length === 0) {
      loadVersions()
    }
  }

  const handleRestoreVersion = async (version: ContentVersion) => {
    if (!currentProjectId || !contentId) return
    setSubmitError(null)
    try {
      await coreApi.updateContent(currentProjectId, contentId, {
        title: version.title,
        fields: version.fields as Record<string, unknown>,
      })
      const updated = await coreApi.getContent(currentProjectId, contentId)
      if (updated) {
        setContent(updated)
        setEditTitle(updated.title)
        setEditSlug(updated.slug)
        setEditParentContentId(updated.parentContentId ?? null)
        setEditOrder(updated.order != null ? String(updated.order) : '')
        setEditTagIds(updated.tagIds ?? [])
        setEditHierarchyIds(updated.hierarchyIds ?? [])
        setEditPrimaryHierarchyId(updated.primaryHierarchyId ?? '')
        setEditSiteIds(updated.siteIds ?? [])
        const nextSiteUrlMap: Record<string, string> = {}
        for (const siteUrl of updated.siteUrls ?? []) {
          nextSiteUrlMap[siteUrl.siteId] = siteUrl.path
        }
        setEditSiteUrlMap(nextSiteUrlMap)
      }
      await loadVersions()
    } catch (e) {
      setSubmitError(e instanceof Error ? e.message : 'Error al restaurar versión')
    }
  }

  const handleReindex = async () => {
    if (!currentProjectId || !contentId || !content) return
    if (!content.publishedAt) {
      setReindexMessage({ type: 'error', text: 'El contenido no está publicado' })
      return
    }
    setReindexMessage(null)
    setReindexing(true)
    try {
      // Obtener la versión publicada
      const version = await coreApi.getContentVersion(currentProjectId, contentId, content.currentVersion)
      if (!version) {
        throw new Error('No se pudo obtener la versión del contenido')
      }
      // Reindexar
      await indexingApi.reindexContent({
        contentId: content.id,
        versionId: version.id,
        title: content.title,
        contentType: content.contentType,
        publishedAt: content.publishedAt,
        fields: content.fields,
      })
      setReindexMessage({ type: 'success', text: 'Contenido reindexado correctamente' })
    } catch (e) {
      setReindexMessage({ type: 'error', text: e instanceof Error ? e.message : 'Error al reindexar contenido' })
    } finally {
      setReindexing(false)
    }
  }

  if (loading) {
    return <p className="edit-content-page__error">Cargando…</p>
  }
  if (error || !content) {
    return (
      <div className="edit-content-page">
        <p className="edit-content-page__error">{error ?? 'Contenido no encontrado'}</p>
        <button type="button" className="edit-content-page__button" onClick={() => navigate('/content')}>
          Volver al listado
        </button>
      </div>
    )
  }

  return (
    <div className="edit-content-page">
      <div className="edit-content-page__header">
        <div>
          <label htmlFor="edit-title" className="edit-content-page__title-label">Título</label>
          <input
            id="edit-title"
            type="text"
            className="edit-content-page__title-input"
            value={editTitle}
            onChange={(e) => setEditTitle(e.target.value)}
          />
          <label htmlFor="edit-slug" className="edit-content-page__title-label">Slug</label>
          <input
            id="edit-slug"
            type="text"
            className="edit-content-page__title-input"
            value={editSlug}
            onChange={(e) => setEditSlug(normalizeSlugInput(e.target.value))}
          />
        </div>
        <span className="edit-content-page__status">
          Estado: {content.status} · v{content.currentVersion}
        </span>
      </div>

      {/* Metadata y auditoría (Fase 2): no se envían en create/update; se muestran solo lectura */}
      <div className="edit-content-page__meta">
        Creado: {new Date(content.createdAt).toLocaleString()}
        {content.updatedAt != null && (
          <> · Actualizado: {new Date(content.updatedAt).toLocaleString()}</>
        )}
        {content.createdBy && (
          <> · Creado por: {content.createdBy}</>
        )}
        {content.updatedBy != null && content.updatedBy !== '' && (
          <> · Actualizado por: {content.updatedBy}</>
        )}
      </div>
      <div className="edit-content-page__meta">
        Site owner: {content.siteId ?? '—'} · Sites shared: {content.siteIds.length > 0 ? content.siteIds.join(', ') : '—'}
      </div>

      {submitError && <p className="edit-content-page__error">{submitError}</p>}

      {currentProjectId && contentId && (
        <>
          <ParentContentSelector
            projectId={currentProjectId}
            value={editParentContentId}
            onChange={setEditParentContentId}
            excludeContentId={contentId}
          />
          <div className="edit-content-page__order-wrap">
            <label htmlFor="edit-order" className="edit-content-page__order-label">Orden (entre hermanos)</label>
            <input
              id="edit-order"
              type="number"
              min={0}
              className="edit-content-page__order-input"
              value={editOrder}
              onChange={(e) => setEditOrder(e.target.value)}
            />
          </div>
          <TagsSelector projectId={currentProjectId} value={editTagIds} onChange={setEditTagIds} />
          <HierarchySelector projectId={currentProjectId} value={editHierarchyIds} onChange={setEditHierarchyIds} />
          {editHierarchyIds.length > 0 && (
            <div className="edit-content-page__order-wrap">
              <label htmlFor="edit-primary-section" className="edit-content-page__order-label">Sección principal (opcional)</label>
              <select
                id="edit-primary-section"
                className="edit-content-page__order-input"
                value={editPrimaryHierarchyId}
                onChange={(e) => setEditPrimaryHierarchyId(e.target.value)}
              >
                <option value="">Sin sección principal</option>
                {editHierarchyIds.map((id) => {
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
            environmentId={content.environmentId}
            value={editSiteIds}
            onChange={setEditSiteIds}
          />
          {editTargetSiteIds.length > 0 && (
            <div className="edit-content-page__order-wrap">
              <label className="edit-content-page__order-label">URLs por sitio</label>
              {editTargetSiteIds.map((siteId) => {
                const site = sites.find((s) => s.id === siteId)
                return (
                  <div key={siteId}>
                    <label htmlFor={`edit-site-url-${siteId}`} className="edit-content-page__order-label">
                      {site?.name ?? siteId} {siteId === content.siteId ? '(owner)' : '(shared)'}
                    </label>
                    <input
                      id={`edit-site-url-${siteId}`}
                      type="text"
                      className="edit-content-page__order-input"
                      value={editSiteUrlMap[siteId] ?? ''}
                      onChange={(e) =>
                        setEditSiteUrlMap((prev) => ({ ...prev, [siteId]: normalizeSlugInput(e.target.value) }))
                      }
                      placeholder={normalizeSlugInput(editSlug || editTitle || content.slug)}
                    />
                  </div>
                )
              })}
            </div>
          )}
          <ContentBlocksSection
            projectId={currentProjectId}
            contentId={contentId}
            content={{ ...content, blocks: content.blocks ?? [] }}
            schema={schema}
            onContentUpdated={refreshContent}
          />
        </>
      )}

      {/* Sección publicación: solo Draft, con entorno y permiso */}
      {content.status === 'Draft' && currentEnvironmentId && (
        <div className="edit-content-page__section edit-content-page__section--publish">
          <h3 className="edit-content-page__section-title">Publicación</h3>
          {requestPublicationMessage && (
            <p className={`edit-content-page__message--${requestPublicationMessage.type === 'error' ? 'error' : 'success'}`}>
              {requestPublicationMessage.text}
            </p>
          )}
          <Can permission="content.publish">
            <button
              type="button"
              className="edit-content-page__button edit-content-page__btn-primary"
              onClick={handleRequestPublication}
              disabled={requestingPublication}
            >
              {requestingPublication ? 'Enviando…' : 'Solicitar publicación'}
            </button>
            <span className="edit-content-page__link">
              <Link to="/publish">Ver solicitudes</Link>
            </span>
          </Can>
        </div>
      )}

      {/* Sección reindexación: solo Published */}
      {content.status === 'Published' && content.publishedAt && (
        <div className="edit-content-page__section edit-content-page__section--reindex">
          <h3 className="edit-content-page__section-title">Indexación</h3>
          <p>
            Si el contenido no aparece en la búsqueda o necesitas actualizar el índice, puedes reindexarlo manualmente.
          </p>
          {reindexMessage && (
            <p className={reindexMessage.type === 'error' ? 'edit-content-page__message--error' : 'edit-content-page__message--success'}>
              {reindexMessage.text}
            </p>
          )}
          <button
            type="button"
            className="edit-content-page__button edit-content-page__btn-reindex"
            onClick={handleReindex}
            disabled={reindexing}
          >
            {reindexing ? 'Reindexando…' : 'Reindexar contenido'}
          </button>
        </div>
      )}

      {schema && (
        <DynamicForm
          projectId={currentProjectId!}
          schemaId={content.schemaId}
          defaultValues={defaultValues}
          onSubmit={handleSubmit}
          submitLabel="Guardar cambios"
        />
      )}

      {!schema && content.schemaId && <p className="edit-content-page__error">No se pudo cargar el schema del contenido.</p>}

      {/* Historial de versiones */}
      <div className="edit-content-page__versions">
        <div className="edit-content-page__versions-header">
          <h3 className="edit-content-page__versions-title">
            Historial de versiones (v{content.currentVersion})
          </h3>
          <button
            type="button"
            className="edit-content-page__button edit-content-page__versions-toggle"
            onClick={handleToggleHistory}
          >
            {showHistory ? 'Ocultar historial' : 'Ver historial'}
          </button>
        </div>

        {showHistory && (
          <div className="edit-content-page__versions-list">
            {versionsLoading && <p className="edit-content-page__versions-loading">Cargando versiones…</p>}
            {!versionsLoading && versions.length === 0 && (
              <p className="edit-content-page__versions-empty">No hay versiones registradas.</p>
            )}
            {!versionsLoading && versions.length > 0 && (
              <div className="edit-content-page__versions-stack">
                {versions.map((v) => {
                  const isCurrent = v.versionNumber === content.currentVersion
                  const isExpanded = expandedVersion === v.versionNumber
                  const statusClass = v.status === 'Published' ? 'edit-content-page__version-status--published' : v.status === 'Draft' ? 'edit-content-page__version-status--draft' : 'edit-content-page__version-status--pending'
                  return (
                    <div
                      key={v.id}
                      className={`edit-content-page__version-card ${isCurrent ? 'edit-content-page__version-card--current' : ''}`}
                    >
                      <div className="edit-content-page__version-row">
                        <div>
                          <strong className="edit-content-page__version-name">
                            v{v.versionNumber}
                            {isCurrent && <span className="edit-content-page__version-badge--current">(actual)</span>}
                          </strong>
                          <span className="edit-content-page__version-title">
                            {v.title}
                          </span>
                        </div>
                        <div className="edit-content-page__version-meta">
                          <span className={`edit-content-page__version-status ${statusClass}`}>
                            {v.status}
                          </span>
                          <span className="edit-content-page__version-date">
                            {new Date(v.createdAt).toLocaleString()}
                          </span>
                        </div>
                      </div>
                      {v.comment && (
                        <p className="edit-content-page__version-comment">
                          {v.comment}
                        </p>
                      )}
                      <div className="edit-content-page__version-actions">
                        <button
                          type="button"
                          className="edit-content-page__version-btn"
                          onClick={() => setExpandedVersion(isExpanded ? null : v.versionNumber)}
                        >
                          {isExpanded ? 'Ocultar campos' : 'Ver campos'}
                        </button>
                        {!isCurrent && (
                          <button
                            type="button"
                            className="edit-content-page__version-btn edit-content-page__version-btn--restore"
                            onClick={() => handleRestoreVersion(v)}
                          >
                            Restaurar esta versión
                          </button>
                        )}
                      </div>
                      {isExpanded && (
                        <div className="edit-content-page__version-fields">
                          <table className="edit-content-page__version-table">
                            <thead>
                              <tr>
                                <th className="edit-content-page__version-th">Campo</th>
                                <th className="edit-content-page__version-th">Valor</th>
                              </tr>
                            </thead>
                            <tbody>
                              {Object.entries(v.fields).map(([key, val]) => (
                                <tr key={key}>
                                  <td className="edit-content-page__version-td edit-content-page__version-td--key">{key}</td>
                                  <td className="edit-content-page__version-td edit-content-page__version-td--val">
                                    {typeof val === 'object' ? JSON.stringify(val) : String(val ?? '')}
                                  </td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </div>
                      )}
                    </div>
                  )
                })}
              </div>
            )}
          </div>
        )}
      </div>

      <div className="edit-content-page__danger-zone">
        {!deleteConfirm ? (
          <button
            type="button"
            className="edit-content-page__button edit-content-page__button--delete"
            onClick={() => setDeleteConfirm(true)}
          >
            Eliminar contenido
          </button>
        ) : (
          <div>
            <p>¿Eliminar este contenido? Esta acción no se puede deshacer.</p>
            <button
              type="button"
              className="edit-content-page__button edit-content-page__button--delete"
              onClick={handleDelete}
            >
              Sí, eliminar
            </button>
            <button type="button" className="edit-content-page__button" onClick={() => setDeleteConfirm(false)}>
              Cancelar
            </button>
          </div>
        )}
      </div>
    </div>
  )
}
