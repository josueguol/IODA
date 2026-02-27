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
import './CreateContentPage.css'

export function CreateContentPage() {
  const navigate = useNavigate()
  const { currentProjectId, currentEnvironmentId, currentSiteId } = useContextStore()
  const { schemaList, loadSchemas, getSchemaSync, listLoading, listError } = useSchemaStore()
  const [selectedSchemaId, setSelectedSchemaId] = useState<string>('')
  const [contentTitle, setContentTitle] = useState('')
  const [parentContentId, setParentContentId] = useState<string | null>(null)
  const [order, setOrder] = useState<string>('')
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
      const orderNum = order === '' ? undefined : parseInt(order, 10)
      await coreApi.createContent(currentProjectId, {
        environmentId: currentEnvironmentId,
        siteId: currentSiteId ?? undefined,
        parentContentId: parentContentId ?? undefined,
        schemaId: selectedSchemaId,
        title: contentTitle.trim(),
        contentType,
        fields,
        order: orderNum !== undefined && !Number.isNaN(orderNum) ? orderNum : undefined,
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
      <div className="create-content-page">
        <h1 className="create-content-page__title">Crear contenido</h1>
        <p className="create-content-page__hint">Selecciona un proyecto en la barra superior.</p>
      </div>
    )
  }

  if (!currentEnvironmentId) {
    return (
      <div className="create-content-page">
        <h1 className="create-content-page__title">Crear contenido</h1>
        <p className="create-content-page__hint">Selecciona un entorno en la barra superior para crear contenido.</p>
      </div>
    )
  }

  return (
    <div className="create-content-page">
      <h1 className="create-content-page__title">Crear contenido</h1>

      <div className="create-content-page__selector">
        <label htmlFor="schema-select">Schema</label>
        <br />
        <select
          id="schema-select"
          className="create-content-page__select"
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
        {listError && <p className="create-content-page__error">{listError}</p>}
      </div>

      {selectedSchemaId && currentProjectId && (
        <>
          <div>
            <label htmlFor="content-title">Título del contenido *</label>
            <br />
            <input
              id="content-title"
              type="text"
              className="create-content-page__input"
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
          <div className="create-content-page__selector">
            <label htmlFor="content-order">Orden (opcional)</label>
            <br />
            <input
              id="content-order"
              type="number"
              min={0}
              className="create-content-page__input"
              value={order}
              onChange={(e) => setOrder(e.target.value)}
              placeholder="Siguiente disponible si se deja vacío"
            />
            <p className="create-content-page__hint">Posición entre hermanos (hijos del mismo padre). 0-based.</p>
          </div>
          <TagsSelector projectId={currentProjectId} value={tagIds} onChange={setTagIds} />
          <HierarchySelector projectId={currentProjectId} value={hierarchyIds} onChange={setHierarchyIds} />
          <SiteSelector
            projectId={currentProjectId}
            environmentId={currentEnvironmentId}
            value={siteIds}
            onChange={setSiteIds}
          />

          {submitError && <p className="create-content-page__error">{submitError}</p>}

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
