# Cómo probar lo ya creado (Fase 4 – Publishing)

Guía para ejecutar y probar el **Publishing API** (solicitudes de publicación, validación, aprobación y llamada al Core API para publicar).

---

## 1. Prerrequisitos

- **.NET 9 SDK**
- **PostgreSQL** (puerto 5432) – local o Docker
- **Core API** levantada y accesible (Publishing llama a Core API para obtener contenido y para publicar)
- Contenido creado en Core (proyecto, environment, schema, contenido en estado Draft) para poder solicitar su publicación

---

## 2. Base de datos

### 2.1 Crear la base de datos

```bash
createdb -U postgres -p 5432 ioda_publishing
# o
psql -h localhost -p 5432 -U postgres -c "CREATE DATABASE ioda_publishing;"
```

### 2.2 Aplicar migraciones

Desde la raíz del repo:

```bash
cd /Users/josuegolivares/desarrollo/csharp/ioda

export ConnectionStrings__DefaultConnection="Host=localhost;Database=ioda_publishing;Username=postgres;Password=postgres;Include Error Detail=true"
dotnet ef database update --project src/Services/Publishing/IODA.Publishing.Infrastructure/IODA.Publishing.Infrastructure.csproj --startup-project src/Services/Publishing/IODA.Publishing.API/IODA.Publishing.API.csproj
```

Si no tienes `dotnet-ef` instalado: `dotnet tool install --global dotnet-ef`

---

## 3. Configuración

Revisa **`src/Services/Publishing/IODA.Publishing.API/appsettings.json`**:

- **ConnectionStrings:DefaultConnection** – debe apuntar a tu PostgreSQL (`ioda_publishing`).
- **CoreApi:BaseUrl** – URL base del Core API. En local suele ser `http://localhost:5001` (o el puerto donde corra Core API; revisa `launchSettings.json` de Core). En Docker usa `http://ioda-core-api:8080`.

---

## 4. Ejecutar la API

### Opción A: Desde la solución

1. Arranca **Core API** en otro terminal (necesaria para aprobar publicaciones):
   ```bash
   dotnet run --project src/Services/Core/IODA.Core.API/IODA.Core.API.csproj
   ```
2. Arranca **Publishing API**:
   ```bash
   dotnet run --project src/Services/Publishing/IODA.Publishing.API/IODA.Publishing.API.csproj
   ```
- Swagger Publishing: **http://localhost:5272/swagger**

### Opción B: Con Docker

```bash
docker compose --profile services up -d ioda-core-api ioda-publishing-api
```

- Swagger Publishing: **http://localhost:5004/swagger**
- Core API debe estar en la misma red (ioda-core-api:8080).

---

## 5. Probar el flujo

### 5.1 Preparar contenido en Core

1. Crea un **proyecto** en Core API (POST `/api/projects`). Si Core exige JWT, usa un token de Identity (ver `docs/COMO_PROBAR_MEJORAS_CORE_API.md`).
2. Crea un **Environment** con **POST** `api/projects/{projectId}/environments` (o insértalo en la base `ioda_core`) y anota `environmentId`.
3. Crea un **schema** (POST `/api/projects/{projectId}/schemas`).
4. Crea **contenido** (POST `/api/projects/{projectId}/content`) con ese environmentId y schemaId. Anota `contentId`, `projectId`, `environmentId`.

### 5.2 Flujo en Publishing API

1. **Solicitar publicación**:  
   `POST /api/publishing/requests`  
   Body: `{ "contentId": "...", "projectId": "...", "environmentId": "...", "requestedBy": "user-guid" }`  
   → Respuesta: ID de la solicitud (requestId).

2. **Listar solicitudes**:  
   `GET /api/publishing/requests` o `GET /api/publishing/requests?status=0` (Pending).

3. **Aprobar publicación**:  
   `POST /api/publishing/requests/{requestId}/approve`  
   Body: `{ "approvedBy": "user-guid" }`  
   → Publishing valida el contenido (llama a Core API), si es válido llama a Core API para publicar. La solicitud pasa a Approved.

4. **Rechazar** (alternativa):  
   `POST /api/publishing/requests/{requestId}/reject`  
   Body: `{ "rejectedBy": "user-guid", "reason": "opcional" }`

### 5.3 Con curl (resumen)

Sustituye `BASE=http://localhost:5272` (o `http://localhost:5004` con Docker). Usa `projectId`, `contentId`, `environmentId` y un `userGuid` reales de Core.

```bash
BASE=http://localhost:5272
PROJECT_ID=...
CONTENT_ID=...
ENV_ID=...
USER_ID=11111111-1111-1111-1111-111111111111

# 1. Solicitar publicación
curl -s -X POST "$BASE/api/publishing/requests" \
  -H "Content-Type: application/json" \
  -d "{\"contentId\":\"$CONTENT_ID\",\"projectId\":\"$PROJECT_ID\",\"environmentId\":\"$ENV_ID\",\"requestedBy\":\"$USER_ID\"}"
# Anotar el id devuelto → REQUEST_ID

# 2. Listar solicitudes
curl -s "$BASE/api/publishing/requests"

# 3. Aprobar (valida y llama a Core API para publicar)
curl -s -X POST "$BASE/api/publishing/requests/REQUEST_ID/approve" \
  -H "Content-Type: application/json" \
  -d "{\"approvedBy\":\"$USER_ID\"}"
```

---

## 6. Validación de contenido

Antes de publicar, Publishing valida:

- El contenido existe en Core API (GET contenido).
- **Title** no está vacío.
- El contenido **no** está ya en estado Published.
- Tiene al menos un campo en **Fields**.

Si falla la validación, la aprobación devuelve error (400) y no se llama a Core API.

---

## 7. Errores frecuentes

- **Core API no accesible**: 502 Bad Gateway o timeout. Comprueba que Core API esté levantada y que **CoreApi:BaseUrl** sea correcta (puerto y host).
- **401 al aprobar (Core API exige JWT):** Si la Core API está protegida con JWT (Fase de mejoras), las llamadas que **Publishing** hace a Core (obtener contenido, publicar) deben ir con un Bearer token. El **CorePublishClient** actual no envía token; si Core tiene JWT activado, la aprobación puede fallar con **401 Unauthorized** al llamar a Core. Opciones: (1) En desarrollo, no configurar Jwt en Core API (dejar SecretKey vacío) para que Core no exija autenticación; (2) O extender Publishing para que envíe un token (p. ej. client credentials o token de sistema) en las llamadas a Core. Ver **`docs/COMO_PROBAR_MEJORAS_CORE_API.md`**.
- **Content not found**: El contentId no existe en Core o el projectId no coincide.
- **Content validation failed**: Título vacío, ya publicado o sin campos. Corrige el contenido en Core y vuelve a solicitar/aprobar.
- **Connection string**: que `appsettings` apunte a la misma base `ioda_publishing` donde aplicaste las migraciones.

---

## 8. Siguientes pasos

- Consumir eventos del Core (ContentCreated, ContentUpdated) con MassTransit para auditar o sincronizar.
- Workflows configurables (pasos y transiciones).
- Integrar con Authorization (comprobar permiso antes de aprobar).

Con esto puedes probar todo lo implementado en la Fase 4 (solicitudes, validación, aprobación y publicación vía Core API).
