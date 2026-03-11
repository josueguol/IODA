# ESTADO

## Resumen ejecutivo

- Estado general: `En progreso`
- Fecha de corte: `2026-03-08`
- Avance estimado: `92%`

## Avances implementados

- Backend:
  - Se mantiene `richtexteditor` como tipo único de editor enriquecido.
  - Se agrega validador `BlockNoteMarkdownFieldValidator` (estructura JSON, limites payload, allowlist de embeds).
  - Se remueve compatibilidad legacy con `richtext`.
- Frontend:
  - Integrado editor BlockNote como componente `RichtextEditor`.
  - Botones de insercion para 2 columnas, 3 columnas, embed y modulo componente.
  - `richtext` removido del catalogo y del runtime de formularios dinámicos.
  - Tipo soportado en runtime para editor enriquecido: `richtexteditor`.
- Datos/Migraciones:
  - Checklist de migración legacy preparado (`MIGRACION_LEGACY_CHECKLIST.md`).
  - Migracion legacy cancelada por decision operativa (entorno de desarrollo reiniciable).

## Validaciones realizadas

- Arranque stack `DEV/QA` local con Docker: `OK` (`docker compose --profile services up -d --build`).
- Estado de servicios Docker: `OK` (5/5 `Up` en puertos host `5001-5005`).
- Logs runtime de servicios: `OK` (APIs escuchando y Core con respuestas `200`).
- Build backend: `OK`
- Build frontend: `OK`
- Pruebas funcionales: `Parcial (sin E2E manual cerrado)`
- Resultado QA preliminar: `APROBADO CON OBSERVACIONES` (ver `QA_GATE_REPORT.md`)

## Hallazgos y bloqueos

- Hallazgo: deuda de lint frontend global (11 errores, 6 warnings) fuera del componente nuevo.
- Bloqueo: pendiente ejecucion de pruebas funcionales end-to-end para cerrar gate QA.
- Nota operativa: despliegue a `PROD` explícitamente fuera de alcance en esta fase.
- Nota operativa: ambiente `DEV/QA` local con backend en Docker (`docker compose --profile services up -d --build`) y frontend local (`npm run dev`).

## Proximos pasos

1. Ejecutar pruebas E2E manuales de happy path/error path/permisos para `RichtextEditor`.
2. Limpiar schemas/contenidos de prueba que aún usen `richtext` y recrearlos con `richtexteditor`.
3. Cerrar/aceptar deuda de lint global fuera de alcance directo.
4. Ejecutar rollout en `DEV/QA` siguiendo `ROLLOUT_RUNBOOK.md` sobre stack local Docker + frontend local.

## Evidencia

- Commit(s):
- PR(s):
- Logs/capturas/rutas relevantes:
  - `docs/011-RICHTEXT-MARKDOWN/QA_GATE_REPORT.md`
  - `docs/011-RICHTEXT-MARKDOWN/ROLLOUT_RUNBOOK.md`
  - `docs/011-RICHTEXT-MARKDOWN/MIGRACION_LEGACY_CHECKLIST.md`
