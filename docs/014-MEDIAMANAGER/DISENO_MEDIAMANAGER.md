# DISENO_MEDIAMANAGER

## 1. Diseno funcional y tecnico propuesto

### 1.1 Navegacion
- Menu principal objetivo:
  - Dashboard
  - Contenido
  - Multimedia
  - Publicar
- `Multimedia` abre el modulo Media Manager.

### 1.2 UX/UI del modulo Multimedia

Vistas principales:
- Listado:
  - Grid/list toggle.
  - Busqueda por titulo, nombre archivo, mime.
  - Filtros: tipo (imagen/video/audio), estado de procesamiento, fecha, tamano.
- Detalle/preview:
  - Imagen: preview directo.
  - Video: poster + metadata.
  - Audio: reproductor basico + portada.
- Upload:
  - Drag and drop + selector.
  - Validacion temprana de restricciones.
  - Estado por archivo (`pending/uploading/processing/ready/failed`).
- Edicion metadata:
  - titulo, descripcion, tags opcionales, alt text (imagenes), portada (audio), segundo poster (video).
- Reemplazo:
  - `Reemplazar archivo` mantiene el registro del medio (id estable) y crea nueva version.

Estados UX obligatorios:
- `loading`, `error`, `empty`, `success`.

Accesibilidad:
- Navegacion por teclado.
- Etiquetas ARIA en botones de accion.
- Mensajes de error descriptivos.

### 1.3 Modelo de medio (canonico)

Entidad `MediaAsset` (alto nivel):
- `id`, `publicId`, `projectId`
- `mediaType`: `image|video|audio|other`
- `title`, `description`
- `originalFileName`, `extension`, `mimeType`, `sizeBytes`
- `width`, `height` (nullable)
- `durationMs` (nullable)
- `storageProvider`: `local|do_spaces`
- `storageKeyOriginal`
- `processingStatus`: `pending|processing|ready|failed`
- `processingError` (nullable)
- `createdAt`, `updatedAt`, `createdBy`, `updatedBy`
- `currentVersion`

Entidad `MediaVariant`:
- `id`, `mediaAssetId`, `name` (`thumbnail|small|medium|large|poster|waveform`)
- `mimeType`, `sizeBytes`, `width`, `height`, `durationMs`
- `storageKey`, `createdAt`

### 1.4 Flujo de carga
1. Cliente solicita upload init (opcional para futuro signed upload).
2. Servicio valida extension/mime/tamano/categoria.
3. Guarda original en provider configurado.
4. Persiste metadata base.
5. Marca estado `ready` (Fase 1) o `processing` (Fase 2 con derivados).

### 1.5 Flujo de procesamiento
- Fase 1: sin pipeline pesado.
- Fase 2:
  - Emite evento `MediaAssetCreatedV1`.
  - Worker procesa derivados.
  - Emite `MediaAssetProcessedV1` o `MediaAssetProcessingFailedV1`.

### 1.6 Imagen, video y audio

Imagen:
- Derivados recomendados: original + thumbnail + small + medium + large.

Video:
- Metadata tecnica (duracion, resolucion, codec si aplica).
- Generacion de poster desde segundo configurable.

Audio:
- Metadata tecnica (duracion, bitrate si aplica).
- Portada manual seleccionable desde imagen existente (MVP).

### 1.7 Estrategia de almacenamiento
- Provider por config:
  - `local`
  - `do_spaces` (DigitalOcean Spaces, S3 API compatible)
- Dominio desacoplado via `IMediaStorage`.
- URLs:
  - Fase 1 proxy (`GET /media/{id}/file`).
  - Fase 3 opcional signed/direct delivery.

## 2. Diseno del nuevo microservicio `MediaManager`

### 2.1 Bounded context
- Contexto: gestion de activos multimedia del CMS.
- Limite: ingest, metadata, variantes, versionado y acceso.

### 2.2 Responsabilidades
- Gestion de lifecycle de medios.
- Validacion de archivo/metadata.
- Orquestacion de procesamiento asincrono.
- Exposicion de API para consulta y operaciones.

### 2.3 Agregados/entidades
- Aggregate root: `MediaAsset`.
- Entidades hijas: `MediaVariant`, `MediaVersion`.
- Value objects: `MediaFormat`, `MediaDimensions`, `MediaDuration`.

### 2.4 Contratos API sugeridos (v1)
- `POST /api/projects/{projectId}/media` (upload/create)
- `GET /api/projects/{projectId}/media` (list, filtros, paginacion)
- `GET /api/projects/{projectId}/media/{mediaId}` (detalle)
- `PATCH /api/projects/{projectId}/media/{mediaId}` (metadata)
- `POST /api/projects/{projectId}/media/{mediaId}/replace` (nueva version)
- `GET /api/projects/{projectId}/media/{mediaId}/file` (proxy)
- `GET /api/projects/{projectId}/media/{mediaId}/variants/{variant}`

### 2.5 Persistencia
- DB propia del servicio si se despliega como microservicio real.
- Tabla de assets + variantes + versiones + jobs.

### 2.6 Procesamiento asincrono
- Cola/event bus:
  - `MediaAssetCreatedV1`
  - `MediaAssetProcessingRequestedV1`
  - `MediaAssetProcessedV1`
  - `MediaAssetProcessingFailedV1`
- Workers idempotentes y con retry.

### 2.7 Observabilidad
- Logs estructurados por mediaId/projectId.
- Metricas: upload rate, fail rate, processing latency, queue depth.
- Trazas distribuidas end-to-end.

### 2.8 Seguridad y permisos
- JWT + policies por accion:
  - `media.read`, `media.upload`, `media.update`, `media.delete`, `media.process`.
- Actor siempre desde JWT (ADR-011).

## 3. Configuracion por ambiente (resumen)

- `Media__Provider=local|do_spaces`
- Local:
  - `Media__Local__RootPath=/var/lib/ioda/media`
- DO Spaces:
  - `Media__DoSpaces__Endpoint=https://<region>.digitaloceanspaces.com`
  - `Media__DoSpaces__Bucket=<bucket>`
  - `Media__DoSpaces__Region=us-east-1`
  - `Media__DoSpaces__AccessKey=...`
  - `Media__DoSpaces__SecretKey=...`
  - `Media__DoSpaces__KeyPrefix=...`
