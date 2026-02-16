# Plan: Reglas de acceso y selección de usuarios

Referencia: [README.md](./README.md) — Problemas P6, P7.

---

## Estado actual

### Pestaña "Reglas de acceso" (`RulesTab` en `RolesPermissionsPage`)

- Campo "Usuario ID" → input de texto libre donde se pide un GUID.
- Botón "Buscar reglas" → carga reglas del usuario por GUID.
- Formulario "Nueva regla de acceso" → campo "ID del usuario" también es un input de GUID.
- Select de roles lista todos los roles (incluido SuperAdmin).

### Página "Usuarios" (`UsersPage`)

- Lista de usuarios (GET /api/auth/users) con columnas: Email, Nombre, Estado, Creado, Acciones.
- Botón "Gestionar roles" → abre `UserRolesPanel` inline con la lista de reglas del usuario y formulario de asignar rol (selecciona rol, proyecto, entorno).
- La asignación de roles aquí ya se hace en contexto del usuario seleccionado (no necesita campo de GUID).

---

## Análisis

La página de **Usuarios** ya tiene un flujo más natural: se ve la lista, se selecciona un usuario y se le asigna un rol. El problema principal está en la pestaña **Reglas de acceso** de `RolesPermissionsPage`, que exige un GUID y no tiene forma de buscar usuarios por correo o nombre.

Hay dos enfoques posibles:

- **Enfoque A:** Mejorar `RulesTab` para incluir un buscador/selector de usuarios (requiere endpoint de búsqueda o cargar lista de usuarios).
- **Enfoque B:** Reconocer que la gestión de reglas ya se hace mejor desde la página de Usuarios (donde se ve la lista) y simplificar `RulesTab` para que sea solo consulta/auditoría, redirigiendo la creación a la página de Usuarios.

**Recomendación:** Enfoque mixto — Mejorar la búsqueda de usuarios en `RulesTab` pero mantener la página de Usuarios como punto principal de gestión de roles.

---

## Plan paso a paso

### Paso 1 — Agregar búsqueda de usuarios por correo/nombre en `RulesTab` ✅

**Qué:** Reemplazar el input "Usuario ID" (GUID) por un campo de búsqueda que permita buscar por email, nombre o ID.

**Por qué:** Ningún administrador sabe de memoria los GUIDs. Buscar por correo o nombre es lo natural.

**Cómo:**
1. Al cargar la pestaña, obtener la lista de usuarios con `identityAdminApi.getUsers()` (ya se usa en `UsersPage`).
2. Reemplazar el input de texto libre por un campo con autocompletado (puede ser un `<input>` con un `<datalist>` o un dropdown filtrable):
   - El usuario escribe correo, nombre o ID parcial.
   - Se filtra la lista en el cliente y se muestran las coincidencias.
   - Al seleccionar, se guarda el `userId` (GUID) internamente.
3. Mostrar junto al campo el nombre/email del usuario seleccionado para confirmar visualmente.
4. Mantener la opción de pegar un GUID directamente (para casos avanzados).

**Archivos:** `RolesPermissionsPage.tsx` → `RulesTab`. Importar `identityAdminApi`.

**Dependencia:** El endpoint GET /api/auth/users debe estar accesible (requiere permiso `user.list`).

---

### Paso 2 — Selector de usuario en el formulario "Nueva regla de acceso" ✅

**Qué:** El campo "ID del usuario" en el formulario de creación de regla debe ser un selector/buscador igual al del paso 1.

**Por qué:** Misma razón: no pedir GUIDs al admin.

**Cómo:**
1. Reutilizar el componente o lógica del paso 1.
2. En el formulario, al seleccionar un usuario, mostrar su email y nombre.
3. El valor enviado al backend (`ruleUserId`) sigue siendo el GUID, pero el admin lo seleccionó por nombre/correo.

**Archivos:** `RolesPermissionsPage.tsx` → `RulesTab` (formulario de creación).

---

### Paso 3 — (Opcional) Componente reutilizable `UserPicker` ✅

**Qué:** Extraer la lógica de búsqueda/selección de usuarios en un componente compartido que pueda usarse en `RulesTab`, `UserRolesPanel` (si se necesita en futuro), y cualquier otra pantalla.

**Por qué:** Evita duplicar código y estandariza la UX de selección de usuarios.

**Cómo:**
1. Crear componente `UserPicker` (o `UserSearchInput`) que reciba:
   - `users: UserListItemDto[]` — lista cargada.
   - `value: string` — userId seleccionado.
   - `onChange: (userId: string) => void`.
   - `placeholder?: string`.
2. Internamente: input con filtro + dropdown de resultados. Muestra email + displayName.
3. Usarlo en `RulesTab` y, si se desea, en el campo de búsqueda de reglas.

**Archivos:** Nuevo componente en `frontend/src/shared/components/` o `frontend/src/modules/authorization/components/`. Consumir en `RolesPermissionsPage.tsx`.

---

### Paso 4 — Filtrar SuperAdmin en selects de roles (repetido de PLAN_ROLES_PERMISOS) ✅

**Qué:** En los selectores de rol de `RulesTab` y `UserRolesPanel`, excluir SuperAdmin.

(Ver [PLAN_ROLES_PERMISOS.md](./PLAN_ROLES_PERMISOS.md) — Paso 3. Ya implementado.)

---

### Paso 5 — Mejorar la tabla de reglas: mostrar email/nombre en vez de solo userId ✅

**Qué:** En la tabla de reglas actuales (pestaña Reglas de acceso), mostrar el email o nombre del usuario junto al rol, en lugar de solo el GUID.

**Por qué:** Si el admin busca reglas de un usuario, la tabla debería confirmar a quién pertenecen de forma legible.

**Cómo:**
1. Al cargar reglas, si se tiene la lista de usuarios (paso 1), cruzar `rule.userId` con la lista para mostrar email/nombre.
2. Si no se tiene la lista (permisos insuficientes), mostrar el GUID como fallback.

**Archivos:** `RolesPermissionsPage.tsx` → `RulesTab`.

---

## Resumen de pasos

| Paso | Acción | Prioridad | Estado |
|------|--------|-----------|--------|
| 1 | Búsqueda de usuarios por correo/nombre en RulesTab | Alta | ✅ |
| 2 | Selector de usuario en formulario de nueva regla | Alta | ✅ |
| 3 | Componente reutilizable UserPicker (opcional) | Media | ✅ |
| 4 | Filtrar SuperAdmin en selects de roles | Alta (ver PLAN_ROLES_PERMISOS) | ✅ |
| 5 | Mostrar email/nombre en tabla de reglas | Media | ✅ |
