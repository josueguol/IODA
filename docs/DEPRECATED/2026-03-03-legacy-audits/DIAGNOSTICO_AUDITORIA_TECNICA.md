# DIAGNÓSTICO DE AUDITORÍA TÉCNICA — IODA CMS

**Alcance:** Código fuente completo del repositorio (backend .NET, frontend React/Vite, configuración, Docker).  
**Criterio:** Evidencia en código; sin suposiciones. Problemas priorizados por impacto real en mantenibilidad, escalabilidad, seguridad y riesgo operativo.

---

## 1. Resumen Ejecutivo

| Aspecto | Valoración |
|--------|------------|
| **Estado general** | **Mejorable** — Varias correcciones críticas ya aplicadas (autorización, CORS, startup, Identity). Persisten deuda estructural y de pruebas. |
| **Deuda técnica** | **Media–Alta** |
| **Recomendación** | No desplegar en producción sin tests automatizados. Priorizar excepciones de dominio (404), middleware compartido, validación de media y documentación de contratos. |

**Riesgos actuales (verificados en código)**  
- Cobertura de tests **0%** (no existen proyectos `*Test*.csproj` en la solución; `IODA.sln` declara carpeta `tests` sin proyectos).  
- **Publishing API** expone tipo de dominio `PublicationRequestStatus` en el contrato HTTP (`PublishingController.cs` línea 66).  
- **ErrorHandlingMiddleware** duplicado en cinco APIs con mapeos distintos (Core 400 para `InvalidOperationException`, Authorization 409).  
- Handlers de Core lanzan `InvalidOperationException` para “recurso no encontrado” (schema, site, project, environment) → respuesta **400** en lugar de **404**.  
- **UploadMediaCommand** sin FluentValidator; validación solo en controller; no hay validación de extensiones/tipo MIME en capa Application.  
- **MediaController.GetFile** con `[AllowAnonymous]` sin documentar si es intencional (CDN) o debe restringirse.  
- **docker-compose**: `depends_on: []` en todos los servicios; no se declaran dependencias de postgres/rabbitmq/elasticsearch.

**Correcciones ya aplicadas (verificadas)**  
- Authorization, Publishing e Indexing tienen `[Authorize]` (o política `Editor`) a nivel de controller.  
- Identity **AuthController** solo usa `IMediator` y `GetSetupStatusQuery` (sin `IUserRepository` en controller).  
- CORS: los cinco `Program.cs` usan `WithOrigins(allowedOrigins)` desde configuración, con fallback solo en Development.  
- Validación en startup: en no-Development se exige `Jwt:SecretKey` y, donde aplica, `ConnectionStrings:DefaultConnection`; si faltan, el API no arranca.  
- `appsettings.json` (Core) usa placeholders `REPLACE_OR_USE_USER_SECRETS`; no se encontraron secretos reales en repositorio.

---

## 2. Arquitectura Predominante

**Patrón estructural:** **Clean Architecture** por servicio, con **microservicios** (Core, Identity, Authorization, Publishing, Indexing).

**Evidencia:**  
- `IODA.sln`: comentario explícito "Clean Architecture + DDD + Microservices".  
- Cada servicio contiene capas: **Domain**, **Application**, **Infrastructure**, **API**.  
- Application usa MediatR (Commands/Queries/Handlers), FluentValidation, y orquesta Domain e Infrastructure.  
- Domain contiene entidades, value objects, excepciones y contratos de repositorio; sin referencias a infraestructura.

**Coherencia interna:**  
- Dependencias entre capas son coherentes: Application → Domain; Infrastructure → Application/Domain; API → Application, Infrastructure y (en todos) Domain.  
- La referencia **API → Domain** existe en los cinco servicios. En Core, Identity, Authorization y Publishing se usa para: (1) excepciones de dominio en `ErrorHandlingMiddleware`; (2) en Publishing además para el tipo `PublicationRequestStatus` en la firma del endpoint. Solo (2) es violación de contrato HTTP; (1) es un trade-off razonable si no se reexportan excepciones desde Application.

---

## 3. Evaluación por Áreas

### 3.1 Seguridad

| Hallazgo | Severidad | Evidencia |
|----------|-----------|-----------|
| Endpoints sensibles protegidos | — | Authorization, Publishing, Indexing: `[Authorize]` o `[Authorize(Policy = "Editor")]` a nivel controller. `src/Services/*/IODA.*.API/Controllers/*.cs`. |
| CORS restringido por configuración | — | Los cinco `Program.cs` usan `Cors:AllowedOrigins` y `WithOrigins(allowedOrigins)`; en Development sin orígenes configurados usan localhost:3000/5173. |
| Startup falla sin JWT/ConnectionString en no-Development | — | Core, Identity, Authorization, Publishing: bloque que lanza `InvalidOperationException` si faltan. Indexing: solo JWT. |
| Secretos en appsettings | Baja | Core `appsettings.json`: placeholders; no hay valores reales. Riesgo: que alguien sustituya por valores reales y haga commit. |
| GetFile AllowAnonymous | Oportunidad | `src/Services/Core/IODA.Core.API/Controllers/MediaController.cs` línea 92. No documentado si es intencional (CDN) o debe pasar a signed URL/token. |
| JWT opcional en Development | Preferencia | Si `Jwt:SecretKey` está vacío no se registra `AddAuthentication`; en Development puede ser aceptable; en no-Development ya se evita con validación de startup. |

### 3.2 Separación de capas y contratos

| Hallazgo | Severidad | Evidencia |
|----------|-----------|-----------|
| Identity API sin lógica de negocio en controller | — | `AuthController` solo envía `GetSetupStatusQuery` y comandos vía MediatR. `src/Services/Identity/IODA.Identity.API/Controllers/AuthController.cs`. |
| Publishing expone tipo de dominio en HTTP | **Problema** | `GetPublicationRequests([FromQuery] PublicationRequestStatus? status)`. `using IODA.Publishing.Domain.Entities` en controller. Archivo: `src/Services/Publishing/IODA.Publishing.API/Controllers/PublishingController.cs` líneas 3 y 66. **Causa raíz:** el contrato HTTP está acoplado al dominio; debería ser string o DTO. |
| API → Domain para excepciones en middleware | Preferencia | Todos los APIs referencian Domain; el middleware usa excepciones de dominio. Alternativa: reexportar desde Application o paquete compartido. No se considera error si se asume que “excepciones” son parte del contrato de presentación. |

### 3.3 Calidad de código

| Hallazgo | Severidad | Evidencia |
|----------|-----------|-----------|
| InvalidOperationException para “not found” | **Problema** | CreateContentCommandHandler: `throw new InvalidOperationException($"Schema with ID '{request.SchemaId}' was not found.")` (línea 28). CreateSiteCommandHandler, CreateEnvironmentCommandHandler, UploadMediaCommandHandler, UpdateContentCommandHandler: mismo patrón para project/environment/site/schema. Core middleware mapea `InvalidOperationException` → **400**. **Causa raíz:** no existen `SchemaNotFoundException`, `ProjectNotFoundException`, `EnvironmentNotFoundException` en Core.Domain; solo `ContentNotFoundException` y `SiteNotFoundException`. El cliente recibe 400 en lugar de 404. |
| Inconsistencia HTTP para InvalidOperationException | **Problema** | Core: 400; Authorization: 409. `IODA.Core.API/Middleware/ErrorHandlingMiddleware.cs` línea 71 (400); `IODA.Authorization.API/Middleware/ErrorHandlingMiddleware.cs` línea 41 (409). Mismo tipo de excepción, distinto código HTTP. |
| ErrorHandlingMiddleware duplicado | **Problema** | Cinco implementaciones: Core, Identity, Authorization, Publishing, Indexing. Estructura y flujo similares; mapeo de excepciones distinto por servicio. Archivos: `*/IODA.*.API/Middleware/ErrorHandlingMiddleware.cs`. **Causa raíz:** no hay paquete compartido para middleware de errores. |
| UploadMediaCommand sin validator | **Problema** | No existe `UploadMediaCommandValidator`. Validación en `MediaController.Upload`: file null/empty, createdBy. No hay validación de extensiones ni MIME en Application. `src/Services/Core/IODA.Core.API/Controllers/MediaController.cs`; `IODA.Core.Application/Commands/Media/UploadMediaCommand.cs`. |
| SchemaValidationService grande | Oportunidad | ~250 líneas, switch por tipo de campo. Métodos privados por tipo. Refactor a estrategias (IFieldValidator) sería mejora de mantenibilidad, no obligatorio. |
| Content.cs largo | Preferencia | Entidad + eventos de dominio en un archivo; aceptable en DDD. |

### 3.4 Testing

| Hallazgo | Severidad | Evidencia |
|----------|-----------|-----------|
| Cobertura 0% | **Crítico** | No hay proyectos `*Test*.csproj`. Búsqueda de `Test` en `.csproj` y solución: carpeta `tests` en solución sin proyectos. `Directory.Build.props` define paquetes para proyectos que contengan "Test" pero no hay ninguno. |
| Módulos sin pruebas | **Crítico** | Handlers, validadores, SchemaValidationService, repositorios, controladores y flujos entre servicios sin tests automatizados. |

### 3.5 Infraestructura y configuración

| Hallazgo | Severidad | Evidencia |
|----------|-----------|-----------|
| docker-compose depends_on vacío | **Problema** | Todos los servicios tienen `depends_on: []`. Si postgres/rabbitmq/elasticsearch se añaden al mismo compose, los APIs pueden arrancar antes que las dependencias. `docker-compose.yml` por servicio. |
| Media.StoragePath en contenedor | Oportunidad | Core `appsettings.json`: `Media.StoragePath: "./temp/ioda-media"`. En contenedor debe configurarse por variable de entorno o volumen para persistencia y rutas válidas. |
| Secretos en compose | Preferencia | `docker-compose.yml`: contraseñas y JWT en `environment`. Aceptable para desarrollo; en producción deben usarse secrets/vault. |

### 3.6 Frontend

| Hallazgo | Severidad | Evidencia |
|----------|-----------|-----------|
| Clientes con JWT | — | Core, Authorization, Publishing, Indexing y Identity (admin) usan `createAuthAwareHttpClient` con `getAccessToken` y `refreshSession`. `frontend/src/modules/*/api/*.ts` y `shared/api/auth-aware-client.ts`. |
| Puertos por defecto | Preferencia | `config/env.ts`: puertos 5269–5273; docker-compose expone 5001–5005. Puede ser intencional (dev local vs Docker). |

---

## 4. Clasificación de Hallazgos

### Críticos (impacto en seguridad, corrección o riesgo operativo)

1. **Ausencia total de tests** — Imposible refactorizar con seguridad; alto riesgo de regresiones.  
2. **(Ningún otro crítico verificado)** — Autorización, CORS y validación de startup ya están abordados.

### Problemas (impacto en mantenibilidad, semántica HTTP o consistencia)

3. **Publishing expone `PublicationRequestStatus` (dominio) en el contrato HTTP.**  
4. **Handlers usan `InvalidOperationException` para “not found”** → 400 en lugar de 404; falta de excepciones de dominio (Schema, Project, Environment).  
5. **Inconsistencia InvalidOperationException** entre Core (400) y Authorization (409).  
6. **ErrorHandlingMiddleware duplicado** en cinco proyectos.  
7. **UploadMediaCommand sin FluentValidator** y sin validación de tipo/extensiones en Application.  
8. **docker-compose sin `depends_on`** hacia bases de datos y colas.

### Oportunidades de mejora

9. **MediaController.GetFile** [AllowAnonymous]: documentar o restringir (signed URL/token).  
10. **SchemaValidationService**: refactor opcional a estrategias por tipo.  
11. **Media.StoragePath** en contenedor: variable de entorno o volumen.  
12. **JWT/CORS**: extraer a extensiones compartidas para reducir duplicación en Program.cs.

### Preferencias de estilo

13. **API → Domain** solo para excepciones en middleware; alternativa: reexportar desde Application.  
14. **TreatWarningsAsErrors: false** en `Directory.Build.props`; activar progresivamente.  
15. **Request/response** de Authorization/Publishing/Indexing en el mismo archivo que el controller; mover a Contracts/DTOs mejora claridad.

---

## 5. Tareas Técnicas Derivadas

Lista accionable para agentes de implementación. Cada ítem es específico, medible y acotado a archivos concretos.

### 5.1 Backend — Contratos y capas

| ID | Tarea | Archivos concretos |
|----|--------|---------------------|
| T1 | Sustituir en el contrato HTTP de `GetPublicationRequests` el tipo `PublicationRequestStatus` por un query param **string** (ej. "Pending", "Approved", "Rejected") o DTO; mapear en el handler/query al enum de dominio. | `src/Services/Publishing/IODA.Publishing.API/Controllers/PublishingController.cs` (firma del endpoint y uso de `GetPublicationRequestsQuery`). |
| T2 | Crear en Core.Domain excepciones: `SchemaNotFoundException`, `ProjectNotFoundException`, `EnvironmentNotFoundException` (heredando de `DomainException` o equivalente usado por el middleware). | Nuevos archivos en `src/Services/Core/IODA.Core.Domain/Exceptions/`. |
| T3 | En los handlers de Core que hoy lanzan `InvalidOperationException` para “no encontrado”, sustituir por las excepciones de T2. Registrar en el middleware de Core el mapeo de esas excepciones a **404**. | `CreateContentCommandHandler.cs`, `UpdateContentCommandHandler.cs`, `CreateContentSchemaCommandHandler.cs`, `CreateSiteCommandHandler.cs`, `CreateEnvironmentCommandHandler.cs`, `UploadMediaCommandHandler.cs`; `IODA.Core.API/Middleware/ErrorHandlingMiddleware.cs`. |
| T4 | Unificar el mapeo de `InvalidOperationException` entre servicios: definir convención (400 vs 409) y aplicar en un middleware compartido o en cada middleware de forma coherente. | `IODA.Core.API/Middleware/ErrorHandlingMiddleware.cs`, `IODA.Authorization.API/Middleware/ErrorHandlingMiddleware.cs` (y opcionalmente el resto). |

### 5.2 Backend — Middleware y validación

| ID | Tarea | Archivos concretos |
|----|--------|---------------------|
| T5 | Crear paquete o proyecto compartido (ej. `IODA.Shared.Api`) con `ErrorHandlingMiddleware` configurable: mapeo de tipos de excepción → (StatusCode, ProblemDetails) inyectable o por convención. | Nuevo proyecto; luego reemplazar los cinco `*/Middleware/ErrorHandlingMiddleware.cs` por el uso del compartido. |
| T6 | Implementar `UploadMediaCommandValidator` (FluentValidation) para `UploadMediaCommand`: al menos ProjectId, CreatedBy, y reglas de tipo/extensiones (lista blanca) y tamaño coherente con `RequestSizeLimit(50*1024*1024)`. | Nuevo archivo en `src/Services/Core/IODA.Core.Application/Commands/Media/` o Validators; registrar en DI. `UploadMediaCommand.cs` como referencia. |
| T7 | Documentar en código (y si aplica en docs) la decisión sobre `MediaController.GetFile` [AllowAnonymous]: uso público (CDN) o requisito de signed URL/token. | `src/Services/Core/IODA.Core.API/Controllers/MediaController.cs` (comentario o doc en endpoint). |

### 5.3 Backend — Tests

| ID | Tarea | Archivos concretos |
|----|--------|---------------------|
| T8 | Añadir proyectos de test para Core e Identity (ej. `IODA.Core.UnitTests`, `IODA.Core.IntegrationTests`, `IODA.Identity.UnitTests`) e incluirlos en `IODA.sln`. | Nuevos `.csproj` bajo `src/Services/Core/`, `src/Services/Identity/`; `IODA.sln`. |
| T9 | Unit tests para: CreateContentCommandHandler, SchemaValidationService, validadores existentes de Content/Schemas/Sites, GetSetupStatusQueryHandler, Login/Register handlers. | Dentro de los proyectos creados en T8; referenciar Application y Domain. |
| T10 | Integration tests para endpoints de Core e Identity (cliente HTTP, respuestas 200/400/401/404). Opcional: Testcontainers para PostgreSQL. | Proyectos de integración; referencia a los APIs. |
| T11 | Configurar cobertura (coverlet) y umbral mínimo en CI. | `Directory.Build.props` o proyectos de test; pipeline CI. |

### 5.4 Infraestructura

| ID | Tarea | Archivos concretos |
|----|--------|---------------------|
| T12 | En `docker-compose.yml`, para servicios que dependan de postgres/rabbitmq/elasticsearch, declarar `depends_on` con condición `service_healthy` cuando esos servicios existan en el mismo compose. | `docker-compose.yml` (por servicio: ioda-core-api, ioda-identity-api, etc.). |
| T13 | Documentar o implementar `Media.StoragePath` por variable de entorno en despliegue en contenedor. | `appsettings.json` Core; documentación o Dockerfile/compose. |

### 5.5 Documentación

| ID | Tarea | Archivos concretos |
|----|--------|---------------------|
| T14 | Crear o completar CONVENTIONS.md (commits, nomenclatura, capas, excepciones). | `docs/` o raíz; enlace en README. |
| T15 | Crear ARCHITECTURE.md (servicios, capas, flujos, decisiones de seguridad y media). | `docs/ARCHITECTURE.md`. |

---

## 6. Impacto en Otros Módulos

### 6.1 ¿Se modificaron contratos públicos?

- **T1 (Publishing status):** Sí. El query param `status` dejaría de ser el enum .NET (`PublicationRequestStatus`) y pasaría a ser un **string**. Los clientes que hoy envían el valor del enum (p. ej. número o nombre) deben usar el nuevo contrato (string).

### 6.2 ¿Se afectaron APIs consumidas por el frontend?

- **T1:** El frontend que llame a `GET .../api/publishing/requests?status=...` debe usar el mismo valor de string que el backend acepte (p. ej. "Pending", "Approved", "Rejected"). Si el frontend hoy envía el enum serializado, hay que alinear nombres/valores.
- **T3/T4:** No cambian contratos HTTP; solo códigos de estado (400 → 404 en casos “not found”). El frontend que interprete 400 como “validación” podría necesitar tratar 404 para “recurso no encontrado”.

### 6.3 ¿Existen cambios breaking?

- **T1:** Breaking si los clientes dependen del tipo/serialización actual del enum. Mitigación: aceptar en el backend tanto el string como, temporalmente, el valor anterior y mapear a enum internamente; deprecar el valor antiguo.
- **T5 (middleware compartido):** No breaking para clientes HTTP; solo cambio interno de implementación.

### 6.4 ¿Se requiere versionado?

- **T1:** Si se mantiene compatibilidad con el valor actual del enum (p. ej. query param con mismo valor), no es estrictamente necesario versionar la API. Si se cambia solo a string con nombres distintos, podría considerarse v2 del endpoint o documentar en changelog.

### 6.5 Tareas concretas para equipos afectados

| Equipo | Tarea derivada |
|--------|-----------------|
| **Frontend** | Tras T1: actualizar llamada a `GET /api/publishing/requests` para enviar `status` como string (ej. "Pending", "Approved", "Rejected") según documentación del backend. |
| **Frontend** | Tras T3/T4: si la UI distingue “error de validación” (400) de “recurso no encontrado” (404), asegurar que las pantallas que crean/editan contenido/schemas/sites muestren mensaje adecuado para 404. |
| **DevOps** | Tras T12: asegurar que los health checks de postgres/rabbitmq (y opcionalmente elasticsearch) estén definidos si se usan en el mismo compose. |
| **QA** | Tras T8–T11: ejecutar batería de tests en CI y regresión manual en flujos de publicación, autorización e indexación. |

---

*Documento generado para soporte a agentes de mejora automática. Revisar y actualizar tras cambios en el código.*
