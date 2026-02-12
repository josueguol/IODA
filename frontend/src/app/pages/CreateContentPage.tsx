import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { coreApi } from '../../modules/core/api/core-api'
import { useContextStore } from '../../modules/core/store/context-store'
import { useAuthStore } from '../../modules/auth/store/auth-store'
import { useSchemaStore } from '../../modules/schema/store/schema-store'
import { DynamicForm, type DynamicFormProps } from '../../modules/schema/components/DynamicForm'

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
  const user = useAuthStore((s) => s.user)
  const { schemaList, loadSchemas, getSchemaSync, listLoading, listError } = useSchemaStore()
  const [selectedSchemaId, setSelectedSchemaId] = useState<string>('')
  const [contentTitle, setContentTitle] = useState('')
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
    const createdBy = user?.userId
    if (!createdBy) {
      setSubmitError('Usuario no identificado.')
      return
    }
    setSubmitError(null)
    try {
      const fields = values as Record<string, unknown>
      await coreApi.createContent(currentProjectId, {
        environmentId: currentEnvironmentId,
        siteId: currentSiteId ?? undefined,
        schemaId: selectedSchemaId,
        title: contentTitle.trim(),
        contentType,
        fields,
        createdBy,
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

      {selectedSchemaId && (
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
