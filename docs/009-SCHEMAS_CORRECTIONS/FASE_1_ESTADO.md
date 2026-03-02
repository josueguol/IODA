# Fase 1 – Estado de implementación (backend dominio + persistencia)

Fecha: 2026-02-28  
Estado: Implementación técnica completada (pendiente validación funcional en Docker)

## Implementado

1. `slug` en contrato + dominio:
- `CreateContent` y `UpdateContent` aceptan `slug`.
- Backend mantiene fallback desde `title` si `slug` viene vacío.
- Validación de `slug` en dominio: solo `a-z`, `0-9`, `-`, `_`.
- Fallback desde título con normalización robusta (incluye remoción de diacríticos).

2. `section` principal única y opcional:
- Se agregó `is_primary` a relación `content_hierarchies`.
- Se agregó índice único filtrado para asegurar máximo una principal por contenido.
- Se soporta `PrimaryHierarchyId` en create/update.

3. URLs por sitio (owner/shared):
- Nueva entidad `ContentSiteUrl`.
- Nueva tabla conceptual `content_site_urls` (config EF + repositorio).
- Reglas de path por backend: normalización y validación.
- Unicidad por `(site_id, path)`.
- Resolución de contenido publicado por `site + path`.

4. Site owner / sites shared:
- Se mantiene mapeo semántico:
  - `site_owner` => `Content.SiteId`
  - `sites_shared` => `ContentSites`
- DTO de contenido ahora expone `SiteUrls` y `PrimaryHierarchyId`.

5. Filtro por sección padre con hijas:
- `ListContentByProject` ahora acepta `sectionId`.
- Se expanden descendientes desde jerarquía padre.
- Se filtran contenidos asociados a cualquier jerarquía del scope.

## Pendiente para cerrar Fase 1

1. Pruebas mínimas (QA gatekeeper técnico):
- rechazar slug inválido
- rechazar path duplicado por sitio
- validar fallback slug desde título
- validar filtro por sección padre con hijos

2. Validación en entorno Docker integrado:
- levantar servicios con tu flujo (`docker compose --profile services up -d --build`)
- smoke de endpoints create/update/list/get-by-path

## Criterio de cierre de Fase 1

- Compilación backend exitosa.
- Migración aplicada sin errores.
- Smoke tests funcionales backend en verde.

## Evidencia técnica disponible

- Build backend OK (`IODA.Core.API`):
  - 0 errores, 0 warnings.
- Migración creada:
  - `20260228051415_AddContentSiteUrlsAndPrimaryHierarchy`.
- Snapshot EF actualizado:
  - `CoreDbContextModelSnapshot`.
