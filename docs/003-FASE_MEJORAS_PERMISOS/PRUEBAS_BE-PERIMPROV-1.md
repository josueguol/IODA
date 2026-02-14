# Pruebas BE-PERIMPROV-1 — Fase 1 permisos centralizados

Rama: `feature/BE_PERIMPROV-1/change-permission-rol-policy`

---

## 1.1 Catálogo de permisos en código

El catálogo es **solo uso interno** (no se expone como API). Para validar el punto 1.1:

1. **Compilación:** El proyecto `IODA.Authorization.Application` debe compilar sin errores.
   ```bash
   dotnet build src/Services/Authorization/IODA.Authorization.Application/IODA.Authorization.Application.csproj
   ```

2. **Contenido del catálogo:** En `PermissionCatalog.All` deben existir exactamente estos 19 códigos (y sus descripciones):
   - content.create, content.edit, content.delete, content.publish
   - project.create, project.edit, project.delete
   - environment.create, environment.edit, environment.delete
   - site.create, site.edit, site.delete
   - schema.create, schema.edit, schema.delete
   - user.list, user.create
   - role.manage

3. **Uso interno:** Se puede comprobar desde código que `PermissionCatalog.IsInCatalog("role.manage")` devuelve `true` y `PermissionCatalog.IsInCatalog("invalid")` devuelve `false` (las tareas 1.2 y 1.3 usarán este catálogo).

No hay endpoints nuevos que probar; el catálogo se usará en el seeder (1.2) y en la validación de asignación (1.3).

---

## 1.2 Seeder de permisos

Al arrancar la Authorization API, si la tabla `permissions` está vacía o faltan códigos del catálogo, se insertan todos. El seeder es idempotente por código (no duplica).

1. **Requisitos:** Base de datos `ioda_authorization` creada y migraciones aplicadas. Connection string y JWT configurados (User Secrets o appsettings) para poder arrancar la API.

2. **Primer arranque (tabla vacía o sin los 19 códigos):**
   ```bash
   cd src/Services/Authorization/IODA.Authorization.API
   dotnet run
   ```
   La API debe arrancar sin error. Llamar a:
   ```bash
   curl -s -X GET https://localhost:<PORT>/api/authorization/permissions -H "Authorization: Bearer <JWT>"
   ```
   **Esperado:** Respuesta 200 con una lista que incluya al menos los 19 permisos del catálogo (content.create, content.edit, …, role.manage). Cada elemento debe tener `id`, `code` y `description`.

3. **Idempotencia:** Reiniciar la API y volver a llamar a `GET /api/authorization/permissions`. El número de permisos no debe aumentar (no se crean duplicados por código).

4. **BD con permisos existentes:** Si la BD ya tenía permisos (p. ej. creados antes por el frontend), al arrancar la API el seeder solo inserta los códigos del catálogo que **faltan**. No debe fallar ni borrar permisos existentes.

---

## 1.3 Validar permisos asignados al rol

Al asignar permisos a un rol (`POST /api/authorization/roles/{roleId}/permissions`), cada `PermissionId` debe existir en BD y su `Code` debe estar en el catálogo. Si no, la API responde **400 Bad Request** con mensaje claro.

1. **Asignación válida:** Crear un rol, obtener IDs de permisos de `GET /api/authorization/permissions` (los del catálogo insertados por 1.2). Llamar a:
   ```bash
   curl -i -X POST https://localhost:<PORT>/api/authorization/roles/<ROLE_ID>/permissions \
     -H "Authorization: Bearer <JWT>" -H "Content-Type: application/json" \
     -d '{"permissionIds": ["<ID1>","<ID2>"]}'
   ```
   **Esperado:** 204 No Content.

2. **ID de permiso inexistente:** Enviar un `permissionIds` que contenga un Guid que no exista en la tabla `permissions`.
   **Esperado:** 400 Bad Request con mensaje del tipo "Permission with id '...' was not found."

3. **Permiso no perteneciente al catálogo:** Si en BD existe un permiso creado fuera del catálogo (p. ej. código "custom.permission" insertado a mano), usar su ID en la asignación.
   **Esperado:** 400 Bad Request con mensaje del tipo "Permission 'custom.permission' (...) is not in the system catalog and cannot be assigned to roles."
