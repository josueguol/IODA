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

- [ ] Centralizar `ErrorHandlingMiddleware` en un paquete compartido (ej. `IODA.Shared.Api` o similar).
- [ ] Diseñar un mapeo configurable de excepciones → ProblemDetails (por tipo o por convención) para que cada API registre solo sus excepciones de dominio.
- [ ] Sustituir las cinco implementaciones actuales (Core, Identity, Authorization, Publishing, Indexing) por el uso del middleware compartido.

**Referencia:** `IODA.Core.API/Middleware/ErrorHandlingMiddleware.cs` y equivalentes en los otros cuatro proyectos.

---

### 2.2 Excepciones de dominio “no encontrado”

- [ ] Crear excepciones de dominio donde falten (ej. `SchemaNotFoundException`, y equivalentes para “parent schema”, “site” si aplica).
- [ ] Sustituir en handlers el uso de `InvalidOperationException` para “no encontrado” por estas excepciones de dominio.
- [ ] Registrar en el middleware compartido el mapeo a 404 (ProblemDetails) para estas excepciones.
- [ ] Revisar **CreateContentCommandHandler** y otros que usen `throw new InvalidOperationException("... was not found.")`.

**Referencia:** `CreateContentCommandHandler.cs` (líneas 27–29), y otros handlers con “parent schema” y “site”.

---

### 2.3 Validación de upload de media

- [ ] Crear `UploadMediaCommandValidator` (FluentValidation) para `UploadMediaCommand`.
- [ ] Incluir validación de tipo/extensiones permitidas y tamaño (el límite de 50 MB ya existe; asegurar que el validador sea coherente).
- [ ] Opcional: validación de tipo MIME según política del proyecto.
- [ ] Mantener o refinar la validación actual en `MediaController` (file null/empty, createdBy) para no duplicar lógica; el validador debe cubrir el comando.

---

### 2.4 Contrato HTTP Publishing API

- [ ] En el endpoint `GetPublicationRequests([FromQuery] PublicationRequestStatus? status)`, dejar de exponer el enum de dominio `PublicationRequestStatus` en el contrato HTTP.
- [ ] Sustituir por un **string** o **DTO** en el contrato (ej. query param string con valores conocidos) y mapear internamente al enum de dominio en Application.

**Referencia:** `PublishingController.cs`, línea 3 (`using IODA.Publishing.Domain.Entities`), línea 61.

---

### 2.5 Extensiones JWT y CORS reutilizables

- [ ] Extraer la configuración repetida de JWT (SecretKey, Issuer, Audience) y CORS en extensiones reutilizables (ej. `AddJwtAuthentication(this IServiceCollection, IConfiguration)`, `AddDefaultCors`).
- [ ] Ubicación: paquete compartido (ej. `IODA.Shared.Api`) o proyecto Shared que ya referencien los APIs.
- [ ] Aplicar estas extensiones en Core, Identity y Authorization (y en los demás si usan JWT/CORS) para reducir duplicación.

---

### 2.6 DTOs / Contracts en Authorization, Publishing e Indexing

- [ ] Mover los request/response (records) que están definidos en el mismo archivo que los controllers a una carpeta **Contracts** o **DTOs** del API.
- [ ] Mantener una separación clara entre contrato HTTP y lógica del controller; mejorar reutilización y claridad.

---

## Fase 3: Optimización y refactorización avanzada

### 3.1 Warnings como errores

- [ ] Activar `TreatWarningsAsErrors: true` en `Directory.Build.props` de forma progresiva (por proyecto o globalmente) y corregir warnings existentes.

---

### 3.2 SchemaValidationService (opcional)

- [ ] Si la complejidad sigue creciendo: refactorizar `SchemaValidationService` a estrategias por tipo (ej. `IFieldValidator` + implementaciones por string, number, boolean, date, enum, etc.) para reducir el tamaño de la clase.

**Referencia:** `src/Services/Core/IODA.Core.Application/Services/SchemaValidationService.cs` (~250 líneas).

---

### 3.3 MediaController.GetFile — acceso público

- [ ] Revisar el uso de `[AllowAnonymous]` en `MediaController.GetFile`.
- [ ] Si el contenido no es público: implementar signed URLs o token de acceso corto y documentar la decisión.
- [ ] Si es intencional (CDN/publicación): documentar en código y en documentación de arquitectura.

---

### 3.4 Consistencia en manejo de excepciones HTTP

- [ ] Unificar el mapeo de `InvalidOperationException` y `ArgumentException` entre servicios (Core devuelve 400 para ambos; Authorization 409 para InvalidOperationException). Definir convención (ej. 400 vs 409) y aplicarla en el middleware compartido.

---

## Criterios de aceptación generales

- Ningún endpoint sensible (Authorization, Publishing, Indexing) accesible sin autenticación/autorización.
- Identity API sin dependencia API → Domain para repositorios; solo Application → Domain.
- CORS restringido a orígenes conocidos en no-Development.
- Startup falla en no-Development si faltan SecretKey o ConnectionString críticos.
- Secretos no commiteados; uso de User Secrets / variables de entorno / vault.
- Middleware de errores unificado y excepciones de dominio coherentes para “not found”.
- Validación de upload de media con FluentValidation y reglas de tipo/tamaño.
