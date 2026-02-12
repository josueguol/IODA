# Cómo probar lo ya creado (Fase 3 – Access Rules / Authorization)

Guía para ejecutar y probar el **Authorization API** (permisos, roles, reglas contextuales, comprobación de acceso).

---

## 1. Prerrequisitos

- **.NET 9 SDK**
- **PostgreSQL** (puerto 5432) – local o Docker
- **Opcional:** Identity API levantada y un usuario registrado (para usar su `UserId` en reglas)

---

## 2. Base de datos

### 2.0 Si PostgreSQL está en otro proyecto

Cuando Postgres corre en **otro proyecto**:

1. **Puerto distinto**: Comprueba con `docker ps` o la configuración del otro proyecto.
2. **La base `ioda_authorization` no existe**: Ejecuta `CREATE DATABASE ioda_authorization;` en ese servidor.
3. **Usuario y contraseña**: Usa el mismo que el otro proyecto.

**Connection string** (ajusta host, puerto y contraseña):

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=ioda_authorization;Username=postgres;Password=postgres;Include Error Detail=true"
```

### 2.1 Crear la base de datos

```bash
createdb -U postgres -p 5432 ioda_authorization
# o
psql -h localhost -p 5432 -U postgres -c "CREATE DATABASE ioda_authorization;"
```

### 2.2 Aplicar migraciones

Desde la raíz del repo:

```bash
cd /Users/josuegolivares/desarrollo/csharp/ioda

export ConnectionStrings__DefaultConnection="Host=localhost;Database=ioda_authorization;Username=postgres;Password=postgres;Include Error Detail=true"
dotnet ef database update --project src/Services/Authorization/IODA.Authorization.Infrastructure/IODA.Authorization.Infrastructure.csproj --startup-project src/Services/Authorization/IODA.Authorization.API/IODA.Authorization.API.csproj
```

Si no tienes `dotnet-ef` instalado: `dotnet tool install --global dotnet-ef`

---

## 3. Configuración de la API

Revisa **`src/Services/Authorization/IODA.Authorization.API/appsettings.json`**:

- **ConnectionStrings:DefaultConnection** – debe apuntar a tu PostgreSQL (`ioda_authorization`).
- **Jwt** (opcional): Si quieres proteger endpoints con JWT, usa el mismo SecretKey, Issuer y Audience que Identity API.

---

## 4. Ejecutar la API

### Opción A: Desde la solución

```bash
cd /Users/josuegolivares/desarrollo/csharp/ioda
dotnet run --project src/Services/Authorization/IODA.Authorization.API/IODA.Authorization.API.csproj
```

- Swagger: **http://localhost:5271/swagger**

### Opción B: Con Docker

```bash
docker compose --profile services up -d ioda-authorization-api
```

- Swagger: **http://localhost:5003/swagger**

---

## 5. Probar los endpoints

### 5.1 Flujo recomendado

1. **Crear permisos** (ej. `content.read`, `content.edit`, `content.publish`).
2. **Crear roles** (ej. Editor, Publisher).
3. **Asignar permisos a cada rol**.
4. **Asignar un rol a un usuario** (AccessRule): necesitas un `userId` (GUID de Identity) y el `roleId`.
5. **Comprobar acceso**: POST /api/authorization/check con userId, permissionCode y contexto opcional.

**Para asignar los permisos que usa el CMS** (content.edit, content.publish) paso a paso, ver **`docs/COMO_ASIGNAR_PERMISOS_CMS.md`**.

### 5.2 Swagger

Abre **http://localhost:5271/swagger** (o **http://localhost:5003/swagger** con Docker) y prueba en este orden:

- **POST /api/authorization/permissions** – Crear permiso (Code, Description).
- **GET /api/authorization/permissions** – Listar permisos.
- **POST /api/authorization/roles** – Crear rol (Name, Description).
- **POST /api/authorization/roles/{roleId}/permissions** – Asignar permisos al rol (body: `{ "permissionIds": [ "guid-del-permiso" ] }`).
- **POST /api/authorization/rules** – Asignar rol a usuario (UserId, RoleId, y opcionalmente ProjectId, EnvironmentId, SchemaId, ContentStatus).
- **POST /api/authorization/check** – Comprobar acceso (UserId, PermissionCode, y opcionalmente ProjectId, EnvironmentId, SchemaId, ContentStatus).

### 5.3 Con curl (resumen)

Sustituye `BASE=http://localhost:5271` (o `http://localhost:5003` con Docker). Necesitas un **userId** de Identity (registra y haz login en Identity API y usa el `userId` de la respuesta).

```bash
BASE=http://localhost:5271
USER_ID=11111111-1111-1111-1111-111111111111   # Sustituir por un UserId real de Identity

# 1. Crear permisos
curl -s -X POST "$BASE/api/authorization/permissions" \
  -H "Content-Type: application/json" \
  -d '{"code":"content.read","description":"Leer contenido"}'
# Anotar el GUID devuelto → PERMISSION_READ_ID

curl -s -X POST "$BASE/api/authorization/permissions" \
  -H "Content-Type: application/json" \
  -d '{"code":"content.write","description":"Escribir contenido"}'
# Anotar → PERMISSION_WRITE_ID

# 2. Crear rol
curl -s -X POST "$BASE/api/authorization/roles" \
  -H "Content-Type: application/json" \
  -d '{"name":"Editor","description":"Puede leer y escribir contenido"}'
# Anotar el GUID → ROLE_ID

# 3. Asignar permisos al rol (sustituir ROLE_ID, PERMISSION_READ_ID, PERMISSION_WRITE_ID)
curl -s -X POST "$BASE/api/authorization/roles/ROLE_ID/permissions" \
  -H "Content-Type: application/json" \
  -d '{"permissionIds":["PERMISSION_READ_ID","PERMISSION_WRITE_ID"]}'

# 4. Asignar rol al usuario (sustituir USER_ID y ROLE_ID)
curl -s -X POST "$BASE/api/authorization/rules" \
  -H "Content-Type: application/json" \
  -d "{\"userId\":\"$USER_ID\",\"roleId\":\"ROLE_ID\"}"

# 5. Comprobar acceso
curl -s -X POST "$BASE/api/authorization/check" \
  -H "Content-Type: application/json" \
  -d "{\"userId\":\"$USER_ID\",\"permissionCode\":\"content.read\"}"
# Respuesta: { "allowed": true } o { "allowed": false }
```

---

## 6. Resumen de flujo de prueba

| Orden | Acción | Endpoint / Acción |
|-------|--------|-------------------|
| 1 | Crear permisos | POST /api/authorization/permissions |
| 2 | Crear roles | POST /api/authorization/roles |
| 3 | Asignar permisos a rol | POST /api/authorization/roles/{roleId}/permissions |
| 4 | Asignar rol a usuario | POST /api/authorization/rules |
| 5 | Comprobar acceso | POST /api/authorization/check |
| 6 | Listar reglas de usuario | GET /api/authorization/users/{userId}/rules |
| 7 | Revocar regla | DELETE /api/authorization/rules/{ruleId} |

---

## 7. Errores frecuentes

- **Connection string**: que `appsettings` apunte a la misma base `ioda_authorization` donde aplicaste las migraciones.
- **RoleNotFoundException / PermissionNotFoundException**: el roleId o permissionId no existe; crea el rol/permiso antes de asignar.
- **UserId**: para asignar reglas necesitas un UserId válido (de Identity API). Registra un usuario en Identity y usa el GUID devuelto.
- **Conflict (409)**: rol o permiso con el mismo nombre/código ya existe.

---

## 8. Siguientes pasos

- Integrar Core API (u otros servicios) con Authorization: llamar a POST /api/authorization/check antes de ejecutar una acción.
- Opcional: consumir eventos de Identity (UserLoggedInEventV1) con MassTransit para auditar o sincronizar.
- Proteger endpoints de la Authorization API con `[Authorize]` y JWT si se desea.

Con esto puedes probar todo lo implementado en la Fase 3 (Domain, Application, Infrastructure, API de Access Rules).
