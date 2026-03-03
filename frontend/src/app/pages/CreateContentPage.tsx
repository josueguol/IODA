import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { coreApi } from '../../modules/core/api/core-api'
import { useContextStore } from '../../modules/core/store/context-store'
import { useSchemaStore } from '../../modules/schema/store/schema-store'
import { DynamicForm, type DynamicFormProps } from '../../modules/schema/components/DynamicForm'
import { TagsSelector } from '../../modules/core/components/TagsSelector'
import { HierarchySelector } from '../../modules/core/components/HierarchySelector'
import { SiteSelector } from '../../modules/core/components/SiteSelector'
import { ContentEditorSplitLayout } from '../components/ContentEditorSplitLayout'
import type { Hierarchy, Site } from '../../modules/core/types'
import './CreateContentPage.css'

function normalizeSlugInput(value: string): string {
  return value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .trim()
    .replace(/\s+/g, '-')
    .replace(/[^a-z0-9_-]/g, '')
}

export function CreateContentPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const schemaIdFromQuery = searchParams.get('schemaId') ?? ''
  const { currentProjectId, currentEnvironmentId, currentSiteId } = useContextStore()
  const { schemaList, loadSchemas, getSchemaSync, loadSchema, listLoading, listError } = useSchemaStore()

  const [selectedSchemaId, setSelectedSchemaId] = useState<string>('')
  const [contentTitle, setContentTitle] = useState('')
  const [contentSlug, setContentSlug] = useState('')
  const [isSlugManual, setIsSlugManual] = useState(false)
  const [tagIds, setTagIds] = useState<string[]>([])
  const [hierarchyIds, setHierarchyIds] = useState<string[]>([])
  const [primaryHierarchyId, setPrimaryHierarchyId] = useState<string>('')
  const [siteIds, setSiteIds] = useState<string[]>([])
  const [siteUrlMap, setSiteUrlMap] = useState<Record<string, string>>({})
  const [hierarchies, setHierarchies] = useState<Hierarchy[]>([])
  const [sites, setSites] = useState<Site[]>([])
  const [submitError, setSubmitError] = useState<string | null>(null)

  useEffect(() => {
    if (currentProjectId) {
      loadSchemas(currentProjectId).catch(() => {})
      coreApi.getHierarchies(currentProjectId).then(setHierarchies).catch(() => setHierarchies([]))
    }
  }, [currentProjectId, loadSchemas])

  useEffect(() => {
    if (!currentProjectId || !currentEnvironmentId) return
    coreApi.getSites(currentProjectId, currentEnvironmentId).then(setSites).catch(() => setSites([]))
  }, [currentProjectId, currentEnvironmentId])

  useEffect(() => {
    if (!currentProjectId || !selectedSchemaId) return
    loadSchema(currentProjectId, selectedSchemaId).catch(() => {})
  }, [currentProjectId, selectedSchemaId, loadSchema])

  useEffect(() => {
    if (!schemaIdFromQuery || schemaList.length === 0) return
    if (selectedSchemaId) return
    if (schemaList.some((s) => s.id === schemaIdFromQuery)) {
      setSelectedSchemaId(schemaIdFromQuery)
    }
  }, [schemaIdFromQuery, schemaList, selectedSchemaId])

  useEffect(() => {
    if (primaryHierarchyId && !hierarchyIds.includes(primaryHierarchyId)) {
      setPrimaryHierarchyId('')
    }
  }, [hierarchyIds, primaryHierarchyId])

  const schema = selectedSchemaId && currentProjectId ? getSchemaSync(currentProjectId, selectedSchemaId) : null

  const targetSiteIds = useMemo(() => {
    const ids = new Set<string>()
    if (currentSiteId) ids.add(currentSiteId)
    for (const id of siteIds) ids.add(id)
    return Array.from(ids)
  }, [currentSiteId, siteIds])

  const handleSchemaChange = (schemaId: string) => {
    setSelectedSchemaId(schemaId)
    setSubmitError(null)
  }

  const handleSubmit: DynamicFormProps['onSubmit'] = async (values) => {
    if (!currentProjectId || !currentEnvironmentId || !selectedSchemaId || !contentTitle.trim() || !contentSlug.trim()) {
      setSubmitError('Proyecto, entorno, schema, título y slug son obligatorios.')
      return
    }
    setSubmitError(null)
    try {
      await coreApi.createContent(currentProjectId, {
        environmentId: currentEnvironmentId,
        siteId: currentSiteId ?? undefined,
        schemaId: selectedSchemaId,
        title: contentTitle.trim(),
        slug: normalizeSlugInput(contentSlug),
        contentType: schema?.schemaType ?? '',
        fields: values as Record<string, unknown>,
        tagIds: tagIds.length > 0 ? tagIds : undefined,
        hierarchyIds: hierarchyIds.length > 0 ? hierarchyIds : undefined,
        primaryHierarchyId: primaryHierarchyId || undefined,
        siteIds: siteIds.length > 0 ? siteIds : undefined,
        siteUrls:
          targetSiteIds.length > 0
            ? targetSiteIds
                .map((siteId) => ({
                  siteId,
                  path: normalizeSlugInput(siteUrlMap[siteId] || contentSlug || contentTitle),
                }))
                .filter((x) => x.path.length > 0)
            : undefined,
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
      <div className="create-content-page__header">
        <h1 className="create-content-page__title">Nuevo contenido</h1>
        <Link to="/content" className="create-content-page__back-link">
          Volver al listado
        </Link>
      </div>

      {listError && <p className="create-content-page__error">{listError}</p>}
      {!listLoading && schemaList.length === 0 && (
        <p className="create-content-page__hint">
          No hay schemas disponibles. Crea uno en{' '}
          <Link to="/admin/schemas" className="create-content-page__inline-link">
            /admin/schemas
          </Link>
          .
        </p>
      )}

      <ContentEditorSplitLayout
        left={
          <>
          <h2 className="create-content-page__section-title">Campos del contenido</h2>

          <div>
            <label htmlFor="content-title" className="create-content-page__label">
              Título del contenido *
            </label>
            <input
              id="content-title"
              type="text"
              className="create-content-page__input"
              value={contentTitle}
              onChange={(e) => {
                const nextTitle = e.target.value
                setContentTitle(nextTitle)
                if (!isSlugManual) {
                  setContentSlug(normalizeSlugInput(nextTitle))
                }
              }}
              placeholder="Ej. Mi primer artículo"
              autoFocus
            />
          </div>

          <div>
            <label htmlFor="content-slug" className="create-content-page__label">
              Slug (URI) *
            </label>
            <input
              id="content-slug"
              type="text"
              className="create-content-page__input"
              value={contentSlug}
              onChange={(e) => {
                const normalized = normalizeSlugInput(e.target.value)
                setContentSlug(normalized)
                setIsSlugManual(normalized.length > 0)
              }}
              placeholder="mi-contenido"
            />
          </div>

          {selectedSchemaId && (
            <div className="create-content-page__dynamic-form">
              <DynamicForm
                projectId={currentProjectId}
                schemaId={selectedSchemaId}
                onSubmit={handleSubmit}
                submitLabel="Crear contenido"
              />
            </div>
          )}
          </>
        }
        right={
          <>
          <h2 className="create-content-page__section-title">Propiedades</h2>

          <div>
            <label htmlFor="schema-select" className="create-content-page__label">
              Schema *
            </label>
            <select
              id="schema-select"
              className="create-content-page__select"
              value={selectedSchemaId}
              onChange={(e) => handleSchemaChange(e.target.value)}
              disabled={listLoading}
            >
              <option value="">— Elegir schema —</option>
              {schemaList.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.schemaName} ({s.schemaType})
                </option>
              ))}
            </select>
          </div>

          <TagsSelector projectId={currentProjectId} value={tagIds} onChange={setTagIds} />
          <HierarchySelector projectId={currentProjectId} value={hierarchyIds} onChange={setHierarchyIds} />

          {hierarchyIds.length > 0 && (
            <div>
              <label htmlFor="content-primary-section" className="create-content-page__label">
                Sección principal (opcional)
              </label>
              <select
                id="content-primary-section"
                className="create-content-page__select"
                value={primaryHierarchyId}
                onChange={(e) => setPrimaryHierarchyId(e.target.value)}
              >
                <option value="">Sin sección principal</option>
                {hierarchyIds.map((id) => {
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
            value={siteIds}
            onChange={setSiteIds}
          />

          {targetSiteIds.length > 0 && (
            <div>
              <p className="create-content-page__label">URLs por sitio (opcional)</p>
              {targetSiteIds.map((siteId) => {
                const site = sites.find((s) => s.id === siteId)
                return (
                  <div key={siteId} className="create-content-page__site-url-item">
                    <label htmlFor={`site-url-${siteId}`} className="create-content-page__label">
                      {site?.name ?? siteId} {siteId === currentSiteId ? '(owner)' : '(shared)'}
                    </label>
                    <input
                      id={`site-url-${siteId}`}
                      type="text"
                      className="create-content-page__input"
                      value={siteUrlMap[siteId] ?? ''}
                      onChange={(e) =>
                        setSiteUrlMap((prev) => ({ ...prev, [siteId]: normalizeSlugInput(e.target.value) }))
                      }
                      placeholder={normalizeSlugInput(contentSlug || contentTitle || 'contenido')}
                    />
                  </div>
                )
              })}
            </div>
          )}

          {submitError && <p className="create-content-page__error">{submitError}</p>}
          </>
        }
      />
    </div>
  )
}
