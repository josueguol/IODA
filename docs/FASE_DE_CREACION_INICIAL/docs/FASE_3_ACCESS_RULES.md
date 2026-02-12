# Fase 3 – Access Rules Service (Autorización)

## Estado

✅ **Access Rules Service implementado** (permisos, roles, reglas contextuales, API de comprobación de acceso).

Guía detallada de ejecución y pruebas: **COMO_PROBAR_FASE_3.md**.

---

## Estructura

- **IODA.Authorization.Domain** – Permission, Role, RolePermission, AccessRule; IPermissionRepository, IRoleRepository, IAccessRuleRepository; excepciones (RoleNotFoundException, PermissionNotFoundException, AccessRuleNotFoundException)
- **IODA.Authorization.Application** – CheckAccessQuery, CreatePermissionCommand, CreateRoleCommand, AssignPermissionsToRoleCommand, CreateAccessRuleCommand, RevokeAccessRuleCommand; GetRolesQuery, GetPermissionsQuery, GetUserAccessRulesQuery; FluentValidation; MediatR
- **IODA.Authorization.Infrastructure** – AuthorizationDbContext (PostgreSQL), configuraciones EF, PermissionRepository, RoleRepository, AccessRuleRepository
- **IODA.Authorization.API** – AuthorizationController (check, roles, permissions, rules), JWT Bearer, ErrorHandlingMiddleware, Swagger; Dockerfile; servicio en docker-compose (puerto 5003)

---

## Modelo de autorización

- **Permission**: código único (ej. `content.read`, `content.write`, `content.publish`, `schema.manage`, `project.manage`) y descripción.
- **Role**: nombre único y descripción; agrupa permisos (muchos-a-muchos con Permission).
- **AccessRule**: asigna un **rol** a un **usuario** (UserId de Identity) en un **ámbito opcional**:
  - **ProjectId** (opcional): rol solo en ese proyecto.
  - **EnvironmentId** (opcional): rol solo en ese entorno.
  - **SchemaId** (opcional): rol solo para ese tipo de contenido (schema).
  - **ContentStatus** (opcional): rol solo para contenido en ese estado (ej. Draft, Published).
  - Si todos son null = rol global para ese usuario.

La **comprobación de acceso** resuelve: dado UserId, PermissionCode y contexto (projectId, environmentId, schemaId, contentStatus), se buscan las reglas del usuario que aplican al contexto y se comprueba si algún rol asignado incluye el permiso.

---

## Cómo ejecutar

### 1. Base de datos

```bash
createdb -U postgres ioda_authorization

export ConnectionStrings__DefaultConnection="Host=localhost;Database=ioda_authorization;Username=postgres;Password=postgres;Include Error Detail=true"
dotnet ef database update --project src/Services/Authorization/IODA.Authorization.Infrastructure/IODA.Authorization.Infrastructure.csproj --startup-project src/Services/Authorization/IODA.Authorization.API/IODA.Authorization.API.csproj
```

### 2. Configuración

En **appsettings.json** (o **appsettings.Development.json**):

- **ConnectionStrings:DefaultConnection** – PostgreSQL (`ioda_authorization`).
- **Jwt:SecretKey**, **Jwt:Issuer**, **Jwt:Audience** – mismos valores que Identity API si se quiere validar JWT en la API de Authorization (opcional).

### 3. Arrancar la API

**Opción A – Desde la solución**
```bash
dotnet run --project src/Services/Authorization/IODA.Authorization.API/IODA.Authorization.API.csproj
```
- Swagger: **http://localhost:5271/swagger**

**Opción B – Con Docker**
```bash
docker compose --profile services up -d ioda-authorization-api
```
- Swagger: **http://localhost:5003/swagger**

---

## Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | /api/authorization/check | Comprobar si un usuario tiene un permiso en el contexto dado |
| GET | /api/authorization/roles | Listar roles |
| POST | /api/authorization/roles | Crear rol |
| POST | /api/authorization/roles/{roleId}/permissions | Asignar permisos a un rol |
| GET | /api/authorization/permissions | Listar permisos |
| POST | /api/authorization/permissions | Crear permiso |
| GET | /api/authorization/users/{userId}/rules | Reglas de acceso de un usuario |
| POST | /api/authorization/rules | Asignar rol a usuario en ámbito opcional |
| DELETE | /api/authorization/rules/{ruleId} | Revocar regla de acceso |

---

## Flujo típico

1. **Crear permisos** (ej. `content.read`, `content.write`, `content.publish`).
2. **Crear roles** (ej. Editor, Publisher) y **asignar permisos** a cada rol.
3. **Asignar rol a usuario** (AccessRule): userId (de Identity), roleId, y opcionalmente projectId, environmentId, schemaId, contentStatus.
4. **Comprobar acceso**: POST /api/authorization/check con `{ userId, permissionCode, projectId?, environmentId?, schemaId?, contentStatus? }` → `{ allowed: true/false }`.

---

## Consumir eventos de Identity (opcional)

- **UserLoggedInEventV1** (Shared.Contracts): se puede consumir con MassTransit para sincronizar o auditar; por ahora no está implementado. Añadir consumer en Infrastructure cuando se requiera.

---

## Documentación relacionada

- **COMO_PROBAR_FASE_3.md** – Guía paso a paso (DB, migraciones, Docker, ejemplos curl).
- **NEXT_STEPS.md** – Estado del proyecto y próximos pasos.
