import { useEffect, useMemo, useRef, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import {
  ArrowDown,
  ArrowUp,
  Braces,
  Calendar,
  CalendarClock,
  CheckSquare,
  Eye,
  Image,
  List,
  Link2,
  NotepadText,
  Sigma,
  SlidersHorizontal,
  Type,
} from 'lucide-react'
import { coreApi } from '../../modules/core/api/core-api'
import { useAuthStore } from '../../modules/auth/store/auth-store'
import { useContextStore } from '../../modules/core/store/context-store'
import type { AllowedBlockTypeRule, ContentSchema, CreateSchemaFieldDto, UpdateSchemaFieldDto, ValidationRules } from '../../modules/core/types'
import { useSchemaStore } from '../../modules/schema/store/schema-store'
import { ErrorBanner } from '../../shared/components'
import './SchemaDesignerPage.css'

const ALLOWED_BLOCK_TYPES_OPTIONS = [
  { value: 'theme_component', label: 'Componente de tema' },
  { value: 'dynamic_content_list', label: 'Listado dinámico de contenido' },
  { value: 'embed', label: 'Embed / Integración externa' },
] as const

const FIELD_TYPES = [
  'string',
  'formattedtext',
  'richtexteditor',
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

const RESERVED_NATIVE_SLUGS = new Set(['content-title', 'content-slug', 'title', 'slug'])
const VIRTUAL_NATIVE_ORDER = ['content-title', 'content-slug']

const FIELD_TYPE_CATALOG = [
  { value: 'string', label: 'Campo de texto simple', icon: Type },
  { value: 'formattedtext', label: 'Texto con formato simple', icon: Type },
  { value: 'richtexteditor', label: 'Editor de texto enriquecido', icon: NotepadText },
  { value: 'number', label: 'Campo numérico', icon: Sigma },
  { value: 'boolean', label: 'Campo booleano', icon: CheckSquare },
  { value: 'date', label: 'Campo fecha', icon: Calendar },
  { value: 'datetime', label: 'Campo fecha y hora', icon: CalendarClock },
  { value: 'list', label: 'Campo de elementos', icon: List },
  { value: 'json', label: 'Campo JSON', icon: Braces },
  { value: 'reference', label: 'Campo de referencia', icon: Link2 },
  { value: 'media', label: 'Campo multimedia', icon: Image },
] as const

const SLUG_REGEX = /^[a-z0-9]+(?:[-_][a-z0-9]+)*$/
const MEDIA_CATEGORIES = ['image', 'video', 'audio'] as const

interface MediaRulesEditorState {
  allowedCategories: string[]
  allowedMimeTypes: string[]
  allowedExtensions: string[]
  maxSizeBytes?: number
}

interface FieldEditor extends CreateSchemaFieldDto {
  _key: string
  existingId?: string | null
  isNativeVirtual?: boolean
}

type RightTab = 'field-config' | 'schema-json'

function labelToSlug(label: string): string {
  return label
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9_-]+/g, '-')
    .replace(/-+/g, '-')
    .replace(/^-+|-+$/g, '')
}

function isValidSlug(slug: string): boolean {
  return slug.length > 0 && SLUG_REGEX.test(slug)
}

function nextKey(): string {
  return `f-${Date.now()}-${Math.random().toString(36).slice(2, 9)}`
}

function normalizeEditorFieldType(fieldType: string): string {
  const lower = (fieldType ?? '').trim().toLowerCase()
  if (lower === 'formatted-text' || lower === 'formatted_text') return 'formattedtext'
  if (lower === 'richtexteditor') return 'richtexteditor'
  return lower
}

function buildVirtualNativeFields(): FieldEditor[] {
  return [
    {
      _key: nextKey(),
      existingId: null,
      isNativeVirtual: true,
      label: 'Título del contenido',
      slug: 'content-title',
      fieldType: 'string',
      isRequired: true,
      defaultValue: undefined,
      helpText: 'Campo nativo del contenido. Solo puedes cambiar su posición.',
      validationRules: null,
      displayOrder: 0,
    },
    {
      _key: nextKey(),
      existingId: null,
      isNativeVirtual: true,
      label: 'Slug (URI)',
      slug: 'content-slug',
      fieldType: 'string',
      isRequired: true,
      defaultValue: undefined,
      helpText: 'Campo nativo del contenido. Solo puedes cambiar su posición.',
      validationRules: null,
      displayOrder: 1,
    },
  ]
}

function sortWithNativeFirst(fields: FieldEditor[]): FieldEditor[] {
  const native = fields
    .filter((f) => f.isNativeVirtual)
    .sort((a, b) => VIRTUAL_NATIVE_ORDER.indexOf(a.slug) - VIRTUAL_NATIVE_ORDER.indexOf(b.slug))
  const rest = fields.filter((f) => !f.isNativeVirtual)
  return [...native, ...rest].map((f, i) => ({ ...f, displayOrder: i }))
}

function withVirtualNatives(fields: FieldEditor[]): FieldEditor[] {
  const virtual = buildVirtualNativeFields()
  const cleaned = fields.filter((f) => !RESERVED_NATIVE_SLUGS.has(f.slug.toLowerCase()))
  return sortWithNativeFirst([...virtual, ...cleaned])
}

function uniqueSlug(base: string, fields: FieldEditor[]): string {
  const normalizedBase = base || 'campo'
  const existing = new Set(fields.map((f) => f.slug.toLowerCase()))
  if (!existing.has(normalizedBase.toLowerCase())) return normalizedBase
  let n = 2
  while (existing.has(`${normalizedBase}-${n}`.toLowerCase())) n += 1
  return `${normalizedBase}-${n}`
}

function fieldTypePlaceholder(type: string): string {
  switch (type) {
    case 'formattedtext':
      return 'Texto con formato inline'
    case 'richtexteditor':
      return 'Editor blocknote markdown'
    case 'number':
      return '0'
    case 'boolean':
      return 'true / false'
    case 'date':
      return 'YYYY-MM-DD'
    case 'datetime':
      return 'YYYY-MM-DD HH:mm'
    case 'json':
      return '{"key":"value"}'
    case 'list':
      return 'Elemento 1, Elemento 2'
    case 'reference':
      return 'ID o referencia'
    case 'media':
      return 'URL o recurso multimedia'
    default:
      return 'Valor de ejemplo'
  }
}

function parseCommaSeparatedList(raw: string): string[] {
  return raw
    .split(',')
    .map((x) => x.trim().toLowerCase())
    .filter(Boolean)
}

function parseMediaRulesFromValidationRules(validationRules: ValidationRules | null | undefined): MediaRulesEditorState {
  const media = validationRules && typeof validationRules === 'object'
    ? (validationRules['media'] as Record<string, unknown> | undefined)
    : undefined

  const categories = Array.isArray(media?.['allowedCategories'])
    ? (media?.['allowedCategories'] as unknown[]).map((x) => String(x).toLowerCase()).filter(Boolean)
    : []
  const mimeTypes = Array.isArray(media?.['allowedMimeTypes'])
    ? (media?.['allowedMimeTypes'] as unknown[]).map((x) => String(x).toLowerCase()).filter(Boolean)
    : []
  const extensions = Array.isArray(media?.['allowedExtensions'])
    ? (media?.['allowedExtensions'] as unknown[]).map((x) => String(x).toLowerCase().replace(/^\./, '')).filter(Boolean)
    : []
  const rawMaxSize = media?.['maxSizeBytes']
  const maxSize = typeof rawMaxSize === 'number'
    ? rawMaxSize
    : typeof rawMaxSize === 'string'
      ? Number(rawMaxSize)
      : undefined

  return {
    allowedCategories: categories,
    allowedMimeTypes: mimeTypes,
    allowedExtensions: extensions,
    maxSizeBytes: maxSize && Number.isFinite(maxSize) && maxSize > 0 ? maxSize : undefined,
  }
}

function mergeMediaRulesIntoValidationRules(
  validationRules: ValidationRules | null | undefined,
  mediaRules: MediaRulesEditorState
): ValidationRules {
  const next: ValidationRules = { ...(validationRules ?? {}) }
  next['media'] = {
    allowedCategories: mediaRules.allowedCategories,
    allowedMimeTypes: mediaRules.allowedMimeTypes,
    allowedExtensions: mediaRules.allowedExtensions,
    ...(mediaRules.maxSizeBytes && mediaRules.maxSizeBytes > 0 ? { maxSizeBytes: mediaRules.maxSizeBytes } : {}),
  }
  return next
}

function mapSchemaToEditor(schema: ContentSchema): FieldEditor[] {
  const mapped = schema.fields
    .slice()
    .sort((a, b) => a.displayOrder - b.displayOrder)
    .map((f, i) => ({
      _key: nextKey(),
      existingId: f.id,
      isNativeVirtual: false,
      label: f.label,
      slug: f.slug,
      fieldType: normalizeEditorFieldType(f.fieldType),
      isRequired: f.isRequired,
      defaultValue: f.defaultValue ?? undefined,
      helpText: f.helpText ?? null,
      validationRules: f.validationRules ?? null,
      displayOrder: i,
    }))
  return withVirtualNatives(mapped)
}

export function SchemaDesignerPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const schemaId = searchParams.get('schemaId')
  const isEditMode = Boolean(schemaId)
  const { currentProjectId } = useContextStore()
  const user = useAuthStore((s) => s.user)
  const { loadSchema, loadSchemas } = useSchemaStore()

  const [schemaName, setSchemaName] = useState('')
  const [schemaType, setSchemaType] = useState('')
  const [description, setDescription] = useState('')
  const [fields, setFields] = useState<FieldEditor[]>([])
  const [selectedFieldKey, setSelectedFieldKey] = useState<string | null>(null)
  const [allowedBlockTypes, setAllowedBlockTypes] = useState<AllowedBlockTypeRule[]>([])
  const [initialFieldTypeById, setInitialFieldTypeById] = useState<Record<string, string>>({})
  const [rightTab, setRightTab] = useState<RightTab>('field-config')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [createdId, setCreatedId] = useState<string | null>(null)
  const successRef = useRef<HTMLParagraphElement>(null)

  const selectedField = useMemo(
    () => fields.find((f) => f._key === selectedFieldKey) ?? null,
    [fields, selectedFieldKey]
  )

  const customFields = useMemo(
    () => fields.filter((f) => !f.isNativeVirtual),
    [fields]
  )

  const previewFields = useMemo(
    () =>
      fields
        .filter((f) => f.label.trim() && f.slug.trim())
        .map((f, i) => ({ ...f, displayOrder: i })),
    [fields]
  )

  useEffect(() => {
    if (createdId && successRef.current) {
      successRef.current.scrollIntoView({ behavior: 'smooth', block: 'nearest' })
    }
  }, [createdId])

  useEffect(() => {
    if (!currentProjectId) return
    setError(null)

    if (isEditMode && schemaId) {
      loadSchema(currentProjectId, schemaId)
        .then((schema) => {
          if (!schema) {
            setError('No se encontró el schema solicitado.')
            return
          }
          setSchemaName(schema.schemaName)
          setSchemaType(schema.schemaType)
          setDescription(schema.description ?? '')
          setInitialFieldTypeById(
            Object.fromEntries(schema.fields.map((f) => [f.id, normalizeEditorFieldType(f.fieldType)]))
          )
          const nextFields = mapSchemaToEditor(schema)
          setFields(nextFields)
          setSelectedFieldKey(nextFields[0]?._key ?? null)
          setAllowedBlockTypes(schema.allowedBlockTypes ?? [])
        })
        .catch(() => setError('No se pudo cargar el schema para edición.'))
      return
    }

    coreApi
      .getDefaultSchemaFields(currentProjectId)
      .then((list) => {
        setInitialFieldTypeById({})
        const custom = list.map((s, i) => ({
          _key: nextKey(),
          existingId: null,
          isNativeVirtual: false,
          label: s.label,
          slug: s.slug,
          fieldType: normalizeEditorFieldType(s.fieldType),
          isRequired: false,
          defaultValue: undefined,
          helpText: null,
          validationRules: null,
          displayOrder: i,
        }))
        const nextFields = withVirtualNatives(custom)
        setFields(nextFields)
        setSelectedFieldKey(nextFields[0]?._key ?? null)
      })
      .catch(() => setFields(withVirtualNatives([])))
  }, [currentProjectId, isEditMode, loadSchema, schemaId])

  useEffect(() => {
    if (!selectedFieldKey && fields.length > 0) {
      setSelectedFieldKey(fields[0]._key)
      return
    }
    if (selectedFieldKey && !fields.some((f) => f._key === selectedFieldKey)) {
      setSelectedFieldKey(fields[0]?._key ?? null)
    }
  }, [fields, selectedFieldKey])

  const updateField = (key: string, patch: Partial<FieldEditor>) => {
    setFields((prev) => prev.map((f) => (f._key === key ? { ...f, ...patch } : f)))
  }

  const addFieldByType = (fieldType: string) => {
    const typeLabel = FIELD_TYPE_CATALOG.find((t) => t.value === fieldType)?.label ?? 'Campo nuevo'
    const baseLabel = typeLabel.replace('Campo ', '').replace('de ', '').trim() || 'Campo nuevo'
    setFields((prev) => {
      const slug = uniqueSlug(labelToSlug(baseLabel), prev)
      const next = [
        ...prev,
        {
          _key: nextKey(),
          existingId: null,
          isNativeVirtual: false,
          label: baseLabel,
          slug,
          fieldType,
          isRequired: false,
          defaultValue: undefined,
          helpText: null,
          validationRules: null,
          displayOrder: prev.length,
        } as FieldEditor,
      ]
      setSelectedFieldKey(next[next.length - 1]._key)
      return next
    })
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

  const validateBeforeSave = (): string | null => {
    if (!schemaName.trim()) return 'El nombre del schema es obligatorio.'
    if (!schemaType.trim()) return 'El tipo de schema es obligatorio (ej. article, page).'
    if (customFields.length === 0) return 'Añade al menos un campo de schema.'

    const validFields = customFields.filter((f) => f.label.trim() && f.slug.trim())
    if (validFields.length === 0) return 'Añade al menos un campo con etiqueta y slug.'

    const slugs = validFields.map((f) => f.slug.trim().toLowerCase())
    if (new Set(slugs).size !== slugs.length) return 'Los slugs de campo no pueden repetirse.'

    for (const f of validFields) {
      if (!isValidSlug(f.slug.trim())) return `El slug «${f.slug}» no es válido. Usa letras, números, guion o guion bajo.`
      if (RESERVED_NATIVE_SLUGS.has(f.slug.trim().toLowerCase())) return `El slug «${f.slug}» está reservado por campos nativos.`
      // La inmutabilidad aplica únicamente a campos activos existentes (con Id persistida).
      if (f.existingId) {
        const originalType = initialFieldTypeById[f.existingId]
        if (originalType && originalType !== normalizeEditorFieldType(f.fieldType)) {
          return `El tipo del campo existente «${f.slug}» es inmutable. Elimina y recrea el campo para cambiar tipo.`
        }
      }
    }
    return null
  }

  const handleSave = async () => {
    setError(null)
    if (!currentProjectId || !user?.userId) {
      setError('Selecciona un proyecto y asegúrate de estar autenticado.')
      return
    }

    const validationError = validateBeforeSave()
    if (validationError) {
      setError(validationError)
      return
    }

    const validFields = customFields.filter((f) => f.label.trim() && f.slug.trim())
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
      if (isEditMode && schemaId) {
        const payload: UpdateSchemaFieldDto[] = validFields.map((f, i) => ({
          id: f.existingId ?? null,
          label: f.label.trim(),
          slug: f.slug.trim(),
          fieldType: f.fieldType,
          isRequired: f.isRequired ?? false,
          defaultValue: f.defaultValue ?? undefined,
          helpText: f.helpText?.trim() || null,
          validationRules: f.validationRules ?? null,
          displayOrder: i,
        }))

        await coreApi.updateSchema(currentProjectId, schemaId, {
          schemaName: schemaName.trim(),
          schemaType: schemaType.trim(),
          description: description.trim() || null,
          fields: payload,
          updatedBy: user.userId,
          allowedBlockTypes: allowedBlocksPayload,
        })
      } else {
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

        const id = await coreApi.createSchema(currentProjectId, {
          schemaName: schemaName.trim(),
          schemaType: schemaType.trim().toLowerCase().replace(/\s+/g, '-'),
          description: description.trim() || null,
          fields: payload,
          createdBy: user.userId,
          allowedBlockTypes: allowedBlocksPayload,
        })
        setCreatedId(id ?? null)
      }

      await loadSchemas(currentProjectId)
      navigate('/admin/schemas')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al guardar el schema')
    } finally {
      setSaving(false)
    }
  }

  const schemaJson = useMemo(
    () =>
      JSON.stringify(
        {
          schemaName,
          schemaType,
          description: description || null,
          nativeFields: previewFields.filter((f) => f.isNativeVirtual).map((f) => ({
            label: f.label,
            slug: f.slug,
            displayOrder: f.displayOrder,
          })),
          schemaFields: previewFields.filter((f) => !f.isNativeVirtual).map((f) => ({
            id: f.existingId ?? null,
            label: f.label,
            slug: f.slug,
            fieldType: f.fieldType,
            isRequired: f.isRequired ?? false,
            helpText: f.helpText ?? null,
            displayOrder: f.displayOrder,
          })),
          allowedBlockTypes,
        },
        null,
        2
      ),
    [allowedBlockTypes, description, previewFields, schemaName, schemaType]
  )

  if (!currentProjectId) {
    return (
      <div className="schema-designer-page">
        <h1 className="schema-designer-page__title">Diseñador de schemas</h1>
        <p className="schema-designer-page__hint">Selecciona un proyecto en la barra superior para diseñar schemas.</p>
      </div>
    )
  }

  return (
    <div className="schema-designer-page schema-designer-page--full">
      <header className="schema-designer-page__topbar">
        <button type="button" className="schema-designer-page__btn" onClick={() => navigate('/admin/schemas')}>
          Volver a schemas
        </button>
        <span className="schema-designer-page__mode">
          {isEditMode ? 'Modo edición' : 'Modo creación'}
        </span>
      </header>

      {error && <ErrorBanner message={error} />}
      {createdId && <p ref={successRef} className="schema-designer-page__success">Schema creado correctamente.</p>}

      <div className="schema-designer-page__workspace">
        <aside className="schema-designer-page__panel schema-designer-page__panel--left">
          <h2 className="schema-designer-page__panel-title">Agregar campo</h2>
          <div className="schema-designer-page__catalog">
            {FIELD_TYPE_CATALOG.map((item) => {
              const Icon = item.icon
              return (
                <button
                  key={item.value}
                  type="button"
                  className="schema-designer-page__catalog-btn"
                  onClick={() => addFieldByType(item.value)}
                >
                  <Icon size={18} />
                  <span>{item.label}</span>
                </button>
              )
            })}
          </div>
        </aside>

        <section className="schema-designer-page__panel schema-designer-page__panel--center">
          <h2 className="schema-designer-page__panel-title">Previsualización del esquema</h2>
          <p className="schema-designer-page__meta">
            {schemaName.trim() || 'Nuevo schema'} - {schemaType.trim() || 'sin-slug'}
          </p>

          <div className="schema-designer-page__canvas">
            {fields.map((f) => {
              const isNative = Boolean(f.isNativeVirtual)
              return (
                <div
                  key={f._key}
                  className={`schema-designer-page__field-card ${selectedFieldKey === f._key ? 'schema-designer-page__field-card--active' : ''}`}
                  onClick={() => setSelectedFieldKey(f._key)}
                >
                  <div className="schema-designer-page__field-header">
                    <label className="schema-designer-page__field-title">
                      {f.label || 'Campo sin nombre'}
                      {f.isRequired ? ' *' : ''}
                      {isNative ? ' (nativo)' : ''}
                    </label>
                    {!isNative && (
                      <button
                        type="button"
                        className="schema-designer-page__btn schema-designer-page__btn--small schema-designer-page__btn--danger"
                        onClick={(e) => {
                          e.stopPropagation()
                          removeField(f._key)
                        }}
                      >
                        Quitar
                      </button>
                    )}
                  </div>
                  <div className="schema-designer-page__field-input-row">
                    <input
                      type="text"
                      className="schema-designer-page__input schema-designer-page__input--fill"
                      readOnly
                      placeholder={fieldTypePlaceholder(f.fieldType)}
                    />
                    <button
                      type="button"
                      className="schema-designer-page__icon-btn"
                      onClick={(e) => {
                        e.stopPropagation()
                        moveField(f._key, 'up')
                      }}
                      title="Subir"
                    >
                      <ArrowUp size={18} />
                    </button>
                    <button
                      type="button"
                      className="schema-designer-page__icon-btn"
                      onClick={(e) => {
                        e.stopPropagation()
                        moveField(f._key, 'down')
                      }}
                      title="Bajar"
                    >
                      <ArrowDown size={18} />
                    </button>
                  </div>
                  <small className="schema-designer-page__field-slug">Slug: {f.slug || 'sin-slug'}</small>
                </div>
              )
            })}
          </div>
        </section>

        <aside className="schema-designer-page__panel schema-designer-page__panel--right">
          <h2 className="schema-designer-page__panel-title">Propiedades del esquema</h2>
          <div className="schema-designer-page__form-row">
            <label className="schema-designer-page__label">Nombre del schema</label>
            <input
              type="text"
              className="schema-designer-page__input schema-designer-page__input--fill"
              value={schemaName}
              onChange={(e) => setSchemaName(e.target.value)}
              placeholder="Ej. Artículo"
            />
          </div>
          <div className="schema-designer-page__form-row">
            <label className="schema-designer-page__label">Tipo (slug único)</label>
            <input
              type="text"
              className="schema-designer-page__input schema-designer-page__input--fill"
              value={schemaType}
              onChange={(e) => {
                if (!isEditMode) setSchemaType(e.target.value)
              }}
              readOnly={isEditMode}
              placeholder="Ej. article"
              title={isEditMode ? 'El tipo es solo lectura en edición.' : undefined}
            />
          </div>
          <div className="schema-designer-page__form-row">
            <label className="schema-designer-page__label">Descripción (opcional)</label>
            <textarea
              className="schema-designer-page__textarea"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Descripción del schema"
            />
          </div>

          <h3 className="schema-designer-page__subsection-title">Configuración de bloques en el esquema</h3>
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
            >
              <option value="">+ Añadir tipo</option>
              {ALLOWED_BLOCK_TYPES_OPTIONS.map((t) => (
                <option key={t.value} value={t.value} disabled={allowedBlockTypes.some((r) => r.blockType === t.value)}>
                  {t.label}
                </option>
              ))}
            </select>
          </div>
          {allowedBlockTypes.length > 0 && (
            <ul className="schema-designer-page__block-list">
              {allowedBlockTypes.map((r, i) => (
                <li key={`${r.blockType}-${i}`} className="schema-designer-page__block-row">
                  <span className="schema-designer-page__block-name">
                    {ALLOWED_BLOCK_TYPES_OPTIONS.find((t) => t.value === r.blockType)?.label ?? r.blockType}
                  </span>
                  <input
                    type="number"
                    min={0}
                    className="schema-designer-page__input schema-designer-page__input--compact"
                    value={r.minOccurrences ?? ''}
                    onChange={(e) => {
                      const val = e.target.value === '' ? undefined : parseInt(e.target.value, 10)
                      setAllowedBlockTypes((prev) => prev.map((x, j) => (j === i ? { ...x, minOccurrences: val } : x)))
                    }}
                    placeholder="Min"
                  />
                  <input
                    type="number"
                    min={0}
                    className="schema-designer-page__input schema-designer-page__input--compact"
                    value={r.maxOccurrences ?? ''}
                    onChange={(e) => {
                      const val = e.target.value === '' ? undefined : parseInt(e.target.value, 10)
                      setAllowedBlockTypes((prev) => prev.map((x, j) => (j === i ? { ...x, maxOccurrences: val } : x)))
                    }}
                    placeholder="Max"
                  />
                  <button
                    type="button"
                    className="schema-designer-page__btn schema-designer-page__btn--small schema-designer-page__btn--danger"
                    onClick={() => setAllowedBlockTypes((prev) => prev.filter((_, j) => j !== i))}
                  >
                    Quitar
                  </button>
                </li>
              ))}
            </ul>
          )}

          <div className="schema-designer-page__tabs">
            <button
              type="button"
              className={`schema-designer-page__tab ${rightTab === 'field-config' ? 'schema-designer-page__tab--active' : ''}`}
              onClick={() => setRightTab('field-config')}
            >
              <SlidersHorizontal size={16} />
              Configuración de campo
            </button>
            <button
              type="button"
              className={`schema-designer-page__tab ${rightTab === 'schema-json' ? 'schema-designer-page__tab--active' : ''}`}
              onClick={() => setRightTab('schema-json')}
            >
              <Eye size={16} />
              Ver esquema
            </button>
          </div>

          {rightTab === 'field-config' ? (
            <div className="schema-designer-page__tab-content">
              {selectedField ? (
                <>
                  {(() => {
                    const isExistingActiveField = Boolean(isEditMode && selectedField.existingId)
                    const isSlugReadOnly = Boolean(selectedField.isNativeVirtual || isExistingActiveField)
                    return (
                      <>
                  <div className="schema-designer-page__form-row">
                    <label className="schema-designer-page__label">Nombre a mostrar</label>
                    <input
                      type="text"
                      className="schema-designer-page__input schema-designer-page__input--fill"
                      value={selectedField.label}
                      onChange={(e) => {
                        if (selectedField.isNativeVirtual) return
                        const label = e.target.value
                        const keepSlugInSync = !isSlugReadOnly && (!selectedField.slug || selectedField.slug === labelToSlug(selectedField.label))
                        updateField(
                          selectedField._key,
                          keepSlugInSync ? { label, slug: labelToSlug(label) } : { label }
                        )
                      }}
                      readOnly={Boolean(selectedField.isNativeVirtual)}
                    />
                  </div>
                  <div className="schema-designer-page__form-row">
                    <label className="schema-designer-page__label">Slug del campo</label>
                    <input
                      type="text"
                      className="schema-designer-page__input schema-designer-page__input--fill"
                      value={selectedField.slug}
                      onChange={(e) => {
                        if (!isSlugReadOnly) updateField(selectedField._key, { slug: e.target.value })
                      }}
                      readOnly={isSlugReadOnly}
                      title={isSlugReadOnly ? 'El slug de campo es solo lectura en edición.' : undefined}
                    />
                  </div>
                  <div className="schema-designer-page__form-row">
                    <label className="schema-designer-page__label">Tipo</label>
                    <select
                      className="schema-designer-page__select schema-designer-page__select--fill"
                      value={selectedField.fieldType}
                      onChange={(e) => {
                        if (!selectedField.isNativeVirtual) updateField(selectedField._key, { fieldType: e.target.value })
                      }}
                      disabled={Boolean(selectedField.isNativeVirtual || isExistingActiveField)}
                      title={selectedField.isNativeVirtual ? 'Campo nativo: tipo de solo lectura.' : isExistingActiveField ? 'El tipo de campo activo existente es de solo lectura.' : undefined}
                    >
                      {FIELD_TYPES.map((t) => (
                        <option key={t} value={t}>
                          {t}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="schema-designer-page__form-row">
                    <label className="schema-designer-page__label">Texto de ayuda</label>
                    <input
                      type="text"
                      className="schema-designer-page__input schema-designer-page__input--fill"
                      value={selectedField.helpText ?? ''}
                      onChange={(e) => {
                        if (!selectedField.isNativeVirtual) updateField(selectedField._key, { helpText: e.target.value || null })
                      }}
                      readOnly={Boolean(selectedField.isNativeVirtual)}
                    />
                  </div>
                  <label className="schema-designer-page__checkbox-label">
                    <input
                      type="checkbox"
                      checked={selectedField.isRequired ?? false}
                      onChange={(e) => {
                        if (!selectedField.isNativeVirtual) updateField(selectedField._key, { isRequired: e.target.checked })
                      }}
                      disabled={Boolean(selectedField.isNativeVirtual)}
                    />
                    Es requerido
                  </label>
                  {selectedField.fieldType === 'media' && !selectedField.isNativeVirtual && (() => {
                    const mediaRules = parseMediaRulesFromValidationRules(selectedField.validationRules)
                    const maxSizeMb = mediaRules.maxSizeBytes ? Math.round(mediaRules.maxSizeBytes / (1024 * 1024)) : ''

                    const updateMediaRules = (nextRules: MediaRulesEditorState) => {
                      updateField(selectedField._key, {
                        validationRules: mergeMediaRulesIntoValidationRules(selectedField.validationRules, nextRules),
                      })
                    }

                    return (
                      <>
                        <div className="schema-designer-page__form-row">
                          <label className="schema-designer-page__label">Categorías permitidas</label>
                          <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap' }}>
                            {MEDIA_CATEGORIES.map((category) => {
                              const checked = mediaRules.allowedCategories.includes(category)
                              return (
                                <label key={category} className="schema-designer-page__checkbox-label" style={{ margin: 0 }}>
                                  <input
                                    type="checkbox"
                                    checked={checked}
                                    onChange={(e) => {
                                      const nextCategories = e.target.checked
                                        ? Array.from(new Set([...mediaRules.allowedCategories, category]))
                                        : mediaRules.allowedCategories.filter((c) => c !== category)
                                      updateMediaRules({ ...mediaRules, allowedCategories: nextCategories })
                                    }}
                                  />
                                  {category}
                                </label>
                              )
                            })}
                          </div>
                        </div>
                        <div className="schema-designer-page__form-row">
                          <label className="schema-designer-page__label">MIME permitidos (csv)</label>
                          <input
                            type="text"
                            className="schema-designer-page__input schema-designer-page__input--fill"
                            value={mediaRules.allowedMimeTypes.join(', ')}
                            onChange={(e) => updateMediaRules({ ...mediaRules, allowedMimeTypes: parseCommaSeparatedList(e.target.value) })}
                            placeholder="image/jpeg, image/png, video/mp4"
                          />
                        </div>
                        <div className="schema-designer-page__form-row">
                          <label className="schema-designer-page__label">Extensiones permitidas (csv)</label>
                          <input
                            type="text"
                            className="schema-designer-page__input schema-designer-page__input--fill"
                            value={mediaRules.allowedExtensions.join(', ')}
                            onChange={(e) => updateMediaRules({ ...mediaRules, allowedExtensions: parseCommaSeparatedList(e.target.value).map((x) => x.replace(/^\./, '')) })}
                            placeholder="jpg, jpeg, png, mp4"
                          />
                        </div>
                        <div className="schema-designer-page__form-row">
                          <label className="schema-designer-page__label">Tamaño máximo (MB, opcional)</label>
                          <input
                            type="number"
                            min={1}
                            className="schema-designer-page__input schema-designer-page__input--fill"
                            value={maxSizeMb}
                            onChange={(e) => {
                              const mb = e.target.value === '' ? undefined : Number(e.target.value)
                              updateMediaRules({
                                ...mediaRules,
                                maxSizeBytes: mb && Number.isFinite(mb) && mb > 0 ? Math.round(mb * 1024 * 1024) : undefined,
                              })
                            }}
                            placeholder="50"
                          />
                        </div>
                      </>
                    )
                  })()}
                      </>
                    )
                  })()}
                </>
              ) : (
                <p className="schema-designer-page__hint">Selecciona un campo para configurar sus propiedades.</p>
              )}
            </div>
          ) : (
            <div className="schema-designer-page__tab-content">
              <pre className="schema-designer-page__json-view">{schemaJson}</pre>
            </div>
          )}

          <div className="schema-designer-page__submit-wrap schema-designer-page__submit-wrap--right">
            <button type="button" className="schema-designer-page__btn" onClick={() => navigate('/admin/schemas')}>
              Cancelar
            </button>
            <button
              type="button"
              className="schema-designer-page__btn schema-designer-page__btn--primary"
              onClick={handleSave}
              disabled={saving || !schemaName.trim() || !schemaType.trim() || customFields.length === 0}
            >
              {saving ? 'Guardando…' : isEditMode ? 'Guardar cambios' : 'Crear schema'}
            </button>
          </div>
        </aside>
      </div>
    </div>
  )
}
