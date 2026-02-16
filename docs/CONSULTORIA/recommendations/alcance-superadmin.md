# Recomendación: Alcance del usuario SuperAdmin

**Pregunta:** Como consultor experto en arquitectura, ¿el usuario SuperAdmin debería poder crear proyectos, ambientes, sitios, usuarios, crear roles y asignar permisos?

---

## 1. Posición del consultor: **Sí**

Desde el punto de vista de arquitectura y gobierno del sistema, **SuperAdmin debe poder hacer todo lo que mencionas**:

| Capacidad | ¿Debe tenerla SuperAdmin? | Razón |
|-----------|---------------------------|--------|
| Crear proyectos | **Sí** | Necesario para bootstrap y operación inicial; sin proyectos no hay sistema usable. |
| Crear ambientes | **Sí** | Los ambientes (dev, staging, prod) son parte del modelo de datos; solo un rol de máximo privilegio debe poder definirlos. |
| Crear sitios | **Sí** | Misma lógica: configuración estructural del CMS. |
| Crear usuarios | **Sí** | Imprescindible para dar de alta al resto de cuentas y recuperar acceso si se pierde el único admin. |
| Crear roles | **Sí** | Definir roles es gestión de seguridad; debe estar en manos de un superadministrador. |
| Asignar permisos (a roles) | **Sí** | Sin esto no se puede configurar quién hace qué; es el núcleo del modelo RBAC. |

En resumen: **SuperAdmin es el rol de máximo privilegio por diseño**. Debe tener capacidad completa sobre proyectos, ambientes, sitios, usuarios, roles y permisos para:

- **Bootstrap:** Poner el sistema en marcha y crear la primera estructura.
- **Recuperación:** Corregir configuraciones o restaurar acceso.
- **Operación:** Gestionar la organización (proyectos, entornos, sitios) y la seguridad (usuarios, roles, permisos) sin depender de otros roles.

Limitar a SuperAdmin en estas áreas obligaría a tener “otro superadmin” o mecanismos especiales (por ejemplo, ejecutar scripts contra BD), lo que aumenta complejidad y riesgo. Es más claro y mantenible que **un único rol** (SuperAdmin) concentre todo el alcance administrativo máximo.

---

## 2. Coherencia con el sistema actual

En el código actual:

- El **catálogo de permisos** (PermissionCatalog) ya incluye los permisos que cubren esas capacidades:
  - `project.create`, `project.edit`, `project.delete`
  - `environment.create`, `environment.edit`, `environment.delete`
  - `site.create`, `site.edit`, `site.delete`
  - `user.list`, `user.create`
  - `role.manage` (crear roles y asignar permisos)
- El rol **SuperAdmin** se crea por seed y recibe **todos** los permisos del catálogo.

Por tanto, **el modelo de permisos ya está alineado con la recomendación**: SuperAdmin, por tener todos los permisos, tiene “derecho” a crear proyectos, ambientes, sitios, usuarios, roles y asignar permisos.

La brecha posible está en la **aplicación** de esos permisos en las APIs:

- **Authorization API:** La policy "Admin" (acceso a CRUD de roles/permisos/reglas) usa `role.manage` o modo bootstrap → coherente con SuperAdmin.
- **Core API:** Hoy solo exige `[Authorize]`; no comprueba `project.create`, `environment.create`, `site.create`, etc. Cualquier usuario autenticado podría en teoría crear proyectos/ambientes/sitios si el frontend lo permite. Para que “solo SuperAdmin (o quien tenga el permiso) pueda crear X” hace falta que Core (y, si aplica, otros servicios) **restrinjan por permiso** según el catálogo.
- **Identity API:** Habría que revisar si los endpoints de listar/crear usuarios están protegidos y si usan `user.list` / `user.create` para restringir acceso.

Conclusión: **el diseño (catálogo + SuperAdmin) ya respalda que SuperAdmin pueda hacer todo eso**. Lo que puede faltar es que **todas las APIs exijan los permisos correspondientes** para que en la práctica solo SuperAdmin (o los roles a los que se asignen esos permisos) tengan ese alcance.

---

## 3. Recomendación operativa

- **Mantener** que SuperAdmin tenga todos los permisos del catálogo y, por tanto, capacidad para crear proyectos, ambientes, sitios, usuarios, roles y asignar permisos.
- **Documentar** explícitamente en arquitectura o en un glosario de roles que “SuperAdmin = máximo privilegio operativo y de seguridad” con la lista de capacidades anteriores.
- **Avanzar** hacia que Core (y los demás servicios que corresponda) protejan los endpoints de creación/edición/eliminación de proyectos, ambientes y sitios con las políticas basadas en los permisos del catálogo (`project.create`, etc.), de modo que el comportamiento real coincida con el modelo de permisos y con este alcance recomendado para SuperAdmin.

---

## Referencias

- Catálogo de permisos: `IODA.Authorization.Application/Permissions/PermissionCatalog.cs`
- FAQ roles y acceso: `docs/CONSULTORIA/faqs/roles-y-regla-acceso-todos-endpoints.md`
- Arquitectura de autorización: `docs/CONSULTORIA/architecture/autorizacion-apis.md`
