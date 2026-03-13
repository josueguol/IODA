# ANALISIS_REQUERIMIENTO - 013 Media Fixes N Features

## 1) Contexto

El campo `media` actualmente existe como tipo de schema, pero su validación funcional está incompleta.

Situación observada:
- Hay validación global de upload (extensiones/tamaño) pero no validación por reglas del campo en create/update content.
- `MediaPicker` usa `accept` fijo y no toma restricciones de schema.
- El almacenamiento local depende de configuración pero requiere estandarización operativa para contenedores.
- El provider S3 objetivo del proyecto debe ser explícitamente **DigitalOcean Spaces Object Storage** (compatibilidad S3 API).

## 2) Problemas a resolver

1. Falta de contrato canónico para restricciones del campo `media`.
2. Falta de enforcement backend de reglas por campo (solo existe validación global de upload).
3. Configuración de almacenamiento local no suficientemente portable por ambiente.
4. Necesidad de soportar provider remoto (DigitalOcean Spaces) sin romper modo local.
5. Alineación de seguridad con ADR-011 en upload media (actor desde JWT).
6. Debe explicitarse semántica funcional `media = un solo archivo` (relación 1:1).

## 3) Impacto por capa

- Dominio/Application:
  - Introducir modelo tipado para `validationRules.media`.
  - Agregar validación semántica de referencia `mediaId` en create/update content.
- API:
  - Mantener contratos existentes.
  - Permitir evolución aditiva de metadatos/URL de media.
- Campo media:
  - Rechazar payloads múltiples (array/lista) en create/update content.
  - Mantener reemplazo de valor al cambiar selección.
- Frontend:
  - UI de configuración para reglas de media.
  - `MediaPicker` dinámico según reglas del campo.
- Infra:
  - Provider strategy configurable (`local`, `do_spaces`).
  - Ruta local persistente y portable en Docker.

## 4) Compatibilidad

- Backward compatible:
  - Schemas legacy con `fieldType=media` y `validationRules=null` siguen funcionando.
  - Endpoint `GET /media/{id}/file` se mantiene como modo de entrega base.
- No se introduce ruptura de payload en esta etapa de gobernanza.

## 5) Riesgos relevantes

- Divergencia FE/BE en reglas de aceptación de archivos.
- Errores operativos por volumen no persistente en contenedores.
- Exposición de credenciales si no se usan secretos de entorno.

## 6) Criterios de cierre de Etapa 0

- Contrato canónico de `validationRules.media` definido.
- Estrategia de compatibilidad y transición definida.
- Política de delivery de media definida (`proxy` inicial).
- Decisiones registradas y trazables en documentación de fase.
