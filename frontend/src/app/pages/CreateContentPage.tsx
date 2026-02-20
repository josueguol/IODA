import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { coreApi } from '../../modules/core/api/core-api'
import { useContextStore } from '../../modules/core/store/context-store'
import { useSchemaStore } from '../../modules/schema/store/schema-store'
import { DynamicForm, type DynamicFormProps } from '../../modules/schema/components/DynamicForm'
import { ParentContentSelector } from '../../modules/core/components/ParentContentSelector'
import { TagsSelector } from '../../modules/core/components/TagsSelector'
import { HierarchySelector } from '../../modules/core/components/HierarchySelector'
import { SiteSelector } from '../../modules/core/components/SiteSelector'

const styles: Record<string, React.CSSProperties> = {
  container: { maxWidth: 640, color: 'var(--page-text)' },
  title: { marginTop: 0, marginBottom: '1rem', color: 'var(--page-text)', fontSize: '1.5rem' },
  selector: { marginBottom: '1.5rem' },
  select: { padding: '0.5rem', fontSize: '0.875rem', minWidth: 220, borderRadius: 4, border: '1px solid var(--input-border)', color: 'var(--input-text)', background: 'var(--input-bg)' },
  hint: { color: 'var(--page-text-muted)', fontSize: '0.875rem', marginTop: '0.5rem' },
  input: { width: '100%', maxWidth: 400, padding: '0.5rem', fontSize: '0.875rem', borderRadius: 4, border: '1px solid var(--input-border)', color: 'var(--input-text)', background: 'var(--input-bg)', marginBottom: '1rem' },
  error: { color: '#dc3545', marginBottom: '0.5rem' },
}

export function CreateContentPage() {
  const navigate = useNavigate()
  const { currentProjectId, currentEnvironmentId, currentSiteId } = useContextStore()
  const { schemaList, loadSchemas, getSchemaSync, listLoading, listError } = useSchemaStore()
  const [selectedSchemaId, setSelectedSchemaId] = useState<string>('')
  const [contentTitle, setContentTitle] = useState('')
  const [parentContentId, setParentContentId] = useState<string | null>(null)
  const [tagIds, setTagIds] = useState<string[]>([])
  const [hierarchyIds, setHierarchyIds] = useState<string[]>([])
  const [siteIds, setSiteIds] = useState<string[]>([])
  const [submitError, setSubmitError] = useState<string | null>(null)

  useEffect(() => {
    if (currentProjectId) {
      loadSchemas(currentProjectId).catch(() => {})
    }
  }, [currentProjectId, loadSchemas])

  const schema = selectedSchemaId && currentProjectId ? getSchemaSync(currentProjectId, selectedSchemaId) : null
  const contentType = schema?.schemaType ?? ''

  const handleSubmit: DynamicFormProps['onSubmit'] = async (values) => {
    if (!currentProjectId || !currentEnvironmentId || !selectedSchemaId || !contentTitle.trim()) {
      setSubmitError('Proyecto, entorno, schema y título son obligatorios.')
      return
    }
    setSubmitError(null)
    try {
      const fields = values as Record<string, unknown>
      await coreApi.createContent(currentProjectId, {
        environmentId: currentEnvironmentId,
        siteId: currentSiteId ?? undefined,
        parentContentId: parentContentId ?? undefined,
        schemaId: selectedSchemaId,
        title: contentTitle.trim(),
        contentType,
        fields,
        tagIds: tagIds.length > 0 ? tagIds : undefined,
        hierarchyIds: hierarchyIds.length > 0 ? hierarchyIds : undefined,
        siteIds: siteIds.length > 0 ? siteIds : undefined,
      })
      navigate('/content', { replace: true })
    } catch (e) {
      setSubmitError(e instanceof Error ? e.message : 'Error al crear contenido')
    }
  }

  if (!currentProjectId) {
    return (
      <div style={styles.container}>
        <h1 style={styles.title}>Crear contenido</h1>
        <p style={styles.hint}>Selecciona un proyecto en la barra superior.</p>
      </div>
    )
  }

  if (!currentEnvironmentId) {
    return (
      <div style={styles.container}>
        <h1 style={styles.title}>Crear contenido</h1>
        <p style={styles.hint}>Selecciona un entorno en la barra superior para crear contenido.</p>
      </div>
    )
  }

  return (
    <div style={styles.container}>
      <h1 style={styles.title}>Crear contenido</h1>

      <div style={styles.selector}>
        <label htmlFor="schema-select">Schema</label>
        <br />
        <select
          id="schema-select"
          style={styles.select}
          value={selectedSchemaId}
          onChange={(e) => setSelectedSchemaId(e.target.value)}
          disabled={listLoading}
        >
          <option value="">— Elegir schema —</option>
          {schemaList.map((s) => (
            <option key={s.id} value={s.id}>
              {s.schemaName} ({s.schemaType})
            </option>
          ))}
        </select>
        {listError && <p style={styles.error}>{listError}</p>}
      </div>

      {selectedSchemaId && currentProjectId && (
        <>
          <div>
            <label htmlFor="content-title">Título del contenido *</label>
            <br />
            <input
              id="content-title"
              type="text"
              style={styles.input}
              value={contentTitle}
              onChange={(e) => setContentTitle(e.target.value)}
              placeholder="Ej. Mi primer artículo"
            />
          </div>

          <ParentContentSelector
            projectId={currentProjectId}
            value={parentContentId}
            onChange={setParentContentId}
          />
          <TagsSelector projectId={currentProjectId} value={tagIds} onChange={setTagIds} />
          <HierarchySelector projectId={currentProjectId} value={hierarchyIds} onChange={setHierarchyIds} />
          <SiteSelector
            projectId={currentProjectId}
            environmentId={currentEnvironmentId}
            value={siteIds}
            onChange={setSiteIds}
          />

          {submitError && <p style={styles.error}>{submitError}</p>}

          <DynamicForm
            projectId={currentProjectId}
            schemaId={selectedSchemaId}
            onSubmit={handleSubmit}
            submitLabel="Crear contenido"
          />
        </>
      )}
    </div>
  )
}
