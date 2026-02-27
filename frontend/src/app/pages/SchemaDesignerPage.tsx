import { useState, useEffect, useMemo, useRef } from 'react'
import { coreApi } from '../../modules/core/api/core-api'
import { useContextStore } from '../../modules/core/store/context-store'
import { useAuthStore } from '../../modules/auth/store/auth-store'
import { useSchemaStore } from '../../modules/schema/store/schema-store'
import { ErrorBanner } from '../../shared/components'
import type { CreateSchemaFieldDto, ContentSchemaListItem, ContentSchema, FieldDefinition, AllowedBlockTypeRule } from '../../modules/core/types'
import './SchemaDesignerPage.css'

const ALLOWED_BLOCK_TYPES_OPTIONS = [
  { value: 'hero', label: 'Hero' },
  { value: 'text', label: 'Texto' },
  { value: 'image', label: 'Imagen' },
] as const

const FIELD_TYPES = [
  'string',
  'richtext',
  'number',
  'boolean',
  'date',
  'datetime',
  'enum',
  'json',
  'list',
  'reference',
  'media',
] as const

function labelToSlug(label: string): string {
  return label
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
}

const SLUG_REGEX = /^[a-z0-9]+(-[a-z0-9]+)*$/
function isValidSlug(slug: string): boolean {
  return slug.length > 0 && SLUG_REGEX.test(slug)
}

interface FieldEditor extends CreateSchemaFieldDto {
  _key: string
}

function nextKey() {
  return `f-${Date.now()}-${Math.random().toString(36).slice(2, 9)}`
}

export function SchemaDesignerPage() {
  const { currentProjectId } = useContextStore()
  const user = useAuthStore((s) => s.user)
  const { loadSchemas, getSchemaListSync } = useSchemaStore()

  const [schemaName, setSchemaName] = useState('')
  const [schemaType, setSchemaType] = useState('')
  const [description, setDescription] = useState('')
  const [parentSchemaId, setParentSchemaId] = useState<string | null>(null)
  const [fields, setFields] = useState<FieldEditor[]>([])
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [createdId, setCreatedId] = useState<string | null>(null)
  const [availableSchemas, setAvailableSchemas] = useState<ContentSchemaListItem[]>([])
  const [parentSchemaDetail, setParentSchemaDetail] = useState<ContentSchema | null>(null)
  const [allowedBlockTypes, setAllowedBlockTypes] = useState<AllowedBlockTypeRule[]>([])
  const successRef = useRef<HTMLParagraphElement>(null)

  useEffect(() => {
    if (createdId && successRef.current) {
      successRef.current.scrollIntoView({ behavior: 'smooth', block: 'nearest' })
    }
  }, [createdId])

  useEffect(() => {
    if (!currentProjectId) return
    coreApi.getSchemas(currentProjectId, true).then(setAvailableSchemas).catch(() => {})
  }, [currentProjectId])

  useEffect(() => {
    if (!currentProjectId || !parentSchemaId) {
      setParentSchemaDetail(null)
      return
    }
    coreApi.getSchema(currentProjectId, parentSchemaId).then(setParentSchemaDetail).catch(() => setParentSchemaDetail(null))
  }, [currentProjectId, parentSchemaId])

  const inheritedFields = useMemo(() => {
    if (!parentSchemaDetail) return []
    const own = parentSchemaDetail.fields ?? []
    const inherited = parentSchemaDetail.inheritedFields ?? []
    return [...inherited, ...own]
  }, [parentSchemaDetail])

  const addField = () => {
    setFields((prev) => [
      ...prev,
      {
        _key: nextKey(),
        label: '',
        slug: '',
        fieldType: 'string',
        isRequired: false,
        defaultValue: undefined,
        helpText: null,
        validationRules: null,
        displayOrder: prev.length,
      },
    ])
  }

  const loadDefaultFields = () => {
    if (!currentProjectId) return
    coreApi
      .getDefaultSchemaFields(currentProjectId)
      .then((list) => {
        setFields(
          list.map((s, i) => ({
            _key: nextKey(),
            label: s.label,
            slug: s.slug,
            fieldType: s.fieldType,
            isRequired: false,
            defaultValue: undefined,
            helpText: null,
            validationRules: null,
            displayOrder: i,
          }))
        )
      })
      .catch(() => {})
  }

  const removeField = (key: string) => {
    setFields((prev) => prev.filter((f) => f._key !== key).map((f, i) => ({ ...f, displayOrder: i })))
  }

  const moveField = (key: string, dir: 'up' | 'down') => {
    setFields((prev) => {
      const i = prev.findIndex((f) => f._key === key)
      if (i < 0) return prev
      if (dir === 'up' && i === 0) return prev
      if (dir === 'down' && i === prev.length - 1) return prev
      const next = [...prev]
      const j = dir === 'up' ? i - 1 : i + 1
      ;[next[i], next[j]] = [next[j], next[i]]
      return next.map((f, idx) => ({ ...f, displayOrder: idx }))
    })
  }

  const updateField = (key: string, patch: Partial<FieldEditor>) => {
    setFields((prev) => prev.map((f) => (f._key === key ? { ...f, ...patch } : f)))
  }

  const handleCreate = async () => {
    setError(null)
    if (!currentProjectId || !user?.userId) {
      setError('Selecciona un proyecto y asegúrate de estar autenticado.')
      return
    }
    if (!schemaName.trim()) {
      setError('El nombre del schema es obligatorio.')
      return
    }
    if (!schemaType.trim()) {
      setError('El tipo de schema es obligatorio (ej. article, page).')
      return
    }
    const validFields = fields.filter((f) => f.label.trim() && f.slug.trim())
    if (validFields.length === 0) {
      setError('Añade al menos un campo con etiqueta y slug.')
      return
    }
    const slugs = validFields.map((f) => f.slug.trim().toLowerCase())
    if (new Set(slugs).size !== slugs.length) {
      setError('Los slugs de campo no pueden repetirse.')
      return
    }
    for (const f of validFields) {
      if (!isValidSlug(f.slug.trim())) {
        setError(`El slug «${f.slug}» no es válido. Debe ser kebab-case (ej. descripcion-corta).`)
        return
      }
    }

    const payload: CreateSchemaFieldDto[] = validFields.map((f, i) => ({
      label: f.label.trim(),
      slug: f.slug.trim(),
      fieldType: f.fieldType,
      isRequired: f.isRequired ?? false,
      defaultValue: f.defaultValue ?? undefined,
      helpText: f.helpText?.trim() || null,
      validationRules: f.validationRules ?? null,
      displayOrder: i,
    }))

    const allowedBlocksPayload =
      allowedBlockTypes.length > 0
        ? allowedBlockTypes.map((r) => ({
            blockType: r.blockType,
            minOccurrences: r.minOccurrences ?? null,
            maxOccurrences: r.maxOccurrences ?? null,
          }))
        : null

    setSaving(true)
    try {
      const id = await coreApi.createSchema(currentProjectId, {
        schemaName: schemaName.trim(),
        schemaType: schemaType.trim().toLowerCase().replace(/\s+/g, '-'),
        description: description.trim() || null,
        fields: payload,
        createdBy: user.userId,
        parentSchemaId: parentSchemaId || null,
        allowedBlockTypes: allowedBlocksPayload,
      })
      setCreatedId(id ?? null)
      await loadSchemas(currentProjectId)
      setAvailableSchemas(getSchemaListSync(currentProjectId))
      setSchemaName('')
      setSchemaType('')
      setDescription('')
      setParentSchemaId(null)
      setFields([])
      setAllowedBlockTypes([])
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al crear el schema')
    } finally {
      setSaving(false)
    }
  }

  const previewFields = fields.filter((f) => f.label.trim() && f.slug.trim()).map((f, i) => ({
    id: `preview-${i}`,
    fieldName: f.slug.trim(),
    label: f.label.trim(),
    slug: f.slug.trim(),
    fieldType: f.fieldType,
    isRequired: f.isRequired ?? false,
    defaultValue: f.defaultValue ?? null,
    helpText: f.helpText ?? null,
    validationRules: f.validationRules ?? null,
    displayOrder: i,
  }))

  if (!currentProjectId) {
    return (
      <div className="schema-designer-page">
        <h1 className="schema-designer-page__title">Diseñador de schemas</h1>
        <p className="schema-designer-page__hint">Selecciona un proyecto en la barra superior para crear tipos de contenido.</p>
      </div>
    )
  }

  return (
    <div className="schema-designer-page">
      <h1 className="schema-designer-page__title">Diseñador de schemas</h1>
      <p className="schema-designer-page__hint">
        Crea un nuevo tipo de contenido (schema) con nombre, tipo y campos. El tipo debe ser único en el proyecto (ej. article, page).
      </p>

      {error && <ErrorBanner message={error} />}
      {createdId && (
        <p ref={successRef} className="schema-designer-page__success">
          Schema creado correctamente. Ya puedes usarlo en «Crear contenido».
        </p>
      )}

      <section className="schema-designer-page__section">
        <h2 className="schema-designer-page__section-title">Datos del schema</h2>
        <div className="schema-designer-page__form-row">
          <label className="schema-designer-page__label">Nombre del schema *</label>
          <input
            type="text"
            className="schema-designer-page__input"
            value={schemaName}
            onChange={(e) => setSchemaName(e.target.value)}
            placeholder="Ej. Article"
          />
        </div>
        <div className="schema-designer-page__form-row">
          <label className="schema-designer-page__label">Tipo (slug único en el proyecto) *</label>
          <input
            type="text"
            className="schema-designer-page__input"
            value={schemaType}
            onChange={(e) => setSchemaType(e.target.value)}
            placeholder="Ej. article, page, landing"
          />
        </div>
        <div className="schema-designer-page__form-row">
          <label className="schema-designer-page__label">Descripción (opcional)</label>
          <input
            type="text"
            className="schema-designer-page__input"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Breve descripción del tipo de contenido"
          />
        </div>
        <div className="schema-designer-page__form-row">
          <label className="schema-designer-page__label">Hereda de (opcional)</label>
          <select
            className="schema-designer-page__select"
            value={parentSchemaId ?? ''}
            onChange={(e) => setParentSchemaId(e.target.value || null)}
          >
            <option value="">— Sin herencia —</option>
            {availableSchemas.map((s) => (
              <option key={s.id} value={s.id}>
                {s.schemaName} ({s.schemaType})
              </option>
            ))}
          </select>
          <p className="schema-designer-page__hint">
            Si seleccionas un schema padre, este schema heredará todos sus campos. Los campos propios se agregan además de los heredados.
          </p>
        </div>
      </section>

      <section className="schema-designer-page__section">
        <h2 className="schema-designer-page__section-title">Bloques permitidos</h2>
        <p className="schema-designer-page__hint">
          Tipos de bloque que podrá tener el contenido con este schema. Si no añades ninguno, no se podrán añadir bloques. Opcional: min/max de ocurrencias por tipo.
        </p>
        <div className="schema-designer-page__actions">
          <select
            className="schema-designer-page__select"
            value=""
            onChange={(e) => {
              const v = e.target.value
              if (v && !allowedBlockTypes.some((r) => r.blockType === v)) {
                setAllowedBlockTypes((prev) => [...prev, { blockType: v }])
              }
              e.target.value = ''
            }}
            aria-label="Añadir tipo de bloque permitido"
          >
            <option value="">— Añadir tipo —</option>
            {ALLOWED_BLOCK_TYPES_OPTIONS.map((t) => (
              <option key={t.value} value={t.value} disabled={allowedBlockTypes.some((r) => r.blockType === t.value)}>
                {t.label}
              </option>
            ))}
          </select>
        </div>
        {allowedBlockTypes.length > 0 && (
          <ul className="schema-designer-page__list" style={{ listStyle: 'none', padding: 0, marginTop: '0.5rem' }}>
            {allowedBlockTypes.map((r, i) => (
              <li key={`${r.blockType}-${i}`} className="schema-designer-page__field-row" style={{ marginBottom: '0.5rem' }}>
                <span style={{ fontWeight: 600, minWidth: 80 }}>{ALLOWED_BLOCK_TYPES_OPTIONS.find((t) => t.value === r.blockType)?.label ?? r.blockType}</span>
                <label className="schema-designer-page__checkbox-label" style={{ marginRight: '0.5rem' }}>
                  Min
                  <input
                    type="number"
                    min={0}
                    className="schema-designer-page__input"
                    style={{ width: 56, marginLeft: '0.25rem', marginBottom: 0 }}
                    value={r.minOccurrences ?? ''}
                    onChange={(e) => {
                      const val = e.target.value === '' ? undefined : parseInt(e.target.value, 10)
                      setAllowedBlockTypes((prev) => prev.map((x, j) => (j === i ? { ...x, minOccurrences: val } : x)))
                    }}
                  />
                </label>
                <label className="schema-designer-page__checkbox-label">
                  Max
                  <input
                    type="number"
                    min={0}
                    className="schema-designer-page__input"
                    style={{ width: 56, marginLeft: '0.25rem', marginBottom: 0 }}
                    value={r.maxOccurrences ?? ''}
                    onChange={(e) => {
                      const val = e.target.value === '' ? undefined : parseInt(e.target.value, 10)
                      setAllowedBlockTypes((prev) => prev.map((x, j) => (j === i ? { ...x, maxOccurrences: val } : x)))
                    }}
                  />
                </label>
                <button
                  type="button"
                  className="schema-designer-page__btn schema-designer-page__btn--small schema-designer-page__btn--danger"
                  onClick={() => setAllowedBlockTypes((prev) => prev.filter((_, j) => j !== i))}
                  aria-label="Quitar tipo de bloque"
                >
                  Quitar
                </button>
              </li>
            ))}
          </ul>
        )}
      </section>

      {inheritedFields.length > 0 && (
        <section className="schema-designer-page__section">
          <h2 className="schema-designer-page__section-title">Campos heredados de «{parentSchemaDetail?.schemaName}»</h2>
          <p className="schema-designer-page__hint">
            Estos campos se incluyen automáticamente al crear contenido con este schema. No se pueden modificar aquí.
          </p>
          <div className="schema-designer-page__inherited-wrap">
            {inheritedFields.map((f, i) => (
              <div key={`inh-${i}`} className="schema-designer-page__inherited-field">
                <span className="schema-designer-page__inherited-label">{(f as FieldDefinition).label ?? f.fieldName}</span>
                <span className="schema-designer-page__inherited-type">{f.fieldType}</span>
                {f.isRequired && <span className="schema-designer-page__inherited-required">Requerido</span>}
                {f.helpText && <span className="schema-designer-page__inherited-help">{f.helpText}</span>}
              </div>
            ))}
          </div>
        </section>
      )}

      <section className="schema-designer-page__section">
        <h2 className="schema-designer-page__section-title">Campos</h2>
        <p className="schema-designer-page__hint">
          Etiqueta (visible en la UI) y slug (técnico, kebab-case, único). El slug se puede autogenerar desde la etiqueta.
        </p>
        <div className="schema-designer-page__actions">
          <button type="button" className="schema-designer-page__btn schema-designer-page__btn--primary" onClick={addField}>
            + Añadir campo
          </button>
          <button type="button" className="schema-designer-page__btn" onClick={loadDefaultFields}>
            Sugerir campos por defecto (título, teaser, imagen, contenido)
          </button>
        </div>

        {fields.map((f) => (
          <div key={f._key} className="schema-designer-page__field-block">
            <div className="schema-designer-page__field-inputs-row">
              <input
                type="text"
                className="schema-designer-page__input schema-designer-page__input--narrow"
                value={f.label}
                onChange={(e) => {
                  const label = e.target.value
                  const keepSlugInSync = !f.slug || f.slug === labelToSlug(f.label)
                  updateField(f._key, keepSlugInSync ? { label, slug: labelToSlug(label) } : { label })
                }}
                onBlur={(e) => {
                  const label = e.target.value.trim()
                  if (label && (!f.slug || f.slug === labelToSlug(f.label))) {
                    updateField(f._key, { slug: labelToSlug(label) })
                  }
                }}
                placeholder="Etiqueta (ej. Título)"
              />
              <input
                type="text"
                className="schema-designer-page__input schema-designer-page__input--slug"
                value={f.slug}
                onChange={(e) => updateField(f._key, { slug: e.target.value })}
                placeholder="slug (ej. titulo)"
                title="kebab-case, único en el schema"
              />
            </div>
            <div className="schema-designer-page__field-actions-row">
              <select
                className="schema-designer-page__select"
                value={f.fieldType}
                onChange={(e) => updateField(f._key, { fieldType: e.target.value })}
              >
                {FIELD_TYPES.map((t) => (
                  <option key={t} value={t}>
                    {t}
                  </option>
                ))}
              </select>
              <label className="schema-designer-page__checkbox-label">
                <input
                  type="checkbox"
                  checked={f.isRequired ?? false}
                  onChange={(e) => updateField(f._key, { isRequired: e.target.checked })}
                />
                Requerido
              </label>
              <input
                type="text"
                className="schema-designer-page__input schema-designer-page__input--narrow"
                value={f.helpText ?? ''}
                onChange={(e) => updateField(f._key, { helpText: e.target.value || null })}
                placeholder="Texto de ayuda"
              />
              <button type="button" className="schema-designer-page__btn schema-designer-page__btn--small" onClick={() => moveField(f._key, 'up')} title="Subir">
                ↑
              </button>
              <button type="button" className="schema-designer-page__btn schema-designer-page__btn--small" onClick={() => moveField(f._key, 'down')} title="Bajar">
                ↓
              </button>
              <button type="button" className="schema-designer-page__btn schema-designer-page__btn--small schema-designer-page__btn--danger" onClick={() => removeField(f._key)}>
                Quitar
              </button>
            </div>
          </div>
        ))}
      </section>

      {(previewFields.length > 0 || inheritedFields.length > 0) && (
        <section className="schema-designer-page__section">
          <h2 className="schema-designer-page__section-title">Vista previa del formulario</h2>
          <p className="schema-designer-page__hint">
            Así se verá el formulario al crear/editar contenido con este schema (solo estructura; no se envía).
          </p>
          <div className="schema-designer-page__preview">
            {inheritedFields.map((f, i) => (
              <div key={`prev-inh-${i}`} className="schema-designer-page__form-row schema-designer-page__preview-row--inherited">
                <label className="schema-designer-page__label">
                  {(f as FieldDefinition).label ?? f.fieldName}
                  {f.isRequired && ' *'}
                  <span className="schema-designer-page__label-inherited">(heredado)</span>
                </label>
                <input
                  type="text"
                  className="schema-designer-page__input"
                  placeholder={`Campo tipo ${f.fieldType}`}
                  readOnly
                  disabled
                />
                {f.helpText && <p className="schema-designer-page__hint">{f.helpText}</p>}
              </div>
            ))}
            {previewFields.map((f) => (
              <div key={f.id} className="schema-designer-page__form-row">
                <label className="schema-designer-page__label">
                  {f.label}
                  {f.isRequired && ' *'}
                </label>
                <input
                  type="text"
                  className="schema-designer-page__input"
                  placeholder={`Campo tipo ${f.fieldType}`}
                  readOnly
                  disabled
                />
                {f.helpText && <p className="schema-designer-page__hint">{f.helpText}</p>}
              </div>
            ))}
          </div>
        </section>
      )}

      <div className="schema-designer-page__submit-wrap">
        <button
          type="button"
          className="schema-designer-page__btn schema-designer-page__btn--primary"
          onClick={handleCreate}
          disabled={saving || !schemaName.trim() || !schemaType.trim() || previewFields.length === 0}
        >
          {saving ? 'Creando…' : 'Crear schema'}
        </button>
      </div>
    </div>
  )
}
