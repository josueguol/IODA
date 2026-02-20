/**
 * Tipos alineados con la Core API (ProjectDto, EnvironmentDto, ContentSchemaDto, FieldDefinitionDto).
 * Los GUIDs se representan como string en JSON.
 */

export interface Project {
  id: string
  publicId: string
  name: string
  slug: string
  description: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string | null
  createdBy: string
}

export interface Environment {
  id: string
  publicId: string
  name: string
  slug: string
  description: string | null
  isActive: boolean
  projectId: string
  createdAt: string
  updatedAt: string | null
}

export interface Site {
  id: string
  publicId: string
  projectId: string
  environmentId: string | null
  name: string
  domain: string
  subdomain: string | null
  subpath: string | null
  themeId: string | null
  /** Plantilla de URL para resolución por path. Ej: /{slug} */
  urlTemplate: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string | null
  createdBy: string
}

export interface MediaItem {
  id: string
  publicId: string
  projectId: string
  fileName: string
  displayName: string | null
  contentType: string
  sizeBytes: number
  storageKey: string
  version: number
  metadata: Record<string, unknown> | null
  createdAt: string
  createdBy: string
}

/** Reglas de validación por nombre (ej. minLength, maxLength, min, max, pattern). */
export type ValidationRules = Record<string, unknown>

export interface FieldDefinition {
  id: string
  fieldName: string
  /** Etiqueta visible en la UI (ej. "Descripción corta"). */
  label: string
  /** Clave técnica, kebab-case, única en el schema (ej. "descripcion-corta"). */
  slug: string
  fieldType: string
  isRequired: boolean
  defaultValue: unknown
  helpText: string | null
  validationRules: ValidationRules | null
  displayOrder: number
}

export interface ContentSchema {
  id: string
  publicId: string
  projectId: string
  schemaName: string
  schemaType: string
  description: string | null
  parentSchemaId: string | null
  schemaVersion: number
  isActive: boolean
  createdAt: string
  updatedAt: string | null
  createdBy: string
  fields: FieldDefinition[]
  /** Campos heredados del schema padre (resueltos por el backend). */
  inheritedFields: FieldDefinition[] | null
}

export interface ContentSchemaListItem {
  id: string
  publicId: string
  schemaName: string
  schemaType: string
  parentSchemaId: string | null
  schemaVersion: number
  isActive: boolean
}

/** Campo para crear schema. POST /api/projects/{projectId}/schemas */
export interface CreateSchemaFieldDto {
  label: string
  slug: string
  fieldType: string
  isRequired?: boolean
  defaultValue?: unknown
  helpText?: string | null
  validationRules?: Record<string, unknown> | null
  displayOrder?: number
}

/** Sugerencia de campo por defecto. GET /api/projects/{projectId}/schemas/default-fields */
export interface DefaultFieldSuggestionDto {
  label: string
  slug: string
  fieldType: string
}

/** Request para crear schema. POST /api/projects/{projectId}/schemas */
export interface CreateSchemaRequest {
  schemaName: string
  schemaType: string
  description?: string | null
  fields: CreateSchemaFieldDto[]
  createdBy: string
  parentSchemaId?: string | null
}

/** Contenido completo (ContentDto). */
export interface Content {
  id: string
  publicId: string
  projectId: string
  environmentId: string
  siteId: string | null
  parentContentId: string | null
  schemaId: string
  title: string
  slug: string
  status: string
  contentType: string
  fields: Record<string, unknown>
  currentVersion: number
  createdAt: string
  updatedAt: string | null
  publishedAt: string | null
  createdBy: string
  updatedBy: string | null
  publishedBy: string | null
  tagIds: string[]
  hierarchyIds: string[]
  siteIds: string[]
}

/** Jerarquía/categoría del proyecto (HierarchyDto). */
export interface Hierarchy {
  id: string
  projectId: string
  name: string
  slug: string
  description: string | null
  imageUrl: string | null
  parentHierarchyId: string | null
}

/** Item de lista (ContentListItemDto). */
export interface ContentListItem {
  id: string
  publicId: string
  title: string
  slug: string
  status: string
  contentType: string
  siteId: string | null
  parentContentId: string | null
  createdAt: string
  publishedAt: string | null
}

/** Etiqueta del proyecto (TagDto). */
export interface Tag {
  id: string
  projectId: string
  name: string
  slug: string
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages?: number
  hasNextPage?: boolean
  hasPreviousPage?: boolean
}

/** Versión de contenido (ContentVersionDto). */
export interface ContentVersion {
  id: string
  contentId: string
  versionNumber: number
  title: string
  fields: Record<string, unknown>
  status: string
  createdAt: string
  createdBy: string
  comment: string | null
}
