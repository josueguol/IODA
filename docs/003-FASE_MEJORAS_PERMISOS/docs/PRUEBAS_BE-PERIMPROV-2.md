# Pruebas BE-PERIMPROV-2 — Fase 2: JWT con permisos y policies por permiso

Rama: `feature/BE_PERIMPROV-2/JWT-permissions-n-policies-x-permission`

---

## 2.1 Integración Identity → Authorization (permisos efectivos)

Identity obtiene los permisos efectivos del usuario desde el servicio Authorization en Login y Refresh. Authorization expone un endpoint para ello; Identity llama con un cliente HTTP (opcionalmente con API key de servicio).

### 2.1.1 Endpoint de permisos efectivos (Authorization API)

**GET** `/api/authorization/users/{userId}/effective-permissions`

- **Respuesta 200:** JSON array de strings (códigos de permiso), sin duplicados. Ejemplo: `["content.read","content.edit","role.manage"]`.
- **Autenticación:** JWT Bearer **o** header `X-Service-Api-Key` con el valor configurado en `Authorization:ServiceApiKey` (servicio Identity).
- **401:** Si no se envía JWT ni API key válida.

**Pasos para probar:**

1. Arrancar la Authorization API (puerto típico 5271).
2. Tener al menos un usuario con reglas de acceso y roles con permisos asignados (p. ej. crear rol, asignar permisos con `POST roles/{id}/permissions`, crear regla con `POST rules` para un `userId`).
3. **Con JWT:** Obtener un token de Identity para ese usuario (o cualquier usuario con rol Admin) y llamar:
   ```bash
   curl -s -X GET "http://localhost:5271/api/authorization/users/<USER_ID>/effective-permissions" \
     -H "Authorization: Bearer <JWT>"
   ```
   **Esperado:** 200 y array de códigos (p. ej. `["content.create","content.edit"]`). Si el usuario no tiene reglas/permisos, array vacío `[]`.
4. **Con API key (servicio a servicio):** Configurar en Authorization API `Authorization:ServiceApiKey` (p. ej. en User Secrets o appsettings) con un valor secreto. Llamar sin JWT:
   ```bash
   curl -s -X GET "http://localhost:5271/api/authorization/users/<USER_ID>/effective-permissions" \
     -H "X-Service-Api-Key: <SERVICE_API_KEY>"
   ```
   **Esperado:** 200 y mismo array de códigos.
5. **Sin credenciales:** Llamar sin `Authorization` ni `X-Service-Api-Key`. **Esperado:** 401 Unauthorized.

### 2.1.2 Cliente en Identity (Login / Refresh)

Identity debe tener configurado `AuthorizationApi:BaseUrl` (URL base del API de Authorization, p. ej. `http://localhost:5271`). Opcional: `AuthorizationApi:ServiceApiKey` para enviar en `X-Service-Api-Key`.

- Si **no** se configura `BaseUrl`, Identity usa `NoOpEffectivePermissionsClient` (devuelve lista vacía; Login/Refresh siguen funcionando).
- Si se configura `BaseUrl`, Identity usa el cliente HTTP y en Login y Refresh obtiene los permisos efectivos antes de generar el access token (en 2.2 esos códigos se incluirán en el JWT).

**Pasos para probar:**

1. Configurar en Identity (User Secrets o appsettings.Development):
   - `AuthorizationApi:BaseUrl` = `http://localhost:5271` (o la URL donde corre Authorization).
   - Opcional: `AuthorizationApi:ServiceApiKey` = mismo valor que `Authorization:ServiceApiKey` en Authorization.
2. Arrancar Identity y Authorization; asegurar que el usuario de prueba tiene al menos una regla de acceso con rol que tenga permisos.
3. **Login:** Hacer login con ese usuario. **Esperado:** 200 y access token (aún sin claims de permisos en 2.1; en 2.2 se validará que el JWT incluya los códigos).
4. **Refresh:** Usar el refresh token. **Esperado:** 200 y nuevo access token.
5. Si Authorization API no está disponible o devuelve error, el cliente debe degradar sin romper Login/Refresh (lista vacía y log de advertencia).

### 2.1.3 Resumen de configuración

| Servicio     | Clave                         | Descripción |
|-------------|--------------------------------|-------------|
| Authorization | `Authorization:ServiceApiKey` | Valor opcional; si está definido, el endpoint `effective-permissions` acepta header `X-Service-Api-Key` con este valor. |
| Identity    | `AuthorizationApi:BaseUrl`    | URL base del API de Authorization (ej. `http://localhost:5271`). Si vacío, no se llama al servicio. |
| Identity    | `AuthorizationApi:ServiceApiKey` | Opcional; se envía en `X-Service-Api-Key` al llamar a Authorization. |

---

## 2.2 Incluir permisos en JWT (Identity)

El access token incluye un claim por cada permiso efectivo del usuario: tipo de claim **`permission`**, valor = código (ej. `content.edit`, `role.manage`). Las APIs que usen policies por permiso (2.4) validarán con `RequireClaim("permission", "<code>")`.

### Pasos para probar

1. Tener 2.1 operativo: Identity con `AuthorizationApi:BaseUrl` y usuario con al menos un rol con permisos asignados en Authorization.
2. **Login:** Hacer login y obtener el `accessToken` de la respuesta.
3. **Decodificar el JWT:** Usar [jwt.io](https://jwt.io) o cualquier decodificador (payload en base64). En el payload deben aparecer claims con tipo **`permission`** y valores los códigos (ej. `"permission": "content.edit"`; si hay varios, varios claims con el mismo tipo).
4. **Refresh:** Hacer refresh con el refresh token. El nuevo access token también debe incluir los mismos claims `permission`.
5. Usuario sin reglas/permisos: el token no debe incluir claims `permission` (o lista vacía); Login/Refresh siguen devolviendo 200.
6. Cuando se implemente 2.4, comprobar que un endpoint protegido por `RequireClaim("permission", "role.manage")` acepta un token que tenga ese claim y rechaza (403) un token sin él.

---

## 2.4 Policies por permiso en cada API

Cada API protege sus endpoints con políticas que exigen el claim **`permission`** con el código correspondiente (emitido por Identity en el JWT). Ya no se usa `RequireRole("Admin")` ni `RequireRole("Editor", "Admin")`.

### Convención policy → permiso

| API            | Nombre policy   | Permiso requerido |
|----------------|-----------------|--------------------|
| Authorization  | Admin           | role.manage        |
| Publishing     | Editor          | content.publish    |
| Core           | content.edit    | content.edit       |
| Core           | project.edit    | project.edit       |
| Core           | schema.edit     | schema.edit        |
| Core           | site.edit       | site.edit          |
| Indexing       | content.edit    | content.edit       |

### Pasos para probar

1. **Usuario con permiso `role.manage`:** Hacer login, obtener JWT. Llamar a Authorization: `POST /api/authorization/roles`, `POST /api/authorization/roles/{id}/permissions`, etc. **Esperado:** 204/201 según el endpoint.
2. **Usuario sin `role.manage`:** JWT sin claim `permission` con valor `role.manage`. Llamar a `POST /api/authorization/roles`. **Esperado:** 403 Forbidden.
3. **Usuario con permiso `content.publish`:** Llamar a Publishing (ej. endpoint que requiera policy Editor). **Esperado:** 200/201 según el caso.
4. **Usuario sin `content.publish`:** Llamar al mismo endpoint de Publishing. **Esperado:** 403 Forbidden.
5. **Core / Indexing:** Con JWT que incluya `content.edit`, llamar a Core (ContentController, MediaController) o Indexing. **Esperado:** 200. Con JWT sin `content.edit`, **esperado:** 403. Igual para `project.edit` (ProjectsController), `schema.edit` (SchemasController), `site.edit` (SitesController).

---

## 2.5 Crear SuperAdmin y asignar primer usuario

### Authorization

- **Al arranque:** Tras el seeder de permisos, se ejecuta el seeder del rol "SuperAdmin": se crea el rol si no existe y se le asignan todos los permisos del catálogo.
- **Endpoint:** **POST** `/api/authorization/bootstrap-first-user` con body `{ "userId": "<guid>" }`. Autenticación: JWT o header `X-Service-Api-Key`.
  - **201 Created:** Se creó la regla de acceso (userId → rol SuperAdmin). Solo ocurre cuando aún no existe ninguna regla en el sistema (primer usuario).
  - **409 Conflict:** Ya existe al menos una regla ("Bootstrap already done").
  - **401:** Sin credenciales válidas.

### Identity

- Tras registrar un usuario, si es el **primer usuario** (no había ninguno antes), Identity llama automáticamente a Authorization `POST bootstrap-first-user` con el `userId` del recién registrado. Si Authorization no está configurado (`AuthorizationApi:BaseUrl` vacío) o falla la llamada, el registro sigue siendo correcto (solo se registra un warning en log).

### Pasos para probar

1. **BD limpia:** Asegurar que la tabla `access_rules` en Authorization está vacía y que Identity tiene 0 usuarios (o borrar el usuario de prueba).
2. **Configurar Identity:** `AuthorizationApi:BaseUrl` y `AuthorizationApi:ServiceApiKey` (y en Authorization `Authorization:ServiceApiKey` con el mismo valor).
3. **Arrancar Authorization** (para que corran los seeders: permisos + rol SuperAdmin).
4. **Registrar el primer usuario** vía Identity (POST register). **Esperado:** 200 y usuario creado; en logs de Identity debe verse la llamada a bootstrap; en Authorization debe crearse una fila en `access_rules` para ese userId con el roleId de SuperAdmin.
5. **Comprobar permisos:** Hacer login con ese usuario. El JWT debe incluir todos los claims `permission` del catálogo (p. ej. content.edit, role.manage, etc.). Llamar a `GET /api/authorization/users/<userId>/effective-permissions` y a algún endpoint que requiera policy Admin (role.manage). **Esperado:** 200 en todos.
6. **Segundo usuario:** Registrar otro usuario. Identity llamará de nuevo a bootstrap-first-user, pero Authorization responderá **409** (ya hay reglas). El segundo usuario no tendrá el rol SuperAdmin; su JWT no incluirá permisos hasta que se le asigne un rol manualmente (vía POST rules).

---

## 2.6 Eliminar políticas por rol

En 2.4 se reemplazaron todas las políticas basadas en rol por políticas por permiso. No queda ningún `RequireRole("Admin")` ni `RequireRole("Editor", "Admin")` en las APIs:

- **Authorization.API:** policy "Admin" → `RequireClaim("permission", "role.manage")`
- **Publishing.API:** policy "Editor" → `RequireClaim("permission", "content.publish")`
- **Core.API / Indexing.API:** solo policies por permiso (`content.edit`, `project.edit`, etc.)

**Comprobación:** Buscar en el código `RequireRole` en los proyectos *API: no debe haber resultados. Las rutas protegidas exigen únicamente el claim `permission` con el código correspondiente.

---

## Fase 3: Limpieza

### 3.1 Eliminar POST /api/authorization/permissions

- El endpoint **POST** `/api/authorization/permissions` ha sido eliminado. No es posible crear permisos por API.
- Los permisos se crean únicamente por el seeder al arranque (catálogo en código). **Breaking:** El frontend no debe ofrecer creación de permisos desde la UI.

**Comprobación:** Llamar a `POST /api/authorization/permissions` con body `{ "code": "test", "description": "Test" }`. **Esperado:** 404 Not Found (ruta no existe).

### 3.2 GET /api/authorization/permissions filtrado por catálogo

- **GET** `/api/authorization/permissions` devuelve solo los permisos cuyo código está en el catálogo (`PermissionCatalog`). Si en BD existieran permisos creados antes (fuera del catálogo), no se incluyen en la respuesta.
- La respuesta está ordenada por código.

**Pasos para probar:**

1. Llamar a `GET /api/authorization/permissions` con JWT válido. **Esperado:** 200 y lista de permisos (los 19 del catálogo o los que el seeder haya insertado). Cada elemento tiene `id`, `code`, `description`.
2. Si la BD tuviera permisos con códigos no incluidos en el catálogo (p. ej. insertados a mano), no deben aparecer en la respuesta.
