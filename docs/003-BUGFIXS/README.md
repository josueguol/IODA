# 003 — Bugfixs: primer usuario, roles y GET /projects

Corrección de errores detectados:

1. **Primer usuario sin permisos de superusuario** — No puede crear nada; el JWT no incluye permisos o el bootstrap no se ejecutó.
2. **POST /api/authorization/roles → 403** — Al hacer "Crear rol" en la sección de roles, el usuario se desloguea y el servicio devuelve 403 Forbidden.
3. **GET /api/projects → 400** — El endpoint de proyectos devuelve 400 Bad Request (posiblemente por falta de permiso o validación).

---

## Configuración Identity ↔ Authorization

Para que el **primer usuario** reciba permisos en el JWT y el **bootstrap** (asignación automática del rol SuperAdmin) funcione, debe configurarse la comunicación entre Identity y Authorization:

### Identity API

| Clave | Descripción |
|-------|-------------|
| `AuthorizationApi:BaseUrl` | URL base del Authorization API (ej. `http://localhost:5271` o `https://localhost:5003`). Si está vacío, Identity usa `NoOpEffectivePermissionsClient` y el JWT **no incluirá** claims de permiso; el bootstrap del primer usuario tampoco se ejecutará. |
| `AuthorizationApi:ServiceApiKey` | Valor secreto compartido con Authorization. Se envía en el header `X-Service-Api-Key` al llamar a `GET users/{userId}/effective-permissions` y `POST bootstrap-first-user`. |

**Ejemplo (appsettings o User Secrets):**
```json
{
  "AuthorizationApi": {
    "BaseUrl": "http://localhost:5271",
    "ServiceApiKey": "clave-secreta-compartida"
  }
}
```

### Authorization API

| Clave | Descripción |
|-------|-------------|
| `Authorization:ServiceApiKey` | Mismo valor que `AuthorizationApi:ServiceApiKey` en Identity. Si está definido, los endpoints `effective-permissions` y `bootstrap-first-user` aceptan el header `X-Service-Api-Key` con este valor (además de JWT Bearer). |

**Ejemplo (appsettings o User Secrets):**
```json
{
  "Authorization": {
    "ServiceApiKey": "clave-secreta-compartida"
  }
}
```

Con esta configuración, Identity podrá obtener los permisos efectivos del usuario (para incluirlos en el JWT) y asignar el rol SuperAdmin al primer usuario tras el registro.

---

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
