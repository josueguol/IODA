# DECISIONES_APROBADAS - Etapa 0

Fecha: 2026-03-07
Estado: `Aprobado para pasar a Etapa 1 (Fullstack)`

Marco aplicado:
- `docs/CONSULTORIA/architecture/principios-cms.md`
- `ai/agents/orchestrator.agent.md`

## D-011-01: Contrato canonico del campo nuevo

- Decision:
  - Se define `richtexteditor` como tipo único de editor enriquecido.
  - El formato canonico persistido sera `markdown`.
  - Para bloques no representables en markdown puro (`table`, `media`, `embed`, `component module`, `columns`) se agrega un `metadata` estructurado en el mismo valor del campo.
- Motivo:
  - Mantener API-first/headless y portabilidad del contenido.
  - Evitar perdida de semantica en bloques avanzados.
- Impacto:
  - Backend: DTO/validadores del tipo de campo.
  - Frontend: serializacion/deserializacion consistente.

## D-011-02: Retiro inmediato de `richtext` (sin retrocompatibilidad)

- Decision:
  - No hay fase transitoria de lectura dual.
  - `richtext` se elimina del runtime backend/frontend y del catalogo de schema.
  - Se acepta reinicio/limpieza de contenido de desarrollo para evitar carga de migración.
- Motivo:
  - Acelerar iteración en fase de desarrollo y concentrar QA en el flujo final.
- Impacto:
  - Frontend: solo render/edición de `richtexteditor`.
  - Backend: solo validación/contrato de `richtexteditor`.

## D-011-03: Política de datos legacy

- Decision:
  - Se cancela migración on-write y batch en esta fase.
  - El contenido de pruebas se recrea directamente con `richtexteditor`.
- Motivo:
  - Reducir esfuerzo no esencial mientras el producto aún no entra a producción.
- Impacto:
  - Se elimina dependencia de scripts de conversión en este sprint.

## D-011-04: Seguridad para media/embed/module

- Decision:
  - Validacion en backend obligatoria:
    - Allowlist de proveedores de embed.
    - Sanitizacion de contenido embebido.
    - Limites de tamano del payload.
  - Frontend solo ayuda UX; no define la politica de seguridad.
- Motivo:
  - Cumplir `security by design` y reducir riesgo XSS/inyeccion.
- Impacto:
  - QA debe ejecutar pruebas negativas de seguridad antes de aprobar gate.

## Condiciones para iniciar Etapa 1 (Fullstack)

- [x] Contrato canonico definido.
- [x] Estrategia de compatibilidad/deprecacion definida.
- [x] Politica de migracion definida.
- [x] Controles de seguridad definidos.
- [x] Decision registrada en memoria arquitectonica (`ai/memory/decisions.log.md`).

## Riesgos residuales aceptados

- Pérdida de acceso a contenido legacy de pruebas al retirar `richtext` sin conversión.
- Necesidad de recrear data de prueba para validar flujos completos con el editor nuevo.
