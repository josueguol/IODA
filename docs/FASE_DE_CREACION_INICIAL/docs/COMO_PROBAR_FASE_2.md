# Cómo probar lo ya creado (Fase 2 – Identity)

Guía para ejecutar y probar el **Identity API** (registro, login, refresh token) con lo implementado hasta ahora.

---

## 1. Prerrequisitos

- **.NET 9 SDK**
- **PostgreSQL** (puerto 5432) – local o Docker

No se requiere RabbitMQ para el Identity Service en esta fase; los eventos de autenticación usan por defecto **NoOpAuthEventPublisher** (no publican a cola).

---

## 2. Base de datos

### 2.0 Si PostgreSQL está en otro proyecto (otro Docker/Compose)

Cuando Postgres corre en **otro proyecto** (otro terminal o otro `docker-compose`):

1. **Puerto distinto**: Si 5432 ya lo usa otro contenedor, tu Postgres puede estar en **5433** (u otro). Comprueba con `docker ps` o la configuración del otro proyecto.
2. **La base `ioda_identity` no existe**: En ese servidor hay que crear la base. Conéctate con el cliente que uses (psql, DBeaver, etc.) al **mismo host y puerto** que usa el otro proyecto y ejecuta: `CREATE DATABASE ioda_identity;`
3. **Usuario y contraseña**: Usa el mismo usuario y contraseña que el otro proyecto (no siempre son `postgres`/`postgres`).

**Connection string** desde tu máquina (si Postgres está en localhost pero en otro puerto):

```bash
# Ejemplo si Postgres del otro proyecto está en el puerto 5433
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=ioda_identity;Username=postgres;Password=TU_PASSWORD;Include Error Detail=true"
```

Ajusta también **`appsettings.json`** (o `appsettings.Development.json`) con el mismo `Host`, `Port`, `Username` y `Password` para que la API se conecte al mismo servidor.

**Comprobar conexión** antes de migrar:

```bash
# Con psql (ajusta -p si usas otro puerto)
psql -h localhost -p 5432 -U postgres -d ioda_identity -c "SELECT 1;"
```

Si ese comando falla, las migraciones también fallarán hasta que la conexión sea correcta.

### 2.1 Crear la base de datos

Si usas PostgreSQL local (o el del otro proyecto, mismo servidor):

```bash
# Con psql o pgAdmin: crear base de datos (ajusta -p si usas otro puerto)
createdb -U postgres -p 5432 ioda_identity
# o
psql -h localhost -p 5432 -U postgres -c "CREATE DATABASE ioda_identity;"
```

Si usas Docker (por ejemplo con `local-dev-network`):

```bash
docker run -d --name ioda-postgres-identity \
  --network local-dev-network \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=ioda_identity \
  -p 5433:5432 \
  postgres:16-alpine
```

*(Usa otro puerto -p 5433:5432 si 5432 ya está ocupado por otra instancia de Postgres.)*

### 2.2 Aplicar migraciones

Desde la raíz del repo:

```bash
cd /Users/josuegolivares/desarrollo/csharp/ioda

# Variable para design-time (EF Core usa IdentityDbContextFactory)
export ConnectionStrings__DefaultConnection="Host=localhost;Database=ioda_identity;Username=postgres;Password=postgres;Include Error Detail=true"

dotnet ef database update --project src/Services/Identity/IODA.Identity.Infrastructure/IODA.Identity.Infrastructure.csproj --startup-project src/Services/Identity/IODA.Identity.API/IODA.Identity.API.csproj
```

Si no tienes `dotnet-ef` instalado:

```bash
dotnet tool install --global dotnet-ef
```

**Importante:** El comando `dotnet ef database update` usa la variable de entorno `ConnectionStrings__DefaultConnection` (el factory de diseño la lee). Si en tu terminal tenías apuntando a **otra base** (por ejemplo `ioda_core`), las migraciones se aplican allí y `ioda_identity` queda sin tablas. La API siempre usa la connection string de `appsettings.json` (base `ioda_identity`), por eso puede salir "relation \"users\" does not exist". Siempre exporta la variable a `ioda_identity` **en la misma sesión** donde ejecutas el `dotnet ef database update`, o aplica las migraciones desde un terminal nuevo donde no esté definida (así el factory usará el valor por defecto `ioda_identity`).

### 2.3 Si dice "already up to date" pero la API devuelve "relation \"users\" does not exist"

Significa que las migraciones se aplicaron a **otra base de datos** (la que tenía la variable de entorno en ese momento), no a `ioda_identity`.

1. **Comprobar** qué tablas hay en `ioda_identity`:

```bash
psql -h localhost -p 5432 -U postgres -d ioda_identity -c "\dt"
```

Si no aparece `users` ni `refresh_tokens`, la base está vacía (o sin migraciones de Identity).

2. **Solución:** Aplicar las migraciones explícitamente contra `ioda_identity` en una sesión donde la variable apunte a esa base:

```bash
cd /Users/josuegolivares/desarrollo/csharp/ioda
export ConnectionStrings__DefaultConnection="Host=localhost;Database=ioda_identity;Username=postgres;Password=postgres;Include Error Detail=true"
dotnet ef database update --project src/Services/Identity/IODA.Identity.Infrastructure/IODA.Identity.Infrastructure.csproj --startup-project src/Services/Identity/IODA.Identity.API/IODA.Identity.API.csproj
```

Si aun así sale "No migrations were applied. The database is already up to date" y `\dt` en `ioda_identity` sigue sin mostrar tablas, entonces la tabla `__EFMigrationsHistory` en **otra** base tiene registrada la migración. En ese caso, la opción más simple es **recrear la base** y volver a aplicar:

```bash
# Cerrar conexiones a ioda_identity si las hay; luego:
psql -h localhost -p 5432 -U postgres -c "DROP DATABASE IF EXISTS ioda_identity;"
psql -h localhost -p 5432 -U postgres -c "CREATE DATABASE ioda_identity;"
export ConnectionStrings__DefaultConnection="Host=localhost;Database=ioda_identity;Username=postgres;Password=postgres;Include Error Detail=true"
dotnet ef database update --project src/Services/Identity/IODA.Identity.Infrastructure/IODA.Identity.Infrastructure.csproj --startup-project src/Services/Identity/IODA.Identity.API/IODA.Identity.API.csproj
```

Tras esto, `\dt` en `ioda_identity` debe mostrar `users`, `refresh_tokens` y `__EFMigrationsHistory`.

---

## 3. Configuración de la API

Revisa **`src/Services/Identity/IODA.Identity.API/appsettings.json`** (o `appsettings.Development.json`):

- **ConnectionStrings:DefaultConnection** – debe apuntar a tu PostgreSQL (`ioda_identity`).
- **Jwt:SecretKey** – Clave secreta para firmar el JWT (mínimo 32 caracteres en producción).
- **Jwt:Issuer** – Emisor del token (ej. `ioda-identity`).
- **Jwt:Audience** – Audiencia (ej. `ioda-cms`).
- **Jwt:ExpirationMinutes** – Caducidad del access token (ej. 60).
- **Jwt:RefreshTokenExpirationDays** – Caducidad del refresh token (ej. 7).

Para desarrollo local suelen bastar los valores por defecto si PostgreSQL está en `localhost`.

---

## 4. Ejecutar la API

### Opción A: Desde la solución (recomendado para desarrollo)

```bash
cd /Users/josuegolivares/desarrollo/csharp/ioda

dotnet run --project src/Services/Identity/IODA.Identity.API/IODA.Identity.API.csproj
```

La API queda en **http://localhost:5270** (según `Properties/launchSettings.json`). Swagger: **http://localhost:5270/swagger**.

### Opción B: Con Docker

```bash
cd /Users/josuegolivares/desarrollo/csharp/ioda

docker compose --profile services up -d ioda-identity-api
```

El servicio está en el puerto **5002** (mapeado al 8080 del contenedor). Ajusta en `docker-compose.yml` las variables de entorno para que apunten a tu Postgres y tengas una **Jwt__SecretKey** de al menos 32 caracteres.

**Nota:** Si usas Docker, la base `ioda_identity` debe existir en el mismo Postgres al que se conecta el contenedor (por defecto `Host=postgres`). Crea la base en ese servidor y aplica las migraciones desde tu máquina apuntando a ese host, o incluye Postgres en tu compose y crea la base al iniciar.

---

## 5. Probar los endpoints

### 5.1 Swagger (recomendado)

1. Abre en el navegador: **http://localhost:5270/swagger** (local) o **http://localhost:5002/swagger** (Docker).
2. Prueba en este orden:
   - **POST /api/auth/register** – Registrar usuario (email, password, displayName opcional).
   - **POST /api/auth/login** – Login con email y password → devuelve `accessToken`, `refreshToken`, `expiresInSeconds`, `userId`, `email`, `displayName`.
   - **POST /api/auth/refresh** – Enviar el `refreshToken` recibido en login para obtener un nuevo par access + refresh token.

### 5.2 Con curl (resumen)

Sustituye `BASE=http://localhost:5270` si usas otro puerto (por ejemplo `http://localhost:5002` con Docker).

```bash
BASE=http://localhost:5270

# 1. Registrar usuario
curl -s -X POST "$BASE/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{"email":"usuario@ejemplo.com","password":"MiClaveSegura123!","displayName":"Usuario Prueba"}'
# Respuesta: UserId (GUID)

# 2. Login
curl -s -X POST "$BASE/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"usuario@ejemplo.com","password":"MiClaveSegura123!"}'
# Respuesta: accessToken, refreshToken, expiresInSeconds, userId, email, displayName

# 3. Refrescar token (usa el refreshToken del paso 2)
curl -s -X POST "$BASE/api/auth/refresh" \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"REFRESH_TOKEN_DEL_PASO_2"}'
# Respuesta: nuevo accessToken, refreshToken, expiresInSeconds, etc.
```

### 5.3 Llamadas autenticadas a otras APIs

Cuando el **Core API** (u otro servicio) esté protegido con JWT, envía el access token en el header:

```bash
curl -s -H "Authorization: Bearer TU_ACCESS_TOKEN" "http://localhost:5001/api/projects"
```

El Core API debe estar configurado con el mismo **Issuer**, **Audience** y **SecretKey** que el Identity API para validar el token.

---

## 6. Resumen de flujo de prueba

| Orden | Acción | Endpoint / Acción |
|-------|--------|-------------------|
| 1 | Registrar usuario | `POST /api/auth/register` |
| 2 | Login | `POST /api/auth/login` |
| 3 | Usar access token | Header `Authorization: Bearer <accessToken>` en APIs protegidas |
| 4 | Refrescar token | `POST /api/auth/refresh` con `refreshToken` |

---

## 7. Errores frecuentes

- **"relation \"users\" does not exist" (500)**: Las migraciones se aplicaron a otra base de datos (p. ej. tenías `ConnectionStrings__DefaultConnection` apuntando a `ioda_core`). La API usa `ioda_identity` desde appsettings. Solución: ver sección **2.3** (exportar la variable a `ioda_identity` y ejecutar `dotnet ef database update`, o recrear la base `ioda_identity` y aplicar de nuevo).
- **Connection string**: que `appsettings` apunte a la misma base `ioda_identity` donde aplicaste las migraciones.
- **Migraciones**: si cambias el modelo (User, RefreshToken), genera y aplica una nueva migración con `dotnet ef migrations add ...` y `dotnet ef database update ...`.
- **JWT SecretKey**: en producción usa una clave de al menos 32 caracteres y no la dejes en valor por defecto. En Docker, pásala con `Jwt__SecretKey` en el `environment` del servicio.
- **Usuario ya existe**: si registras el mismo email dos veces, la API devuelve error (UserAlreadyExistsException). Usa otro email o elimina el usuario en la base para repetir la prueba.
- **Credenciales inválidas**: en login, email o contraseña incorrectos devuelven 401 (InvalidCredentialsException).
- **Refresh token inválido o expirado**: `POST /api/auth/refresh` con un token ya usado, expirado o inexistente devuelve error (InvalidRefreshTokenException).

---

## 8. Siguientes pasos (según FASE_2_IDENTITY y NEXT_STEPS)

- Integrar el **Core API** (u otros servicios) con JWT: mismo Issuer, Audience y SecretKey para validar el token emitido por Identity.
- Opcional: implementar **MassTransitAuthEventPublisher** para publicar eventos de autenticación (p. ej. UserLoggedInEventV1) a RabbitMQ.
- Añadir proveedores externos (Google, Microsoft, etc.) si se requiere.

Con esto puedes probar todo lo ya creado (Domain, Application, Infrastructure, API de Auth) antes de continuar con la Fase 3 (Authorization Service) o con tests.
