# TAREAS_POR_AGENTE - 013 Media Fixes N Features

Estado: `Listo para ejecución`

## 1) Gobernanza / Arquitectura (`ai/agents/orchestrator.agent.md`)

## Objetivo

Garantizar diseño consistente con DDD + Clean Architecture y reglas del CMS para el campo `media`.

## Backlog obligatorio

1. Contrato y decisiones
- [x] Definir estructura canónica de `validationRules.media`.
- [x] Definir estrategia de compatibilidad hacia atrás y transición.
- [x] Confirmar política de URL de media (`proxy` inicial, evolución `signed/direct`).
- [x] Registrar decisiones en documentos de fase (análisis + decisiones aprobadas).

2. Alineación con arquitectura
- [x] Validar separación dominio/infra para providers de storage.
- [x] Validar que no haya lógica de negocio en controllers.
- [x] Validar cumplimiento ADR-011 (actor desde JWT en upload).

3. Evidencia mínima
- [x] Documento de diseño aprobado.
- [x] Riesgos/tradeoffs documentados.
- [x] Plan de rollout/rollback validado.

## 2) Fullstack (`ai/agents/fullstack.agent.md`)

## Objetivo

Implementar corrección integral BE + FE + configuración operativa de media.

## Backend
- [x] Implementar modelo tipado para `validationRules.media` en Application.
- [x] Validar reglas al crear/editar schema (`fieldType=media`).
- [x] Implementar validación de `mediaId` en create/update content (proyecto, tipo, extensión, tamaño).
- [x] Corregir `MediaController.Upload` para obtener actor desde JWT (remover dependencia de `createdBy` en body).
- [x] Implementar `IMediaStorage` con provider selector por configuración (`local|do_spaces`).
- [x] Agregar `DoSpacesMediaStorage` (DigitalOcean Spaces - S3 API).
- [x] Mantener `LocalMediaStorage` como default sin ruptura.

## Frontend
- [x] Agregar editor de propiedades `media` en Schema Designer.
- [x] Persistir reglas en `validationRules.media`.
- [x] Aplicar `accept` dinámico y filtros por reglas en `MediaPicker`.
- [x] Mejorar UX de errores de tipo/tamaño no permitido.

## Infra/Config
- [x] Definir variables de entorno para local y `do_spaces`.
- [x] Añadir ejemplo compose con bind mount persistente.
- [ ] Documentar lineamientos de seguridad de credenciales.

## Evidencia mínima
- [x] Build backend OK.
- [x] Build frontend OK.
- [ ] Pruebas manuales happy/error path de media.
- [ ] Evidencia de persistencia tras recreación de contenedor.

## 3) QA Gatekeeper (`ai/agents/qa-tester.agent.md`)

## Objetivo

Validar que la solución no rompe flujos existentes y cumple restricciones de media.

## Cobertura obligatoria

1. Reglas de schema
- [ ] Guardar schema media con categorías individuales y combinadas.
- [ ] Guardar schema con MIME/extensiones permitidas personalizadas.
- [ ] Validar rechazo de configuración inválida.

2. Flujos de contenido
- [ ] Upload + selección exitosa de archivo permitido.
- [ ] Rechazo de archivo no permitido por reglas del campo.
- [ ] Rechazo de `mediaId` de otro proyecto.

3. Operación/infra
- [ ] Persistencia local con volumen tras `docker compose down/up`.
- [ ] Lectura de archivos existentes después de recrear contenedor.
- [ ] Validación básica con provider `do_spaces` en ambiente de prueba.

4. Seguridad/regresión
- [x] Validar actor desde JWT en upload.
- [x] Validar no regresión de endpoints media actuales (smoke técnico).
- [ ] Validar que schemas legacy sin reglas siguen operativos.

## Decisión
- [x] Emitir `QA_GATE_REPORT.md` con resultado final:
  - `APROBADO | APROBADO CON OBSERVACIONES | RECHAZADO`
