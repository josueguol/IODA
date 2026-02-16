# 004 — Mejoras de comunicación Backend ↔ Frontend (SuperAdmin sin permisos la primera vez)

**Objetivo:** Documentar el análisis de por qué el SuperAdmin no tiene todos los permisos la primera vez, verificar el consumo correcto de endpoints por el frontend y definir tareas por rol para corregirlo.

---

## 1. Resumen del problema

- **Síntoma:** Tras crear el primer usuario (que debería ser SuperAdmin), no puede crear proyectos ni usar correctamente la sección de roles (p. ej. "Crear rol" devuelve 403 y en algunos flujos desloguea). En la pantalla "Acceso al CMS" aparece el error "Parámetros de búsqueda no válidos" (asociado a GET /projects).
- **Causa raíz:** El JWT que recibe el usuario **no incluye claims de permisos** (`permission`). Sin esos claims, Core API (policy `project.edit`) y Authorization API (policy `Admin` = `role.manage`) deniegan con 403. El 400 en GET /projects puede ser adicional (parámetros de paginación) o confusión en la UI con otro código de error.

---

## 2. Análisis del flujo Backend

### 2.1 Identity API

| Momento | Comportamiento | Condición |
|--------|----------------|-----------|
| **Registro (primer usuario)** | Tras crear el usuario, Identity llama a `IFirstUserBootstrapClient.BootstrapFirstUserAsync(user.Id)`. | Siempre si es el primer usuario (`!hasUsers`). |
| **Login** | Obtiene permisos efectivos con `IEffectivePermissionsClient.GetEffectivePermissionsAsync(user.Id)` e incluye esos códigos en el JWT como claims `permission`. | Solo si está configurado el cliente real (ver abajo). |
| **Refresh** | Igual que login: llama a effective-permissions y genera un nuevo access token con esos permisos. | Misma condición. |

**Configuración crítica (Identity):**

- `AuthorizationApi:BaseUrl`: URL base del Authorization API (ej. `http://localhost:5003`). Si está **vacío**, Identity registra:
  - `IEffectivePermissionsClient` → **NoOpEffectivePermissionsClient** (devuelve lista vacía).
  - `IFirstUserBootstrapClient` → **NoOpFirstUserBootstrapClient** (no hace nada).
- `AuthorizationApi:ServiceApiKey`: Clave que Identity envía en el header `X-Service-Api-Key` al llamar a Authorization. Debe coincidir con la configurada en Authorization.

**Consecuencia:** Si `AuthorizationApi:BaseUrl` está vacío (como en `appsettings.json`: `"BaseUrl": ""`), el primer usuario **nunca** recibe la regla SuperAdmin por parte del backend y el JWT **nunca** incluye permisos. El frontend depende entonces de su propio `setupSuperAdmin` y del refresh posterior.

### 2.2 Authorization API

| Endpoint | Propósito | Autenticación | Uso desde Identity / Frontend |
|----------|-----------|---------------|---------------------------------|
| `GET api/authorization/users/{userId}/effective-permissions` | Devuelve códigos de permisos efectivos del usuario (unión de roles). | JWT o `X-Service-Api-Key` (mismo valor que `Authorization:ServiceApiKey`). | Identity lo usa en login/refresh para rellenar el JWT. |
| `POST api/authorization/bootstrap-first-user` | Crea la regla de acceso SuperAdmin para el usuario. Solo si `AccessRuleRepository.CountAsync() == 0`. | JWT o `X-Service-Api-Key`. | Identity lo llama tras registrar al primer usuario. |
| `GET api/authorization/permissions` | Lista permisos del catálogo. | JWT (usuario autenticado). | Frontend para setup SuperAdmin (obtener IDs y asignar al rol). |
| `POST api/authorization/roles` | Crear rol. | Policy `Admin` (claim `permission` = `role.manage` o modo bootstrap si 0 reglas). | Frontend en setupSuperAdmin. |
| `POST api/authorization/rules` | Crear regla de acceso usuario–rol. | Policy `Admin`. | Frontend en setupSuperAdmin. |

**Bootstrap del primer usuario (Authorization):**

- `BootstrapFirstUserCommandHandler` comprueba: (1) que no exista ninguna regla (`CountAsync() == 0`); (2) que exista el rol "SuperAdmin" (creado por `SuperAdminRoleSeeder`). Si se cumple, crea `AccessRule(userId, superAdminRoleId)`.
- Los seeders `PermissionSeeder` y `SuperAdminRoleSeeder` se ejecutan al arrancar la Authorization API (Program.cs). Si la BD está vacía o recién migrada, deben existir permisos del catálogo y el rol SuperAdmin con todos esos permisos asignados.

**Posibles fallos:**

- Identity no tiene `AuthorizationApi:BaseUrl` → no llama a bootstrap ni a effective-permissions → JWT sin permisos.
- Identity tiene BaseUrl pero `ServiceApiKey` no coincide con `Authorization:ServiceApiKey` → bootstrap-first-user y effective-permissions devuelven **401** → Identity usa lista vacía de permisos (y no crea regla).
- Authorization no ha ejecutado seeders o la BD no tiene el rol SuperAdmin → bootstrap-first-user devuelve conflicto ("SuperAdmin role not found").

### 2.3 Core API

| Endpoint | Autorización | Comportamiento |
|----------|--------------|----------------|
| `GET api/projects` | Policy `project.edit` → `RequireClaim("permission", "project.edit")`. | Sin claim → **403 Forbidden**. |
| | Query: `page`, `pageSize`. Validador: `Page >= 1`, `PageSize` entre 1 y 100. | Si no se cumplen → **400 Bad Request** (FluentValidation). |

El controlador usa `[FromQuery] int page = 1, [FromQuery] int pageSize = 20`. Si el cliente no envía query, en principio se usan esos valores por defecto; en algunos entornos o clientes, la ausencia de query podría bindear a 0 y disparar el validador (400). Por tanto, puede haber tanto 403 (sin permisos) como 400 (parámetros inválidos) según el caso.

---

## 3. Análisis del consumo Frontend

### 3.1 Flujo del primer usuario (RegisterPage)

1. **Register** → `POST api/auth/register` (Identity). Respuesta incluye `isFirstUser`, `userId`.
2. Si `isFirstUser`: **Login** → `POST api/auth/login`. El frontend guarda el resultado en el store (JWT sin permisos si Identity no llamó a Authorization).
3. **setupSuperAdmin(userId):**
   - Opcionalmente comprueba `getUserRules(userId)`; si ya hay reglas, no hace nada (asume que el backend ya hizo bootstrap).
   - `getPermissions()` → GET api/authorization/permissions.
   - `createRole({ name: 'SuperAdmin', ... })` → POST api/authorization/roles (puede devolver 403 si el JWT no tiene `role.manage` y el backend no está en modo bootstrap).
   - `assignPermissionsToRole(roleId, { permissionIds })` → POST api/authorization/roles/{id}/permissions.
   - `createAccessRule({ userId, roleId })` → POST api/authorization/rules.
4. **refreshSession()** → `POST api/auth/refresh`. El frontend espera recibir un nuevo JWT con permisos (si Identity tiene configurado Authorization y la regla ya existe).

**Puntos débiles:**

- Si Identity no tiene BaseUrl: el JWT del paso 2 y el del paso 4 no llevan permisos. El paso 3 puede fallar en POST /roles o POST /rules si el backend exige Admin y no hay modo bootstrap (0 reglas).
- Si el refresh falla (p. ej. Authorization no disponible en ese momento), el usuario se queda con el JWT antiguo sin permisos y las siguientes peticiones (GET /projects, crear rol, etc.) reciben 403.
- El frontend no envía explícitamente `page` y `pageSize` en GET /projects (ver abajo).

### 3.2 Consumo de GET /api/projects

- **context-store:** `loadProjects()` llama a `coreApi.getProjects()` **sin argumentos**.
- **core-api:** `getProjects(params?)` construye la URL así: si `params?.page` y `params?.pageSize` son `null`/`undefined`, no añade query → petición a `api/projects` **sin query string**.
- En el backend, los valores por defecto del controlador deberían aplicar (page=1, pageSize=20), pero si en algún cliente o proxy se envía `page=0` o se interpreta la ausencia como 0, el validador devuelve 400 → la UI muestra "Parámetros de búsqueda no válidos".
- **Conclusión:** El frontend no está enviando de forma explícita `page=1` y `pageSize=20`, lo que puede contribuir a 400 en algunos escenarios. Además, si el problema principal es 403 (sin permisos), el mensaje mostrado puede no coincidir si hay confusión entre 400 y 403 en la capa de presentación.

### 3.3 Cliente Core API y 403

- **core-api** usa `createAuthAwareHttpClient` con `onUnauthorized` que, ante **401 o 403**, hace logout y redirección a login. Por tanto, cuando GET /projects devuelve 403, el usuario es deslogueado y redirigido sin ver un mensaje claro de "falta de permiso" en la página de Acceso al CMS.

### 3.4 Cliente Authorization API

- En la implementación actual (según 003-BUGFIXS), el cliente de Authorization puede estar configurado para no desloguear en 403 y solo hacerlo en 401, dejando que la UI muestre el error. Conviene verificar que ese comportamiento esté alineado con el de Core (mostrar mensaje en 403 en lugar de desloguear siempre).

---

## 4. Correlación endpoint ↔ frontend

| Backend endpoint | Consumido por frontend | Observación |
|------------------|------------------------|-------------|
| POST api/auth/register | authApi.register | Correcto. |
| POST api/auth/login | authApi.login | Correcto. |
| POST api/auth/refresh | refreshSession → authApi.refresh | Correcto; el token devuelto debe incluir permisos si Identity tiene Authorization configurado. |
| GET api/authorization/users/{userId}/effective-permissions | No lo llama el frontend (solo Identity) | Correcto. |
| POST api/authorization/bootstrap-first-user | No lo llama el frontend (solo Identity) | Correcto. |
| GET api/authorization/permissions | authorizationApi.getPermissions() | Correcto. |
| GET api/authorization/users/{userId}/rules | authorizationApi.getUserRules(userId) | Correcto. |
| POST api/authorization/roles | authorizationApi.createRole | Correcto; puede devolver 403 si no hay modo bootstrap y JWT sin role.manage. |
| POST api/authorization/roles/{id}/permissions | authorizationApi.assignPermissionsToRole | Correcto. |
| POST api/authorization/rules | authorizationApi.createAccessRule | Correcto. |
| GET api/projects | coreApi.getProjects() | Falta enviar siempre `page` y `pageSize` para evitar 400; además 403 provoca logout sin mensaje contextual. |

---

## 5. Tareas por rol

Las tareas concretas para Backend, Frontend y Config/DevOps se detallan en:

- **[BACKEND.md](./BACKEND.md)** — Identity, Authorization, Core.
- **[FRONTEND.md](./FRONTEND.md)** — Flujo primer usuario, clientes HTTP, mensajes de error y GET /projects.
- **[CONFIG.md](./CONFIG.md)** — Configuración Identity ↔ Authorization y comprobaciones de entorno.
