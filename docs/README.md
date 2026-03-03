# Indice maestro de documentacion

Este indice clasifica la documentacion para separar claramente:

- estado operativo vigente,
- historial util por fases,
- contenido deprecado.

Fecha de actualizacion: 2026-03-03.

## 1) Vigente operativo (fuente primaria)

Usar primero estos documentos para decisiones actuales.

- Arquitectura vigente:
  - `CONSULTORIA/architecture/principios-cms.md`
- Fase activa de correcciones de schemas:
  - `009-SCHEMAS_CORRECTIONS/README.md`
  - `009-SCHEMAS_CORRECTIONS/PLAN_EJECUCION.md`
  - `009-SCHEMAS_CORRECTIONS/FASE_3_QA_GATE_REPORT.md`
- Requerimientos frontend vigentes:
  - `010-FRONTEND-IMPROVEMENTS/REQUERIMIENTOS.md`

## 2) Historico util (trazabilidad de avance)

Documentos de fases previas que sirven para contexto y decisiones pasadas:

- `000-FASE_DE_CREACION_INICIAL/`
- `001-FASE_DE_MEJORAS/`
- `002-FASE_DE_SEGUIMIENTO/`
- `003-BUGFIXS/`
- `003-FASE_MEJORAS_PERMISOS/`
- `004-MEJORAS_COMUNICACION/`
- `005-MEJORAS_FRONTEND/`
- `006-SCHEME-N-SITECONFIG/`
- `008-SCHEMAS_BACKEND/`

Nota: `007-REDISENO_FRONTEND/` contiene artefactos de diseno (`.pen`), no bitacora Markdown.

## 3) Consulta puntual (no reemplaza estado vigente)

Documentacion de analisis/faq/recomendaciones que puede ser util como referencia:

- `CONSULTORIA/analysis/`
- `CONSULTORIA/faqs/`
- `CONSULTORIA/recommendations/`
- `CONSULTORIA/architecture/modular-monolith-vs-microservicios.md`

## 4) Archivado / deprecado

No usar como fuente de estado actual, salvo para auditoria historica:

- `DEPRECATED/`
  - `DEPRECATED/2026-03-03-legacy-audits/README.md`

## 5) Agentes archivados

Perfiles antiguos no vigentes:

- `ARCHIVED_AGENTS/`

El perfil consolidado actual esta en `ai/agents/orchestrator.agent.md` (fuera de `docs/`).

## 6) Regla de gobierno documental

Para mantener claridad del progreso:

- Cambios nuevos deben registrarse en la fase activa correspondiente.
- Si un documento queda desfasado, moverlo a `DEPRECATED/<fecha>-<motivo>/`.
- Evitar crear diagnosticos globales fuera de fase sin indicar fecha y estado del codigo.
