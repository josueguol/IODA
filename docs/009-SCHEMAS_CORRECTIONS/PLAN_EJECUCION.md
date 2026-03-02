# Plan de ejecución – Gobernanza Técnica + QA Gatekeeper

Fecha: 2026-02-27
Estado: Aprobado en definición (listo para implementación)

## Fase 0 – Gobernanza técnica (obligatoria)

Referencia de decisiones cerradas: `DECISIONES_APROBADAS.md`.

Objetivo: cerrar decisiones de diseño para evitar retrabajo.

Estado fase: Completada.

Entregables:

- Decisión de contrato de `slug` (backend-only vs frontend+backend).
- Decisión de modelado `section` principal.
- Decisión de modelado `urls` por sitio.
- Mapeo formal `site_owner` y `sites_shared` a modelo actual.
- ADR si la decisión impacta contratos o dominio.

Gate salida:

- Decisiones aprobadas por negocio.
- Lista de cambios de contrato y migraciones definida.

---

## Fase 1 – Backend dominio + persistencia

Objetivo: modelar datos y reglas base sin romper lo actual.

Bloques:

1. Dominio `Content`:
- `PrimarySection` (según decisión aprobada).
- URLs por sitio (tabla dedicada y unicidad por sitio+ruta).
- Reglas de unicidad y normalización de slug/url.

2. Persistencia:
- Migraciones (nuevas tablas/columnas/índices).
- Constraints de unicidad por sitio.

3. Aplicación/API:
- Ajuste create/update content.
- Queries por sección padre con expansión de hijas.

Gate salida:

- Compila backend.
- Migraciones aplican.
- Smoke tests de create/update/list/filter pasan.

---

## Fase 2 – Frontend y contratos

Objetivo: adaptar formularios y filtros al nuevo contrato.

Bloques:

- Form create/edit content:
  - `slug` editable (prenormalizado en front, validado en backend).
  - `section` principal opcional.
  - `tags` múltiples.
  - URLs por sitio owner/shared.
- Listados:
  - filtro por sección padre incluyendo hijas.

Gate salida:

- Build frontend OK.
- Flujo UI create/edit/publicar validado.

---

## Fase 3 – QA Gatekeeper completo

Objetivo: aprobar o rechazar cambio con evidencia.

Cobertura mínima:

1. Happy path:
- Crear contenido con defaults.
- Publicar y resolver URL por sitio owner.

2. Shared site:
- Compartir contenido y asignar URL propia en sitio compartido.

3. Validaciones:
- Rechazar URL duplicada por mismo sitio.
- Rechazar slug/url inválidos.

4. Filtros:
- Sección padre devuelve contenidos de secciones hijas.

5. No regresión:
- versionado, timestamps, actor y status se mantienen.

Decisión QA:

- `Aprobado` / `Aprobado con condiciones` / `Rechazado`.

---

## Estrategia de rollout

- Implementar por feature flag funcional si el impacto de contrato es alto.
- Migración backward-compatible primero.
- En caso de cambio breaking, publicar versión de endpoint.
