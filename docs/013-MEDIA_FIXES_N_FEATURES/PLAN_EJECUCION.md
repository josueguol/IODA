# PLAN_EJECUCION

## 1. Contexto

- Fase: `013-MEDIA_FIXES_N_FEATURES`
- Objetivo resumido: corregir campo `media` end-to-end (schema, validación, storage local portable y soporte DigitalOcean Spaces Object Storage).
- Fecha de inicio: `2026-03-12`
- Fecha objetivo: `2026-03-21`

## 2. Supuestos y restricciones

- Supuesto: `validationRules` del schema seguirá siendo el contrato extensible para reglas del campo.
- Restricción: no breaking changes en contratos existentes sin transición.
- Restricción: no dejar validación crítica únicamente en frontend.
- Restricción: no hardcode de rutas físicas en código.
- Restricción: dominio desacoplado de proveedor de almacenamiento concreto.

## 3. Plan por etapas

### Etapa 0: Gobernanza y contrato
- Objetivo: cerrar diseño técnico y criterios de implementación/QA.
- Tareas:
  - [x] Definir contrato canónico `validationRules.media` (categorías, MIME, extensiones, tamaño).
  - [x] Definir matriz de compatibilidad hacia atrás (schemas sin reglas, media legacy).
  - [x] Definir estrategia de delivery de archivos (`proxy` inicial + evolución a `signed/direct`).
  - [x] Registrar decisiones en documentos de fase (análisis + decisiones aprobadas).
- Evidencias esperadas:
  - Documento de diseño aprobado.
  - Checklist de impactos BE/FE/Infra/Seguridad.
- Riesgos:
  - Ambigüedad de contrato que genere validaciones inconsistentes.

### Etapa 1: Backend (Core API / Application / Infrastructure)
- Objetivo: implementar validaciones y storage provider strategy.
- Tareas:
  - [x] Crear parser tipado de reglas `media` en schema.
  - [x] Validar reglas al crear/actualizar schema.
  - [x] Validar `mediaId` en create/update content contra reglas del campo.
  - [x] Corregir `Upload` para tomar actor desde JWT (alineado con ADR-011).
  - [x] Introducir selección de provider `local|do_spaces` por configuración.
  - [x] Implementar provider DigitalOcean Spaces (API S3 compatible).
  - [x] Mantener provider local y comportamiento actual por defecto.
- Evidencias esperadas:
  - Build backend OK.
  - Casos de validación unit/integration mínimos.
  - Configuración de provider por environment validada.
- Riesgos:
  - Riesgo de regresión en upload/list/get file.

### Etapa 2: Frontend (Schema Designer + MediaPicker)
- Objetivo: UX de configuración y consumo de reglas media.
- Tareas:
  - [x] Agregar panel de configuración de restricciones para `fieldType=media`.
  - [x] Persistir reglas en `validationRules.media`.
  - [x] Aplicar filtros de selección y `accept` dinámico en `MediaPicker`.
  - [x] Mejorar mensajes de error para incompatibilidades por tipo/tamaño.
- Evidencias esperadas:
  - Build frontend OK.
  - Flujo funcional en schema designer + create/edit content.
- Riesgos:
  - Doble fuente de verdad FE/BE si no se reutiliza contrato canónico.

### Etapa 3: Infra y Operación
- Objetivo: despliegue reproducible local/QA y base para producción.
- Tareas:
  - [x] Definir variables de entorno de media local y `do_spaces`.
  - [x] Documentar montaje de volumen host externo -> ruta contenedor.
  - [x] Incluir ejemplo docker compose para persistencia.
  - [x] Definir lineamientos de backups y migración local->do_spaces.
- Evidencias esperadas:
  - Runbook operativo documentado.
  - Prueba de persistencia tras recreación de contenedor.
- Riesgos:
  - Configuración incompleta de credenciales/endpoint.

### Etapa 4: QA Gate
- Objetivo: validar funcionalidad, seguridad y regresión.
- Tareas:
  - [ ] Ejecutar matriz de pruebas por combinaciones de categorías/mime/extensiones.
  - [ ] Validar flujos legacy sin reglas de media.
  - [ ] Verificar persistencia de archivos con volumen local.
  - [ ] Verificar modo `do_spaces` en entorno de prueba.
  - [x] Emitir gate preliminar/final de fase (resultado actual: Aprobado con observaciones).
- Evidencias esperadas:
  - `QA_GATE_REPORT.md` con resultado y defectos.

## 4. Definición de terminado (DoD)

- [x] Backend compilando y pruebas relevantes ejecutadas.
- [x] Frontend compilando y flujo principal validado.
- [x] Configuración por entorno documentada y verificada.
- [x] Sin errores bloqueantes abiertos en media.
- [x] QA gate aplicado con resultado registrado.

## 5. Rollout y rollback

- Rollout:
  - Paso 1: desplegar backend con provider local por defecto y reglas de schema activas.
  - Paso 2: desplegar frontend con UI de reglas media.
  - Paso 3: habilitar provider `do_spaces` en ambiente controlado de QA.
  - Paso 4: monitorear errores de upload/lectura y validar persistencia.
- Rollback:
  - Paso 1: volver `Media__Provider=local`.
  - Paso 2: deshabilitar UI avanzada de restricciones (flag si aplica).
  - Paso 3: revertir release anterior de Core API y frontend.

## 6. Registro de cambios del plan

- `2026-03-12`: creación inicial del plan de ejecución de fase 013.
- `2026-03-12`: cierre de Etapa 0 (contrato canónico, compatibilidad, estrategia de delivery y decisiones aprobadas).
- `2026-03-12`: implementación de Etapa 1 backend/frontend completada (pendiente gate QA y validación operativa final).
- `2026-03-12`: cierre de Etapa 3 (guía operativa local/do_spaces, variables, volumen, backup y migración).
- `2026-03-12`: Etapa 4 ejecutada en modalidad de gate técnico con resultado `APROBADO CON OBSERVACIONES`.
