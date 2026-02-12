# Fase 2 – Identity Service (Autenticación)

## Estado

✅ **Identity Service implementado** (registro, login, JWT, refresh tokens, eventos de autenticación).

Guía detallada de ejecución y pruebas: **COMO_PROBAR_FASE_2.md** (migraciones, configuración JWT, Docker, errores frecuentes).

---

## Estructura

- **IODA.Identity.Domain** – User, RefreshToken, IUserRepository, IRefreshTokenRepository, excepciones (UserNotFoundException, InvalidCredentialsException, InvalidRefreshTokenException, UserAlreadyExistsException)
- **IODA.Identity.Application** – LoginCommand, RefreshTokenCommand, RegisterCommand; IJwtTokenGenerator, IPasswordHasher, IRefreshTokenGenerator, IAuthEventPublisher; DTOs; FluentValidation; MediatR
- **IODA.Identity.Infrastructure** – IdentityDbContext (PostgreSQL), BCryptPasswordHasher, JwtTokenGenerator, RefreshTokenGenerator, UserRepository, RefreshTokenRepository, NoOpAuthEventPublisher (sustituible por MassTransit)
- **IODA.Identity.API** – AuthController (register, login, refresh), JWT Bearer, ErrorHandlingMiddleware, Swagger

---

## Cómo ejecutar

### 1. Base de datos

```bash
# Crear base de datos
createdb -U postgres ioda_identity

# Aplicar migraciones (desde la raíz del repo)
export ConnectionStrings__DefaultConnection="Host=localhost;Database=ioda_identity;Username=postgres;Password=postgres;Include Error Detail=true"
dotnet ef database update --project src/Services/Identity/IODA.Identity.Infrastructure/IODA.Identity.Infrastructure.csproj --startup-project src/Services/Identity/IODA.Identity.API/IODA.Identity.API.csproj
```

### 2. Configuración

En **appsettings.json** (o **appsettings.Development.json**):

- **ConnectionStrings:DefaultConnection** – PostgreSQL (`ioda_identity`)
- **Jwt:SecretKey** – Clave secreta (mín. 32 caracteres en producción)
- **Jwt:Issuer** – Emisor del token (ej. `ioda-identity`)
- **Jwt:Audience** – Audiencia (ej. `ioda-cms`)
- **Jwt:ExpirationMinutes** – Caducidad del access token (ej. 60)
- **Jwt:RefreshTokenExpirationDays** – Caducidad del refresh token (ej. 7)

### 3. Arrancar la API

**Opción A – Desde la solución**
```bash
dotnet run --project src/Services/Identity/IODA.Identity.API/IODA.Identity.API.csproj
```
- Swagger: **http://localhost:5270/swagger**

**Opción B – Con Docker**
```bash
docker compose --profile services up -d ioda-identity-api
```
- Swagger: **http://localhost:5002/swagger** (puerto mapeado en docker-compose)

---

## Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | /api/auth/register | Registrar usuario (Email, Password, DisplayName opcional) |
| POST | /api/auth/login | Login (Email, Password) → AccessToken, RefreshToken, ExpiresIn, UserId, Email, DisplayName |
| POST | /api/auth/refresh | Nuevo access token usando RefreshToken |

---

## Flujo típico

1. **Registro:** `POST /api/auth/register` con `{ "email": "...", "password": "...", "displayName": "..." }` → devuelve el `UserId`.
2. **Login:** `POST /api/auth/login` con `{ "email": "...", "password": "..." }` → devuelve `accessToken`, `refreshToken`, `expiresInSeconds`, `userId`, `email`, `displayName`.
3. **Llamadas autenticadas:** Header `Authorization: Bearer <accessToken>` en las APIs que usen JWT (p. ej. Core API cuando se añada autorización).
4. **Refresco:** Cuando el access token caduque, `POST /api/auth/refresh` con `{ "refreshToken": "..." }` → nuevo par access + refresh token.

---

## Eventos de autenticación

- **UserLoggedInEventV1** (Shared.Contracts): UserId, Email, LoggedInAt.
- **IAuthEventPublisher**: el handler de Login llama a `PublishUserLoggedInAsync`. Por defecto está implementado con **NoOpAuthEventPublisher** (no envía a cola). Para publicar a RabbitMQ, añadir una implementación con MassTransit en Infrastructure y registrarla en lugar de NoOp.

---

## Documentación relacionada

- **COMO_PROBAR_FASE_2.md** – Guía paso a paso (prerrequisitos, base de datos, migraciones, Docker, curl, errores frecuentes).
- **NEXT_STEPS.md** – Estado del proyecto y próximos pasos (Fase 3, tests, mejoras).

## Próximos pasos (opcionales)

- Integrar proveedor externo (Google, Microsoft, etc.).
- Implementar **MassTransitAuthEventPublisher** para publicar UserLoggedInEventV1 a RabbitMQ.
- Proteger endpoints del Core API con `[Authorize]` y validar el JWT emitido por Identity (mismo Issuer/Audience y SecretKey).
