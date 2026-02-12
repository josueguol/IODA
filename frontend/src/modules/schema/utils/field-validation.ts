import { z } from 'zod'
import type { FieldDefinition, ValidationRules } from '../../core/types'

/** Convierte ValidationRules del backend en restricciones Zod. */
function validationToZod(rules: ValidationRules | null, isRequired: boolean, fieldType: string): z.ZodTypeAny {
  const typeLower = fieldType.toLowerCase()

  switch (typeLower) {
    case 'string':
    case 'richtext':
    case 'text': {
      let base: z.ZodTypeAny = z.string()
      if (rules) {
        const min = rules['minLength'] as number | undefined
        const max = rules['maxLength'] as number | undefined
        const pattern = rules['pattern'] as string | undefined
        if (min != null) base = (base as z.ZodString).min(min)
        if (max != null) base = (base as z.ZodString).max(max)
        if (pattern != null) base = (base as z.ZodString).regex(new RegExp(pattern))
      }
      if (!isRequired) base = (base as z.ZodString).optional().or(z.literal(''))
      return base
    }
    case 'number':
    case 'integer': {
      let base: z.ZodTypeAny = z.coerce.number()
      if (!isRequired) base = base.optional().nullable()
      if (rules) {
        const min = rules['min'] as number | undefined
        const max = rules['max'] as number | undefined
        if (min != null) base = (base as z.ZodNumber).min(min)
        if (max != null) base = (base as z.ZodNumber).max(max)
      }
      return base
    }
    case 'boolean':
      return isRequired ? z.boolean() : z.boolean().optional()
    case 'date':
    case 'datetime':
      return isRequired ? z.string().min(1, 'Requerido') : z.string().optional().nullable()
    case 'enum':
      return isRequired ? z.string().min(1, 'Requerido') : z.string().optional().nullable()
    case 'json':
      return z.union([z.string(), z.record(z.string(), z.unknown())]).optional().nullable()
    case 'reference':
      return z.string().optional().nullable()
    case 'list':
      return isRequired
        ? z.array(z.string()).min(1, 'Al menos un elemento')
        : z.array(z.string()).optional().nullable()
    default:
      return isRequired ? z.string().min(1, 'Requerido') : z.string().optional()
  }
}

/** Construye un objeto Zod a partir de las definiciones de campos. */
export function buildZodSchema(fields: FieldDefinition[]): z.ZodObject<Record<string, z.ZodTypeAny>> {
  const shape: Record<string, z.ZodTypeAny> = {}
  const sorted = [...fields].sort((a, b) => a.displayOrder - b.displayOrder)
  for (const field of sorted) {
    shape[field.fieldName] = validationToZod(
      field.validationRules ?? null,
      field.isRequired,
      field.fieldType
    )
  }
  return z.object(shape)
}

export type DynamicFormValues = Record<
  string,
  string | number | boolean | string[] | null | undefined
>
