# Plan: Pantalla de Roles y Permisos (`RolesPermissionsPage`)

Referencia: [README.md](./README.md) — Problemas P1, P2, P3, P4.

---

## Estado actual

- Tres pestañas: **Permisos**, **Roles**, **Reglas de acceso**.
- Pestaña Permisos: tabla de solo lectura (GET /permissions). Actualmente muestra "No hay permisos" porque la llamada falla o la Authorization API no devuelve datos (posiblemente por 403 o seeders no ejecutados).
- Pestaña Roles: lista todos los roles (incluido SuperAdmin) con botón "Asignar permisos" para todos.
- Pestaña Reglas de acceso: búsqueda por GUID, formulario con select de roles (incluido SuperAdmin), botón "Revocar" sin restricciones.

---

## Plan paso a paso

### Paso 1 — Eliminar la pestaña "Permisos" de la vista principal ✅

**Qué:** Quitar la pestaña "Permisos" de `RolesPermissionsPage`. Los permisos se gestionan en backend y el usuario final no necesita verlos en una pestaña dedicada.

**Por qué:** No aporta valor. Los permisos son del catálogo (backend) y no se pueden crear ni editar desde la UI. Mostrar una tabla vacía o de solo lectura confunde al usuario.

**Cómo:**
1. En `RolesPermissionsPage`, eliminar el componente `PermissionsTab` y su botón de pestaña.
2. Que el tab por defecto sea `'roles'` en lugar de `'permissions'`.
3. El componente `PermissionsTab` puede conservarse en el código (por si se necesita para diagnóstico futuro) pero no renderizarse en la navegación principal.

**Archivos:** `RolesPermissionsPage.tsx`.

---

### Paso 2 — Proteger el rol SuperAdmin contra edición ✅

**Qué:** En la pestaña Roles, el rol SuperAdmin no debe mostrar el botón "Asignar permisos". Opcionalmente, marcarlo visualmente como rol del sistema (badge, icono o texto).

**Por qué:** SuperAdmin ya tiene todos los permisos por seed. Permitir reasignarle permisos manualmente es peligroso (podrían quitarle alguno por error) y no tiene sentido operativo.

**Cómo:**
1. Definir una constante `SUPERADMIN_ROLE_NAME = 'SuperAdmin'`.
2. En el `map` de roles dentro de `RolesTab`, si `r.name === SUPERADMIN_ROLE_NAME`:
   - No mostrar el botón "Asignar permisos".
   - Mostrar un badge/texto como "Rol del sistema — todos los permisos" en la columna Acciones.
3. No impedir que SuperAdmin aparezca en la tabla de roles (el admin debe poder verlo), solo bloquear la edición.

**Archivos:** `RolesPermissionsPage.tsx` → `RolesTab`.

---

### Paso 3 — Excluir SuperAdmin del selector de roles al asignar reglas ✅

**Qué:** En el formulario "Nueva regla de acceso" (pestaña Reglas de acceso) y en `UserRolesPanel` (página Usuarios), el select de roles no debe incluir SuperAdmin.

**Por qué:** SuperAdmin es un rol de máximo privilegio que solo debería asignarse automáticamente al primer usuario (vía bootstrap). Permitir asignarlo libremente a cualquier usuario diluye el control de seguridad.

**Cómo:**
1. En `RulesTab`, al rellenar el `<select>` de roles, filtrar: `roles.filter(r => r.name !== SUPERADMIN_ROLE_NAME)`.
2. En `UserRolesPanel` (dentro de `UsersPage.tsx`), aplicar el mismo filtro al `<select>` de roles.
3. Documentar en un comentario que SuperAdmin se asigna solo por bootstrap (backend) o por el flujo de primer usuario.

**Archivos:** `RolesPermissionsPage.tsx` → `RulesTab`, `UsersPage.tsx` → `UserRolesPanel`.

---

### Paso 4 — Impedir revocar la regla SuperAdmin del primer usuario ✅

**Qué:** Si una regla de acceso es del rol SuperAdmin, el botón "Revocar" debe estar deshabilitado o no mostrarse (con un tooltip explicativo).

**Por qué:** Si se revoca la última regla SuperAdmin, el sistema queda sin admin y nadie puede gestionar roles/permisos (salvo acceder a la BD directamente).

**Cómo:**
1. En `RulesTab`, al renderizar cada regla, comprobar si `roleName(rule.roleId) === SUPERADMIN_ROLE_NAME`. Si es así:
   - Deshabilitar el botón "Revocar" (`disabled`).
   - Agregar tooltip: "No se puede revocar el rol SuperAdmin".
2. En `UserRolesPanel`, misma lógica con el botón ✕ de cada tag de rol.
3. (Opcional) Validación adicional en backend: no permitir DELETE /rules si es la última regla con rol SuperAdmin. Esto es tarea de backend, no de este plan frontend, pero conviene documentarlo como recomendación.

**Archivos:** `RolesPermissionsPage.tsx` → `RulesTab`, `UsersPage.tsx` → `UserRolesPanel`.

---

### Paso 5 — Mover los permisos al panel de asignación (contexto del rol) ✅

**Qué:** Los permisos del catálogo deben seguir cargándose (GET /permissions) pero mostrarse solo en el contexto donde se necesitan: el panel de "Asignar permisos" de un rol. No como pestaña independiente.

**Por qué:** El usuario necesita ver los permisos disponibles solo cuando va a asignarlos a un rol. No necesita una tabla dedicada de permisos.

**Cómo:**
1. Mantener la llamada a `authorizationApi.getPermissions()` dentro de `RolesTab` (ya existe en el `load`).
2. Cuando se pulse "Asignar permisos" en un rol (distinto de SuperAdmin), mostrar el panel con checkboxes de permisos como hoy.
3. Si la lista de permisos viene vacía, mostrar un mensaje: "No se pudieron cargar los permisos del sistema. Verifica que la Authorization API está ejecutándose y los seeders se han completado."

**Archivos:** `RolesPermissionsPage.tsx` → `RolesTab`.

---

## Resumen de pasos

| Paso | Acción | Prioridad | Estado |
|------|--------|-----------|--------|
| 1 | Eliminar pestaña Permisos de la vista principal | Alta | ✅ |
| 2 | Bloquear edición del rol SuperAdmin | Alta | ✅ |
| 3 | Excluir SuperAdmin del selector de roles en reglas | Alta | ✅ |
| 4 | Impedir revocar regla SuperAdmin | Alta | ✅ |
| 5 | Permisos solo en contexto de asignación a rol | Media | ✅ |
