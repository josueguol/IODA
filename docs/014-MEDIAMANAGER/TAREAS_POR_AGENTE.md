# TAREAS_POR_AGENTE - 014 MediaManager

Estado: `Completado`

## 1) Gobernanza / Arquitectura (`ai/agents/orchestrator.agent.md`)

- [x] Clasificar impacto y riesgos de iniciativa.
- [x] Definir blueprint funcional/tecnico del modulo.
- [x] Definir roadmap por fases y estrategia de transicion.
- [x] Definir decisiones para memoria/ADR.
- [x] Supervisar implementacion por etapas y gates.

## 2) Fullstack (`ai/agents/fullstack.agent.md`)

### Fase 1 (obligatorio)
- [x] Frontend: agregar menu/ruta `Multimedia` despues de `Contenido`.
- [x] Frontend: modulo listado + filtros + preview + upload + editar metadata.
- [x] Backend: endpoints base MediaManager v1.
- [x] Backend: reemplazo de archivo por version (id estable).
- [x] Backend: enforce `media` 1:1 en contenidos.
- [x] Infra: config portable local + do_spaces.

### Fase 2
- [x] Pipeline asincrono para derivados de imagen/video/audio.
- [x] Estados de procesamiento y reintentos.

### Fase 3
- [x] Integracion de campo `media` y RichTextEditor al selector central de Multimedia.

### Fase 4
- [x] Retention/lifecycle para archivos no referenciados (`cleanup-orphans`, dry-run/apply).
- [x] Migracion controlada local -> DO Spaces (script operable + runbook).
- [x] Observabilidad operativa (health live/ready + métricas lifecycle).
- [x] Smoke de carga y resiliencia API.

## 3) QA Gatekeeper (`ai/agents/qa-tester.agent.md`)

### Gate Fase 1
- [x] Navegacion visible: `Dashboard > Contenido > Multimedia > Publicar`.
- [x] Listado con filtros y estados UX (loading/error/empty/success).
- [x] Upload/preview/edit metadata/reemplazo OK.
- [x] Validacion backend de restricciones media OK.
- [x] Regresion basica de contenido existente OK.

### Gate Fase 2+
- [x] Procesamiento asincrono validado por tipo de medio.
- [x] Metricas y trazabilidad de jobs validadas.
