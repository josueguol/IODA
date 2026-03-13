import { Controller, useFormContext } from 'react-hook-form'
import type { FieldDefinition } from '../../core/types'
import { MediaPicker } from './MediaPicker'
import { ReferenceSelector } from './ReferenceSelector'
import { ListRepeater } from './ListRepeater'
import { RichtextEditor } from './RichtextEditor'
import { FormattedTextEditor } from './FormattedTextEditor'
import { config } from '../../../config/env'

const styles: Record<string, React.CSSProperties> = {
  field: { marginBottom: '1rem', color: 'var(--page-text)' },
  label: { display: 'block', fontWeight: 600, marginBottom: '0.25rem', fontSize: '0.875rem', color: 'var(--page-text)' },
  help: { fontSize: '0.75rem', color: 'var(--page-text-muted)', marginTop: '0.25rem' },
  error: { fontSize: '0.75rem', color: '#dc3545', marginTop: '0.25rem' },
  input: { width: '100%', maxWidth: 400, padding: '0.5rem', fontSize: '0.875rem', borderRadius: 4, border: '1px solid var(--input-border)', color: 'var(--input-text)', background: 'var(--input-bg)' },
  textarea: { width: '100%', maxWidth: 600, minHeight: 100, padding: '0.5rem', fontSize: '0.875rem', borderRadius: 4, border: '1px solid var(--input-border)', color: 'var(--input-text)', background: 'var(--input-bg)' },
  checkbox: { marginRight: '0.5rem' },
}

function getInputType(fieldType: string): string {
  const t = fieldType.trim().toLowerCase()
  if (t === 'number' || t === 'integer') return 'number'
  if (t === 'date') return 'date'
  if (t === 'datetime') return 'datetime-local'
  return 'text'
}

const fieldLabel = (field: FieldDefinition): string => field.label ?? field.slug

function normalizeFieldType(fieldType: string): string {
  const normalized = fieldType.trim().toLowerCase()
  if (normalized === 'formatted-text' || normalized === 'formatted_text') return 'formattedtext'
  return normalized
}

const defaultForType = (field: FieldDefinition): string | number | boolean | string[] => {
  const t = normalizeFieldType(field.fieldType)
  if (field.defaultValue !== undefined && field.defaultValue !== null) {
    if (t === 'list' && Array.isArray(field.defaultValue)) return field.defaultValue as string[]
    if (t === 'list') return typeof field.defaultValue === 'string' ? [field.defaultValue] : []
    return field.defaultValue as string | number | boolean
  }
  if (t === 'boolean') return false
  if (t === 'list') return []
  return ''
}

export function DynamicField({ field, projectId }: { field: FieldDefinition; projectId?: string }) {
  const { control, formState: { errors } } = useFormContext()
  const typeLower = normalizeFieldType(field.fieldType)
  const error = errors[field.slug]

  return (
    <div style={styles.field}>
      <Controller
        name={field.slug}
        control={control}
        defaultValue={defaultForType(field)}
        render={({ field: f }) => (
          <>
            {(() => {
              const shouldRenderRichtextEditor =
                config.enableRichtextEditor &&
                typeLower === 'richtexteditor'

              if (typeLower === 'boolean') {
                return (
                  <label style={{ display: 'flex', alignItems: 'center' }}>
                    <input
                      type="checkbox"
                      style={styles.checkbox}
                      checked={!!f.value}
                      onChange={(e) => f.onChange(e.target.checked)}
                      onBlur={f.onBlur}
                    />
                    <span style={styles.label}>{fieldLabel(field)}</span>
                  </label>
                )
              }

              if (typeLower === 'list') {
                return (
                  <>
                    <label style={styles.label}>
                      {fieldLabel(field)}
                      {field.isRequired && ' *'}
                    </label>
                    <ListRepeater
                      value={Array.isArray(f.value) ? f.value : []}
                      onChange={(arr) => f.onChange(arr)}
                      disabled={false}
                      placeholder={field.helpText ?? 'Valor'}
                      helpText={field.helpText}
                    />
                  </>
                )
              }

              if (typeLower === 'reference' && projectId) {
                return (
                  <>
                    <label style={styles.label}>
                      {fieldLabel(field)}
                      {field.isRequired && ' *'}
                    </label>
                    <ReferenceSelector
                      projectId={projectId}
                      value={typeof f.value === 'string' ? f.value : null}
                      onChange={(id) => f.onChange(id ?? '')}
                      validationRules={field.validationRules}
                    />
                  </>
                )
              }

              if (typeLower === 'media' && projectId) {
                return (
                  <>
                    <label style={styles.label}>
                      {fieldLabel(field)}
                      {field.isRequired && ' *'}
                    </label>
                    <MediaPicker
                      projectId={projectId}
                      value={typeof f.value === 'string' ? f.value : null}
                      onChange={(id) => f.onChange(id ?? '')}
                      allowUpload
                    />
                  </>
                )
              }

              if (shouldRenderRichtextEditor) {
                return (
                  <>
                    <label style={styles.label}>
                      {fieldLabel(field)}
                      {field.isRequired && ' *'}
                    </label>
                    <RichtextEditor
                      value={f.value}
                      onChange={(next) => f.onChange(next)}
                    />
                  </>
                )
              }

              if (typeLower === 'formattedtext') {
                return (
                  <>
                    <label style={styles.label}>
                      {fieldLabel(field)}
                      {field.isRequired && ' *'}
                    </label>
                    <FormattedTextEditor
                      value={f.value}
                      onChange={(next) => f.onChange(next)}
                    />
                  </>
                )
              }

              if (typeLower === 'text' || typeLower === 'json') {
                return (
                  <>
                    <label style={styles.label} htmlFor={field.slug}>
                      {fieldLabel(field)}
                      {field.isRequired && ' *'}
                    </label>
                    <textarea
                      id={field.slug}
                      style={styles.textarea}
                      value={f.value ?? ''}
                      onChange={(e) => f.onChange(e.target.value)}
                      onBlur={f.onBlur}
                      placeholder={field.helpText ?? undefined}
                      rows={typeLower === 'json' ? 6 : 4}
                    />
                  </>
                )
              }

              return (
                <>
                  <label style={styles.label} htmlFor={field.slug}>
                    {fieldLabel(field)}
                    {field.isRequired && ' *'}
                  </label>
                  <input
                    id={field.slug}
                    type={getInputType(field.fieldType)}
                    style={styles.input}
                    value={f.value ?? ''}
                    onChange={(e) =>
                      f.onChange(
                        typeLower === 'number' || typeLower === 'integer'
                          ? (e.target.value === '' ? undefined : Number(e.target.value))
                          : e.target.value
                      )
                    }
                    onBlur={f.onBlur}
                    placeholder={field.helpText ?? undefined}
                  />
                </>
              )
            })()}
            {field.helpText && <p style={styles.help}>{field.helpText}</p>}
            {error?.message && (
              <p style={styles.error}>{String(error.message)}</p>
            )}
          </>
        )}
      />
    </div>
  )
}
