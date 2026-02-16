# Tareas Backend — 004 Mejoras comunicación (SuperAdmin sin permisos la primera vez)

Responsable: **Equipo Backend**.  
Referencia: [README.md](./README.md).

---

## Identity API

### 1. Configuración y clientes Authorization

- [x] **1.1** Documentar en README o guía de despliegue que, para que el primer usuario reciba permisos en el JWT y se ejecute el bootstrap: `AuthorizationApi:BaseUrl` debe ser la URL base del Authorization API (ej. `http://localhost:5003`); `AuthorizationApi:ServiceApiKey` debe coincidir con `Authorization:ServiceApiKey` en la Authorization API.
  > Ya documentado en `docs/003-BUGFIXS/README.md` (sección "Configuración Identity ↔ Authorization") y en `docs/004-MEJORAS_COMUNICACION/CONFIG.md`.
- [x] **1.2** Mantener o reforzar el warning al arranque cuando `AuthorizationApi:BaseUrl` está vacío (p. ej. en no-Development), indicando que el JWT no incluirá permisos y que el bootstrap del primer usuario no se ejecutará.
  > Reforzado: el warning ahora se emite en **todos** los entornos (no solo no-Development) y el mensaje incluye instrucciones sobre `AuthorizationApi:ServiceApiKey`. Ver `Identity.API/Program.cs`.
- [x] **1.3** (Opcional) Si bootstrap-first-user o effective-permissions fallan (no 2xx), registrar en logs el status code para facilitar diagnóstico.
  > Mejorado: los clientes `AuthorizationEffectivePermissionsClient` y `AuthorizationBootstrapFirstUserClient` ahora registran tanto el **status code** como el **body** de la respuesta en caso de fallo.

### 2. Orden de operaciones en registro

- [x] **2.1** Verificar que el registro del primer usuario es síncrono: primero se persiste el usuario, luego se llama a BootstrapFirstUserAsync y solo después se devuelve la respuesta. Así, cuando el frontend haga login inmediatamente después, la regla ya debería existir en Authorization (si Identity tiene BaseUrl y API key correctos).
  > Verificado en `RegisterCommandHandler.Handle()`: `await _userRepository.AddAsync(user)` → `await _firstUserBootstrapClient.BootstrapFirstUserAsync(user.Id)` → `return new RegisterResultDto(...)`. El flujo es totalmente síncrono.
- [x] **2.2** No cambiar el orden actual sin documentar el impacto en el frontend.
  > Confirmado: no se ha modificado el orden. El frontend asume que tras recibir la respuesta del registro, la regla SuperAdmin ya existe (si el backend está configurado correctamente).

---

## Authorization API

### 3. Endpoints consumidos por Identity

- [x] **3.1** Confirmar que GET effective-permissions está en `{BaseUrl}/api/authorization/users/{userId}/effective-permissions` y que Identity usa la misma BaseAddress + path.
  > Confirmado. Controller: `[HttpGet("users/{userId:guid}/effective-permissions")]` bajo `[Route("api/authorization")]`. Identity: BaseAddress = `{BaseUrl}/api/authorization/`, path relativo = `users/{userId}/effective-permissions`. Coinciden.
- [x] **3.2** Confirmar que POST bootstrap-first-user acepta cuerpo con `userId` (guid) y que Identity envía ese formato.
  > Confirmado. Controller: `[FromBody] BootstrapFirstUserRequest request` con `record BootstrapFirstUserRequest(Guid UserId)`. Identity envía: `PostAsJsonAsync("bootstrap-first-user", new { UserId = userId })`. Coinciden.
- [x] **3.3** Documentar que ambos endpoints exigen JWT válido o header X-Service-Api-Key igual a Authorization:ServiceApiKey. Si Identity no envía la clave o no coincide, devuelven 401.
  > Documentado. Ambos endpoints usan `[AllowAnonymous]` + `AllowEffectivePermissionsAccess()` que valida `X-Service-Api-Key` contra `Authorization:ServiceApiKey` o JWT autenticado. Si ninguno es válido → 401.

### 4. Seeders y bootstrap

- [x] **4.1** Verificar que al arrancar la Authorization API se ejecutan en orden: PermissionSeeder.SeedAsync(), SuperAdminRoleSeeder.SeedAsync(). Sin esto, bootstrap-first-user devolverá conflicto (SuperAdmin role not found).
  > Verificado en `Authorization.API/Program.cs`: primero `PermissionSeeder.SeedAsync()`, luego `SuperAdminRoleSeeder.SeedAsync()`, ambos con scopes independientes.
- [x] **4.2** En entornos donde falle el primer usuario, comprobar en logs si bootstrap-first-user fue llamado y qué status devolvió (201 vs 401 vs 409).
  > Los logs ya están implementados en `AuthorizationBootstrapFirstUserClient`: LogInformation en éxito, LogWarning con status code y body en fallo, LogWarning con excepción si hay error de conexión.

### 5. Política Admin y modo bootstrap

- [x] **5.1** Si está implementado el modo bootstrap (acceso Admin cuando no hay ninguna AccessRule), documentarlo. POST /roles, POST /rules deben estar permitidos sin claim role.manage cuando CountAsync() == 0.
  > Implementado y documentado. Archivos: `BootstrapOrAdminRequirement.cs` (define el requirement), `BootstrapOrAdminHandler.cs` (handler que verifica `CountAsync() == 0` para bootstrap, o claim `permission=role.manage` para operación normal). La política "Admin" en Program.cs usa este requirement. POST /roles, POST /rules y otros endpoints `[Authorize(Policy = "Admin")]` permiten acceso cuando no hay ninguna AccessRule.
- [x] **5.2** Si no está implementado, definir tarea de implementación (handler que permita Admin cuando no hay reglas) para alinear con el flujo del primer usuario en frontend.
  > N/A — Ya implementado en 5.1.

---

## Core API

### 6. GET /api/projects

- [x] **6.1** Dejar claro en documentación que sin claim permission = project.edit se devuelve 403 Forbidden (no 400).
  > Documentado en el XML doc del controlador: `/// <summary>Listar proyectos (paginado). Requiere permiso project.edit. Sin permiso → 403 Forbidden.</summary>` y atributo `[ProducesResponseType(StatusCodes.Status403Forbidden)]`.
- [x] **6.2** Revisar el validador de GetProjectsPagedQuery (Page >= 1, PageSize 1-100). Valorar valores por defecto defensivos en el controlador (si page < 1 usar 1, si pageSize fuera de rango usar 20) para evitar 400 cuando el cliente no envía query.
  > Implementado. El controlador ahora aplica **clamping defensivo** antes de enviar al MediatR: `if (page < 1) page = 1; if (pageSize < 1 || pageSize > 100) pageSize = 20;`. Los defaults de ASP.NET (`page = 1, pageSize = 20`) cubren el caso normal; el clamping cubre edge cases. El validador de FluentValidation se mantiene como segunda red de seguridad.
- [x] **6.3** No devolver 400 por motivos de autorización; reservar 400 para validación de entrada (paginación).
  > Correcto por diseño: la autorización (policy `project.edit` → `RequireClaim`) se evalúa **antes** de la ejecución del action; si falta el claim, ASP.NET devuelve 403 antes de llegar al validador. El 400 solo ocurre por parámetros de paginación inválidos (ahora mitigado con clamping defensivo).

---

## Resumen de prioridad

1. **Alta:** Configuración Identity y Authorization (1.1, 1.2) y verificación de seeders (4.1). ✅
2. **Media:** Documentación de rutas y API key (3.1-3.3, 6.1), y comportamiento 403 vs 400 en GET /projects (6.2, 6.3). ✅
3. **Según diseño:** Modo bootstrap en Authorization (5.1, 5.2) y logs de diagnóstico (1.3, 4.2). ✅
