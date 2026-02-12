/**
 * Re-exportación de tipos de schema desde core (alineados con Core API).
 * El módulo schema añade store y componentes; los tipos DTO viven en core.
 */
export type {
  ContentSchema,
  ContentSchemaListItem,
  FieldDefinition,
  ValidationRules,
} from '../core/types'
