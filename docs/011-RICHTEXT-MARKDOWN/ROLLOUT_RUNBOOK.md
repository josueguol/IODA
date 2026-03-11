# ROLLOUT_RUNBOOK - Etapa 3

Fecha: 2026-03-08  
Estado: `Listo para ejecucion controlada`

## 1. Objetivo

Habilitar `richtexteditor` de forma controlada en `DEV/QA` local como único editor enriquecido y garantizar rollback rápido si se detectan regresiones.

## 2. Pre-checks obligatorios

- [ ] Gate QA en estado `APROBADO` o `APROBADO CON OBSERVACIONES` aceptadas formalmente.
- [ ] Build backend/frontend en verde en entorno objetivo.
- [ ] Backups de BD de Core (`schemas`, `contents`, `versions`) disponibles y verificados.
- [ ] Monitoreo de errores habilitado para editor/render de contenido.

## 3. Arranque del entorno local (DEV/QA)

1. Levantar servicios backend en Docker:
   - `docker compose --profile services up -d --build`
2. Levantar frontend en local:
   - `cd frontend && npm run dev`
3. Verificar flag del editor en `frontend/.env`:
   - `VITE_ENABLE_RICHTEXT_EDITOR=true`
   - APIs apuntando a Docker local: `5001` a `5005`.

## 4. Estrategia por entorno (solo DEV/QA)

1. `DEV`
- Activar `richtexteditor` como único tipo permitido para texto enriquecido.
- Ejecutar smoke funcional mínimo.
- Configurar feature flag:
  - `VITE_ENABLE_RICHTEXT_EDITOR=true`

2. `QA`
- Repetir activación.
- Ejecutar checklist funcional completo (happy/error/no-regresión/permisos).
- Medir tasa de fallos de guardado/render.
- Configurar feature flag:
  - `VITE_ENABLE_RICHTEXT_EDITOR=true`

## 5. Migración legacy (ejecución operativa)

- Cancelada en esta fase por decisión operativa de desarrollo.
- Acción requerida: limpiar datos legacy y recrear contenidos de prueba con `richtexteditor`.

## 6. Validación post-cambio

- [ ] Crear schema nuevo con `richtexteditor`.
- [ ] Crear/editar/publicar contenido con:
  - H2-H6, quote, list, paragraph, code
  - table/media/embed/component module
  - multi-column 2 y 3
- [ ] Confirmar que ya no se usan campos `richtext` en schemas activos.
- [ ] Validar permisos de edición/publicación.

## 7. Criterios de rollback

Activar rollback si ocurre cualquiera:
- Error crítico de guardado o pérdida de contenido.
- Incremento sostenido de errores de render/editor.
- Regresión funcional bloqueante en publicación.

## 8. Pasos de rollback

1. Desactivar exposición del editor nuevo para altas nuevas.
   - `VITE_ENABLE_RICHTEXT_EDITOR=false` en entorno afectado.
2. Revertir frontend/backend del cambio si hay fallo crítico.
3. Notificar incident y abrir análisis RCA.

## 9. Evidencia requerida

- Log de ejecución por entorno.
- Resultado de smoke/QA post-deploy.
- Registro explícito de decisión: continuar, pausar o rollback.
