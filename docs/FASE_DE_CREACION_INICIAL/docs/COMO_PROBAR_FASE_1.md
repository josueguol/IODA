# Cómo probar lo ya creado (Fase 1 – CMS Core)

Guía para ejecutar y probar el **CMS Core API** (proyectos, schemas, contenido) con lo implementado hasta ahora.

> **Nota (Fase de mejoras):** La Core API puede estar protegida con **JWT**. En ese caso necesitas un token de la **Identity API** para llamar a proyectos, schemas y contenido. Guía detallada de las mejoras (Environment por API, paginación, health RabbitMQ, JWT): **`docs/COMO_PROBAR_MEJORAS_CORE_API.md`**.

---

## 1. Prerrequisitos

- **.NET 9 SDK**
- **PostgreSQL** (puerto 5432) – local o Docker
- **Identity API** (opcional pero recomendado si Core tiene JWT activado) – para obtener un token y probar los endpoints.
- **RabbitMQ** (puertos 5672, 15672) – **opcional en desarrollo**.  
  Por defecto, en entorno `Development` la API arranca **sin** RabbitMQ (`RabbitMQ:Enabled = false` en `appsettings.Development.json`). Puedes probar todo el CRUD (proyectos, schemas, contenido); los eventos de integración no se publican.  
  Si quieres usar RabbitMQ, pon `RabbitMQ:Enabled: true` y las credenciales correctas (Host, Username, Password). Si tu RabbitMQ está en otro proyecto, usa el mismo Host, puerto y usuario/contraseña que ese proyecto.

---

## 2. Base de datos

### 2.0 Si PostgreSQL está en otro proyecto (otro Docker/Compose)

Cuando Postgres corre en **otro proyecto** (otro terminal o otro `docker-compose`), suele pasar:

1. **Puerto distinto**: Si 5432 ya lo usa otro contenedor, tu Postgres puede estar en **5433** (o otro). Comprueba con `docker ps` o la configuración del otro proyecto.
2. **La base `ioda_core` no existe**: En ese servidor hay que crear la base. Conéctate con el cliente que uses (psql, DBeaver, etc.) al **mismo host y puerto** que usa el otro proyecto y ejecuta: `CREATE DATABASE ioda_core;`
3. **Usuario y contraseña**: Usa el mismo usuario y contraseña que el otro proyecto (no siempre son `postgres`/`postgres`).

**Connection string** desde tu máquina (si Postgres está en localhost pero en otro puerto):

```bash
# Ejemplo si Postgres del otro proyecto está en el puerto 5433
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=ioda_core;Username=postgres;Password=TU_PASSWORD;Include Error Detail=true"
```

Ajusta también **`appsettings.json`** (o `appsettings.Development.json`) con el mismo `Host`, `Port`, `Username` y `Password` para que la API se conecte al mismo servidor.

**Comprobar conexión** antes de migrar:

```bash
# Con psql (ajusta -p si usas otro puerto)
psql -h localhost -p 5432 -U postgres -d ioda_core -c "SELECT 1;"
```

Si ese comando falla, las migraciones también fallarán hasta que la conexión sea correcta.

### 2.1 Crear la base de datos

Si usas PostgreSQL local (o el del otro proyecto, mismo servidor):

```bash
# Con psql o pgAdmin: crear base de datos (ajusta -p si usas otro puerto)
createdb -U postgres -p 5432 ioda_core
# o
psql -h localhost -p 5432 -U postgres -c "CREATE DATABASE ioda_core;"
```

Si usas Docker (por ejemplo con `local-dev-network`):

```bash
docker run -d --name ioda-postgres \
  --network local-dev-network \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=ioda_core \
  -p 5432:5432 \
  postgres:16-alpine
```

### 2.2 Aplicar migraciones

Desde la raíz del repo:

```bash
cd /Users/josuegolivares/desarrollo/csharp/ioda

# Variable para design-time (EF Core usa CoreDbContextFactory)
export ConnectionStrings__DefaultConnection="Host=localhost;Database=ioda_core;Username=postgres;Password=postgres;Include Error Detail=true"

dotnet ef database update --project src/Services/Core/IODA.Core.Infrastructure/IODA.Core.Infrastructure.csproj --startup-project src/Services/Core/IODA.Core.API/IODA.Core.API.csproj
```

**Nota:** Si aparece *"An error occurred using the connection..."* pero al final sale **"Done."**, la migración se aplicó correctamente (EF Core a veces muestra ese mensaje al obtener el bloque y luego continúa). Si el comando termina con error y **no** dice "Done.", revisa la sección 2.0 (PostgreSQL en otro proyecto).

Si no tienes `dotnet-ef` instalado:

```bash
dotnet tool install --global dotnet-ef
```

---

## 3. Configuración de la API

Revisa **`src/Services/Core/IODA.Core.API/appsettings.json`** (o `appsettings.Development.json`):

- **ConnectionStrings:DefaultConnection** – debe apuntar a tu PostgreSQL (`ioda_core`).
- **RabbitMQ** – Host, VirtualHost, Username, Password (por defecto localhost, `/`, guest, guest).

Para desarrollo local suele bastar con los valores por defecto si PostgreSQL y RabbitMQ están en `localhost`.

---

## 4. Ejecutar la API

### Opción A: Desde la solución (recomendado para desarrollo)

```bash
cd /Users/josuegolivares/desarrollo/csharp/ioda

dotnet run --project src/Services/Core/IODA.Core.API/IODA.Core.API.csproj
```

La API queda en **http://localhost:5000** (o el puerto que indique la consola; revisa `Properties/launchSettings.json`).

### Opción B: Con Docker

```bash
cd /Users/josuegolivares/desarrollo/csharp/ioda

docker compose --profile services up -d ioda-core-api
```

El servicio está en el puerto **5001** (mapeado al 8080 del contenedor). Ajusta en `docker-compose.yml` las variables de entorno para que apunten a tu Postgres y RabbitMQ.

---

## 5. Probar los endpoints

### 5.1 Si la Core API exige JWT

Si en **appsettings.json** de Core API tienes configurado **Jwt:SecretKey** (y Issuer/Audience), **todos** los endpoints de proyectos, schemas y contenido requieren `Authorization: Bearer <token>`. Obtén el token desde la **Identity API** (login) y en Swagger haz clic en **Authorize** e introduce el token. Ver **`docs/COMO_PROBAR_MEJORAS_CORE_API.md`** para detalles.

### 5.2 Swagger (recomendado)

1. Abre en el navegador: **http://localhost:5000/swagger** (o el puerto que uses; a menudo 5269 en launchSettings).
2. Si Core usa JWT: **Authorize** → pega el accessToken de Identity.
3. Prueba en este orden:
   - **Projects** → crear proyecto y obtener por ID. El listado de proyectos es **paginado** (`page`, `pageSize`); la respuesta tiene `items`, `totalCount`, etc.
   - **Environments** → **POST** `api/projects/{projectId}/environments` para crear un entorno (ver apartado 6); **GET** para listar u obtener por id.
   - **Schemas** → crear schema en ese proyecto y listar/obtener por ID.
   - **Content** → crear contenido (necesitas un **Environment**; ver apartado 6).

### 5.3 Archivo .http (VS Code / Rider)

En el proyecto está **`src/Services/Core/IODA.Core.API/IODA.Core.API.http`**. Ahí hay ejemplos de:

- Crear proyecto.
- Obtener proyecto por ID.
- Crear schema (con campos).
- Listar schemas.
- Crear contenido (tras tener `projectId`, `environmentId`, `schemaId`).
- Listar contenido, publicar, etc.

Añade el header `Authorization: Bearer {{token}}` si Core API tiene JWT activado. Ajusta la variable `baseUrl` al puerto donde corra la API (por ejemplo `http://localhost:5269`).

### 5.4 Con curl (resumen)

Sustituye `BASE=http://localhost:5269` (o el puerto que uses). Si Core API exige JWT, obtén un token de Identity y usa `-H "Authorization: Bearer $TOKEN"` en todas las peticiones.

```bash
BASE=http://localhost:5269
# Si Core usa JWT (obtén el token desde Identity API /api/auth/login):
# TOKEN="eyJhbGciOiJIUzI1NiIs..."
# AUTH="-H \"Authorization: Bearer $TOKEN\""

# 1. Crear proyecto
curl -s -X POST "$BASE/api/projects" \
  -H "Content-Type: application/json" \
  $AUTH \
  -d '{"name":"Mi Blog","description":"Proyecto de prueba","createdBy":"11111111-1111-1111-1111-111111111111"}' 
# Respuesta: GUID del proyecto (ej. "a1b2c3d4-...")

# 2. Obtener proyecto (usa el ID del paso 1)
curl -s "$BASE/api/projects/{projectId}" $AUTH

# 3. Listar proyectos (paginado; respuesta tiene "items", "totalCount", "page", "pageSize")
curl -s "$BASE/api/projects?page=1&pageSize=20" $AUTH

# 4. Crear schema en el proyecto
curl -s -X POST "$BASE/api/projects/{projectId}/schemas" \
  -H "Content-Type: application/json" \
  $AUTH \
  -d '{
    "schemaName": "Article",
    "schemaType": "article",
    "description": "Artículo de blog",
    "fields": [
      {"fieldName": "body", "fieldType": "RichText", "isRequired": true, "displayOrder": 0},
      {"fieldName": "author", "fieldType": "String", "isRequired": true, "displayOrder": 1}
    ],
    "createdBy": "11111111-1111-1111-1111-111111111111"
  }'
# Respuesta: GUID del schema

# 5. Listar schemas del proyecto
curl -s "$BASE/api/projects/{projectId}/schemas" $AUTH
```

Para **Content** necesitas además un **Environment** (ver apartado 6). Puedes crearlo por API con **POST** `api/projects/{projectId}/environments`.

---

## 6. Probar contenido (Environment)

Puedes crear un **Environment** por API (recomendado) o, si lo prefieres, insertarlo en la base de datos.

### 6.1 Obtener IDs

1. Crea un **proyecto** (POST `/api/projects`) → anota `projectId`.
2. Crea un **schema** (POST `/api/projects/{projectId}/schemas`) → anota `schemaId`.

### 6.2 Crear Environment por API (recomendado)

**POST** `api/projects/{projectId}/environments` con body:

```json
{
  "name": "Development",
  "description": "Entorno de desarrollo",
  "createdBy": "11111111-1111-1111-1111-111111111111"
}
```

Sustituye `createdBy` por un GUID de usuario (por ejemplo el `userId` que devuelve Identity al hacer login). La respuesta es el **GUID del entorno** → ese es tu `environmentId`. Requiere JWT si Core API tiene autenticación activada. Ver **`docs/COMO_PROBAR_MEJORAS_CORE_API.md`**.

### 6.3 Alternativa: Insertar Environment en PostgreSQL

Si no usas el endpoint (o no tienes JWT), puedes insertar el entorno en la base de datos. Ejecuta en `ioda_core` (sustituye `TU_PROJECT_ID` por el GUID del proyecto).

**Importante:** La tabla fue creada por EF Core con nombres de columna en snake_case en la BD. En PostgreSQL las columnas son `project_id`, `public_id`, etc. (minúsculas). Si tu migración usa PascalCase con comillas, cita con comillas dobles (`"Id"`).

```sql
INSERT INTO environments ("Id", project_id, public_id, name, slug, description, is_active, created_at, updated_at)
VALUES (
  gen_random_uuid(),
  'TU_PROJECT_ID',
  'env_' || substr(md5(random()::text), 1, 8),
  'Development',
  'development',
  'Entorno de desarrollo',
  true,
  NOW(),
  NULL
)
RETURNING "Id";
```

Anota el **Id** devuelto → ese es tu `environmentId`.

### 6.4 Crear contenido

```bash
curl -s -X POST "$BASE/api/projects/{projectId}/content" \
  -H "Content-Type: application/json" \
  -d '{
    "environmentId": "ENVIRONMENT_ID_DEL_PASO_ANTERIOR",
    "schemaId": "SCHEMA_ID_DEL_PASO_6.1",
    "title": "Mi primer artículo",
    "contentType": "article",
    "fields": {
      "body": "<p>Hola mundo</p>",
      "author": "Yo"
    },
    "createdBy": "11111111-1111-1111-1111-111111111111"
  }'
```

Luego puedes listar contenido, obtener por ID, actualizar, publicar y despublicar usando los endpoints de **Content** en Swagger o en el `.http`.

---

## 7. Resumen de flujo de prueba

| Orden | Acción | Endpoint / Acción |
|-------|--------|-------------------|
| 1 | Crear proyecto | `POST /api/projects` |
| 2 | Obtener proyecto | `GET /api/projects/{id}` |
| 3 | Crear schema | `POST /api/projects/{id}/schemas` |
| 4 | Listar / obtener schema | `GET /api/projects/{id}/schemas`, `GET .../schemas/{schemaId}` |
| 5 | Crear environment | `POST /api/projects/{id}/environments` (recomendado) o SQL en `ioda_core` (ver 6.3) |
| 6 | Crear contenido | `POST /api/projects/{id}/content` |
| 7 | Listar / obtener contenido | `GET .../content`, `GET .../content/{contentId}` |
| 8 | Actualizar contenido | `PUT .../content/{contentId}` |
| 9 | Publicar / despublicar | `POST .../content/{contentId}/publish`, `.../unpublish` |
| 10 | Contenido publicado (por entorno) | `GET .../environments/{envId}/content/published` |
| 11 | Versión de contenido | `GET .../content/{contentId}/versions/{versionNumber}` |

---

## 8. Errores frecuentes

- **Connection string**: que `appsettings` apunte a la misma base `ioda_core` donde aplicaste las migraciones.
- **Migraciones**: si cambias el modelo, genera y aplica una nueva migración con `dotnet ef migrations add ...` y `dotnet ef database update ...`.
- **RabbitMQ**: si no lo tienes levantado, la API puede arrancar pero al publicar eventos (crear/actualizar/publicar contenido, crear schema) puede fallar; para probar solo CRUD sin eventos, en teoría podrías comentar temporalmente la publicación, pero lo normal es tener RabbitMQ para probar el flujo completo.
- **Environment**: para Content siempre hace falta un `environmentId` válido; créalo con **POST** `api/projects/{projectId}/environments` (ver 6.2) o con el SQL del apartado 6.3.
- **401 Unauthorized**: si Core API tiene JWT configurado, todas las peticiones a proyectos/schemas/content deben llevar `Authorization: Bearer <token>`. Obtén el token desde Identity API (ver `docs/COMO_PROBAR_MEJORAS_CORE_API.md`).
- **RabbitMQ ACCESS_REFUSED**: si activas RabbitMQ (`RabbitMQ:Enabled: true`) y ves *"Login was refused"* o *"ACCESS_REFUSED"*, el usuario/contraseña no coinciden con los del broker. Usa en `appsettings` el mismo **Username** y **Password** que tenga tu RabbitMQ (en otro proyecto suele ser distinto de guest/guest). Para probar solo CRUD sin cola, deja `RabbitMQ:Enabled: false` en `appsettings.Development.json`.

---

## 9. Siguientes pasos (según FASE_1_PROGRESO y NEXT_STEPS)

- **CreateEnvironment** y **GET/LIST** de entornos ya están implementados (Fase de mejoras). Paginación de proyectos, health RabbitMQ y JWT también; ver **`docs/COMO_PROBAR_MEJORAS_CORE_API.md`**.
- Opcional: integración con Authorization (comprobar permiso en Core), más health checks.

Con esto puedes probar todo lo que ya está creado (Domain, Application, Infrastructure, API de Projects/Environments/Schemas/Content) antes de continuar.
