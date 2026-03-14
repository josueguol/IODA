# ANALISIS_REQUERIMIENTO - 014 MediaManager

## 1. Clasificacion del cambio

- Arquitectura: `Alto`
  - Introduce bounded context dedicado y estrategia de transicion desde capacidades media en Core.
- Dominio: `Alto`
  - Define ciclo de vida de medio, metadatos y estado de procesamiento.
- Frontend: `Alto`
  - Nuevo modulo `Multimedia` con UX operativa completa.
- Backend: `Alto`
  - Nuevos contratos y capas de aplicacion para gestion multimedia.
- Contratos API: `Medio/Alto`
  - Se agregan endpoints nuevos y se mantiene compatibilidad legacy por transicion.
- Infraestructura: `Medio/Alto`
  - Persistencia local portable + opcion DO Spaces.
- Seguridad: `Medio`
  - Requiere permisos granulares y hardening de credenciales.
- Operacion: `Alto`
  - Requiere runbook, observabilidad y estrategia de migracion.

## 2. Analisis de impacto

### Compatibilidad hacia atras
- Mantener endpoints actuales de media durante Fase 1-2.
- Introducir API `MediaManager` en paralelo y migrar consumidores por fases.

### Impacto en schemas existentes
- Campo `media` mantiene semantica 1:1.
- No se introduce `mediaCollection` en este alcance.

### Validaciones backend
- Tipo, mime, extension, tamano y proyecto deben validarse siempre en backend.
- Reemplazo de media en campo debe persistir un solo `mediaId`.

### Persistencia actual
- No borrar archivos historicos al reemplazar en campo.
- Mantener rastreabilidad para futuro media manager avanzado.

### Riesgos operativos
- Deuda de almacenamiento por archivos no referenciados.
- Fallas de procesamiento asincrono sin DLQ/retry definidos.

### Estrategia de migracion
- Patrón strangler: facade + dual read/write controlado por flags.
- Migraciones de datos por lotes, idempotentes y auditables.

### Observabilidad y trazabilidad
- CorrelationId en upload/procesamiento.
- Metricas minimas: uploads, fallos, tiempo de procesamiento, cola pendiente.
- Auditoria por actor desde JWT (ADR-011).

## 3. Conflictos con ADRs aceptadas

- No se detecta conflicto directo con ADR-001..ADR-023.
- Nota de gobernanza:
  - Si `MediaManager` se implementa como microservicio nuevo, debe respetar ADR-003 (DB propia) y ADR-004/005 (eventos versionados).
  - Si temporalmente vive dentro de Core como modulo aislado, debe declararse como transicion hacia servicio independiente.
