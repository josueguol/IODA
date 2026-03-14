# PLAN_EJECUCION

## 1. Contexto

- Fase: `014-MEDIAMANAGER`
- Objetivo resumido: habilitar un Media Manager serio del CMS, con modulo frontend `Multimedia` y arquitectura backend evolucionable hacia microservicio dedicado.
- Fecha de inicio: `2026-03-13`
- Fecha objetivo: `2026-04-05`

## 2. Supuestos y restricciones

- Supuesto: se puede evolucionar desde capacidades actuales de media en Core sin ruptura de contratos.
- Restriccion: no romper arquitectura por microservicios reales.
- Restriccion: no logica de dominio en controllers.
- Restriccion: no acoplar dominio a filesystem o proveedor concreto.
- Restriccion: validacion critica siempre en backend.

## 3. Plan por etapas

### Etapa 0: Gobernanza y diseno
- Objetivo: cerrar blueprint funcional/tecnico y decisiones.
- Tareas:
  - [x] Clasificar impacto arquitectonico y operativo.
  - [x] Definir UX target del modulo `Multimedia`.
  - [x] Definir modelo de dominio/contratos base de `MediaManager`.
  - [x] Definir roadmap por fases y criterios de aceptacion.
- Evidencias esperadas:
  - `ANALISIS_REQUERIMIENTO.md`
  - `DISENO_MEDIAMANAGER.md`
  - `DECISIONES_APROBADAS.md`
- Riesgos:
  - Scope creep en Fase 1.

### Etapa 1: Base operativa minima (MVP)
- Objetivo: entregar modulo usable para gestion de media sin procesamiento pesado.
- Tareas:
  - [x] Frontend: ruta/menu `Multimedia`, listado, filtros basicos, preview, upload, edit metadata.
  - [x] Backend: API de media con CRUD operativo de metadata + upload/download + reemplazo logico.
  - [x] Backend: enforce `media` 1:1 por campo y reglas de tipo/mime/ext.
  - [x] Infra: storage provider seleccionable `local|do_spaces`.
- Evidencias esperadas:
  - Build BE/FE OK.
  - Flujos E2E de upload/seleccion/reemplazo funcionando.
- Riesgos:
  - Deuda de performance en listados sin paginacion/filtros server-side.

### Etapa 2: Procesamiento y derivados
- Objetivo: introducir pipeline asincrono de procesamiento.
- Tareas:
  - [x] Imagen: variantes (`thumbnail`, `small`, `medium`, `large`) en modo logico inicial.
  - [x] Video: variante `poster` logica inicial.
  - [x] Audio: variante `cover` logica inicial.
  - [x] Estados de procesamiento: `pending|processing|ready|failed`.
- Evidencias esperadas:
  - Jobs/eventos trazables por medio.
  - Derivados consultables por API.
- Riesgos:
  - Costos CPU/IO por conversiones.

### Etapa 3: Integraciones CMS
- Objetivo: integrar MediaManager como proveedor unico para otros modulos.
- Tareas:
  - [x] Campo `media` consume MediaManager en lugar de acceso directo legado.
  - [x] RichTextEditor consume selector de media centralizado.
  - [x] Publicacion/indexacion consumen URLs/derivados estables.
- Evidencias esperadas:
  - Contratos cross-modulo versionados.
- Riesgos:
  - Regresiones por transicion de contratos.

### Etapa 4: Endurecimiento operativo
- Objetivo: preparar operacion de produccion.
- Tareas:
  - [x] Politicas de retention/lifecycle para archivos no referenciados.
  - [x] Migracion controlada local -> DO Spaces.
  - [x] Observabilidad completa (metricas, trazas, alertas).
  - [x] Pruebas de carga y resiliencia.
- Evidencias esperadas:
  - Runbook y tablero de observabilidad.
- Riesgos:
  - Errores de operacion por credenciales/rotacion.

## 4. Definicion de terminado (DoD)

- [x] Backend compilando y pruebas relevantes ejecutadas.
- [x] Frontend compilando y flujo principal validado.
- [x] Cambios documentados en `ESTADO.md`.
- [x] QA gate aplicado con resultado registrado.

## 5. Rollout y rollback

- Rollout:
  - Paso 1: activar menu/ruta `Multimedia` con feature flag.
  - Paso 2: habilitar API base MediaManager en ambiente dev/qa.
  - Paso 3: activar integraciones progresivas por modulo.
- Rollback:
  - Paso 1: desactivar feature flag `multimedia_module`.
  - Paso 2: volver a endpoints/flujo legacy de media.
  - Paso 3: mantener archivos sin borrado fisico para recuperacion.

## 6. Registro de cambios del plan

- `2026-03-13`: creacion inicial del plan de ejecucion y roadmap por fases.
- `2026-03-13`: Etapa 1 implementada en frontend y backend (menu/ruta Multimedia, pagina MVP, PATCH metadata y replace de media).
- `2026-03-13`: Etapa 2 base implementada (cola asinc + worker + estados + variantes logicas por tipo de media).
- `2026-03-13`: Etapa 3 parcial implementada (integracion selector multimedia en RichTextEditor y consumo centralizado en campo media).
- `2026-03-13`: Etapa 3 completada tecnicamente (evento de publicacion con fields proyectados y consumer de indexing consumiendo fields enriquecidos con media URLs).
- `2026-03-13`: Etapa 3 validada E2E en local (`unpublish/publish` en Core + consumo Rabbit + documento Elasticsearch con `fields.image.url`).
- `2026-03-13`: smoke manual consolidado en `SMOKE_MANUAL_ETAPA1.md` con checklist ejecutado; queda pendiente validacion visual final de RichText + Multimedia con cuenta admin real.
- `2026-03-13`: validado login admin real y permisos por API (`content.edit` + acceso `GET /media`), cerrando bloqueo de credenciales; pendiente solo validacion visual final del Caso 9.
- `2026-03-13`: Caso 9 ejecutado de forma automatizada (Playwright) con cuenta admin real; detectado hallazgo `MM-014-005` por `400 Schema Validation Error` al guardar contenido con media insertada desde libreria (host embed `localhost` no permitido).
- `2026-03-13`: MM-014-005 corregido en backend y revalidado en PASS con `smoke_case9_playwright.mjs` (`saveStatus=200`, persistencia OK).
- `2026-03-13`: Etapa 4 implementada en alcance operativo: endpoint de lifecycle cleanup (`dry-run/apply`), scripts de migracion local->DO Spaces y smoke de carga/resiliencia (`80 req`, `5xx=0`).
- `2026-03-14`: cierre documental de fase: DoD completado, QA gate actualizado, estado final en `Completado`.
