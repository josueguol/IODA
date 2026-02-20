import { useState, useEffect, useMemo } from 'react'
import { coreApi } from '../../modules/core/api/core-api'
import { useContextStore } from '../../modules/core/store/context-store'
import { useAuthStore } from '../../modules/auth/store/auth-store'
import { useSchemaStore } from '../../modules/schema/store/schema-store'
import { ErrorBanner } from '../../shared/components'
import type { CreateSchemaFieldDto, ContentSchemaListItem, ContentSchema, FieldDefinition } from '../../modules/core/types'

const FIELD_TYPES = [
  'string',
  'text',
  'richtext',
  'number',
  'integer',
  'boolean',
  'date',
  'datetime',
  'enum',
  'json',
  'list',
  'reference',
  'media',
] as const

/** Convierte label a slug kebab-case (solo letras, números, guiones). */
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

const styles: Record<string, React.CSSProperties> = {
  container: { maxWidth: 800, color: 'var(--page-text)' },
  title: { marginTop: 0, marginBottom: '1rem', color: 'var(--page-text)', fontSize: '1.5rem' },
  section: { marginBottom: '1.5rem', padding: '1rem', background: 'var(--page-bg-elevated)', borderRadius: 8, border: '1px solid var(--page-border)', color: 'var(--page-text)' },
  sectionTitle: { margin: '0 0 0.75rem 0', fontSize: '1rem', fontWeight: 600, color: 'var(--page-text)' },
  formRow: { marginBottom: '0.75rem' },
  label: { display: 'block', marginBottom: '0.25rem', fontSize: '0.875rem', fontWeight: 600, color: 'var(--page-text)' },
  input: { width: '100%', maxWidth: 400, padding: '0.5rem', fontSize: '0.875rem', borderRadius: 4, border: '1px solid var(--input-border)', color: 'var(--input-text)', background: 'var(--input-bg)' },
  select: { padding: '0.5rem', fontSize: '0.875rem', minWidth: 160, borderRadius: 4, border: '1px solid var(--input-border)', color: 'var(--input-text)', background: 'var(--input-bg)' },
  button: { padding: '0.5rem 1rem', fontSize: '0.875rem', cursor: 'pointer', borderRadius: 6, border: '1px solid var(--input-border)', background: 'var(--input-bg)', color: 'var(--page-text)', marginRight: '0.5rem', marginBottom: '0.5rem' },
  buttonPrimary: { background: '#0d6efd', color: 'white', border: 'none' },
  buttonDanger: { background: '#dc3545', color: 'white', border: 'none' },
  buttonSmall: { padding: '0.35rem 0.6rem', fontSize: '0.8125rem' },
  fieldRow: { display: 'flex', gap: '0.5rem', alignItems: 'center', marginBottom: '0.5rem', flexWrap: 'wrap' },
  preview: { padding: '1rem', background: 'var(--input-bg)', borderRadius: 6, border: '1px solid var(--page-border)', marginTop: '0.5rem', color: 'var(--page-text)' },
  hint: { fontSize: '0.8125rem', color: 'var(--page-text-muted)', marginTop: '0.25rem' },
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
  const { loadSchemas } = useSchemaStore()

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

  // Load available schemas for parent selection
  useEffect(() => {
    if (!currentProjectId) return
    coreApi.getSchemas(currentProjectId, true).then(setAvailableSchemas).catch(() => {})
  }, [currentProjectId, createdId])

  // Load parent schema detail when selected
  useEffect(() => {
    if (!currentProjectId || !parentSchemaId) {
      setParentSchemaDetail(null)
      return
    }
    coreApi.getSchema(currentProjectId, parentSchemaId).then(setParentSchemaDetail).catch(() => setParentSchemaDetail(null))
  }, [currentProjectId, parentSchemaId])

  // Inherited fields from parent
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

    setSaving(true)
    try {
      const id = await coreApi.createSchema(currentProjectId, {
        schemaName: schemaName.trim(),
        schemaType: schemaType.trim().toLowerCase().replace(/\s+/g, '-'),
        description: description.trim() || null,
        fields: payload,
        createdBy: user.userId,
        parentSchemaId: parentSchemaId || null,
      })
      setCreatedId(id ?? null)
      await loadSchemas(currentProjectId)
      setSchemaName('')
      setSchemaType('')
      setDescription('')
      setParentSchemaId(null)
      setFields([])
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
      <div style={styles.container}>
        <h1 style={styles.title}>Diseñador de schemas</h1>
        <p style={styles.hint}>Selecciona un proyecto en la barra superior para crear tipos de contenido.</p>
      </div>
    )
  }

  return (
    <div style={styles.container}>
      <h1 style={styles.title}>Diseñador de schemas</h1>
      <p style={styles.hint}>Crea un nuevo tipo de contenido (schema) con nombre, tipo y campos. El tipo debe ser único en el proyecto (ej. article, page).</p>

      {error && <ErrorBanner message={error} />}
      {createdId && (
        <p style={{ color: '#0f5132', marginBottom: '1rem', fontSize: '0.875rem' }}>
          Schema creado correctamente. Ya puedes usarlo en «Crear contenido».
        </p>
      )}

      <section style={styles.section}>
        <h2 style={styles.sectionTitle}>Datos del schema</h2>
        <div style={styles.formRow}>
          <label style={styles.label}>Nombre del schema *</label>
          <input
            type="text"
            style={styles.input}
            value={schemaName}
            onChange={(e) => setSchemaName(e.target.value)}
            placeholder="Ej. Article"
          />
        </div>
        <div style={styles.formRow}>
          <label style={styles.label}>Tipo (slug único en el proyecto) *</label>
          <input
            type="text"
            style={styles.input}
            value={schemaType}
            onChange={(e) => setSchemaType(e.target.value)}
            placeholder="Ej. article, page, landing"
          />
        </div>
        <div style={styles.formRow}>
          <label style={styles.label}>Descripción (opcional)</label>
          <input
            type="text"
            style={styles.input}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Breve descripción del tipo de contenido"
          />
        </div>
        <div style={styles.formRow}>
          <label style={styles.label}>Hereda de (opcional)</label>
          <select
            style={styles.select}
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
          <p style={styles.hint}>
            Si seleccionas un schema padre, este schema heredará todos sus campos. Los campos propios se agregan además de los heredados.
          </p>
        </div>
      </section>

      {inheritedFields.length > 0 && (
        <section style={styles.section}>
          <h2 style={styles.sectionTitle}>Campos heredados de «{parentSchemaDetail?.schemaName}»</h2>
          <p style={styles.hint}>Estos campos se incluyen automáticamente al crear contenido con este schema. No se pueden modificar aquí.</p>
          <div style={{ opacity: 0.7 }}>
            {inheritedFields.map((f, i) => (
              <div key={`inh-${i}`} style={{ ...styles.fieldRow, background: 'var(--page-bg)', padding: '0.4rem 0.5rem', borderRadius: 4 }}>
                <span style={{ fontWeight: 600, minWidth: 120 }}>{(f as FieldDefinition).label ?? f.fieldName}</span>
                <span style={{ color: 'var(--page-text-muted)', fontSize: '0.8125rem' }}>{f.fieldType}</span>
                {f.isRequired && <span style={{ color: '#dc3545', fontSize: '0.75rem', fontWeight: 600 }}>Requerido</span>}
                {f.helpText && <span style={{ color: 'var(--page-text-muted)', fontSize: '0.75rem', fontStyle: 'italic' }}>{f.helpText}</span>}
              </div>
            ))}
          </div>
        </section>
      )}

      <section style={styles.section}>
        <h2 style={styles.sectionTitle}>Campos</h2>
        <p style={styles.hint}>
          Etiqueta (visible en la UI) y slug (técnico, kebab-case, único). El slug se puede autogenerar desde la etiqueta.
        </p>
        <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '0.75rem', flexWrap: 'wrap' }}>
          <button type="button" style={{ ...styles.button, ...styles.buttonPrimary }} onClick={addField}>
            + Añadir campo
          </button>
          <button type="button" style={styles.button} onClick={loadDefaultFields}>
            Sugerir campos por defecto (título, teaser, imagen, contenido)
          </button>
        </div>

        {fields.map((f) => (
          <div key={f._key} style={{ ...styles.fieldRow, flexDirection: 'column', alignItems: 'stretch', gap: '0.35rem' }}>
            <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', flexWrap: 'wrap' }}>
              <input
                type="text"
                style={{ ...styles.input, minWidth: 140, maxWidth: 220 }}
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
                style={{
                  ...styles.input,
                  minWidth: 140,
                  maxWidth: 220,
                  fontFamily: 'monospace',
                  fontSize: '0.8125rem',
                }}
                value={f.slug}
                onChange={(e) => updateField(f._key, { slug: e.target.value })}
                placeholder="slug (ej. titulo)"
                title="kebab-case, único en el schema"
              />
            </div>
            <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', flexWrap: 'wrap' }}>
            <select
              style={styles.select}
              value={f.fieldType}
              onChange={(e) => updateField(f._key, { fieldType: e.target.value })}
            >
              {FIELD_TYPES.map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </select>
            <label style={{ display: 'flex', alignItems: 'center', gap: '0.25rem', fontSize: '0.875rem' }}>
              <input
                type="checkbox"
                checked={f.isRequired ?? false}
                onChange={(e) => updateField(f._key, { isRequired: e.target.checked })}
              />
              Requerido
            </label>
            <input
              type="text"
              style={{ ...styles.input, minWidth: 140, maxWidth: 220 }}
              value={f.helpText ?? ''}
              onChange={(e) => updateField(f._key, { helpText: e.target.value || null })}
              placeholder="Texto de ayuda"
            />
            <button type="button" style={{ ...styles.button, ...styles.buttonSmall }} onClick={() => moveField(f._key, 'up')} title="Subir">
              ↑
            </button>
            <button type="button" style={{ ...styles.button, ...styles.buttonSmall }} onClick={() => moveField(f._key, 'down')} title="Bajar">
              ↓
            </button>
            <button type="button" style={{ ...styles.button, ...styles.buttonSmall, ...styles.buttonDanger }} onClick={() => removeField(f._key)}>
              Quitar
            </button>
            </div>
          </div>
        ))}
      </section>

      {(previewFields.length > 0 || inheritedFields.length > 0) && (
        <section style={styles.section}>
          <h2 style={styles.sectionTitle}>Vista previa del formulario</h2>
          <p style={styles.hint}>Así se verá el formulario al crear/editar contenido con este schema (solo estructura; no se envía).</p>
          <div style={styles.preview}>
            {inheritedFields.map((f, i) => (
              <div key={`prev-inh-${i}`} style={{ ...styles.formRow, opacity: 0.7 }}>
                <label style={styles.label}>
                  {(f as FieldDefinition).label ?? f.fieldName}
                  {f.isRequired && ' *'}
                  <span style={{ fontSize: '0.7rem', color: 'var(--page-text-muted)', marginLeft: '0.5rem' }}>(heredado)</span>
                </label>
                <input
                  type="text"
                  style={styles.input}
                  placeholder={`Campo tipo ${f.fieldType}`}
                  readOnly
                  disabled
                />
                {f.helpText && <p style={styles.hint}>{f.helpText}</p>}
              </div>
            ))}
            {previewFields.map((f) => (
              <div key={f.id} style={styles.formRow}>
                <label style={styles.label}>
                  {f.label}
                  {f.isRequired && ' *'}
                </label>
                <input
                  type="text"
                  style={styles.input}
                  placeholder={`Campo tipo ${f.fieldType}`}
                  readOnly
                  disabled
                />
                {f.helpText && <p style={styles.hint}>{f.helpText}</p>}
              </div>
            ))}
          </div>
        </section>
      )}

      <div style={{ marginTop: '1.5rem' }}>
        <button
          type="button"
          style={{ ...styles.button, ...styles.buttonPrimary }}
          onClick={handleCreate}
          disabled={saving || !schemaName.trim() || !schemaType.trim() || previewFields.length === 0}
        >
          {saving ? 'Creando…' : 'Crear schema'}
        </button>
      </div>
    </div>
  )
}
