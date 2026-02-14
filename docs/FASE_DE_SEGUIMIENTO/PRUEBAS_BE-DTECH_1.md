# Guía de pruebas — Fase 1 Backend (1.1 a 1.5)

Rama: `fix/BE-DTECH-1/add-auth-on-apis`  
Objetivo: validar autorización en APIs, CORS, separación de capas Identity, validación en startup y secretos.

---

## Requisitos previos

- **PostgreSQL** en ejecución.
- **JWT:** Todos los APIs que usan JWT deben compartir el mismo `Jwt:SecretKey`, `Issuer` y `Audience` (lo emite Identity).
- **Herramientas:** `curl` o Postman (o similar) para llamar a los endpoints.

### Configuración para desarrollo (1.5 — User Secrets)

En `appsettings.json` no se guardan valores reales de secretos (solo placeholders). Para que los APIs arranquen y funcionen en local hay que configurar **User Secrets** en cada proyecto API.

Desde la raíz del repositorio, en cada carpeta del API:

```bash
# Identity API (obligatorio para login/register)
cd src/Services/Identity/IODA.Identity.API
dotnet user-secrets set "Jwt:SecretKey" "TU_CLAVE_DEV_MINIMO_32_CARACTERES"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=ioda_identity;Username=postgres;Password=postgres;Include Error Detail=true"

# Authorization API
cd src/Services/Authorization/IODA.Authorization.API
dotnet user-secrets set "Jwt:SecretKey" "TU_CLAVE_DEV_MINIMO_32_CARACTERES"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=ioda_authorization;Username=postgres;Password=postgres;Include Error Detail=true"

# Core API
cd src/Services/Core/IODA.Core.API
dotnet user-secrets set "Jwt:SecretKey" "TU_CLAVE_DEV_MINIMO_32_CARACTERES"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=ioda_core;Username=postgres;Password=postgres;Include Error Detail=true"

# Publishing API
cd src/Services/Publishing/IODA.Publishing.API
dotnet user-secrets set "Jwt:SecretKey" "TU_CLAVE_DEV_MINIMO_32_CARACTERES"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=ioda_publishing;Username=postgres;Password=postgres;Include Error Detail=true"

# Indexing API (solo JWT; no usa BD)
cd src/Services/Indexing/IODA.Indexing.API
dotnet user-secrets set "Jwt:SecretKey" "TU_CLAVE_DEV_MINIMO_32_CARACTERES"
```

```bash
# CORE
export ConnectionStrings__DefaultConnection="Host=localhost;Database=ioda_core;Username=postgres;Password=postgres;Include Error Detail=true"
dotnet ef database update --project src/Services/Core/IODA.Core.Infrastructure --startup-project src/Services/Core/IODA.Core.API

# IDENTITY
export ConnectionStrings__DefaultConnection="Host=localhost;Database=ioda_identity;Username=postgres;Password=postgres;Include Error Detail=true"
dotnet ef database update --project src/Services/Identity/IODA.Identity.Infrastructure/IODA.Identity.Infrastructure.csproj --startup-project src/Services/Identity/IODA.Identity.API/IODA.Identity.API.csproj

# AUTHORIZATION
export ConnectionStrings__DefaultConnection="Host=localhost;Database=ioda_authorization;Username=postgres;Password=postgres;Include Error Detail=true"
dotnet ef database update --project src/Services/Authorization/IODA.Authorization.Infrastructure/IODA.Authorization.Infrastructure.csproj --startup-project src/Services/Authorization/IODA.Authorization.API/IODA.Authorization.API.csproj

# PUBLISHING
export ConnectionStrings__DefaultConnection="Host=localhost;Database=ioda_publishing;Username=postgres;Password=postgres;Include Error Detail=true"
dotnet ef database update --project src/Services/Publishing/IODA.Publishing.Infrastructure/IODA.Publishing.Infrastructure.csproj --startup-project src/Services/Publishing/IODA.Publishing.API/IODA.Publishing.API.csproj
```

Usa **el mismo** `Jwt:SecretKey` en todos. En producción se usan variables de entorno o vault, no User Secrets.

### Bases de datos y tablas (referencia)

Si creas las bases manualmente, usa estos nombres. En desarrollo lo habitual es aplicar migraciones con `dotnet ef database update` desde cada proyecto Infrastructure.

| Servicio     | Base de datos    | Tablas principales                                  |
|-------------|------------------|-----------------------------------------------------|
| Identity    | `ioda_identity`  | `users`, `refresh_tokens`                           |
| Authorization | `ioda_authorization` | `roles`, `permissions`, `role_permissions`, `access_rules` |
| Publishing  | `ioda_publishing`| `publication_requests`                             |
| Core        | `ioda_core`      | `projects`, `environments`, `content_schemas`, `contents`, etc. |
| Indexing    | —                | No usa PostgreSQL; usa Elasticsearch (opcional para estas pruebas). |

**Importante para 1.3 (Identity):** Para probar “primer usuario” y “auto-registro deshabilitado” hace falta que la base **Identity esté vacía** (sin filas en `users`). Las **tablas** deben existir (creadas con migraciones). Ver sección siguiente.

---

## Preparar la base Identity (obligatorio antes de 1.1 y 1.3)

El error `relation "users" does not exist` indica que la base existe pero **no se han creado las tablas**. Hay que aplicar las migraciones de Entity Framework.

### Paso 1: Crear la base de datos (si no existe)

En PostgreSQL:

```bash
psql -U postgres -c "CREATE DATABASE ioda_identity;"
```

(Si ya existe, omitir o ignorar el error "already exists".)

### Paso 2: Aplicar migraciones (crear tablas `users` y `refresh_tokens`)

Desde la **raíz del repositorio**:

```bash
cd src/Services/Identity
dotnet ef database update --project IODA.Identity.Infrastructure --startup-project IODA.Identity.API
```

- Se usa la connection string del proyecto `IODA.Identity.API` (appsettings.json). Comprueba que `ConnectionStrings:DefaultConnection` apunte a la base `ioda_identity`.
- Si hace falta instalar la herramienta EF: `dotnet tool install --global dotnet-ef`

Tras ejecutarlo, en `ioda_identity` deben existir las tablas **users** y **refresh_tokens**. Comprobación:

```bash
psql -U postgres -d ioda_identity -c "\dt"
```

### Base "vacía" para 1.3

- **Vacía** = tablas creadas y **sin filas** en `users` (y en `refresh_tokens`).
- Si ya hay usuarios y quieres dejar la base vacía para repetir pruebas: ejecutar en la base `ioda_identity`:  
  `TRUNCATE TABLE refresh_tokens, users RESTART IDENTITY CASCADE;`  
  **Advertencia:** borra todos los usuarios y tokens.

---

## 1.1 — Autorización en APIs

Comprobar que los endpoints exigen JWT y que las políticas (Admin/Editor) se aplican.

### 1.1.1 Arrancar los servicios

1. Identity API (para obtener JWT):
   ```bash
   cd src/Services/Identity/IODA.Identity.API
   dotnet run
   ```
   Debe quedar en escucha (p. ej. `https://localhost:7xxx` o `http://localhost:5xxx`).

2. Authorization API:
   ```bash
   cd src/Services/Authorization/IODA.Authorization.API
   dotnet run
   ```

3. Publishing API:
   ```bash
   cd src/Services/Publishing/IODA.Publishing.API
   dotnet run
   ```

4. Indexing API:
   ```bash
   cd src/Services/Indexing/IODA.Indexing.API
   dotnet run
   ```

Anota los puertos de cada uno (o usa los que tengas en `launchSettings.json` / URLs que muestre la consola).

### 1.1.2 Obtener un JWT (Identity)

- **Login** (si ya existe un usuario):
  ```bash
  curl -X POST https://localhost:<IDENTITY_PORT>/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"TU_EMAIL","password":"TU_PASSWORD"}'
  ```
  Guarda `accessToken` de la respuesta.

- **Registro** (si la base Identity está vacía — ver 1.3):
  ```bash
  curl -X POST https://localhost:<IDENTITY_PORT>/api/auth/register \
    -H "Content-Type: application/json" \
    -d '{"email":"admin@test.com","password":"Test123!","displayName":"Admin"}'
  ```
  Luego haz login con ese usuario y guarda el `accessToken`.

Sustituye en los siguientes pasos `TU_ACCESS_TOKEN` por ese valor.

### 1.1.3 Sin token → 401

Llamar **sin** cabecera `Authorization` debe devolver **401 Unauthorized**:

- **Authorization API**
  ```bash
  curl -i -X GET https://localhost:<AUTHZ_PORT>/api/authorization/roles
  ```
  Esperado: **401**.

- **Publishing API**
  ```bash
  curl -i -X GET https://localhost:<PUBLISHING_PORT>/api/publishing/requests
  ```
  Esperado: **401**.

- **Indexing API**
  ```bash
  curl -i -X GET "https://localhost:<INDEXING_PORT>/api/indexing/search?q=test"
  ```
  Esperado: **401**.

### 1.1.4 Con token → 200 (según permiso)

Con JWT válido en la cabecera:

- **Authorization API** (lectura de roles; cualquier usuario autenticado):
  ```bash
  curl -i -X GET https://localhost:<AUTHZ_PORT>/api/authorization/roles \
    -H "Authorization: Bearer TU_ACCESS_TOKEN"
  ```
  Esperado: **200** y lista de roles (puede ser vacía).

- **Publishing API** (policy "Editor"; el JWT debe incluir rol `Editor` o `Admin`):
  ```bash
  curl -i -X GET https://localhost:<PUBLISHING_PORT>/api/publishing/requests \
    -H "Authorization: Bearer TU_ACCESS_TOKEN"
  ```
  Si el token no tiene rol Editor/Admin: **403 Forbidden**.  
  Si tiene rol correcto: **200**.

- **Indexing API** (solo autenticado):
  ```bash
  curl -i -X GET "https://localhost:<INDEXING_PORT>/api/indexing/search?q=test" \
    -H "Authorization: Bearer TU_ACCESS_TOKEN"
  ```
  Esperado: **200** (o respuesta de búsqueda según configuración de Elasticsearch).

### 1.1.5 Resumen 1.1

| Comprobación              | Resultado esperado |
|---------------------------|--------------------|
| Authorization/roles sin JWT | 401                |
| Authorization/roles con JWT | 200                |
| Publishing/requests sin JWT | 401                |
| Publishing/requests con JWT sin rol Editor/Admin | 403 |
| Indexing/search sin JWT    | 401                |
| Indexing/search con JWT    | 200                |

---

## 1.2 — CORS

Comprobar que solo se aceptan orígenes configurados (o los por defecto en Development).

### 1.2.1 Comportamiento en Development

- En **Development**, si `Cors:AllowedOrigins` está vacío o no existe, se usan por defecto:  
  `http://localhost:3000`, `http://localhost:5173`, `https://localhost:3000`, `https://localhost:5173`.

### 1.2.2 Cómo probar

1. Desde el navegador: abrir una app frontend en uno de esos orígenes (p. ej. `http://localhost:5173`) y llamar a cualquier API (Identity, Authorization, etc.). No debe aparecer error CORS.
2. Desde un origen no permitido (p. ej. `http://localhost:9999` si no está en la lista): la petición desde el navegador debe ser bloqueada por CORS (mensaje de CORS en consola del navegador).

### 1.2.3 Configurar orígenes explícitos (opcional)

En `appsettings.json` del API correspondiente (o por entorno):

```json
"Cors": {
  "AllowedOrigins": ["http://localhost:5173", "https://mi-frontend.com"]
}
```

Reiniciar el API y comprobar que solo esos orígenes pueden llamar sin error CORS.

### 1.2.4 Resumen 1.2

| Comprobación                    | Resultado esperado                    |
|---------------------------------|--------------------------------------|
| Petición desde origen permitido | Sin error CORS                       |
| Petición desde origen no permitido | Bloqueada por CORS (navegador)   |

---

## 1.3 — Identity API: separación de capas

Comprobar que el setup status y el registro usan solo MediatR (sin `IUserRepository`/`IConfiguration` en el controller) y que la lógica de “primer usuario” y “auto-registro” está en Application.

**Requisito:** Base Identity **vacía** (ver requisitos previos) para probar primer usuario y auto-registro deshabilitado.

### 1.3.1 Setup status (sin usuarios)

Con la base Identity vacía:

```bash
curl -s https://localhost:<IDENTITY_PORT>/api/auth/setup-status
```

Esperado: algo como `{"hasUsers":false,"selfRegistrationEnabled":true}` (o el valor que tengas en `SelfRegistration:Enabled` en appsettings).

### 1.3.2 Registro del primer usuario

```bash
curl -s -X POST https://localhost:<IDENTITY_PORT>/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"first@test.com","password":"Test123!","displayName":"First"}'
```

Esperado: **201 Created** y cuerpo con `userId` y `isFirstUser: true`.

### 1.3.3 Setup status (con usuarios)

Sin borrar los usuarios:

```bash
curl -s https://localhost:<IDENTITY_PORT>/api/auth/setup-status
```

Esperado: `{"hasUsers":true,"selfRegistrationEnabled":true}` (o el valor configurado).

### 1.3.4 Auto-registro deshabilitado

1. En `appsettings.json` de Identity API:
   ```json
   "SelfRegistration": { "Enabled": false }
   ```
2. Reiniciar Identity API.
3. Intentar registrar **otro** usuario (no el primero):
   ```bash
   curl -i -X POST https://localhost:<IDENTITY_PORT>/api/auth/register \
     -H "Content-Type: application/json" \
     -d '{"email":"second@test.com","password":"Test123!","displayName":"Second"}'
   ```
   Esperado: **403 Forbidden** y mensaje indicando que el auto-registro está deshabilitado.

4. Volver a poner `"Enabled": true` si quieres seguir probando registro.

### 1.3.5 Login y refresh

Para asegurarte de que el controller solo usa MediatR y no ha roto nada:

- **Login:**
  ```bash
  curl -s -X POST https://localhost:<IDENTITY_PORT>/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"first@test.com","password":"Test123!"}'
  ```
  Esperado: **200** y `accessToken`, `refreshToken`, etc.

- **Refresh:** usar el `refreshToken` recibido en el login:
  ```bash
  curl -s -X POST https://localhost:<IDENTITY_PORT>/api/auth/refresh \
    -H "Content-Type: application/json" \
    -d '{"refreshToken":"<REFRESH_TOKEN>"}'
  ```
  Esperado: **200** y nuevo par de tokens.

### 1.3.6 Resumen 1.3

| Comprobación                         | Resultado esperado                          |
|-------------------------------------|---------------------------------------------|
| GET /api/auth/setup-status (0 users)| 200, hasUsers: false                        |
| POST /api/auth/register (1.º user) | 201, isFirstUser: true                      |
| GET /api/auth/setup-status (con users) | 200, hasUsers: true                     |
| POST /api/auth/register (2.º user, auto-registro off) | 403              |
| POST /api/auth/login                | 200, tokens                                 |
| POST /api/auth/refresh             | 200, nuevos tokens                          |

---

## 1.4 — Validación en startup (seguridad)

En entorno **no-Development**, los APIs no deben arrancar si faltan `Jwt:SecretKey` o `ConnectionStrings:DefaultConnection`. **Prueba:** ejecutar un API con `ASPNETCORE_ENVIRONMENT=Production` sin User Secrets ni variables para JWT/ConnectionString; debe lanzar `InvalidOperationException`. Con config (env o User Secrets en dev), debe arrancar bien.

---

## 1.5 — Secretos y configuración sensible

En cada API, `appsettings.json` no debe tener claves/contraseñas reales (solo placeholders o vacío). En desarrollo se usan User Secrets (ver sección al inicio).

---

## Checklist final

- [ ] 1.1: Endpoints de Authorization, Publishing e Indexing devuelven 401 sin JWT.
- [ ] 1.1: Con JWT válido, los endpoints permitidos responden 200 (o 403 si falta rol).
- [ ] 1.2: CORS solo permite orígenes configurados (o los por defecto en Development).
- [ ] 1.3: Setup status y registro correctos; 403 cuando auto-registro deshabilitado y ya hay usuarios.
- [ ] 1.4: En no-Development sin JWT/ConnectionString la app no arranca; con config sí.
- [ ] 1.5: appsettings.json sin valores reales; en dev User Secrets permiten ejecutar los APIs.

Si algo no coincide, revisar puertos, JWT (issuer/audience/secret) y que la base Identity esté vacía cuando se prueban los casos de “primer usuario” y “auto-registro deshabilitado”.
