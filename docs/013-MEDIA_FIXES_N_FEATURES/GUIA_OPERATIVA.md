# GUIA_OPERATIVA - Media (local + DigitalOcean Spaces)

## 1. Objetivo

Definir la operación mínima segura y portable del módulo de media para:
- almacenamiento local persistente por volumen Docker,
- almacenamiento remoto en DigitalOcean Spaces (S3 API),
- y transición controlada entre ambos.

## 2. Variables de configuración

### 2.1 Local (default recomendado en DEV)

```env
Media__Provider=local
Media__Local__RootPath=/var/lib/ioda/media
```

### 2.2 DigitalOcean Spaces

```env
Media__Provider=do_spaces
Media__DoSpaces__Endpoint=https://nyc3.digitaloceanspaces.com
Media__DoSpaces__Bucket=ioda-media-dev
Media__DoSpaces__Region=us-east-1
Media__DoSpaces__AccessKey=REEMPLAZAR
Media__DoSpaces__SecretKey=REEMPLAZAR
Media__DoSpaces__KeyPrefix=core
Media__DoSpaces__UsePathStyle=false
```

## 3. Ruta local canónica en contenedor

- Ruta interna de la app: `/var/lib/ioda/media`
- Debe montarse a un directorio persistente del host.

Ejemplo host:
- `/user/name/volumenes/ioda-media`

## 4. Ejemplo Docker Compose (volumen externo)

```yaml
services:
  ioda-core-api:
    environment:
      - Media__Provider=local
      - Media__Local__RootPath=/var/lib/ioda/media
    volumes:
      - /user/name/volumenes/ioda-media:/var/lib/ioda/media
```

## 5. Evidencia mínima operativa (DEV/QA)

1. `docker compose --profile services up -d --build ioda-core-api`
2. `docker compose ps` con `ioda-core-api` en estado `Up`.
3. Smoke check HTTP local:
   - `GET /swagger/index.html -> 200`
   - endpoints protegidos sin JWT -> `401`.

## 6. Seguridad de credenciales

- No subir `AccessKey`/`SecretKey` a repositorio.
- Inyectar por variables de entorno o gestor de secretos.
- No loggear valores de secretos ni endpoint firmado.
- Rotar claves por entorno (dev/qa/prod).

## 7. Backups y migración

### Local
- Respaldar directorio host montado (snapshot o rsync).
- Verificar restauración en entorno de prueba antes de producción.

### Local -> DigitalOcean Spaces
1. Congelar escritura o ejecutar ventana de baja concurrencia.
2. Copiar objetos con script de migración (key conservada).
3. Verificar conteo de objetos y checksum por muestra.
4. Cambiar `Media__Provider` a `do_spaces`.
5. Mantener rollback rápido a `local` hasta validación funcional.

## 8. Rollback operativo rápido

1. Restaurar `Media__Provider=local`.
2. Reiniciar `ioda-core-api`.
3. Confirmar lectura de media histórico desde volumen local.
