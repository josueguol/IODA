# GUIA_OPERATIVA - MediaManager

## 1. Variables de configuracion recomendadas

```env
Media__Provider=local
Media__Local__RootPath=/var/lib/ioda/media

# DigitalOcean Spaces (S3 compatible)
Media__DoSpaces__Endpoint=https://nyc3.digitaloceanspaces.com
Media__DoSpaces__Bucket=ioda-media-dev
Media__DoSpaces__Region=us-east-1
Media__DoSpaces__AccessKey=REEMPLAZAR
Media__DoSpaces__SecretKey=REEMPLAZAR
Media__DoSpaces__KeyPrefix=core
Media__DoSpaces__UsePathStyle=false
```

## 2. Ruta local canonica de contenedor

- Ruta interna: `/var/lib/ioda/media`

## 3. Ejemplo Docker Compose (volumen externo)

```yaml
services:
  ioda-core-api:
    environment:
      - Media__Provider=local
      - Media__Local__RootPath=/var/lib/ioda/media
    volumes:
      - /user/name/volumenes/ioda-media:/var/lib/ioda/media
```

## 4. Recomendaciones operativas

- No usar rutas efimeras dentro del contenedor.
- Respaldar periodicamente el volumen host.
- No guardar credenciales en repo; usar secretos por entorno.
- Mantener rollback rapido `do_spaces -> local` por configuracion.

## 5. Migracion local -> DigitalOcean Spaces

1. Congelar uploads o ventana controlada.
2. Copiar objetos y validar conteos/checksums por muestra.
3. Cambiar `Media__Provider=do_spaces`.
4. Ejecutar smoke upload/read y monitorear errores.

Script recomendado:
- `bash docs/014-MEDIAMANAGER/scripts/migrate_local_to_dospaces.sh` (`DRY_RUN=true|false`)

## 6. Lifecycle cleanup (retention de huérfanos)

Endpoint:
- `POST /api/projects/{projectId}/media/cleanup-orphans`

Body:
```json
{
  "dryRun": true,
  "maxDeletes": 1000
}
```

Notas:
- `dryRun=true` solo detecta y reporta huérfanos.
- `dryRun=false` elimina hasta `maxDeletes` claves huérfanas.
- Ejecutar primero en dry-run y revisar `sampleOrphanKeys` antes de aplicar.

## 7. Observabilidad operativa

Health endpoints:
- `GET /health`
- `GET /health/live`
- `GET /health/ready`

Métricas de lifecycle (meter):
- `media_lifecycle_cleanup_runs_total`
- `media_lifecycle_orphans_found_total`
- `media_lifecycle_deleted_total`

## 8. Carga y resiliencia (smoke)

Script:
- `bash docs/014-MEDIAMANAGER/scripts/load_resilience_smoke.sh`

Parámetros útiles:
- `REQUESTS` (default `80`)
- `CONCURRENCY` (default `8`)
- `TIMEOUT_SECONDS` (default `20`)
