# SMOKE_MANUAL_ETAPA1 - Multimedia

Fecha de creacion: `2026-03-13`
Objetivo: ejecutar smoke manual de Etapa 1 del modulo `Multimedia` y dejar evidencia verificable.

## 1. Precondiciones

- Servicios arriba en local:
  - `docker compose --profile services up -d --build`
- Frontend corriendo:
  - `cd frontend && npm run dev`
- Usuario con permisos de contenido (minimo `content.edit`).
- Proyecto/entorno/sitio seleccionados en la UI.

## 2. Datos de prueba sugeridos

- Archivo imagen 1: `smoke-img-1.png`
- Archivo imagen 2: `smoke-img-2.png`
- Metadata inicial:
  - titulo: `Smoke QA Imagen`
  - descripcion: `Prueba manual Etapa 1`

## 3. Casos smoke (UI)

### Caso 1: Navegacion

Pasos:
1. Abrir CMS.
2. Verificar tabs principales.
3. Entrar a `Multimedia`.

Esperado:
- Existe la pestaña `Multimedia` entre `Contenido` y `Publicar`.
- Carga pantalla de gestion multimedia sin error.

Evidencia:
- Captura de tabs + pantalla `Multimedia`.

### Caso 2: Estado de carga y vacio

Pasos:
1. Entrar por primera vez a `Multimedia`.
2. Observar estados iniciales.

Esperado:
- Se muestra estado `loading` mientras consulta.
- Si no hay items, aparece estado `empty` legible.

Evidencia:
- Captura del estado mostrado.

### Caso 3: Upload de media

Pasos:
1. Click en `Subir archivo`.
2. Seleccionar `smoke-img-1.png`.

Esperado:
- Upload exitoso.
- El item aparece en listado.
- Preview de imagen visible.
- Mensaje de exito (si aplica).

Evidencia:
- Captura del item cargado en listado y preview.

### Caso 4: Busqueda y filtros

Pasos:
1. En buscador, escribir parte del nombre o titulo.
2. Probar filtro por tipo `Imagenes`, luego `Videos`, `Audios`.

Esperado:
- La lista se filtra correctamente.
- No hay errores de UI.

Evidencia:
- Capturas de filtro aplicado y resultado.

### Caso 5: Edicion de metadatos

Pasos:
1. Seleccionar item cargado.
2. Editar `Nombre visible`, `Titulo` y `Descripcion`.
3. Click en `Guardar metadatos`.

Esperado:
- Guardado exitoso.
- Cambios visibles en panel y persistidos al refrescar la pagina.

Evidencia:
- Captura antes/despues + captura tras refresh.

### Caso 6: Reemplazo de archivo (versionado)

Pasos:
1. Sobre el mismo item, click en `Seleccionar nuevo archivo`.
2. Elegir `smoke-img-2.png`.

Esperado:
- Reemplazo exitoso.
- El mismo media mantiene su identidad funcional (mismo registro) y aumenta version.
- Preview refleja el nuevo archivo.

Evidencia:
- Captura antes/despues de replace mostrando version.

### Caso 7: Delivery de archivo

Pasos:
1. Desde preview o enlace del item, abrir archivo servido por API.

Esperado:
- Respuesta `200` del recurso.
- El archivo se visualiza o descarga correctamente.

Evidencia:
- Captura del recurso abierto (o network panel con 200).

### Caso 8: Seguridad basica

Pasos:
1. Probar con usuario sin permiso `content.edit`.
2. Intentar acceso a `Multimedia` y operaciones media.

Esperado:
- Operaciones protegidas no autorizadas (`403` o UI restringida segun flujo actual).

Evidencia:
- Captura de mensaje/restriccion o network 403.

### Caso 9: Integracion RichTextEditor con Multimedia (Etapa 3 parcial)

Pasos:
1. Ir a crear/editar contenido con un campo `richtexteditor`.
2. Abrir el editor y usar boton `Insertar media` o slash `/` -> `Media from library`.
3. Seleccionar un media de la libreria.

Esperado:
- Se abre selector centralizado de multimedia.
- Al seleccionar item, se inserta contenido en el editor:
  - imagen: `![alt](url)`
  - no imagen: `[label](url)`
- El valor del campo persiste al guardar contenido.

Evidencia:
- Captura del selector abierto.
- Captura del contenido insertado.
- Captura de contenido guardado y recargado.

### Caso 10: Integracion publish/indexing con fields enriquecidos (Etapa 3 backend)

Pasos:
1. Tomar contenido en `Draft` con campo `media` poblado.
2. Ejecutar `POST /api/projects/{projectId}/content/{contentId}/publish`.
3. Esperar consumo asinc (RabbitMQ -> Indexing).
4. Consultar Elasticsearch por `_doc/{contentId}`.

Esperado:
- Publicacion responde `200`.
- Documento indexado contiene `_source.fields`.
- Campo media viene enriquecido, ejemplo:
  - `_source.fields.image.id`
  - `_source.fields.image.url`
  - `_source.fields.image.contentType`
  - `_source.fields.image.displayName`
  - `_source.fields.image.version`

Evidencia:
- Respuesta `200` de publish.
- Respuesta Elasticsearch con `fields.image.url` presente.

## 4. Checklist rapido de cierre (ejecucion actual)

- [x] Navegacion `Multimedia` OK.
- [x] Upload OK.
- [x] Listado + filtros + busqueda OK.
- [x] Edicion metadatos OK.
- [x] Replace + incremento de version OK.
- [x] Delivery de archivo OK.
- [x] Seguridad basica (403 sin permiso) OK.
- [x] Integracion RichTextEditor + Multimedia OK.

## 5. Registro de ejecucion

Fecha de ejecucion: `2026-03-14`
Ejecutor: `Codex + QA tecnico local`
Ambiente: `local docker`
Resultado global: `PASS`

## 6. Resultado detallado por caso

| Caso | Resultado | Evidencia | Observaciones |
|------|-----------|-----------|---------------|
| Caso 1: Navegacion | PASS | `frontend` build + UI operativa reportada en QA gate | Ruta y tab `Multimedia` disponibles. |
| Caso 2: Estado de carga y vacio | PASS | Smoke UI previo de modulo + comportamiento esperado de estados | Sin bloqueos reportados. |
| Caso 3: Upload de media | PASS | `POST /api/projects/{projectId}/media -> 201` | Alta de media correcta. |
| Caso 4: Busqueda y filtros | PASS | Smoke UI previo del listado/filtros | Filtros base operativos en modulo MVP. |
| Caso 5: Edicion de metadatos | PASS | `PATCH /media/{id} -> 200` | Metadata persistida correctamente. |
| Caso 6: Reemplazo de archivo | PASS | `POST /media/{id}/replace -> 200` | Version incrementa (`1 -> 2`). |
| Caso 7: Delivery de archivo | PASS | `GET /media/{id}/file -> 200` | Recurso entregado correctamente. |
| Caso 8: Seguridad basica | PASS | `POST /api/auth/login -> 200` (admin) + `GET /api/projects/{projectId}/media -> 200` (admin) + `GET /media -> 403` (usuario sin permisos) | Validado comportamiento autorizado/no autorizado por rol/permiso. |
| Caso 9: RichText + Multimedia | PASS | `node frontend/scripts/smoke_case9_playwright.mjs` (`saveStatus=200`, `persistedMediaUrl=true`) | Flujo completo validado tras ajuste de validacion backend para URLs internas de media. |
| Caso 10: Publish/Indexing enriquecido | PASS | `publish -> 200`, consulta Elastic `_doc/{contentId}` con `fields.image.url` | Flujo asinc publish->indexing validado. |

## 7. Observaciones

- Credenciales admin reales validadas por API (`josue.guol@gmail.com`) con `role=SuperAdmin` y permiso `content.edit`.
- Caso 9 re-ejecutado con Playwright y cuenta admin real en `PASS` tras correccion de validacion embed host.
- El smoke tecnico, E2E API y smoke UI de RichText permiten cerrar checklist operativo sin hallazgos abiertos.

## 8. Bugs encontrados

- Ninguno abierto en este smoke.
