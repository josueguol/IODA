# 013 - Media Fixes N Features

## Objetivo

Corregir y evolucionar integralmente el campo `media` del CMS para soportar restricciones por tipo/categoria/MIME/extensión en schemas, resolver almacenamiento portable en contenedores y habilitar provider remoto en **DigitalOcean Spaces Object Storage** (API compatible S3) sin romper el flujo local actual.

La fase debe mantener compatibilidad hacia atrás, cumplir DDD + Clean Architecture y evitar acoplamientos entre dominio e infraestructura.
Además, el campo `media` se define explícitamente como relación **uno a uno** (un solo archivo por campo).

## Alcance

- Incluye:
  - Diseño de contrato de configuración avanzada para `fieldType=media`.
  - Validaciones backend de schema y de contenido contra reglas de media.
  - UX de configuración en schema designer y filtrado dinámico en MediaPicker.
  - Normalización de ruta local de almacenamiento y guía de volumen Docker persistente.
  - Diseño e implementación de storage provider seleccionable (`local` / `do_spaces`).
- No incluye:
  - Migración masiva automática de objetos existentes a DigitalOcean Spaces en esta fase (solo estrategia + tooling base).
  - Rediseño del módulo completo de permisos fuera del alcance de media.

## Entregables de la fase

- `PLAN_EJECUCION.md`
- `TAREAS_POR_AGENTE.md`
- `ANALISIS_REQUERIMIENTO.md`
- `DECISIONES_APROBADAS.md`
- `GUIA_OPERATIVA.md`
- `ESTADO.md`
- `QA_GATE_REPORT.md`
- Evidencias técnicas (builds, pruebas funcionales, logs de contenedores, configuración por entorno).

## Criterios de aceptación

- [ ] Campo `media` configurable por categorías y lista explícita de MIME/extensiones.
- [ ] Validación backend aplicada en schema y en create/update de contenido.
- [ ] Frontend permite configurar restricciones y respeta filtros al seleccionar/subir archivo.
- [ ] Ruta de almacenamiento local configurable por ambiente sin hardcode.
- [ ] Proveedor DigitalOcean Spaces habilitable por configuración sin romper provider local.
- [ ] QA gate ejecutado con evidencia y decisión formal.

## Dependencias

- `docs/CONSULTORIA/architecture/principios-cms.md`
- `ai/memory/project.context.md`
- `ai/memory/decisions.log.md`
- `ai/agents/orchestrator.agent.md`
- `ai/agents/fullstack.agent.md`
- `ai/agents/qa-tester.agent.md`

## Riesgos conocidos

- Riesgo de discrepancia FE/BE en validación de tipos de archivo si no hay contrato canónico.
- Riesgo operativo de pérdida de archivos si no se monta volumen persistente en local/QA.
- Riesgo de seguridad si credenciales de DigitalOcean Spaces se manejan fuera de secretos por entorno.

## Estado actual

- Estado: `Completado (Aprobado con observaciones)`
- Última actualización: `2026-03-12`
- Responsable: `Gobernanza Técnica / Orquestador`

## Referencias

- ADR-011 (actor desde JWT, no desde body) - requiere alineación en upload media.
- ADR-014 (API-first, DTOs y ProblemDetails).
- ADR-018 (schema-driven runtime).
