# 008 – Esquemas y Backend (Auditoría CMS)

Carpeta de la **auditoría de arquitectura** del CMS según las reglas definidas en `ai/agents/auditor.agent.md` (diseño schema-driven y basado en composición).

## Contenido

- **AUDITORIA_ESQUEMAS_BACKEND.md** – Resultado de la auditoría, violaciones clasificadas por severidad y tareas de mejora para el backend (esquemas, identidad estructural, capa de bloques).
- **TAREAS_AGENTE_FULLSTACK.md** – Desglose técnico de las tareas para el agente de desarrollo fullstack: archivos a modificar, contratos (DTOs, API), persistencia, frontend (tipos, API, UI) y criterios de aceptación por tarea.

## Uso

Consultar `AUDITORIA_ESQUEMAS_BACKEND.md` para:

1. Ver el análisis por regla (Single Root Model, Strict Layer Separation, Hierarchy Rules, Architectural Constraints).
2. Revisar hallazgos por severidad (Crítico, Alto, Medio, Bajo).
3. Planificar e implementar las tareas del backlog (C.1, C.2, H.1, M.1, M.2, L.1).

Para **implementación concreta** (agente fullstack o desarrollador), usar **TAREAS_AGENTE_FULLSTACK.md**: ahí se detallan por tarea los archivos, propiedades, comandos, endpoints, DTOs, migraciones y cambios de frontend sin ambigüedad.
