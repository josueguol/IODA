# CHECKLIST_QA_MANUAL_DEV_QA

Fecha base: `2026-03-08`  
Entorno objetivo: `DEV/QA local`

## 1. Preparación

1. Levantar backend:
   - `docker compose --profile services up -d --build`
2. Levantar frontend:
   - `cd frontend && npm run dev`
3. Verificar `frontend/.env`:
   - `VITE_ENABLE_RICHTEXT_EDITOR=true`
   - APIs en `http://localhost:5001` a `http://localhost:5005`

## 2. Happy path (obligatorio)

1. Crear schema nuevo con campo `richtexteditor`.
2. Crear contenido con `RichtextEditor` y validar:
   - `h2`, `h4`, `h6`
   - `quote`
   - `bullet list` y `numbered list`
   - `paragraph` multilinea
   - `code block`
   - `table`
   - `media` (imagen/video)
   - `embed`
   - `component module`
3. Insertar `2 columnas` y `3 columnas` y escribir contenido en cada columna.
4. Guardar, publicar, reabrir y confirmar persistencia sin pérdida de formato.

## 3. Error path (obligatorio)

1. Intentar embed con provider no permitido y validar rechazo controlado.
2. Forzar payload inválido/excedido (si hay herramienta de edición raw o API) y validar rechazo backend.
3. Confirmar que ante error no se pierde el contenido ya escrito en el editor.

## 4. No regresión (obligatorio)

1. Confirmar que no existen schemas activos con tipo `richtext`.
2. Validar permisos:
   - Usuario con permiso de edición: puede guardar.
   - Usuario sin permiso de publicación: no puede publicar.

## 5. Evidencia a adjuntar en gate

1. Capturas UI por caso (happy/error/no regresión).
2. Registro de resultado por caso: `PASS/FAIL`.
3. Lista de defectos con severidad y pasos de reproducción.
4. Actualización de `QA_GATE_REPORT.md` con resultado final del gate.
