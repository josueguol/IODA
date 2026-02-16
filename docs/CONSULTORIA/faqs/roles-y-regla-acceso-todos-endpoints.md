# FAQ: Roles existentes y regla con acceso a todos los endpoints

**Preguntas:** ¿Cuáles son los roles existentes? ¿Cuál es la regla que tiene acceso a todos los endpoints?

---

## 1. Roles existentes

El sistema distingue entre **roles como entidades** (tabla en Authorization) y **qué se usa en el JWT**.

### Roles creados por el sistema (seed)

- **SuperAdmin** — Único rol creado por código (seeder). Se crea al arrancar la API de Authorization y se le asignan **todos los permisos del catálogo** (PermissionCatalog). Se asigna al **primer usuario** registrado cuando aún no existe ninguna regla de acceso (bootstrap).

### Otros roles

- Cualquier otro rol (**Editor**, **Admin**, **Publisher**, etc.) **solo existe si se creó** vía API (`POST /api/authorization/roles`) y se le asignaron permisos. No hay seed de “Admin” ni “Editor” por nombre.

En resumen: el único rol **garantizado por diseño** es **SuperAdmin**. El resto depende de la configuración en base de datos.

---

## 2. Qué da acceso a “todos” los endpoints

Las APIs no comprueban un “rol con acceso total”, sino **permisos** (claims en el JWT) o **modo bootstrap**:

| API / zona | Condición de acceso |
|------------|---------------------|
| **Authorization** (endpoints con policy "Admin") | Modo bootstrap (0 reglas de acceso) **o** claim `permission` = `role.manage`. |
| **Publishing** (policy "Editor") | Claim `permission` = `content.publish`. |
| **Core**, **Indexing**, **Authorization** (endpoints sin policy "Admin") | Solo estar autenticado (`[Authorize]`). |

El JWT no se rellena con “nombres de rol”, sino con **códigos de permiso** que Identity obtiene de Authorization (`GET users/{userId}/effective-permissions`). Los permisos efectivos de un usuario son la **unión** de los permisos de todos los roles que tiene asignados (vía reglas de acceso).

Por tanto:

- La **regla** (en sentido lógico) que tiene acceso a todos los endpoints es: **tener en el JWT los permisos que cada API exige**.
- En la práctica, el único rol que el sistema deja **con todos los permisos del catálogo** es **SuperAdmin**. Un usuario que solo tiene asignado el rol **SuperAdmin** tendrá, entre otros, `role.manage` y `content.publish`, y al estar autenticado podrá usar también Core e Indexing.

**Conclusión:** El rol que, por diseño, tiene acceso a todos los endpoints es **SuperAdmin** (porque tiene todos los permisos). Cualquier otro rol con acceso “total” sería uno creado manualmente al que se le hayan asignado los mismos permisos (incluidos `role.manage` y `content.publish`).

---

## Referencias

- Catálogo de permisos: `IODA.Authorization.Application/Permissions/PermissionCatalog.cs`
- Constante del rol: `AuthorizationConstants.SuperAdminRoleName`
- Políticas: Authorization API `Program.cs` (Admin = BootstrapOrAdminRequirement), Publishing API `Program.cs` (Editor = RequireClaim "content.publish")
- Arquitectura de autorización: `docs/architecture/autorizacion-apis.md`
