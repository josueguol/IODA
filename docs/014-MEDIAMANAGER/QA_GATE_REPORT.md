# QA_GATE_REPORT

## 1. Datos del gate

- Fase: `014-MEDIAMANAGER`
- Fecha: `2026-03-14`
- Responsable QA: `QA Gatekeeper`
- Version/commit evaluado: `working tree local (sin commit final)`

## 2. Alcance validado

- Flujo(s) evaluados:
  - Gate de diseno/arquitectura (Etapa 0).
  - Trazabilidad de plan por fases.
  - Etapa 1 E2E API: upload, list, get-by-id, patch metadata, replace file/version, download file.
  - Etapa 2 smoke tecnico: estado asinc y variantes logicas en metadata.
  - Checklist smoke manual consolidado y documentado en carpeta de fase.
- Fuera de alcance:
  - Automatizacion E2E formal integrada en pipeline CI (el smoke se ejecuto como script local).

## 3. Checklist de gate

- [x] Build backend exitoso.
- [x] Build frontend exitoso.
- [x] Migraciones aplicadas y verificadas.
- [x] Endpoints criticos validados.
- [x] Permisos/autorizacion validados.
- [x] Regresion basica de flujos existentes.
- [x] No hay errores bloqueantes abiertos para iniciar Etapa 1.

## 4. Resultados de pruebas

| Caso | Resultado | Evidencia | Observaciones |
|------|-----------|-----------|---------------|
| Revision de diseno contra principios CMS | PASS | docs/014-MEDIAMANAGER/*.md | Sin conflictos directos con ADRs aceptadas. |
| Revision de plan por fases y responsables | PASS | PLAN_EJECUCION + TAREAS_POR_AGENTE | Roadmap incremental definido. |
| Build backend Core API | PASS | `dotnet build src/Services/Core/IODA.Core.API/IODA.Core.API.csproj` | Compilacion correcta sin errores. |
| Migraciones Core aplicadas | PASS | `docker compose --profile services up -d --build ioda-core-api` | Core API inicia y sirve endpoints tras rebuild/recreate. |
| Build frontend | PASS | `npm run build` | Compilacion Vite/TS correcta. |
| E2E API - Upload media | PASS | `POST /api/projects/{projectId}/media -> 201` | Media creado: `75ac8d7c-b1c6-4410-8abc-d970bed1c253`. |
| E2E API - Get/List media | PASS | `GET /media/{id} -> 200`, `GET /media -> 200` | El media aparece en listado paginado. |
| E2E API - Patch metadata | PASS | `PATCH /media/{id} -> 200` | `displayName` y `metadata` actualizados. |
| E2E API - Replace file/version | PASS | `POST /media/{id}/replace -> 200` | Version incremento `1 -> 2` y storageKey actualizado. |
| E2E API - File delivery | PASS | `GET /media/{id}/file -> 200` | Archivo descargable tras reemplazo. |
| Auth real de usuario QA registrado | PASS | `POST /api/auth/register -> 201`, `POST /api/auth/login -> 200` | Usuario sin permisos de contenido (esperado). |
| Authorization real para Core media con usuario QA | PASS | `GET /media -> 403` | Validacion de policy `content.edit` funcionando. |
| Auth admin real para cierre de smoke | PASS | `POST /api/auth/login -> 200` (`josue.guol@gmail.com`) | Token contiene `role=SuperAdmin` y claim `content.edit`. |
| Authorization real para Core media con admin | PASS | `GET /api/projects/{projectId}/media -> 200` | Acceso autorizado confirmado con cuenta admin real. |
| Etapa 2 - estado de procesamiento | PASS | upload image + polling GET media | `processingStatus`: `pending -> ready`. |
| Etapa 2 - variantes logicas | PASS | GET media metadata.variants | Imagen registra 5 variantes (`original`, `thumbnail`, `small`, `medium`, `large`). |
| Etapa 3 - build integracion RichText + Multimedia | PASS | `npm run build` | Compila selector central y slash item `Media from library`. |
| Etapa 3 - smoke UI insercion desde libreria | PASS | `node frontend/scripts/smoke_case9_playwright.mjs` | Flujo login->editor->insertar media->guardar->persistencia validado (`saveStatus=200`, `persistedMediaUrl=true`). |
| Checklist smoke manual consolidado | PASS | `docs/014-MEDIAMANAGER/SMOKE_MANUAL_ETAPA1.md` | Checklist ejecutado y trazabilidad por caso registrada con observaciones. |
| Etapa 3 - compilacion cross-service | PASS | `dotnet build` Core + Publishing + Indexing | Contrato de evento y consumidores compilan sin errores. |
| Etapa 3 - variante URL estable | PASS | `GET /media/{id}/file?variant=thumbnail -> 200` | Resolucion de variante desde metadata funcional. |
| Etapa 3 - E2E publish/indexing con fields enriquecidos | PASS | `unpublish -> 200`, `publish -> 200`, `GET elastic _doc/{contentId}` | Documento indexado contiene `_source.fields.image` con `id/url/contentType/displayName/version`. |
| Etapa 4 - lifecycle cleanup dry-run | PASS | `POST /media/cleanup-orphans {dryRun:true}` | Detecta huérfanos sin borrar (`orphanKeys=1`). |
| Etapa 4 - lifecycle cleanup apply | PASS | `POST /media/cleanup-orphans {dryRun:false,maxDeletes:1}` | Elimina huérfano de forma controlada (`deletedKeys=1`). |
| Etapa 4 - lifecycle cleanup post-check | PASS | `POST /media/cleanup-orphans {dryRun:true}` | Sin huérfanos remanentes (`orphanKeys=0`). |
| Etapa 4 - carga/resiliencia API smoke | PASS | `bash docs/014-MEDIAMANAGER/scripts/load_resilience_smoke.sh` | `LIST 2xx=80/80`, `FILE 2xx=80/80`, `5xx=0`. |

## 5. Defectos encontrados

| ID | Severidad | Estado | Descripcion | Accion |
|----|-----------|--------|-------------|--------|
| MM-014-001 | Media | Cerrado | Etapa 1 ejecutada tecnicamente (API E2E y build FE/BE) para validar contratos base. | Sin accion adicional. |
| MM-014-002 | Baja | Cerrado | Bloqueo inicial por falta de credenciales admin para validar acceso real. | Resuelto con login admin real y validacion de permisos por API. |
| MM-014-003 | Baja | Cerrado | Pendiente historico de validacion visual de integracion RichText + Multimedia. | Ejecutado mediante smoke automatizado; sustituido por hallazgo concreto MM-014-005. |
| MM-014-004 | Baja | Cerrado | Evidencia E2E de evento `ContentPublishedEventV1` consumido en Indexing con campos enriquecidos en ambiente completo. | Ejecutado escenario publicar->consumir->indexar con evidencia en Elasticsearch `_doc/{contentId}`. |
| MM-014-005 | Media | Cerrado | Guardado de contenido fallaba tras insertar media desde RichText por validacion de host embed (`localhost`). | Corregido permitiendo URLs internas de media (`/api/projects/{projectId}/media/{mediaId}/file`) y revalidado con smoke en PASS. |

## 6. Riesgo residual

- Riesgo de sobrealcance si se mezcla procesamiento pesado en MVP.
- Riesgo de regresion si no se versionan contratos durante integracion cross-modulo.
- Riesgo bajo: falta integrar estos smokes a CI para evitar regresiones silenciosas.
- Riesgo funcional: variantes actuales son logicas (sin archivos derivados reales), requiere etapa adicional para transcodificacion real.

## 7. Decision de gate

- Resultado: `APROBADO CON OBSERVACIONES`
- Motivo: Etapas 1-4 validadas en alcance local, sin defectos funcionales abiertos bloqueantes.
- Condiciones para liberar (si aplica):
  - Integrar `smoke_case9_playwright.mjs` y `load_resilience_smoke.sh` en pipeline CI/CD.

## 8. Firma

- QA: `Aprobado`
- Tech lead/arquitectura: `Aprobado (diseno)`
- Fecha: `2026-03-14`
