import { useEffect, useState } from 'react'
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

const styles: Record<string, React.CSSProperties> = {
  container: { maxWidth: 640, color: 'var(--page-text)' },
  title: { marginTop: 0, marginBottom: '1rem', color: 'var(--page-text)' },
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem', flexWrap: 'wrap', gap: '0.5rem' },
  status: { fontSize: '0.875rem', color: 'var(--page-text-muted)' },
  button: { padding: '0.5rem 1rem', fontSize: '0.875rem', cursor: 'pointer', borderRadius: 4, border: 'none' },
  deleteBtn: { background: '#dc3545', color: 'white' },
  error: { color: '#dc3545', marginBottom: '0.5rem' },
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
  const [editParentContentId, setEditParentContentId] = useState<string | null>(null)
  const [editTagIds, setEditTagIds] = useState<string[]>([])
  const [editHierarchyIds, setEditHierarchyIds] = useState<string[]>([])
  const [editSiteIds, setEditSiteIds] = useState<string[]>([])
  const [requestingPublication, setRequestingPublication] = useState(false)
  const [requestPublicationMessage, setRequestPublicationMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null)
  const [reindexing, setReindexing] = useState(false)
  const [reindexMessage, setReindexMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null)
  const [versions, setVersions] = useState<ContentVersion[]>([])
  const [versionsLoading, setVersionsLoading] = useState(false)
  const [showHistory, setShowHistory] = useState(false)
  const [expandedVersion, setExpandedVersion] = useState<number | null>(null)

  const { currentEnvironmentId } = useContextStore()

  useEffect(() => {
    if (!currentProjectId || !contentId) {
      setLoading(false)
      return
    }
    setLoading(true)
    coreApi
      .getContent(currentProjectId, contentId)
      .then((data) => {
        setContent(data ?? null)
        setEditTitle(data?.title ?? '')
        setEditParentContentId(data?.parentContentId ?? null)
        setEditTagIds(data?.tagIds ?? [])
        setEditHierarchyIds(data?.hierarchyIds ?? [])
        setEditSiteIds(data?.siteIds ?? [])
        if (data?.schemaId) {
          loadSchema(currentProjectId, data.schemaId).catch(() => {})
        }
      })
      .catch(() => setError('Contenido no encontrado'))
      .finally(() => setLoading(false))
  }, [currentProjectId, contentId, loadSchema])

  const schema = content && currentProjectId ? getSchemaSync(currentProjectId, content.schemaId) : null

  const defaultValues: DynamicFormValues | undefined = content?.fields
    ? Object.fromEntries(
        Object.entries(content.fields).map(([k, v]) => [k, v as string | number | boolean | null | undefined])
      )
    : undefined

  const handleSubmit: DynamicFormProps['onSubmit'] = async (values) => {
    if (!currentProjectId || !contentId || !content) return
    setSubmitError(null)
    try {
      await coreApi.updateContent(currentProjectId, contentId, {
        title: editTitle.trim() || content.title,
        fields: values as Record<string, unknown>,
        parentContentId: editParentContentId ?? undefined,
        tagIds: editTagIds,
        hierarchyIds: editHierarchyIds,
        siteIds: editSiteIds,
      })
      const updated = await coreApi.getContent(currentProjectId, contentId)
      if (updated) {
        setContent(updated)
        setEditTitle(updated.title)
        setEditParentContentId(updated.parentContentId ?? null)
        setEditTagIds(updated.tagIds ?? [])
        setEditHierarchyIds(updated.hierarchyIds ?? [])
        setEditSiteIds(updated.siteIds ?? [])
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
        setEditParentContentId(updated.parentContentId ?? null)
        setEditTagIds(updated.tagIds ?? [])
        setEditHierarchyIds(updated.hierarchyIds ?? [])
        setEditSiteIds(updated.siteIds ?? [])
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
    return <p style={styles.error}>Cargando…</p>
  }
  if (error || !content) {
    return (
      <div style={styles.container}>
        <p style={styles.error}>{error ?? 'Contenido no encontrado'}</p>
        <button type="button" style={styles.button} onClick={() => navigate('/content')}>
          Volver al listado
        </button>
      </div>
    )
  }

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <div>
          <label htmlFor="edit-title" style={{ fontSize: '0.75rem', color: '#666' }}>Título</label>
          <input
            id="edit-title"
            type="text"
            value={editTitle}
            onChange={(e) => setEditTitle(e.target.value)}
            style={{ width: '100%', maxWidth: 400, padding: '0.5rem', fontSize: '1rem', fontWeight: 600, border: '1px solid #ccc', borderRadius: 4 }}
          />
        </div>
        <span style={styles.status}>
          Estado: {content.status} · v{content.currentVersion}
        </span>
      </div>

      {/* Metadata y auditoría (Fase 2): no se envían en create/update; se muestran solo lectura */}
      <div style={{ marginBottom: '1rem', fontSize: '0.8125rem', color: 'var(--page-text-muted)' }}>
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

      {submitError && <p style={styles.error}>{submitError}</p>}

      {currentProjectId && contentId && (
        <>
          <ParentContentSelector
            projectId={currentProjectId}
            value={editParentContentId}
            onChange={setEditParentContentId}
            excludeContentId={contentId}
          />
          <TagsSelector projectId={currentProjectId} value={editTagIds} onChange={setEditTagIds} />
          <HierarchySelector projectId={currentProjectId} value={editHierarchyIds} onChange={setEditHierarchyIds} />
          <SiteSelector
            projectId={currentProjectId}
            environmentId={content.environmentId}
            value={editSiteIds}
            onChange={setEditSiteIds}
          />
        </>
      )}

      {/* Sección publicación: solo Draft, con entorno y permiso */}
      {content.status === 'Draft' && currentEnvironmentId && (
        <div style={{ marginBottom: '1.5rem', padding: '1rem', background: 'var(--page-bg-elevated)', borderRadius: 6, color: 'var(--page-text)' }}>
          <h3 style={{ margin: '0 0 0.5rem 0', fontSize: '1rem' }}>Publicación</h3>
          {requestPublicationMessage && (
            <p style={{ color: requestPublicationMessage.type === 'error' ? '#dc3545' : '#0d6efd', fontSize: '0.875rem', marginBottom: '0.5rem' }}>
              {requestPublicationMessage.text}
            </p>
          )}
          <Can permission="content.publish">
            <button
              type="button"
              style={{ ...styles.button, background: '#0d6efd', color: 'white' }}
              onClick={handleRequestPublication}
              disabled={requestingPublication}
            >
              {requestingPublication ? 'Enviando…' : 'Solicitar publicación'}
            </button>
            <span style={{ marginLeft: '0.5rem', fontSize: '0.875rem', color: '#666' }}>
              <Link to="/publish" style={{ color: '#0d6efd', textDecoration: 'none' }}>Ver solicitudes</Link>
            </span>
          </Can>
        </div>
      )}

      {/* Sección reindexación: solo Published */}
      {content.status === 'Published' && content.publishedAt && (
        <div style={{ marginBottom: '1.5rem', padding: '1rem', background: '#fff3cd', borderRadius: 6, border: '1px solid #ffc107' }}>
          <h3 style={{ margin: '0 0 0.5rem 0', fontSize: '1rem' }}>Indexación</h3>
          <p style={{ fontSize: '0.875rem', color: '#666', marginBottom: '0.5rem' }}>
            Si el contenido no aparece en la búsqueda o necesitas actualizar el índice, puedes reindexarlo manualmente.
          </p>
          {reindexMessage && (
            <p style={{ color: reindexMessage.type === 'error' ? '#dc3545' : '#0d6efd', fontSize: '0.875rem', marginBottom: '0.5rem' }}>
              {reindexMessage.text}
            </p>
          )}
          <button
            type="button"
            style={{ ...styles.button, background: '#ffc107', color: '#000' }}
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

      {!schema && content.schemaId && <p style={styles.error}>No se pudo cargar el schema del contenido.</p>}

      {/* Historial de versiones */}
      <div style={{ marginTop: '2rem', padding: '1rem', background: 'var(--page-bg-elevated)', borderRadius: 6, border: '1px solid var(--page-border, #ddd)' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h3 style={{ margin: 0, fontSize: '1rem', color: 'var(--page-text)' }}>
            Historial de versiones (v{content.currentVersion})
          </h3>
          <button
            type="button"
            style={{ ...styles.button, background: 'var(--input-bg, #f8f9fa)', color: 'var(--page-text)', border: '1px solid var(--input-border, #ccc)' }}
            onClick={handleToggleHistory}
          >
            {showHistory ? 'Ocultar historial' : 'Ver historial'}
          </button>
        </div>

        {showHistory && (
          <div style={{ marginTop: '1rem' }}>
            {versionsLoading && <p style={{ fontSize: '0.875rem', color: 'var(--page-text-muted)' }}>Cargando versiones…</p>}
            {!versionsLoading && versions.length === 0 && (
              <p style={{ fontSize: '0.875rem', color: 'var(--page-text-muted)' }}>No hay versiones registradas.</p>
            )}
            {!versionsLoading && versions.length > 0 && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                {versions.map((v) => {
                  const isCurrent = v.versionNumber === content.currentVersion
                  const isExpanded = expandedVersion === v.versionNumber
                  return (
                    <div
                      key={v.id}
                      style={{
                        padding: '0.75rem',
                        borderRadius: 4,
                        border: isCurrent ? '2px solid #0d6efd' : '1px solid var(--page-border, #ddd)',
                        background: isCurrent ? 'rgba(13,110,253,0.05)' : 'var(--input-bg, #fff)',
                      }}
                    >
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '0.5rem' }}>
                        <div>
                          <strong style={{ fontSize: '0.875rem', color: 'var(--page-text)' }}>
                            v{v.versionNumber}
                            {isCurrent && <span style={{ marginLeft: '0.5rem', color: '#0d6efd', fontWeight: 400, fontSize: '0.75rem' }}>(actual)</span>}
                          </strong>
                          <span style={{ marginLeft: '0.75rem', fontSize: '0.8125rem', color: 'var(--page-text-muted)' }}>
                            {v.title}
                          </span>
                        </div>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                          <span style={{
                            fontSize: '0.75rem',
                            padding: '0.15rem 0.5rem',
                            borderRadius: 10,
                            background: v.status === 'Published' ? '#198754' : v.status === 'Draft' ? '#6c757d' : '#ffc107',
                            color: v.status === 'Published' ? '#fff' : v.status === 'Draft' ? '#fff' : '#000',
                          }}>
                            {v.status}
                          </span>
                          <span style={{ fontSize: '0.75rem', color: 'var(--page-text-muted)' }}>
                            {new Date(v.createdAt).toLocaleString()}
                          </span>
                        </div>
                      </div>
                      {v.comment && (
                        <p style={{ margin: '0.25rem 0 0', fontSize: '0.8125rem', color: 'var(--page-text-muted)', fontStyle: 'italic' }}>
                          {v.comment}
                        </p>
                      )}
                      <div style={{ marginTop: '0.5rem', display: 'flex', gap: '0.5rem' }}>
                        <button
                          type="button"
                          style={{ ...styles.button, padding: '0.25rem 0.5rem', fontSize: '0.75rem', background: 'var(--input-bg, #f8f9fa)', border: '1px solid var(--input-border, #ccc)', color: 'var(--page-text)' }}
                          onClick={() => setExpandedVersion(isExpanded ? null : v.versionNumber)}
                        >
                          {isExpanded ? 'Ocultar campos' : 'Ver campos'}
                        </button>
                        {!isCurrent && (
                          <button
                            type="button"
                            style={{ ...styles.button, padding: '0.25rem 0.5rem', fontSize: '0.75rem', background: '#0d6efd', color: 'white' }}
                            onClick={() => handleRestoreVersion(v)}
                          >
                            Restaurar esta versión
                          </button>
                        )}
                      </div>
                      {isExpanded && (
                        <div style={{ marginTop: '0.5rem', padding: '0.5rem', background: 'var(--page-bg, #f5f5f5)', borderRadius: 4, fontSize: '0.8125rem', maxHeight: 300, overflow: 'auto' }}>
                          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                            <thead>
                              <tr>
                                <th style={{ textAlign: 'left', padding: '0.25rem 0.5rem', borderBottom: '1px solid var(--page-border, #ddd)', color: 'var(--page-text)' }}>Campo</th>
                                <th style={{ textAlign: 'left', padding: '0.25rem 0.5rem', borderBottom: '1px solid var(--page-border, #ddd)', color: 'var(--page-text)' }}>Valor</th>
                              </tr>
                            </thead>
                            <tbody>
                              {Object.entries(v.fields).map(([key, val]) => (
                                <tr key={key}>
                                  <td style={{ padding: '0.25rem 0.5rem', borderBottom: '1px solid var(--page-border, #eee)', fontWeight: 600, color: 'var(--page-text)' }}>{key}</td>
                                  <td style={{ padding: '0.25rem 0.5rem', borderBottom: '1px solid var(--page-border, #eee)', color: 'var(--page-text)', wordBreak: 'break-word' }}>
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

      <div style={{ marginTop: '2rem', paddingTop: '1rem', borderTop: '1px solid #eee' }}>
        {!deleteConfirm ? (
          <button
            type="button"
            style={{ ...styles.button, ...styles.deleteBtn }}
            onClick={() => setDeleteConfirm(true)}
          >
            Eliminar contenido
          </button>
        ) : (
          <div>
            <p>¿Eliminar este contenido? Esta acción no se puede deshacer.</p>
            <button
              type="button"
              style={{ ...styles.button, ...styles.deleteBtn, marginRight: '0.5rem' }}
              onClick={handleDelete}
            >
              Sí, eliminar
            </button>
            <button type="button" style={styles.button} onClick={() => setDeleteConfirm(false)}>
              Cancelar
            </button>
          </div>
        )}
      </div>
    </div>
  )
}
