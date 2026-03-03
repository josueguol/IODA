# DIAGNÓSTICO TÉCNICO DEL CMS

## 1. Resumen Ejecutivo

| Aspecto | Valoración |
|--------|------------|
| **Estado general del proyecto** | **Riesgoso** |
| **Nivel de deuda técnica estimado** | **Alto** |
| **Recomendación general** | No desplegar en producción sin abordar seguridad y tests. Priorizar autorización en APIs, eliminación de violaciones de capas y pruebas automatizadas. |

**Riesgos principales**
- Tres APIs (Authorization, Publishing, Indexing) expuestas sin autenticación ni autorización.
- Cobertura de tests inexistente (no hay proyectos de test en el repositorio).
- Violación de Clean Architecture en Identity API (Controller depende de Domain/Repositories).
- CORS con `AllowAnyOrigin()` en todos los servicios.
- Secretos y rutas sensibles en `appsettings.json` (JWT, ConnectionStrings, Media.StoragePath).
- Código duplicado en middleware de errores en cinco servicios.

---

## 2. Evaluación de Arquitectura

### ¿Se respeta la separación por capas?
**Parcialmente.** La estructura Domain / Application / Infrastructure / API se sigue en Core, Identity, Authorization, Publishing e Indexing. Sin embargo:

- **Identity API** referencia **Identity.Domain** y el `AuthController` inyecta `IUserRepository` y llama directamente a `_userRepository.AnyAsync()` para `GetSetupStatus` y para la lógica de auto-registro. La capa de presentación no debe depender de repositorios del dominio; esa responsabilidad debe estar en Application (por ejemplo, un `GetSetupStatusQuery` / `IsFirstUserQuery`).
- **Publishing API** referencia **Publishing.Domain** y el controller usa el tipo de dominio `PublicationRequestStatus` en la firma del endpoint `GetPublicationRequests([FromQuery] PublicationRequestStatus? status)`. La API expone un enum del dominio; debería usar un DTO o string en el contrato HTTP.

**Ejemplos concretos:**
- `src/Services/Identity/IODA.Identity.API/Controllers/AuthController.cs`: líneas 4, 16, 19, 35, 47 — uso de `IUserRepository` e `IConfiguration` para lógica que debería vivir en Application.
- `src/Services/Publishing/IODA.Publishing.API/Controllers/PublishingController.cs`: línea 3 `using IODA.Publishing.Domain.Entities`, línea 61 `PublicationRequestStatus? status`.

### ¿Existen dependencias incorrectas entre módulos?
- **API → Domain:** Core.API, Identity.API, Authorization.API, Publishing.API referencian su respectivo Domain. En Core y Authorization es principalmente para que `ErrorHandlingMiddleware` conozca excepciones de dominio (ContentNotFoundException, SchemaValidationException, etc.). En Identity y Publishing, además se usa para repositorio y tipos de request. La dependencia API → Domain para *solo* excepciones es discutible; lo ideal sería que el middleware trate excepciones por tipo base o que las excepciones de dominio se reexporten desde Application/Contracts.
- **Application → Domain:** Correcto. Application orquesta y usa entidades y repositorios del dominio.
- **Infrastructure → Domain / Application:** Correcto.

### ¿Hay violaciones de principios SOLID?
- **SRP:** `AuthController` mezcla presentación HTTP con decisión de negocio (si hay usuarios, si el auto-registro está habilitado). Esa lógica debería estar en un use case.
- **DIP:** Identity.API depende de la abstracción `IUserRepository` pero también de la implementación concreta (registrada en Infrastructure). El problema no es DIP en sí, sino que la capa de entrada (API) no debería depender del contrato de persistencia; debe depender solo de Application.
- **ISP:** No se detectan interfaces sobredimensionadas.

### ¿Existen clases con demasiadas responsabilidades?
- **SchemaValidationService** (`src/Services/Core/IODA.Core.Application/Services/SchemaValidationService.cs`): ~250 líneas, valida por tipo (string, number, boolean, date, enum, etc.). Está bien dividido en métodos privados por tipo, pero la clase es grande; podría extraerse por tipo de validación (IFieldValidator + implementaciones) si se quiere reducir complejidad.
- **Content** (`src/Services/Core/IODA.Core.Domain/Entities/Content.cs`): entidad rica con muchos métodos y eventos de dominio en el mismo archivo; aceptable en DDD pero el archivo es largo (~290 líneas).

---

## 3. Calidad de Código

### Métodos excesivamente largos
- `ErrorHandlingMiddleware.HandleExceptionAsync` en Core (líneas 37–99): switch con muchos casos. Podría extraerse un `IExceptionToProblemDetailsMapper` o un diccionario de mapeo por tipo de excepción.
- `SchemaValidationService.Validate`: método principal corto; los privados por tipo están acotados.

### Clases demasiado grandes
- `SchemaValidationService`: ya citada.
- `Content.cs` (Domain): entidad + 5 records de eventos en el mismo archivo.

### Código duplicado
- **ErrorHandlingMiddleware** implementado de forma casi idéntica en cinco proyectos:
  - `IODA.Core.API/Middleware/ErrorHandlingMiddleware.cs`
  - `IODA.Identity.API/Middleware/ErrorHandlingMiddleware.cs`
  - `IODA.Authorization.API/Middleware/ErrorHandlingMiddleware.cs`
  - `IODA.Publishing.API/Middleware/ErrorHandlingMiddleware.cs`
  - `IODA.Indexing.API/Middleware/ErrorHandlingMiddleware.cs`
  Cada uno adapta el `switch` a las excepciones de su dominio. Debería centralizarse en un paquete compartido (por ejemplo `IODA.Shared.Api` o similar) con un mapeo configurable o por convención.

### Uso inconsistente de patrones
- Handlers lanzan tanto excepciones de dominio (ContentNotFoundException, SchemaValidationException) como `InvalidOperationException` para “no encontrado” (ej. schema no encontrado en `CreateContentCommandHandler`, líneas 27–29). Debería usarse una excepción de dominio (ej. `SchemaNotFoundException`) para consistencia y para que el middleware devuelva 404 de forma uniforme.
- **CreateContentCommandHandler** (`src/Services/Core/IODA.Core.Application/Commands/Content/CreateContentCommandHandler.cs`): `throw new InvalidOperationException($"Schema with ID '{request.SchemaId}' was not found.")` — mismo patrón en otros handlers con “parent schema” y “site”.

### Problemas de naming
- Naming en general es claro (Commands, Queries, DTOs, Handlers). Sin hallazgos graves.

### Uso incorrecto de Optional o manejo de null
- Nullable reference types están habilitados. Se usa `result is null` / `??` de forma coherente.
- En `CreateContentSchemaCommandHandler` se usa `request.ParentSchemaId.Value` sin comprobar `HasValue` antes (líneas 31–34); si el validador garantiza que cuando se usa está presente, es aceptable, pero conviene revisar que el validator cubra ese caso.
- No se usa un tipo Optional explícito; se usan `Guid?` y comprobaciones de null.

### Inconsistencias en manejo de excepciones
- Core `ErrorHandlingMiddleware` convierte `ArgumentException` e `InvalidOperationException` en 400; Authorization convierte `InvalidOperationException` en 409 (Conflict). Mismo tipo de excepción, distinto código HTTP según servicio.
- En desarrollo se expone `exception.Message` en 500; en producción solo "An unexpected error occurred." — correcto para no filtrar información sensible.

---

## 4. Convenciones y Estandarización

### ¿Se siguen convenciones consistentes?
- Sí en C#: PascalCase, interfaces con `I`, namespaces por capa/feature. README menciona convenciones (Commits, eventos, nomenclatura).

### ¿Estructura de carpetas coherente?
- Sí. Cada servicio tiene Domain, Application, Infrastructure, API con subcarpetas (Commands, Queries, Validators, Behaviors, Persistence, etc.).

### ¿DTOs correctamente separados?
- En Core, los DTOs están en Application (ContentDto, PagedResultDto, etc.) y los requests del API en los controllers como records (CreateContentRequest, UpdateContentRequest, etc.). Mezcla aceptable.
- En Authorization, Publishing e Indexing, los request/response están definidos en el mismo archivo que el controller (records al final del archivo). Sería preferible moverlos a una carpeta Contracts o DTOs del API para claridad y reutilización.

### ¿Separación clara entre lógica de negocio y presentación?
- No del todo en Identity: la lógica “¿hay usuarios?” y “¿está habilitado el auto-registro?” está en el controller. Debería estar en Application.
- En el resto de servicios, los controllers solo envían Commands/Queries vía MediatR y devuelven resultados; la separación es correcta.

---

## 5. Seguridad

### Endpoints sin validación adecuada
- **Authorization, Publishing, Indexing:** ningún endpoint tiene `[Authorize]`. Cualquier cliente puede crear roles, permisos, reglas de acceso, solicitudes de publicación, aprobar/rechazar, indexar y eliminar del índice. **Crítico.**
- Core y Identity (excepto login/register/refresh/setup-status) están protegidos con `[Authorize]`.

### Validaciones ausentes
- **UploadMediaCommand** no tiene FluentValidator registrado. La validación es manual en `MediaController` (file null/empty, createdBy). Debería existir `UploadMediaCommandValidator` y validación de tipo/tamaño de archivo (extensiones permitidas, límite ya existe con `RequestSizeLimit(50*1024*1024)`).
- Los comandos de Content, Schemas, Sites, etc. sí tienen validadores.

### Manejo incorrecto de autenticación/autorización
- Si `Jwt:SecretKey` está vacío o no está configurado, en Core, Identity y Authorization **no se registra** `AddAuthentication`/`AddAuthorization`. Las APIs quedan sin protección efectiva (en Identity no se podría firmar el token; en Core/Authorization no se validaría). Debería fallar el arranque si en producción no hay SecretKey.
- **MediaController.GetFile** tiene `[AllowAnonymous]`: la descarga del archivo binario es pública. Si el contenido es sensible, esto es un riesgo; si es solo para CDN/publicación, podría ser intencional pero debería documentarse y/o restringirse por signed URL o token corto.

### Posibles vulnerabilidades evidentes
- **CORS:** todos los `Program.cs` usan `AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()`. En producción debe restringirse a orígenes conocidos.
- **Secretos en configuración:** `appsettings.json` de Core contiene `ConnectionStrings`, `Jwt:SecretKey`, `RabbitMQ` con contraseñas y `Media:StoragePath` con ruta absoluta local. No deben subirse a repositorio con valores reales; usar User Secrets o variables de entorno y valores por defecto solo en Development.
- **docker-compose.yml:** contraseñas y `Jwt__SecretKey` en texto plano en `environment`. Aceptable para desarrollo local; en producción usar secrets de Docker o un vault.

### Exposición innecesaria de información sensible
- En 500 solo se devuelve mensaje genérico en no-Development; correcto. Los ProblemDetails de 4xx pueden incluir mensajes de validación; aceptable.

---

## 6. Testing

### Cobertura estimada
- **0%.** No existe ningún proyecto `*Tests` o `*.UnitTests` en la solución. La búsqueda de `.Tests` en `.csproj` no devuelve resultados. `Directory.Build.props` define paquetes para proyectos que contengan "Test" en el nombre, pero no hay ninguno.

### Módulos críticos sin pruebas
- Toda la solución: Domain (entidades, value objects), Application (handlers, validadores, SchemaValidationService), Infrastructure (repositorios, CorePublishClient, mensajería), API (controladores no probados de forma aislada). Los flujos de publicación, autorización, indexación y contenido no tienen tests automatizados.

### Tests frágiles o mal estructurados
- N/A (no hay tests).

### Recomendaciones de mejora
1. Añadir proyectos de test por servicio (o al menos para Core y Identity): unit tests para handlers, validadores y dominio; integration tests para API y persistencia.
2. Priorizar: CreateContentCommandHandler, Publish/Unpublish, ApprovePublicationCommandHandler, Login/Register, CheckAccessQuery, SchemaValidationService.
3. Usar TestContainers para PostgreSQL (y opcionalmente RabbitMQ/Elasticsearch) en integration tests.
4. Configurar cobertura (coverlet) y umbral mínimo en CI.

---

## 7. Infraestructura y Configuración

### Dockerfiles correctos o mejorables
- Los cinco Dockerfiles (Core, Identity, Authorization, Publishing, Indexing) siguen el mismo patrón: multi-stage con `dotnet/sdk:9.0` para build y `aspnet:9.0` para runtime. Copian `global.json`, `Directory.Build.props`, `src/Shared/` y su servicio. **Correctos.** Podría estandarizarse en un Dockerfile base o script para reducir duplicación si se añaden pasos comunes (ej. healthcheck, usuario no root).

### Variables de entorno bien gestionadas
- En `Program.cs` se lee configuración con `builder.Configuration["..."]`. En docker-compose las variables están definidas por servicio. No hay validación al arranque (ej. `Options` pattern con validación); si falta `ConnectionStrings__DefaultConnection` o `Jwt:SecretKey`, el fallo puede ser tardío.
- Frontend: `config/env.ts` usa `import.meta.env` (Vite) con fallbacks a localhost; puertos en frontend (5269–5273) no coinciden con los del docker-compose (5001–5005). Puede ser intencional (dev sin Docker vs dev con Docker).

### Configuraciones repetidas
- JWT (SecretKey, Issuer, Audience) y CORS se repiten en Core, Identity y Authorization con el mismo código. Podría extraerse una extensión `AddJwtAuthentication(this IServiceCollection, IConfiguration)` y `AddDefaultCors` en un paquete compartido.
- Swagger con Bearer se repite en todos los APIs; mismo candidato a extensión compartida.

### Problemas potenciales en despliegue
- **docker-compose:** todos los servicios tienen `depends_on: []`. No se declara dependencia de postgres ni rabbitmq; si se levanta el stack completo, los APIs pueden arrancar antes que la base de datos y fallar. Debería usarse `depends_on` con condition (ej. service_healthy) cuando se definan postgres/rabbitmq en el mismo compose.
- **Media.StoragePath** en appsettings de Core: ruta absoluta local (`/Users/.../_temp/ioda-media`). En contenedor no existirá; debe configurarse por variable de entorno o volumen.

---

## 8. Deuda Técnica Detectada

### Alta
1. **APIs Authorization, Publishing e Indexing sin [Authorize].** Cualquier cliente puede ejecutar operaciones sensibles.
2. **Ausencia total de tests.** Imposible refactorizar con seguridad y alto riesgo de regresiones.
3. **Identity API depende de IUserRepository en el controller.** Rompe la separación de capas y dificulta testing y evolución.
4. **CORS AllowAnyOrigin** en todos los servicios.
5. **Secretos y rutas sensibles en appsettings.json** (JWT, ConnectionStrings, Media.StoragePath) susceptibles de commit.

### Media
6. **ErrorHandlingMiddleware duplicado** en cinco proyectos con lógica muy similar.
7. **Uso de InvalidOperationException** en lugar de excepciones de dominio para “no encontrado” en varios handlers.
8. **Falta de UploadMediaCommandValidator** y validación de tipo de archivo.
9. **Publishing API expone PublicationRequestStatus** (tipo de dominio) en el contrato HTTP.
10. **JWT opcional:** si no hay SecretKey, la API arranca sin autenticación; debería fallar en producción.
11. **Request/response DTOs** de Authorization, Publishing, Indexing en el mismo archivo que el controller; mejor en Contracts/DTOs.

### Baja
12. **TreatWarningsAsErrors: false** en Directory.Build.props; recomendable activarlo progresivamente.
13. **Configuración de test** en Directory.Build.props sin proyectos de test existentes.
14. **Content y ContentSchema:** archivos largos; podría extraerse eventos de dominio a archivos separados por legibilidad.
15. **Docker compose** sin `depends_on` hacia postgres/rabbitmq cuando se usen en el mismo compose.

---

## 9. Recomendaciones Prioritarias (Plan de Acción)

### Fase 1: Correcciones críticas (1–2 sprints)
1. Añadir `[Authorize]` (o políticas concretas) a todos los endpoints de Authorization, Publishing e Indexing. Definir qué roles/permisos se requieren (ej. admin para CRUD de roles/permisos, editor para publicación).
2. Restringir CORS a orígenes conocidos (lista de URLs del frontend) y no usar `AllowAnyOrigin` en producción.
3. Eliminar la dependencia del AuthController sobre IUserRepository: crear `GetSetupStatusQuery`/`IsFirstUserQuery` en Application y que el controller solo llame a MediatR. Mover la lógica de “primer usuario” y “auto-registro habilitado” a los handlers.
4. Validar en startup que en entornos no-Development existan `Jwt:SecretKey` y `ConnectionStrings:DefaultConnection`; si no, lanzar y no arrancar.
5. Sacar secretos y rutas sensibles de appsettings.json (User Secrets en dev, variables de entorno o vault en producción). No commitear valores reales.

### Fase 2: Mejoras estructurales (2–3 sprints)
6. Introducir proyectos de test (unit + integration) para al menos Core e Identity; cubrir handlers críticos, validadores y SchemaValidationService. Configurar cobertura en CI.
7. Centralizar ErrorHandlingMiddleware en un paquete compartido (ej. IODA.Shared.Api) con mapeo de excepciones a ProblemDetails configurable por servicio.
8. Estandarizar excepciones “no encontrado”: crear SchemaNotFoundException, etc., y usarlas en lugar de InvalidOperationException en handlers; registrar en el middleware compartido.
9. Añadir UploadMediaCommandValidator y validación de extensiones/tipo MIME para uploads.
10. Sustituir en el contrato de Publishing el enum PublicationRequestStatus por un string o DTO en el endpoint GetPublicationRequests.
11. Extraer configuración JWT y CORS a extensiones reutilizables en un Shared o en cada API.

### Fase 3: Optimización y refactorización avanzada
12. Activar TreatWarningsAsErrors y corregir warnings.
13. Refactorizar SchemaValidationService a estrategias por tipo (IFieldValidator) si la complejidad sigue creciendo.
14. Definir health checks y `depends_on` con condiciones en docker-compose cuando se incluyan postgres y rabbitmq.
15. Revisar AllowAnonymous en MediaController.GetFile: si el contenido no es público, implementar signed URLs o token de acceso corto.
16. Documentar arquitectura y convenciones (ARCHITECTURE.md, CONVENTIONS.md) como indica el README.

---

## 10. Evaluación de Escalabilidad

### ¿El proyecto está preparado para crecer?
**Parcialmente.** La arquitectura por servicios (Core, Identity, Authorization, Publishing, Indexing) y el uso de MediatR, eventos y mensajería (RabbitMQ) permiten escalar horizontalmente cada API y desacoplar dominios. Sin embargo:

- La ausencia de tests hace que cualquier cambio sea arriesgado y frena la evolución.
- Las violaciones de capas (Identity API → Domain) y la duplicación de middleware aumentan el coste de mantener y extender el sistema.
- No hay rate limiting ni circuit breaker en las llamadas entre servicios (ej. Publishing → Core API); un Core lento o caído puede propagar fallos.

### ¿Qué áreas podrían colapsar primero?
1. **Core API:** punto central de contenido, schemas y media; si no escala o la base de datos se satura, todo el flujo de edición y publicación se resiente.
2. **Publishing → Core:** llamada HTTP síncrona en `ApprovePublicationCommandHandler`; si Core tiene latencia alta o errores, las aprobaciones fallan y la cola de solicitudes no avanza.
3. **Indexing:** si Elasticsearch no está o está saturado, el índice se desactualiza; el consumo de eventos RabbitMQ puede acumular cola.
4. **Identity:** único emisor de JWT; cuello de botella para login/refresh en picos de tráfico.
5. **Almacenamiento de media:** actualmente en disco local (`Media.StoragePath`); en múltiples instancias o en contenedores efímeros no es escalable; debería considerarse almacenamiento distribuido (S3, blob).

---

*Documento generado como base para agentes de mejora automática. Revisar y actualizar tras cambios en el código.*
