# Cómo probar las mejoras en Core API (Fase de mejoras)

Guía para probar lo añadido en la **Fase de mejoras** al CMS Core API: endpoint de entornos, paginación de proyectos, health check de RabbitMQ y protección JWT.

**Referencia:** `docs/FASE_DE_MEJORAS/BACKEND_STEPS.md` (sección 1 completada).

---

## 1. Prerrequisitos

- Haber aplicado las **mejoras en Core API** (CreateEnvironment, paginación proyectos, health RabbitMQ, JWT).
- **Identity API** levantada (para obtener un JWT y probar Core con autenticación).
- **PostgreSQL** con la base `ioda_core` y migraciones aplicadas (igual que en COMO_PROBAR_FASE_1).

---

## 2. Autenticación (JWT)

La Core API **exige un JWT válido** en todos los endpoints de proyectos, contenido y schemas (salvo `/health`).

### 2.1 Obtener un token

1. Arranca **Identity API** (ver `docs/FASE_DE_CREACION_INICIAL/docs/COMO_PROBAR_FASE_2.md`).
2. Registra o inicia sesión y obtén el **accessToken**:
   - **Desde Swagger Identity:** `POST /api/auth/login` → copia `accessToken` de la respuesta.
   - **Desde el frontend:** inicia sesión y en DevTools (Application → Local Storage o en la petición de red) puedes ver el token si lo guardas allí, o usa el flujo de login por API.

### 2.2 Usar el token en Swagger (Core API)

1. Abre **http://localhost:5269/swagger** (o el puerto donde corra Core API).
2. Haz clic en **Authorize**.
3. En "Bearer" escribe solo el token (sin la palabra "Bearer").
4. Cierra el cuadro. A partir de ahí, todas las peticiones llevarán `Authorization: Bearer <token>`.

### 2.3 Usar el token con curl

```bash
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."   # tu accessToken de Identity
BASE=http://localhost:5269

curl -s "$BASE/api/projects" -H "Authorization: Bearer $TOKEN"
```

### 2.4 Si no configuras JWT en Core API

Si en **appsettings.json** de Core API **no** pones `Jwt:SecretKey` (o lo dejas vacío), la API **no** activa la autenticación y los endpoints siguen siendo accesibles sin token. Útil solo para pruebas locales sin Identity. En entornos compartidos o producción debes configurar JWT (mismo SecretKey, Issuer y Audience que Identity).

---

## 3. Crear entornos (Environment)

Ya **no** hace falta insertar entornos a mano en la base de datos. Puedes crearlos por API.

### 3.1 Desde Swagger

1. **Authorize** con tu JWT (ver apartado 2).
2. **POST** `api/projects/{projectId}/environments`  
   Body (ejemplo):
   ```json
   {
     "name": "Development",
     "description": "Entorno de desarrollo",
     "createdBy": "11111111-1111-1111-1111-111111111111"
   }
   ```
   Sustituye `createdBy` por un GUID de usuario válido (por ejemplo el `userId` que devuelve Identity al hacer login).
3. La respuesta es el **GUID del entorno** creado y el header `Location` apunta a `GET api/projects/{projectId}/environments/{environmentId}`.

### 3.2 Obtener un entorno por ID

- **GET** `api/projects/{projectId}/environments/{environmentId}`  
  Devuelve el entorno (id, publicId, name, slug, description, isActive, projectId, createdAt, updatedAt).

### 3.3 Listar entornos (ya existía)

- **GET** `api/projects/{projectId}/environments`  
  Devuelve la lista de entornos del proyecto.

### 3.4 Con curl

```bash
TOKEN="..."
BASE=http://localhost:5269
PROJECT_ID=...   # GUID del proyecto
USER_ID=...      # GUID del usuario (p. ej. de Identity)

# Crear entorno
curl -s -X POST "$BASE/api/projects/$PROJECT_ID/environments" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"name\":\"Staging\",\"description\":\"Staging\",\"createdBy\":\"$USER_ID\"}"
# Respuesta: GUID del entorno

# Listar entornos
curl -s "$BASE/api/projects/$PROJECT_ID/environments" -H "Authorization: Bearer $TOKEN"

# Obtener uno por id
curl -s "$BASE/api/projects/$PROJECT_ID/environments/{environmentId}" -H "Authorization: Bearer $TOKEN"
```

---

## 4. Paginación de proyectos

El listado de proyectos pasa a ser **paginado**.

### 4.1 Formato de respuesta

- **GET** `api/projects?page=1&pageSize=20`  
  Respuesta:
  ```json
  {
    "items": [ { "id": "...", "name": "...", ... } ],
    "totalCount": 42,
    "page": 1,
    "pageSize": 20,
    "totalPages": 3,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
  ```

### 4.2 Parámetros

- **page** (opcional): número de página, por defecto 1.
- **pageSize** (opcional): tamaño de página, por defecto 20 (máximo 100 en el handler).

### 4.3 Probar

1. Con JWT (Authorize en Swagger).
2. `GET /api/projects` → devuelve primera página (20 proyectos por defecto).
3. `GET /api/projects?page=2&pageSize=10` → segunda página, 10 por página.

El **frontend del CMS** ya usa esta respuesta (lee `response.items` para la lista de proyectos).

---

## 5. Health check (incl. RabbitMQ)

El endpoint **GET** `/health` sigue devolviendo JSON con el estado de los checks.

### 5.1 Sin RabbitMQ

Si `RabbitMQ:Enabled` es `false` o no está configurado, solo verás el check **database**:

```json
{
  "status": "Healthy",
  "checks": [
    { "name": "database", "status": "Healthy", "description": null }
  ]
}
```

### 5.2 Con RabbitMQ

Si en **appsettings.json** (o Development) tienes:

- `RabbitMQ:Enabled`: `true`
- `RabbitMQ:Host`: por ejemplo `localhost` (o `localhost:5672`)

entonces se registra un segundo check, **rabbitmq**. Ejemplo de respuesta si ambos están bien:

```json
{
  "status": "Healthy",
  "checks": [
    { "name": "database", "status": "Healthy", "description": null },
    { "name": "rabbitmq", "status": "Healthy", "description": null }
  ]
}
```

Si RabbitMQ no está accesible, el check **rabbitmq** aparecerá como **Degraded** (no Unhealthy) y el estado global puede ser Degraded.

### 5.3 Probar

```bash
curl -s http://localhost:5269/health
```

No requiere autenticación.

---

## 6. Resumen de cambios que afectan a “cómo probar”

| Cambio | Cómo probar |
|--------|-------------|
| **JWT obligatorio** | Identity API → login → usar `accessToken` en Swagger (Authorize) o en header `Authorization: Bearer <token>` en curl / .http. |
| **Crear entorno** | POST `api/projects/{projectId}/environments` con name, description, createdBy. Sustituye el SQL manual del COMO_PROBAR_FASE_1. |
| **Listado proyectos paginado** | GET `api/projects?page=1&pageSize=20`; la respuesta tiene `items`, `totalCount`, `page`, `pageSize`. |
| **Health RabbitMQ** | GET `/health`; si RabbitMQ está habilitado, aparece el check "rabbitmq". |

---

## 7. Impacto en otras guías

- **COMO_PROBAR_FASE_1:** Los ejemplos de curl y Swagger deben usar JWT para Core API; la sección de “Environment manual” puede sustituirse por el nuevo POST de entornos (ver actualización en ese archivo).
- **COMO_PROBAR_FASE_4 (Publishing):** Cuando Publishing llama a Core API para aprobar/publicar, Core ahora exige JWT. Si el `CorePublishClient` de Publishing **no** envía un Bearer token, la llamada a Core devolverá **401**. Para probar el flujo completo de aprobación, puede ser necesario configurar un token en Publishing o temporalmente no proteger Core en desarrollo; ver nota añadida en COMO_PROBAR_FASE_4.
- **COMO_PROBAR_FASE_2 y FASE_3:** No cambian (Identity y Authorization no dependen de estos cambios de Core).
- **COMO_PROBAR_FASE_5:** No cambia (Indexing no llama a Core en el flujo de búsqueda/indexación).

---

## 8. Guías de la Fase de mejoras (Core API)

| Guía | Contenido |
|------|-----------|
| **COMO_PROBAR_BACKEND_FASE_1.md** (esta) | Mejoras Core: entornos, paginación proyectos, health RabbitMQ, JWT. |
| **COMO_PROBAR_MEDIA_FASE_2.md** | Media Service: subida de archivos, listado, metadatos, stream/descarga. Requiere migración AddMediaItems, Identity, proyecto existente. |
| **COMO_PROBAR_SITES_FASE_4.md** | Soporte para Sitios: CRUD de sitios por proyecto (dominio, subdominio, subruta, tema). Requiere migración AddSitesTable, Identity, proyecto existente. |

Para probar **Media** o **Sitios**, usa el mismo JWT y BASE/PROJECT_ID que en esta guía; las variables de entorno y el flujo de login son los mismos.

---

**Última actualización:** 2026-01-24  
**Relacionado:** BACKEND_STEPS.md (sección 1), COMO_PROBAR_FASE_1.md, COMO_PROBAR_MEDIA_FASE_2.md, COMO_PROBAR_SITES_FASE_4.md.
