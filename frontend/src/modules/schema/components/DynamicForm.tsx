import { useEffect, useMemo } from 'react'
import { FormProvider, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useSchemaStore } from '../store/schema-store'
import { buildZodSchema, type DynamicFormValues } from '../utils/field-validation'
import { DynamicField } from './DynamicField'

const formStyles: Record<string, React.CSSProperties> = {
  form: { maxWidth: 600, color: 'var(--page-text)' },
  actions: { marginTop: '1.5rem', display: 'flex', gap: '0.75rem' },
  submit: { padding: '0.5rem 1rem', background: '#0d6efd', color: 'white', border: 'none', borderRadius: 6, cursor: 'pointer', fontSize: '0.875rem' },
  reset: { padding: '0.5rem 1rem', background: '#6c757d', color: 'white', border: 'none', borderRadius: 6, cursor: 'pointer', fontSize: '0.875rem' },
  loading: { color: 'var(--page-text-muted)', marginBottom: '1rem' },
  error: { color: '#dc3545', marginBottom: '1rem' },
}

export interface DynamicFormProps {
  projectId: string
  schemaId: string
  defaultValues?: DynamicFormValues
  onSubmit: (values: DynamicFormValues) => void
  submitLabel?: string
}

/** Resuelve el schema por projectId/schemaId, construye formulario dinámico y envía valores al submit. */
export function DynamicForm({
  projectId,
  schemaId,
  defaultValues,
  onSubmit,
  submitLabel = 'Enviar',
}: DynamicFormProps) {
  const { loadSchema, getSchemaSync, schemaLoading, schemaError } = useSchemaStore()
  const schema = getSchemaSync(projectId, schemaId)

  useEffect(() => {
    if (projectId && schemaId && !schema) {
      loadSchema(projectId, schemaId).catch(() => {})
    }
  }, [projectId, schemaId, schema, loadSchema])

  // Combine inherited fields (from parent schema) with own fields
  const allFields = useMemo(() => {
    const inherited = schema?.inheritedFields ?? []
    const own = schema?.fields ?? []
    return [...inherited, ...own]
  }, [schema])

  const zodSchema = useMemo(() => {
    if (!allFields.length) return null
    return buildZodSchema(allFields)
  }, [allFields])

  const initialDefaults = useMemo(() => {
    if (defaultValues && Object.keys(defaultValues).length > 0) return defaultValues
    const out: DynamicFormValues = {}
    for (const f of allFields) {
      const t = f.fieldType?.toLowerCase()
      if (f.defaultValue !== undefined && f.defaultValue !== null) {
        if (t === 'list') {
          out[f.fieldName] = Array.isArray(f.defaultValue)
            ? (f.defaultValue as string[])
            : typeof f.defaultValue === 'string'
              ? [f.defaultValue]
              : []
        } else {
          out[f.fieldName] = f.defaultValue as string | number | boolean
        }
      } else if (t === 'boolean') {
        out[f.fieldName] = false
      } else if (t === 'list') {
        out[f.fieldName] = []
      } else {
        out[f.fieldName] = ''
      }
    }
    return out
  }, [defaultValues, allFields])

  const form = useForm<DynamicFormValues>({
    resolver: zodSchema ? (zodResolver(zodSchema) as any) : undefined,
    defaultValues: initialDefaults,
  })

  // All hooks are above — conditional returns below are safe
  if (schemaLoading && !schema) {
    return <p style={formStyles.loading}>Cargando schema…</p>
  }
  if (schemaError && !schema) {
    return <p style={formStyles.error}>{schemaError}</p>
  }
  if (!allFields.length) {
    return <p style={formStyles.error}>Schema sin campos o no encontrado.</p>
  }

  const sortedFields = [...allFields].sort((a, b) => a.displayOrder - b.displayOrder)

  return (
    <FormProvider {...form}>
      <form
        style={formStyles.form}
        onSubmit={form.handleSubmit((values) => onSubmit(values))}
      >
        {sortedFields.map((field) => (
          <DynamicField key={field.id} field={field} projectId={projectId} />
        ))}
        <div style={formStyles.actions}>
          <button type="submit" style={formStyles.submit}>
            {submitLabel}
          </button>
          <button
            type="button"
            style={formStyles.reset}
            onClick={() => form.reset(defaultValues ?? {})}
          >
            Restablecer
          </button>
        </div>
      </form>
    </FormProvider>
  )
}
