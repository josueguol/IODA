# Fase 2 – Estado de implementación (frontend + contratos)

Fecha: 2026-03-02  
Estado: Implementado y compilando

## Ajustes implementados

1. Tipos frontend alineados con backend:
- `Content.primaryHierarchyId`
- `Content.siteUrls`
- tipo `ContentSiteUrl`

2. Cliente Core API actualizado:
- `getContentList` ahora acepta `sectionId`.
- `createContent` y `updateContent` aceptan:
  - `slug`
  - `primaryHierarchyId`
  - `siteUrls`

3. Página de listado y creación de contenido (`ContentListPage`):
- Campo `slug` editable con prenormalización.
- Selector de sección principal (`primaryHierarchyId`) a partir de jerarquías seleccionadas.
- Inputs de URL por sitio (owner/shared) con prenormalización.
- Filtro por sección (`sectionId`) en listado.

4. Página de edición de contenido (`EditContentPage`):
- Campo `slug` editable con prenormalización.
- Selector de sección principal.
- Inputs de URL por sitio (owner/shared).
- Visualización explícita de `site owner` y `sites shared`.

## Verificación técnica

- Build frontend OK (`npm --prefix frontend run build`).

## Pendiente de QA funcional en entorno Docker

- Validar create/edit con:
  - slug personalizado
  - sección principal
  - urls por sitio owner/shared
- Validar filtro por sección padre/hija en listado.
- Validar mensajes de error para colisión de URL por sitio.
