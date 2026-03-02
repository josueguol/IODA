# Análisis del requerimiento – Schemas Corrections

Fecha: 2026-02-27
Fase: Análisis (sin implementación)

## 1) Entendimiento del cambio solicitado

Se requiere consolidar un set de campos base para todos los contenidos y asegurar herencia/consistencia entre esquemas, contemplando además capacidades ya existentes (versionado, auditoría y estados).

### Campos de contenido requeridos

- `title` (string): ya existe en entidad `Content`.
- `slug` (string): hoy se genera desde `title` en backend; se solicita que venga desde frontend.
- `content` (contenido principal).
- `section` (opcional, única, representa sección principal; si se filtra por sección padre, incluir contenidos de secciones hijas).
- `tags` (opcional, múltiples).

### Campos de propiedades requeridos

- `urls` (array, únicas por sitio, default basado en `title`; soporte por sitio compartido y personalizable).
- `site_owner` (sitio propietario por defecto).
- `sites_shared` (array de sitios compartidos).
- `status` (draft/published/trash/otros estados de ciclo de vida).
- `created_at`, `updated_at`, `published_at`.

### Reglas adicionales indicadas

- Mantener versionado.
- Mantener auditoría (`createdBy`, `updatedBy`, `publishedBy`).
- Eliminar por ahora plantillas dinámicas de URI (ej. `{hierarchy}/{created_at}/{title}`).
- Regla de URL final por defecto: `site.com/{slug}` con slug derivado de título (o recibido explícitamente si se aprueba contrato).

---

## 2) Estado actual detectado en backend

### Ya existente (alineado)

- En `Content` existen:
  - `Title`, `Slug`, `Status`, `CreatedAt`, `UpdatedAt`, `PublishedAt`.
  - `CreatedBy`, `UpdatedBy`, `PublishedBy`.
  - `CurrentVersion` + historial de `ContentVersion`.
- Existen relaciones y soporte para:
  - `TagIds` (múltiples, opcionales).
  - `HierarchyIds` (clasificación, múltiples, opcionales).
  - `SiteId` + `SiteIds` (asignaciones de sitio).
- `DefaultSchemaFields` ya sugiere `title` y `content`.

### Huecos respecto al requerimiento

- `slug` no viene en contrato de create/update; hoy se calcula con `Slug.FromTitle(title)`.
- No existe aún un modelo explícito de `urls` por sitio dentro de `Content`.
- `site_owner` y `sites_shared` no están modelados explícitamente como conceptos de dominio (hay cercanos: `SiteId`/`SiteIds`).
- `section única principal` no existe como concepto directo (hoy hay jerarquías M:N).
- Filtro por sección padre incluyendo hijas requiere lógica de consulta explícita.

---

## 3) Decisiones de diseño que deben confirmarse antes de implementar

1. `slug` como fuente de verdad:
- Opción A: backend sigue generándolo desde `title` (contract actual, más seguro).
- Opción B: frontend envía `slug` y backend valida/normaliza/garantiza unicidad.
- Recomendación: B con fallback a A (si `slug` vacío, calcularlo).

2. `section` (única principal):
- Opción A: nuevo campo `PrimaryHierarchyId` en `Content`.
- Opción B: mantener M:N actual y marcar una relación como `is_primary`.
- Recomendación: B para no romper modelo actual de clasificación.

3. `urls` por sitio:
- Opción A: `Dictionary<siteId, slug/url>` JSONB en `Content`.
- Opción B: tabla dedicada `content_urls` con constraints de unicidad por `(site_id, slug)`.
- Recomendación: B por integridad y escalabilidad.

4. `site_owner` / `sites_shared`:
- Opción A: mapear semánticamente sobre `SiteId` (owner) + `ContentSites` (shared).
- Opción B: crear conceptos nuevos adicionales.
- Recomendación: A para evitar duplicación conceptual.

---

## 4) Riesgos principales

- Riesgo de breaking change si se modifica create/update content sin versionar contrato.
- Riesgo de incoherencia URL si no se define unicidad por sitio y reglas de conflicto.
- Riesgo de complejidad innecesaria si se duplica modelo de sitios (owner/shared vs site/siteIds).
- Riesgo de regresión en publicación si URL final no queda unificada con Publishing.

---

## 5) Criterio de éxito funcional

- Todo contenido mantiene metadatos base existentes (status, timestamps, versionado, actor).
- Existe soporte explícito de `section` principal opcional + `tags` múltiples.
- Existe soporte de URLs por sitio con unicidad y default `/{slug}`.
- Compartir contenido entre sitios permite URL propia por cada sitio compartido.
- Filtro por sección padre incluye contenidos asignados a secciones hijas.
