# Fase 3 – QA Gatekeeper Report

Fecha: 2026-03-02  
Dictamen: **Aprobado con condiciones**

## Evidencia automática ejecutada

1. Build técnico:
- Core API: OK (`0 errores`, `0 warnings`)
- Authorization API: OK (`0 errores`, `0 warnings`)
- Frontend: OK (`npm run build`)

2. Migraciones (DB real en Docker):
- `ioda_core`:
  - aplicada `20260220100000_AddContentBlocksTable`
  - aplicada `20260228051415_AddContentSiteUrlsAndPrimaryHierarchy`
  - tabla `content_blocks` existe
  - tabla `content_site_urls` existe
- `ioda_authorization`:
  - aplicada `20260202182600_InitialCreate`
  - tabla `permissions` existe

3. Arranque de servicios:
- `ioda-core-api` arriba con migraciones al arranque.
- `ioda-authorization-api` arriba con migraciones antes de seed.
- `ioda-core-api /health`: responde `200`.
- `ioda-authorization-api`: responde en Swagger y endpoints protegidos (401 esperado sin token).

## Hallazgos relevantes

1. Incidente resuelto:
- Error `42P01 relation "content_blocks" does not exist`.
- Causa raíz: migración `AddContentBlocksTable` no estaba siendo detectada por EF.
- Corrección aplicada y validada con evidencia en DB.

2. Observación no bloqueante:
- Health de Core reporta `Degraded` por check RabbitMQ (carga de tipo de cliente).
- Base de datos y API están operativas.
- No bloquea el flujo de contenidos de esta fase, pero debe revisarse en hardening de infraestructura.

## Cobertura funcional pendiente (manual E2E)

Para pasar de **Aprobado con condiciones** a **Aprobado**, ejecutar y evidenciar:

1. Happy path owner:
- Registrar/login.
- Crear proyecto, environment, sitio owner.
- Crear contenido con `title`, `slug`, `section` opcional y publicar.
- Validar resolución por URL owner.

2. Shared site:
- Compartir contenido a sitio adicional.
- Definir URL propia por sitio compartido.
- Validar publicación por sitio compartido.

3. Validaciones:
- Intentar URL duplicada por mismo sitio (debe rechazar).
- Intentar slug/path inválido (debe rechazar por backend).

4. Filtro por sección:
- Asignar contenido a sección hija.
- Filtrar por sección padre y confirmar inclusión.

5. No regresión:
- Versionado al editar.
- `created_at/updated_at/published_at`.
- `createdBy/updatedBy/publishedBy`.
- Ciclo de estado (`Draft`/`Published`/otros).

## Criterio de cierre de fase

Al completar los 5 bloques manuales anteriores sin defectos críticos:
- Dictamen final: **Aprobado**.
