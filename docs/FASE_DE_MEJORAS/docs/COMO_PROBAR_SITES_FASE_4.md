# Cómo probar el Soporte para Sitios (Fase 4)

Guía para probar la **API de Sitios** del Core: creación, listado, actualización, activación/desactivación y eliminación de sitios dentro de proyectos.

**Referencia:** `docs/FASE_DE_MEJORAS/BACKEND_STEPS.md` (sección 4. Soporte para Sitios).

---

## 1. Prerrequisitos

- **Conexión a PostgreSQL:** La migración y la Core API usan la connection string de configuración. Si no usas `appsettings.json` con el usuario correcto, define la variable de entorno **antes** de ejecutar la migración o levantar la API (en bash/zsh):
  ```bash
  export ConnectionStrings__DefaultConnection="Host=localhost;Database=ioda_core;Username=postgres;Password=postgres;Include Error Detail=true"
  ```
  Ajusta `Username` y `Password` si en tu entorno usas otros valores. En Windows (PowerShell): `$env:ConnectionStrings__DefaultConnection="Host=localhost;Database=ioda_core;Username=postgres;Password=postgres;Include Error Detail=true"`.
- **Core API** con la migración `AddSitesTable` aplicada:
  ```bash
  dotnet ef database update --project src/Services/Core/IODA.Core.Infrastructure --startup-project src/Services/Core/IODA.Core.API
  ```
- **Identity API** levantada para obtener JWT (igual que en COMO_PROBAR_BACKEND_FASE_1).
- **Proyecto existente** en Core (un `projectId` válido).
- **Entorno opcional:** Si quieres asociar sitios a un entorno específico, necesitas un `environmentId` válido (creado previamente con `POST /api/projects/{projectId}/environments`).

### Si aparece "relation projects already exists"

Si la base `ioda_core` ya tiene las tablas de migraciones anteriores pero EF no tiene registrada la migración inicial en `__EFMigrationsHistory`, marca la migración inicial como aplicada:

1. Conéctate a PostgreSQL (psql, DBeaver, etc.) a la base `ioda_core` y ejecuta:
   ```sql
   INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
   VALUES ('20260201033638_InitialCreate', '9.0.0')
   ON CONFLICT DO NOTHING;
   ```
2. Vuelve a ejecutar:
   ```bash
   dotnet ef database update --project src/Services/Core/IODA.Core.Infrastructure --startup-project src/Services/Core/IODA.Core.API
   ```

---

## 2. Obtener JWT y variables de entorno

Igual que en COMO_PROBAR_BACKEND_FASE_1:

1. Login en Identity: `POST /api/auth/login` con email y password.
2. Copiar el `accessToken` de la respuesta.
3. En Swagger Core: **Authorize** → pegar el token (solo el valor, sin "Bearer").

Para **curl** o **.http**:

```bash
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."   # tu accessToken
BASE=http://localhost:5269
PROJECT_ID=ff2223f4-2e1b-4c49-8cb5-87df8ff13f04   # GUID de un proyecto existente
ENVIRONMENT_ID=...   # GUID de un entorno (opcional, solo si quieres asociar el sitio a un entorno)
USER_ID=...          # GUID del usuario (p. ej. el que devuelve Identity en login)
```

---

## 3. Crear un sitio (POST sites)

### 3.1 Desde Swagger

1. **POST** `api/projects/{projectId}/sites`
2. Body (JSON):
   ```json
   {
     "environmentId": null,
     "name": "Mi Sitio Principal",
     "domain": "example.com",
     "subdomain": null,
     "subpath": null,
     "themeId": "theme-default",
     "createdBy": "00000000-0000-0000-0000-000000000000"
   }
   ```
   - `environmentId`: Opcional. Si es `null`, el sitio es global al proyecto. Si proporcionas un GUID, debe existir un entorno con ese ID en el proyecto.
   - `name`: Nombre del sitio (requerido, máx. 200 caracteres).
   - `domain`: Dominio principal (requerido, máx. 255 caracteres, debe ser un dominio válido, p. ej. "example.com").
   - `subdomain`: Opcional (p. ej. "blog" para "blog.example.com").
   - `subpath`: Opcional (p. ej. "/blog" para "example.com/blog"). Debe empezar con "/" si se proporciona.
   - `themeId`: Opcional, identificador del tema asociado.
   - `createdBy`: GUID del usuario que crea el sitio.

3. Respuesta `201 Created` con el GUID del sitio creado:
   ```json
   "ff2223f4-2e1b-4c49-8cb5-87df8ff13f04"
   ```

### 3.2 Ejemplos con curl

**Sitio global al proyecto (sin entorno):**
```bash
curl -X POST "$BASE/api/projects/$PROJECT_ID/sites" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "environmentId": null,
    "name": "Sitio Principal",
    "domain": "example.com",
    "createdBy": "'$USER_ID'"
  }'
```

**Sitio con subdominio:**
```bash
curl -X POST "$BASE/api/projects/$PROJECT_ID/sites" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "environmentId": null,
    "name": "Blog",
    "domain": "example.com",
    "subdomain": "blog",
    "createdBy": "'$USER_ID'"
  }'
```

**Sitio con subruta:**
```bash
curl -X POST "$BASE/api/projects/$PROJECT_ID/sites" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "environmentId": null,
    "name": "Tienda",
    "domain": "example.com",
    "subpath": "/shop",
    "themeId": "theme-ecommerce",
    "createdBy": "'$USER_ID'"
  }'
```

**Sitio asociado a un entorno:**
```bash
curl -X POST "$BASE/api/projects/$PROJECT_ID/sites" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "environmentId": "'$ENVIRONMENT_ID'",
    "name": "Sitio Staging",
    "domain": "staging.example.com",
    "createdBy": "'$USER_ID'"
  }'
```

---

## 4. Listar sitios (GET sites)

### 4.1 Listar todos los sitios de un proyecto

**Desde Swagger:**
- **GET** `api/projects/{projectId}/sites`

**Con curl:**
```bash
curl -X GET "$BASE/api/projects/$PROJECT_ID/sites" \
  -H "Authorization: Bearer $TOKEN"
```

**Respuesta `200 OK`:**
```json
[
  {
    "id": "ff2223f4-2e1b-4c49-8cb5-87df8ff13f04",
    "publicId": "sit-abc123",
    "projectId": "ff2223f4-2e1b-4c49-8cb5-87df8ff13f04",
    "environmentId": null,
    "name": "Mi Sitio Principal",
    "domain": "example.com",
    "subdomain": null,
    "subpath": null,
    "themeId": "theme-default",
    "isActive": true,
    "createdAt": "2026-01-24T10:00:00Z",
    "updatedAt": null,
    "createdBy": "00000000-0000-0000-0000-000000000000"
  }
]
```

### 4.2 Listar sitios filtrados por entorno

**Desde Swagger:**
- **GET** `api/projects/{projectId}/sites?environmentId={environmentId}`

**Con curl:**
```bash
curl -X GET "$BASE/api/projects/$PROJECT_ID/sites?environmentId=$ENVIRONMENT_ID" \
  -H "Authorization: Bearer $TOKEN"
```

---

## 5. Obtener un sitio por ID (GET sites/{siteId})

**Desde Swagger:**
- **GET** `api/projects/{projectId}/sites/{siteId}`

**Con curl:**
```bash
SITE_ID=ff2223f4-2e1b-4c49-8cb5-87df8ff13f04

curl -X GET "$BASE/api/projects/$PROJECT_ID/sites/$SITE_ID" \
  -H "Authorization: Bearer $TOKEN"
```

**Respuesta `200 OK`:**
```json
{
  "id": "ff2223f4-2e1b-4c49-8cb5-87df8ff13f04",
  "publicId": "sit-abc123",
  "projectId": "ff2223f4-2e1b-4c49-8cb5-87df8ff13f04",
  "environmentId": null,
  "name": "Mi Sitio Principal",
  "domain": "example.com",
  "subdomain": null,
  "subpath": null,
  "themeId": "theme-default",
  "isActive": true,
  "createdAt": "2026-01-24T10:00:00Z",
  "updatedAt": null,
  "createdBy": "00000000-0000-0000-0000-000000000000"
}
```

**Si no existe:** `404 Not Found`

---

## 6. Actualizar un sitio (PUT sites/{siteId})

**Desde Swagger:**
- **PUT** `api/projects/{projectId}/sites/{siteId}`
- Body (JSON):
  ```json
  {
    "name": "Sitio Actualizado",
    "domain": "nuevo-dominio.com",
    "subdomain": "www",
    "subpath": null,
    "themeId": "theme-new"
  }
  ```

**Con curl:**
```bash
curl -X PUT "$BASE/api/projects/$PROJECT_ID/sites/$SITE_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Sitio Actualizado",
    "domain": "nuevo-dominio.com",
    "subdomain": "www",
    "themeId": "theme-new"
  }'
```

**Respuesta `200 OK`** con el sitio actualizado (mismo formato que GET).

**Errores comunes:**
- `400 Bad Request`: Validación fallida (dominio inválido, nombre vacío, subpath sin "/", etc.).
- `404 Not Found`: Sitio no encontrado o no pertenece al proyecto indicado.

---

## 7. Activar un sitio (POST sites/{siteId}/activate)

**Desde Swagger:**
- **POST** `api/projects/{projectId}/sites/{siteId}/activate`

**Con curl:**
```bash
curl -X POST "$BASE/api/projects/$PROJECT_ID/sites/$SITE_ID/activate" \
  -H "Authorization: Bearer $TOKEN"
```

**Respuesta `204 No Content`** si se activó correctamente.

---

## 8. Desactivar un sitio (POST sites/{siteId}/deactivate)

**Desde Swagger:**
- **POST** `api/projects/{projectId}/sites/{siteId}/deactivate`

**Con curl:**
```bash
curl -X POST "$BASE/api/projects/$PROJECT_ID/sites/$SITE_ID/deactivate" \
  -H "Authorization: Bearer $TOKEN"
```

**Respuesta `204 No Content`** si se desactivó correctamente.

---

## 9. Eliminar un sitio (DELETE sites/{siteId})

**Desde Swagger:**
- **DELETE** `api/projects/{projectId}/sites/{siteId}`

**Con curl:**
```bash
curl -X DELETE "$BASE/api/projects/$PROJECT_ID/sites/$SITE_ID" \
  -H "Authorization: Bearer $TOKEN"
```

**Respuesta `204 No Content`** si se eliminó correctamente.

**⚠️ Advertencia:** Esta operación es **irreversible**. El sitio se elimina permanentemente de la base de datos.

---

## 10. Resumen de endpoints

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `POST` | `/api/projects/{projectId}/sites` | Crear sitio |
| `GET` | `/api/projects/{projectId}/sites` | Listar sitios (opcional: `?environmentId={id}`) |
| `GET` | `/api/projects/{projectId}/sites/{siteId}` | Obtener sitio por ID |
| `PUT` | `/api/projects/{projectId}/sites/{siteId}` | Actualizar sitio |
| `POST` | `/api/projects/{projectId}/sites/{siteId}/activate` | Activar sitio |
| `POST` | `/api/projects/{projectId}/sites/{siteId}/deactivate` | Desactivar sitio |
| `DELETE` | `/api/projects/{projectId}/sites/{siteId}` | Eliminar sitio |

Todos los endpoints requieren autenticación JWT (`Authorization: Bearer <token>`).

---

## 11. Errores comunes

### 11.1 `400 Bad Request` - Validación fallida

**Causa:** Datos inválidos en la petición.

**Ejemplos:**
- Dominio inválido (no cumple formato de dominio válido).
- Nombre vacío o demasiado largo (>200 caracteres).
- Subpath sin empezar con "/".
- Subdomain o Subpath demasiado largos.

**Solución:** Revisa el body de la petición y corrige los campos según las reglas de validación.

**Ejemplo de respuesta:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "Domain": ["Domain must be a valid domain name (e.g., example.com)."],
    "Name": ["Site name is required."]
  }
}
```

### 11.2 `404 Not Found` - Sitio no encontrado

**Causa:** El `siteId` no existe o no pertenece al `projectId` indicado.

**Solución:** Verifica que el sitio existe con `GET /api/projects/{projectId}/sites` y usa el `id` correcto.

### 11.3 `400 Bad Request` - Project o Environment no encontrado

**Causa:** Al crear un sitio, el `projectId` o `environmentId` (si se proporciona) no existe.

**Solución:** Verifica que el proyecto existe con `GET /api/projects` y que el entorno existe (si lo usas) con `GET /api/projects/{projectId}/environments`.

### 11.4 `401 Unauthorized` - Token inválido o faltante

**Causa:** No se proporcionó token JWT o el token expiró.

**Solución:** Obtén un nuevo token desde Identity API (`POST /api/auth/login`) y úsalo en el header `Authorization: Bearer <token>`.

---

## 12. Uso desde el frontend

### Ejemplo con fetch (JavaScript/TypeScript)

```typescript
const BASE_URL = 'http://localhost:5269';
const token = '...'; // JWT token

// Crear sitio
async function createSite(projectId: string, siteData: {
  name: string;
  domain: string;
  subdomain?: string;
  subpath?: string;
  themeId?: string;
  environmentId?: string;
  createdBy: string;
}) {
  const response = await fetch(`${BASE_URL}/api/projects/${projectId}/sites`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      environmentId: siteData.environmentId || null,
      name: siteData.name,
      domain: siteData.domain,
      subdomain: siteData.subdomain || null,
      subpath: siteData.subpath || null,
      themeId: siteData.themeId || null,
      createdBy: siteData.createdBy,
    }),
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.detail || 'Error al crear sitio');
  }

  return await response.text(); // GUID del sitio creado
}

// Listar sitios
async function listSites(projectId: string, environmentId?: string) {
  const url = environmentId
    ? `${BASE_URL}/api/projects/${projectId}/sites?environmentId=${environmentId}`
    : `${BASE_URL}/api/projects/${projectId}/sites`;

  const response = await fetch(url, {
    headers: {
      'Authorization': `Bearer ${token}`,
    },
  });

  if (!response.ok) {
    throw new Error('Error al listar sitios');
  }

  return await response.json();
}

// Obtener sitio
async function getSite(projectId: string, siteId: string) {
  const response = await fetch(`${BASE_URL}/api/projects/${projectId}/sites/${siteId}`, {
    headers: {
      'Authorization': `Bearer ${token}`,
    },
  });

  if (!response.ok) {
    if (response.status === 404) {
      return null;
    }
    throw new Error('Error al obtener sitio');
  }

  return await response.json();
}

// Actualizar sitio
async function updateSite(projectId: string, siteId: string, updates: {
  name: string;
  domain: string;
  subdomain?: string;
  subpath?: string;
  themeId?: string;
}) {
  const response = await fetch(`${BASE_URL}/api/projects/${projectId}/sites/${siteId}`, {
    method: 'PUT',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(updates),
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.detail || 'Error al actualizar sitio');
  }

  return await response.json();
}

// Activar/Desactivar sitio
async function toggleSite(projectId: string, siteId: string, activate: boolean) {
  const endpoint = activate ? 'activate' : 'deactivate';
  const response = await fetch(
    `${BASE_URL}/api/projects/${projectId}/sites/${siteId}/${endpoint}`,
    {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
      },
    }
  );

  if (!response.ok) {
    throw new Error(`Error al ${activate ? 'activar' : 'desactivar'} sitio`);
  }
}

// Eliminar sitio
async function deleteSite(projectId: string, siteId: string) {
  const response = await fetch(`${BASE_URL}/api/projects/${projectId}/sites/${siteId}`, {
    method: 'DELETE',
    headers: {
      'Authorization': `Bearer ${token}`,
    },
  });

  if (!response.ok) {
    throw new Error('Error al eliminar sitio');
  }
}
```

---

## 13. Notas adicionales

- **Relación con entornos:** Un sitio puede estar asociado a un entorno específico (`environmentId`) o ser global al proyecto (`environmentId = null`). Esto permite tener diferentes configuraciones de sitios por entorno (dev, staging, prod).
- **Dominios únicos:** El sistema permite múltiples sitios con el mismo dominio siempre que difieran en `subdomain` o `subpath`. El método `GetByDomainAsync` en el repositorio busca sitios activos por dominio, subdominio y subruta.
- **Temas:** El campo `themeId` es opcional y puede usarse para asociar un tema específico al sitio. La validación del formato del `themeId` queda a cargo del frontend o de futuras validaciones de negocio.
- **Filtrado de contenido por sitio:** Implementado. La entidad `Content` tiene `SiteId` opcional. Al crear contenido puedes enviar `siteId` en el body. Para listar solo el contenido de un sitio: `GET /api/projects/{projectId}/content?siteId={siteId}`. Para contenido publicado por sitio: `GET /api/projects/{projectId}/environments/{environmentId}/content/published?siteId={siteId}`. Si no envías `siteId`, se devuelve todo el contenido del proyecto (o todo el publicado del entorno).

---

**Última actualización:** 2026-01-24  
**Relacionado:** BACKEND_STEPS.md (sección 4), COMO_PROBAR_BACKEND_FASE_1.md (JWT y prerrequisitos), COMO_PROBAR_MEDIA_FASE_2.md.

¡Listo! Ya puedes probar la gestión completa de sitios en tu CMS.
