# PLAN_EJECUCION

## 1. Contexto

- Fase: `011-RICHTEXT-MARKDOWN`
- Objetivo resumido: usar `richtexteditor` (BlockNote) como único editor enriquecido con bloques base, multicolumna (2/3) y toolbar requerida.
- Fecha de inicio: `2026-03-07`
- Fecha objetivo: `2026-03-14`

## 2. Supuestos y restricciones

- Supuesto: BlockNote cubre toolbar base requerida y es extensible para bloques custom.
- Restriccion: respetar `docs/CONSULTORIA/architecture/principios-cms.md`.
- Restriccion: no introducir breaking changes en API sin versionado.
- Restriccion: seguridad de embeds/media validada en backend (no solo en frontend).
- Restriccion: no desplegar a `PROD` en esta fase; solo `DEV/QA`.
- Restriccion operativa: `DEV/QA` corre en local con backend por Docker (`docker compose --profile services up -d --build`) y frontend por Vite (`npm run dev`).

## 3. Plan por etapas

### Etapa 0: Gobernanza y contrato (orchestrator/arquitectura)
- Objetivo: cerrar decisiones tecnicas antes de ejecucion fullstack.
- Tareas:
  - [x] Definir contrato canonico del campo `richtexteditor`.
  - [x] Definir estrategia de retiro directo de `richtext` sin retrocompatibilidad (fase desarrollo).
  - [x] Definir politica de migracion de datos legacy.
  - [x] Definir controles de seguridad para embed/media/module.
- Evidencias esperadas:
  - `DECISIONES_APROBADAS.md` completado.
  - Memoria arquitectonica actualizada.
- Riesgos:
  - Decisiones ambiguas que generen retrabajo backend/frontend.

### Etapa 1: Ejecucion Fullstack (`ai/agents/fullstack.agent.md`)
- Objetivo: implementar soporte end-to-end sin romper arquitectura.
- Tareas Backend:
  - [x] Mantener un único tipo de editor enriquecido en contratos/validaciones (`richtexteditor`).
  - [x] Remover compatibilidad runtime de `richtext` legacy.
  - [x] Implementar validaciones server-side de estructura/sanitizacion para payload richtext editor + allowlist de embeds.
  - [x] Cancelar migracion/backfill legacy (ambiente reiniciable en desarrollo).
- Tareas Frontend:
  - [x] Integrar editor BlockNote como componente `RichtextEditor` con toolbar personalizada.
  - [x] Habilitar bloques base en slash/toolbar (`h2-h6`, quote, lists, paragraph, code, table, media, embed, component module) con ajustes de catálogo para CMS.
  - [x] Habilitar bloques multicolumna (2 y 3) con API oficial `@blocknote/xl-multi-column`.
  - [x] Remover `richtext` de alta nueva en schema designer y sin fallback legacy.
- Evidencias esperadas:
  - Builds backend/frontend en verde. (Cumplido)
  - Evidencia de flujos de authoring y rendering.
  - Nota de migracion ejecutable/repetible.
- Riesgos:
  - Diferencias de serializacion markdown para bloques complejos.
  - Dependencia de paquete XL multi-column (licenciamiento/uso controlado por proyecto).

### Etapa 2: QA Gate (`ai/agents/qa-tester.agent.md`)
- Objetivo: validar funcionalidad, seguridad y no regresion antes de aprobar.
- Tareas:
  - [ ] Validar happy path: creacion/edicion/publicacion con bloques base y multicolumna.
  - [ ] Validar error path: embeds no permitidos, payload invalido, limites de tamano.
  - [x] No aplica validacion legacy: `richtext` retirado del alcance funcional.
  - [ ] Validar autorizacion/permisos en operaciones de contenido.
  - [x] Ejecutar gate preliminar y emitir decision inicial (`APROBADO CON OBSERVACIONES`, ver `QA_GATE_REPORT.md`).
- Evidencias esperadas:
  - `QA_GATE_REPORT.md` con matriz de casos y hallazgos por severidad.
- Riesgos:
  - Cobertura QA incompleta en bloques complejos (table/embed/module).

### Etapa 3: Rollout controlado en desarrollo
- Objetivo: estabilizar `richtexteditor` sin soporte legacy.
- Tareas:
  - [x] Definir runbook operativo de rollout/rollback por entorno (`ROLLOUT_RUNBOOK.md`).
  - [x] Registrar cancelacion formal de migracion legacy (`MIGRACION_LEGACY_CHECKLIST.md`).
  - [ ] Activar rollout gradual en `DEV/QA` (sin `PROD`) con feature flag `VITE_ENABLE_RICHTEXT_EDITOR`.
  - [x] Cancelar migracion masiva legacy por decision de desarrollo.
  - [x] Retirar soporte runtime de `richtext`.
- Evidencias esperadas:
  - `ROLLOUT_RUNBOOK.md` ejecutado por entorno.
  - Reporte de conversion.
  - Registro de rollback probado.

## 4. Definicion de terminado (DoD)

- [x] Backend compilando y validaciones relevantes ejecutadas.
- [x] Frontend compilando y flujo principal validado.
- [x] `richtext` eliminado del runtime y de nuevas configuraciones de schema.
- [ ] Cambios documentados en `ESTADO.md`.
- [ ] QA gate aplicado con resultado registrado.

## 5. Rollout y rollback

- Rollout:
  - Paso 1: desplegar solo editor nuevo (`richtexteditor`).
  - Paso 2: habilitar nuevo tipo en schema/editor por entorno.
  - Paso 3: migrar datos y monitorear errores de render/serializacion.
  - Paso 4: mantener `richtext` fuera de runtime.
- Rollback:
  - Paso 1: desactivar feature flag del nuevo editor.
  - Paso 2: revertir frontend/backend del cambio si fuera necesario.
  - Paso 3: recrear datos de prueba si el entorno fue limpiado.

## 6. Registro de cambios del plan

- `2026-03-07`: creacion inicial del plan con etapas obligatorias de Fullstack y QA.
- `2026-03-07`: cierre de Etapa 0 con decisiones aprobadas y trazabilidad en memoria.
- `2026-03-08`: avance Etapa 1 backend/frontend (alias de tipos, `RichtextEditor`, toolbar/slash para CMS, multi-column oficial, builds OK).
- `2026-03-08`: ejecucion parcial de Etapa 2 (QA gate preliminar) con dictamen `APROBADO CON OBSERVACIONES` por evidencia de build/lint focalizado y pendientes funcionales globales.
- `2026-03-08`: preparación de Etapa 3 con runbook de rollout/rollback y checklist de migración legacy.
- `2026-03-08`: ajuste operativo del plan/runbook para entorno local `DEV/QA` con Docker Compose profile `services` + frontend local.
