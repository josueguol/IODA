# 005 — Mejoras Frontend: Flujo SuperAdmin, Roles y UX

**Objetivo:** Corregir y mejorar la experiencia del primer usuario (SuperAdmin) y la gestión de roles/permisos/reglas en el frontend, alineando con la recomendación de `alcance-superadmin.md` y el flujo de instalación nueva descrito por el equipo.

---

## Flujo esperado (instalación nueva)

1. No hay usuarios → se redirige a `/register`.
2. Se crea el primer usuario → se le asigna automáticamente el rol SuperAdmin (backend o frontend).
3. SuperAdmin puede ver y crear de todo: proyectos, entornos, sitios, schemas, contenido, usuarios, todo.
4. Crea un proyecto → un entorno → un sitio → schemas → contenido.
5. Desde la sección de usuarios puede crear otros usuarios y asignarles roles.

---

## Problemas detectados (estado actual)

| # | Problema | Ubicación | Impacto |
|---|---------|-----------|---------|
| P1 | **Pestaña "Permisos" muestra "No hay permisos"** — el endpoint GET /permissions devuelve lista vacía o la llamada falla silenciosamente. | `RolesPermissionsPage` → `PermissionsTab` | El usuario ve una tabla vacía y no entiende por qué; además, en la pestaña Roles al "Asignar permisos" dice "No hay permisos en el sistema" y no se puede asignar nada. |
| P2 | **La pestaña Permisos no aporta valor al usuario final** — los permisos se gestionan en backend (catálogo). El usuario no puede crear ni editar permisos desde la UI. | `PermissionsTab` | Confusión: pantalla que muestra datos de solo lectura sin acción posible. |
| P3 | **El rol SuperAdmin se puede editar (asignar permisos manualmente)** — no debería permitirse cambiar los permisos de SuperAdmin desde la UI; ya tiene todos por seed. | `RolesTab` → botón "Asignar permisos" visible para SuperAdmin | Riesgo de que alguien quite permisos al SuperAdmin o lo modifique por error. |
| P4 | **El rol SuperAdmin aparece en la lista al crear reglas de acceso** — Al asignar un rol a otro usuario, SuperAdmin debería estar excluido o, al menos, advertir que es el rol de máximo privilegio. | `RulesTab` → select de roles; `UserRolesPanel` → select de roles | Un operador podría asignar SuperAdmin a cualquier usuario sin restricción. |
| P5 | **La regla SuperAdmin del primer usuario se puede revocar** — No hay protección contra revocar la última regla SuperAdmin, lo que dejaría el sistema sin admin. | `RulesTab` → botón "Revocar"; `UserRolesPanel` → botón ✕ | Sistema inoperante: nadie puede gestionar roles/permisos. |
| P6 | **Búsqueda de usuarios solo por GUID** — en "Reglas de acceso" el campo "ID del usuario" pide un GUID; no hay forma de buscar por correo o nombre. | `RulesTab` → input `lookupUserId` | Muy incómodo: el admin necesita copiar el GUID de otra pantalla. |
| P7 | **Al crear una regla de acceso, el campo de usuario es un input de GUID** — debería ser un buscador/selector de usuarios (por correo/nombre/ID). | `RulesTab` → formulario "Nueva regla de acceso"; `UserRolesPanel` ya resuelve esto parcialmente porque opera desde la tabla de usuarios. | Mala UX para administradores. |
| P8 | **GET /projects se llama sin `page` ni `pageSize`** — puede producir 400 si el backend valida que page >= 1 y el binding por defecto falla en algún entorno. | `context-store` → `loadProjects()` | Error "Parámetros de búsqueda no válidos" en la Home. |
| P9 | **Core API client hace logout en 403** — ante falta de permisos, el usuario es redirigido a login sin mensaje contextual. | `core-api.ts` → `onUnauthorized` | El SuperAdmin recién creado no ve proyectos y es deslogueado sin explicación. |

---

## Documentos del plan

| Archivo | Contenido |
|---------|-----------|
| [PLAN_ROLES_PERMISOS.md](./PLAN_ROLES_PERMISOS.md) | Plan paso a paso para la pantalla de Roles y Permisos (`RolesPermissionsPage`). |
| [PLAN_REGLAS_USUARIOS.md](./PLAN_REGLAS_USUARIOS.md) | Plan paso a paso para Reglas de acceso y búsqueda/selección de usuarios. |
| [PLAN_FLUJO_PRIMER_USUARIO.md](./PLAN_FLUJO_PRIMER_USUARIO.md) | Plan paso a paso para el flujo de instalación nueva (register → home → proyectos). |

---

## Referencia

- Recomendación: `docs/CONSULTORIA/recommendations/alcance-superadmin.md`
- FAQ roles: `docs/CONSULTORIA/faqs/roles-y-regla-acceso-todos-endpoints.md`
- Catálogo de permisos: `PermissionCatalog.cs` (19 permisos, incluido `role.manage`)
- Pantallas afectadas: `RolesPermissionsPage.tsx`, `UsersPage.tsx`, `RegisterPage.tsx`, `HomePage.tsx`, `context-store.ts`, `core-api.ts`
