# Tareas Backend — Bugfixs primer usuario, 403 en roles, GET /projects

Responsable: **Equipo Backend**.  
Referencia: [README.md](./README.md).

---

## Diagnóstico

- **JWT actual (payload):** solo `sub`, `email`, `jti`, `exp`, `iss`, `aud`. No hay claims `permission` ni `role`.
- **Identity** incluye permisos en el JWT solo si `AuthorizationApi:BaseUrl` está configurado; entonces usa `AuthorizationEffectivePermissionsClient` y llama a GET `/api/authorization/users/{userId}/effective-permissions`. Si `BaseUrl` está vacío, usa `NoOpEffectivePermissionsClient` → JWT sin permisos.
- **Bootstrap del primer usuario:** Identity llama a Authorization `POST bootstrap-first-user` tras registrar al primer usuario. Ese endpoint es `[AllowAnonymous]` pero exige `AllowEffectivePermissionsAccess()` (API key en header `X-Service-Api-Key` que coincida con `Authorization:ServiceApiKey` en Authorization API). Si Identity no envía la clave o no coincide, la llamada devuelve 401 y el primer usuario no recibe la regla SuperAdmin.
- **POST /api/authorization/roles** exige `[Authorize(Policy = "Admin")]` → `RequireClaim("permission", "role.manage")`. Sin ese claim en el JWT → **403**.
- **GET /api/projects** (Core) exige `[Authorize(Policy = "project.edit")]`. Sin claim `project.edit` → denegación. En ASP.NET Core la denegación por policy suele ser **403**; si se recibe **400**, revisar validadores de `GetProjectsPagedQuery` (p. ej. `page` / `pageSize`).

---

## Tareas Backend

### 1. Modo bootstrap en Authorization API (prioritario)

- [x] **1.1** Implementar una política o handler de autorización que permita acceso a los endpoints que hoy exigen "Admin" **cuando aún no existe ninguna regla de acceso** (tabla AccessRules vacía), para que el primer usuario pueda crear rol SuperAdmin, permisos y asignarse el rol desde el frontend aunque su JWT no tenga aún `role.manage`.
- [x] **1.2** Aplicar esta lógica a: POST /roles, POST /permissions, POST /roles/{roleId}/permissions, POST /rules. Criterio: si `IAccessRuleRepository.CountAsync() == 0` → autorizado; si no, exigir claim `permission` con valor `role.manage` como hasta ahora.
- [x] **1.3** Implementación sugerida: crear un `IAuthorizationHandler` (p. ej. `BootstrapOrPermissionHandler`) que, para una policy concreta "Admin", compruebe primero si hay 0 AccessRules y en ese caso marque el contexto como satisfecho; si no, delegar en `RequireClaim("permission", "role.manage")`. Registrar el handler y seguir usando `[Authorize(Policy = "Admin")]` en los mismos endpoints.

**Implementado:** `BootstrapOrAdminRequirement` + `BootstrapOrAdminHandler` en `IODA.Authorization.API/Authorization/`. La policy "Admin" usa ese requirement; el handler inyecta `IAccessRuleRepository`, si `CountAsync() == 0` autoriza; si no, exige claim `permission` = `role.manage`. POST /permissions fue eliminado en Fase 3 (solo catálogo); el resto de endpoints Admin usan la misma policy.
- **Riesgo:** Bajo. Solo permite crear roles/permisos/reglas cuando nadie tiene aún ninguna regla; tras la primera asignación, se exige el permiso como siempre.
- **Capa:** Authorization.API (handler) y Authorization.Application o Infrastructure (consulta de conteo de reglas si se hace desde un servicio).

### 2. Configuración Identity ↔ Authorization

- [ ] **2.1** Documentar en README o en appsettings.example que, para que el primer usuario reciba permisos en el JWT y el bootstrap funcione, debe configurarse:
  - **Identity:** `AuthorizationApi:BaseUrl` = URL base del Authorization API (ej. `http://localhost:5003`), y `AuthorizationApi:ServiceApiKey` = valor secreto compartido.
  - **Authorization:** `Authorization:ServiceApiKey` = el mismo valor. Así Identity puede llamar a `effective-permissions` y `bootstrap-first-user` con el header `X-Service-Api-Key`.
- [ ] **2.2** (Opcional) En Identity, si `AuthorizationApi:BaseUrl` está vacío en entorno no-Development, registrar un warning en el log al arrancar indicando que el JWT no incluirá permisos y el bootstrap del primer usuario no se ejecutará.
- **Capa:** Documentación; opcionalmente Identity API (startup).

### 3. GET /api/projects — 400 vs 403

- [ ] **3.1** Confirmar el código de estado real que devuelve Core API cuando el usuario no tiene el claim `project.edit`: debe ser **403 Forbidden** (fallo de autorización). Si en las pruebas se obtiene 403, no hay cambio en Core para este punto; el arreglo es que el JWT incluya permisos (tras bootstrap + refresco de token).
- [ ] **3.2** Si efectivamente se recibe **400** en GET /projects, revisar validadores de `GetProjectsPagedQuery` (p. ej. FluentValidation) y parámetros de ruta/query: asegurar que no se devuelva 400 por parámetros inválidos cuando el problema sea solo de permisos. Ajustar el validador para que errores de autorización no se traduzcan en 400.
- **Capa:** Core.API o Core.Application (validadores).

### 4. Seeders y bootstrap

- [ ] **4.1** Verificar que al arrancar Authorization API se ejecuten correctamente `PermissionSeeder` y `SuperAdminRoleSeeder` (ya están en Program.cs). Si la base está vacía, debe existir el rol "SuperAdmin" con todos los permisos del catálogo para que `BootstrapFirstUserCommandHandler` pueda asignar ese rol al primer usuario.
- **Capa:** Authorization.Infrastructure / API. Sin cambio de código si ya corre; solo verificación en el entorno donde falla.

---

## Orden recomendado

1. Implementar **modo bootstrap** (1.1–1.3) para que el primer usuario pueda usar la UI de roles/permisos y completar el setup aunque su JWT no tenga aún `role.manage`.
2. Documentar y revisar **configuración** (2.1–2.2) para entornos donde sí se quiera que Identity llame a Authorization (JWT con permisos y bootstrap automático).
3. Revisar **GET /projects** (3.1–3.2) según si el problema es 403 o 400.
4. Verificar **seeders** (4.1) en el entorno afectado.
