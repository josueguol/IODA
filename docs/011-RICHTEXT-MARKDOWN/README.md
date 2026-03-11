# 011 - RichText Markdown con BlockNote

## Objetivo

Usar `richtexteditor` como unico componente de contenido enriquecido basado en BlockNote, con serializacion Markdown y arquitectura schema-driven.

El nuevo componente debe habilitar bloques base de edicion, soporte multicolumna (2 y 3 columnas) y formateo consistente por bloque segun capacidades de toolbar de BlockNote.

## Alcance

- Incluye:
  - Analisis funcional/tecnico del reemplazo `richtext` -> `richtexteditor`.
  - Plan de trabajo previo para `ai/agents/fullstack.agent.md`.
  - Plan de validacion y gate previo para `ai/agents/qa-tester.agent.md`.
  - Definicion de estrategia de rollout/rollback y riesgos.
- No incluye:
  - Implementacion de codigo en este documento.
  - Ejecucion de migracion productiva.

## Entregables de la fase

- `ANALISIS_REQUERIMIENTO.md`
- `DECISIONES_APROBADAS.md`
- `PLAN_EJECUCION.md`
- `TAREAS_POR_AGENTE.md`
- `ESTADO.md`
- `QA_GATE_REPORT.md`
- `ROLLOUT_RUNBOOK.md`
- `MIGRACION_LEGACY_CHECKLIST.md`

## Criterios de aceptacion

- [x] Requerimiento descompuesto en impacto funcional, tecnico y contractual.
- [x] Plan de ejecucion incluye tareas fullstack (backend+frontend) y restricciones de arquitectura.
- [x] Plan QA incluye cobertura happy path, error path y no regresion.
- [x] Estrategia de reemplazo del `richtext` queda explicita y trazable.
- [x] El runtime queda unificado en `richtexteditor` sin retrocompatibilidad legacy.

## Dependencias

- `docs/CONSULTORIA/architecture/principios-cms.md`
- `ai/agents/orchestrator.agent.md`
- `ai/agents/fullstack.agent.md`
- `ai/agents/qa-tester.agent.md`
- Referencia funcional BlockNote toolbar: `https://www.blocknotejs.org/examples/ui-components/formatting-toolbar-buttons`

## Riesgos conocidos

- Riesgo de diferencias de render entre editor y consumo headless si no se define contrato canonico.
- Riesgo de recreacion manual de datos de prueba tras retirar `richtext`.

## Estado actual

- Estado: `En progreso`
- Ultima actualizacion: `2026-03-09`
- Responsable: `Arquitectura/Orchestrator`

## Referencias

- `docs/CONSULTORIA/architecture/principios-cms.md`
- `ai/agents/orchestrator.agent.md`
- `docs/011-RICHTEXT-MARKDOWN/DECISIONES_APROBADAS.md`
