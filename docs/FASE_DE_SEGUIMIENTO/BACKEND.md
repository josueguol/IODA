# Tareas Backend (APIs y servicios)

Responsable: **Equipo Backend / APIs**.  
Referencia: [DIAGNOSTICO_TECNICO_CMS.md](../DIAGNOSTICO_TECNICO_CMS.md).

---

## Fase 1: Correcciones críticas (1–2 sprints)

### 1.1 Autorización en APIs expuestas

- [x] **Authorization API:** Añadir `[Authorize]` (o políticas concretas) a todos los endpoints. Definir roles/permisos requeridos (ej. admin para CRUD de roles/permisos).
- [x] **Publishing API:** Añadir `[Authorize]` a todos los endpoints. Definir permisos (ej. editor para solicitudes de publicación, aprobar/rechazar).
- [x] **Indexing API:** Añadir `[Authorize]` a todos los endpoints (indexar, eliminar del índice).
- [x] Documentar en cada API qué rol/permiso se requiere por endpoint.

**Archivos afectados (referencia):** Controllers de Authorization, Publishing e Indexing.

---

### 1.2 CORS

- [x] Dejar de usar `AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()` en todos los `Program.cs`.
- [x] Restringir CORS a orígenes conocidos (lista de URLs del frontend) en todos los servicios (Core, Identity, Authorization, Publishing, Indexing).
- [x] Asegurar que en producción solo se permitan orígenes explícitos (configuración por entorno).

---

### 1.3 Identity API — Separación de capas

- [x] Eliminar la dependencia de `AuthController` sobre `IUserRepository` y lógica de negocio en el controller.
- [x] Crear en **Identity.Application**:
  - `GetSetupStatusQuery` / `IsFirstUserQuery` (o equivalente) para “¿hay usuarios?”.
  - Handler que consulte si está habilitado el auto-registro (config) y si existe al menos un usuario.
- [x] Hacer que `AuthController` solo llame a MediatR con esas queries y devuelva el resultado; sin inyección de `IUserRepository` ni `IConfiguration` para esta lógica.
- [x] Mover la lógica de “primer usuario” y “auto-registro habilitado” a los handlers de Application.

**Archivos de referencia:** `src/Services/Identity/IODA.Identity.API/Controllers/AuthController.cs` (líneas 4, 16, 19, 35, 47).

---

### 1.4 Validación en startup (seguridad)

- [x] En todos los APIs (o al menos Core, Identity, Authorization): validar en startup que en entornos **no-Development** existan:
  - `Jwt:SecretKey` (cuando el servicio use JWT).
  - `ConnectionStrings:DefaultConnection` (cuando el servicio use base de datos).
- [x] Si faltan, **no arrancar** (lanzar excepción o `Environment.FailFast`). Evitar que las APIs queden sin protección efectiva por configuración faltante.

---

### 1.5 Secretos y configuración sensible

- [x] Sacar de `appsettings.json` (y de commits) los valores reales de: `Jwt:SecretKey`, `ConnectionStrings`, `RabbitMQ` (contraseñas), `Media:StoragePath`.
- [x] Usar **User Secrets** en desarrollo y variables de entorno o vault en producción.
- [x] Mantener en `appsettings.json` solo valores por defecto seguros para Development (o placeholders sin datos reales).

---

## Fase 2: Mejoras estructurales (2–3 sprints)

### 2.1 Middleware de errores compartido

- [x] Centralizar `ErrorHandlingMiddleware` en un paquete compartido (ej. `IODA.Shared.Api` o similar).
- [x] Diseñar un mapeo configurable de excepciones → ProblemDetails (por tipo o por convención) para que cada API registre solo sus excepciones de dominio.
- [x] Sustituir las cinco implementaciones actuales (Core, Identity, Authorization, Publishing, Indexing) por el uso del middleware compartido.

**Referencia:** `IODA.Shared.Api/Middleware/`; cada API usa `AddSharedErrorHandling(mapper)` y `UseMiddleware<ErrorHandlingMiddleware>()`. y equivalentes en los otros cuatro proyectos.

---

### 2.2 Excepciones de dominio “no encontrado”

- [x] Crear excepciones de dominio donde falten (ej. `SchemaNotFoundException`, y equivalentes para “parent schema”, “site” si aplica).
- [x] Sustituir en handlers el uso de `InvalidOperationException` para “no encontrado” por estas excepciones de dominio.
- [x] Registrar en el middleware compartido el mapeo a 404 (ProblemDetails) para estas excepciones.
- [x] Revisar **CreateContentCommandHandler** y otros que usen `throw new InvalidOperationException("... was not found.")`.

**Referencia:** `CreateContentCommandHandler.cs` (líneas 27–29), y otros handlers con “parent schema” y “site”.

---

### 2.3 Validación de upload de media

- [x] Crear `UploadMediaCommandValidator` (FluentValidation) para `UploadMediaCommand`.
- [x] Incluir validación de tipo/extensiones permitidas y tamaño (el límite de 50 MB ya existe; asegurar que el validador sea coherente).
- [ ] Opcional: validación de tipo MIME según política del proyecto.
- [x] Mantener o refinar la validación actual en `MediaController` (file null/empty, createdBy) para no duplicar lógica; el validador cubre el comando.

---

### 2.4 Contrato HTTP Publishing API

- [x] En el endpoint `GetPublicationRequests([FromQuery] PublicationRequestStatus? status)`, dejar de exponer el enum de dominio `PublicationRequestStatus` en el contrato HTTP.
- [x] Sustituir por un **string** en el contrato (query param: Pending | Approved | Rejected; frontend debe enviar string) y mapear internamente al enum de dominio en el controller.

**Referencia:** `PublishingController.cs`, línea 3 (`using IODA.Publishing.Domain.Entities`), línea 61.

---

### 2.5 Extensiones JWT y CORS reutilizables

- [x] Extraer la configuración repetida de JWT (SecretKey, Issuer, Audience) y CORS en extensiones reutilizables (ej. `AddJwtAuthentication(this IServiceCollection, IConfiguration)`, `AddDefaultCors`).
- [x] Ubicación: `IODA.Shared.Api/Extensions/` (JwtAuthenticationExtensions, CorsExtensions). o proyecto Shared que ya referencien los APIs.
- [x] Aplicar estas extensiones en Core; los demás APIs pueden migrar cuando convenga. (y en los demás si usan JWT/CORS) para reducir duplicación.

---

### 2.6 DTOs / Contracts en Authorization, Publishing e Indexing

- [x] Mover los request/response (records) que están definidos en el mismo archivo que los controllers a una carpeta **Contracts** o **DTOs** del API.
- [x] Mantener una separación clara entre contrato HTTP y lógica del controller (`*/Contracts/` en cada API).; mejorar reutilización y claridad.

---

## Fase 3: Optimización y refactorización avanzada

### 3.1 Warnings como errores

- [x] Activar `TreatWarningsAsErrors: true` en `Directory.Build.props` de forma progresiva (por proyecto o globalmente) y corregir warnings existentes.

---

### 3.2 SchemaValidationService (opcional)

- [x] Si la complejidad sigue creciendo: refactorizar `SchemaValidationService` a estrategias por tipo (ej. `IFieldValidator` + implementaciones por string, number, boolean, date, enum, etc.) para reducir el tamaño de la clase.

**Referencia:** `src/Services/Core/IODA.Core.Application/Services/SchemaValidationService.cs`; validadores en `Validators/Schema/`.

---

### 3.3 MediaController.GetFile — acceso público

- [x] Revisar el uso de `[AllowAnonymous]` en `MediaController.GetFile`.
- [x] Si es intencional (CDN/publicación): documentar en código y en documentación de arquitectura.

---

### 3.4 Consistencia en manejo de excepciones HTTP

- [x] Unificar el mapeo de `InvalidOperationException` y `ArgumentException` entre servicios. Convención en `IODA.Shared.Api.ExceptionMappingConvention`: ArgumentException → 400; InvalidOperationException con "already exists" → 409, resto → 400. Aplicada en Core, Authorization, Publishing, Indexing.

---

## Criterios de aceptación generales

- Ningún endpoint sensible (Authorization, Publishing, Indexing) accesible sin autenticación/autorización.
- Identity API sin dependencia API → Domain para repositorios; solo Application → Domain.
- CORS restringido a orígenes conocidos en no-Development.
- Startup falla en no-Development si faltan SecretKey o ConnectionString críticos.
- Secretos no commiteados; uso de User Secrets / variables de entorno / vault.
- Middleware de errores unificado y excepciones de dominio coherentes para “not found”.
- Validación de upload de media con FluentValidation y reglas de tipo/tamaño.
