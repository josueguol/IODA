# DECISIONES_APROBADAS - Etapa 0

Fecha: `2026-03-12`  
Responsable: `Gobernanza Técnica / Orquestador`

## D-013-001: Proveedor S3 explícito

- Decisión: el storage remoto S3 de esta fase se implementa específicamente para **DigitalOcean Spaces Object Storage** (compatibilidad S3 API).
- Consecuencia: no se deja ambigüedad de proveedor en implementación/configuración.
- Nota: el diseño mantiene extensibilidad a otros S3-compatible en fases posteriores.

## D-013-002: Contrato canónico para reglas del campo media

- Decisión: usar `validationRules.media` como contrato único para restricciones del campo `media`.

Contrato canónico:

```json
{
  "media": {
    "allowedCategories": ["image", "video", "audio"],
    "allowedMimeTypes": ["image/jpeg", "image/png", "video/mp4"],
    "allowedExtensions": ["jpg", "jpeg", "png", "mp4"],
    "maxSizeBytes": 52428800
  }
}
```

Reglas:
- `allowedCategories` obligatorio (1..3).
- `allowedMimeTypes` opcional (si no viene, se infiere por categoría).
- `allowedExtensions` opcional (si no viene, se infiere por categoría).
- `maxSizeBytes` opcional, sin superar máximo global del sistema.

## D-013-003: Compatibilidad hacia atrás

- Decisión: schema media legacy con `validationRules` vacío/null se considera válido y sin restricciones por campo.
- Consecuencia: no se rompen schemas existentes.

## D-013-004: Enfoque de validación

- Decisión: las reglas del campo `media` deben validarse en backend en dos puntos:
  - al guardar schema (consistencia del contrato),
  - al guardar contenido (media referenciado cumple reglas del campo).
- Consecuencia: el frontend no queda como única barrera.

## D-013-005: Estrategia de delivery de archivos

- Decisión: `proxy` como modo inicial de entrega, manteniendo `GET /api/projects/{projectId}/media/{mediaId}/file`.
- Evolución: `signed/direct` se contempla como mejora posterior.

## D-013-006: Storage provider strategy

- Decisión: seleccionar provider por configuración:
  - `Media__Provider=local`
  - `Media__Provider=do_spaces`
- Consecuencia: desacople de infraestructura en runtime sin afectar dominio.

## D-013-007: Ruta local canónica en contenedor

- Decisión: ruta interna recomendada para media local: `/var/lib/ioda/media`.
- Consecuencia: despliegue portable y persistente mediante volumen montado.

## D-013-008: Alineación con ADR-011

- Decisión: upload de media debe tomar actor desde JWT; no depender de `createdBy` enviado por body/form.
- Consecuencia: aumenta integridad de auditoría y consistencia con decisiones arquitectónicas aceptadas.

## D-013-009: Semántica funcional del campo media (uno a uno)

- Decisión: el campo `media` representa una relación **1:1** con un archivo.
- Regla:
  - `media` no soporta galería, colección ni selección múltiple.
  - al seleccionar/subir un nuevo archivo para el campo, el valor anterior se reemplaza.
  - el archivo anterior puede seguir existiendo en la librería de media para uso futuro, pero ya no queda asociado al campo.
- Consecuencia:
  - backend rechaza payloads múltiples para `fieldType=media`.
  - frontend mantiene selección única por campo.
  - cualquier caso de múltiples archivos queda fuera de alcance hasta un futuro tipo dedicado (`mediaCollection` u otro).

## Matriz de compatibilidad (resumen)

| Escenario | Resultado esperado |
|---|---|
| Schema `media` legacy sin reglas | Válido, comportamiento actual |
| Schema `media` con reglas nuevas | Válido, backend aplica restricciones |
| Provider `local` | Flujo actual mantenido |
| Provider `do_spaces` | Habilitado por configuración, sin romper local |
