# 003 — Bugfixs: primer usuario, roles y GET /projects

Corrección de errores detectados:

1. **Primer usuario sin permisos de superusuario** — No puede crear nada; el JWT no incluye permisos o el bootstrap no se ejecutó.
2. **POST /api/authorization/roles → 403** — Al hacer "Crear rol" en la sección de roles, el usuario se desloguea y el servicio devuelve 403 Forbidden.
3. **GET /api/projects → 400** — El endpoint de proyectos devuelve 400 Bad Request (posiblemente por falta de permiso o validación).

## Documentos

| Documento | Contenido |
|-----------|------------|
| [BACKEND.md](./BACKEND.md) | Tareas de corrección Backend (modo bootstrap, configuración, validación GET /projects). |
| [FRONTEND.md](./FRONTEND.md) | Tareas de corrección Frontend (refresco de token tras setup primer usuario). |

## Causa raíz (resumen)

- El JWT que recibes solo tiene `sub`, `email`, `jti`, `exp`, `iss`, `aud` — **no tiene claims de tipo `permission`**.
- Identity solo incluye permisos en el JWT si está configurado con `AuthorizationApi:BaseUrl` y obtiene los permisos del usuario desde Authorization API. Si no, usa `NoOpEffectivePermissionsClient` → JWT sin permisos.
- El bootstrap del primer usuario (asignar SuperAdmin) lo hace Identity llamando a Authorization `POST bootstrap-first-user`, pero esa llamada requiere API key coincidente (`AuthorizationApi:ServiceApiKey` en Identity y `Authorization:ServiceApiKey` en Authorization). Si falla, el primer usuario queda sin AccessRule → sin permisos en JWT.
- Authorization API exige policy "Admin" (= claim `permission` con valor `role.manage`) para POST /roles. Sin ese claim → **403**.
- Core API exige policy `project.edit` en ProjectsController. Sin claim `project.edit` → **403** (o 400 si hay otro validador).
- Por tanto: mientras el primer usuario no tenga permisos en el JWT, no puede crear roles ni listar proyectos. Hay que permitir el “modo bootstrap” cuando aún no hay reglas de acceso y/o asegurar configuración y refresco de token.
