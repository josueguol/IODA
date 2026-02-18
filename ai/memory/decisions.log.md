# Architecture Decision Records (ADRs) — IODA CMS

Registro de decisiones arquitectónicas y de diseño tomadas durante el proyecto. Formato: contexto, decisión, consecuencias.

Este archivo contiene decisiones arquitectónicas aceptadas.

Regla obligatoria:
- Las decisiones con status "Aceptado" no pueden ser contradichas.
- Cualquier nueva propuesta debe evaluarse contra estas decisiones.
- Si una propuesta entra en conflicto, debe señalarse explícitamente.

---

## Índice

1. [ADR-001: Microservicios reales y mono-repo](#adr-001-microservicios-reales-y-mono-repo)
2. [ADR-002: Clean Architecture y DDD por servicio](#adr-002-clean-architecture-y-ddd-por-servicio)
3. [ADR-003: Base de datos por servicio](#adr-003-base-de-datos-por-servicio)
4. [ADR-004: Comunicación HTTP + mensajería (event-driven)](#adr-004-comunicación-http--mensajería-event-driven)
5. [ADR-005: Eventos versionados e inmutables](#adr-005-eventos-versionados-e-inmutables)
6. [ADR-006: MassTransit para RabbitMQ](#adr-006-masstransit-para-rabbitmq)
7. [ADR-007: Autenticación JWT y autorización por permisos](#adr-007-autenticación-jwt-y-autorización-por-permisos)
8. [ADR-008: Permisos centralizados en código (catálogo)](#adr-008-permisos-centralizados-en-código-catálogo)
9. [ADR-009: JWT con claims de permisos efectivos](#adr-009-jwt-con-claims-de-permisos-efectivos)
10. [ADR-010: Primer usuario SuperAdmin en backend](#adr-010-primer-usuario-superadmin-en-backend)
11. [ADR-011: Usuario actual (actor) desde JWT, no desde body](#adr-011-usuario-actual-actor-desde-jwt-no-desde-body)
12. [ADR-012: Convenciones de código y nomenclatura](#adr-012-convenciones-de-código-y-nomenclatura)
13. [ADR-013: CQRS con MediatR y FluentValidation](#adr-013-cqrs-con-mediatr-y-fluentvalidation)
14. [ADR-014: API-First, DTOs y ProblemDetails](#adr-014-api-first-dtos-y-problemdetails)
15. [ADR-015: Consistencia eventual entre servicios](#adr-015-consistencia-eventual-entre-servicios)
16. [ADR-016: Modo bootstrap en Authorization API](#adr-016-modo-bootstrap-en-authorization-api)
17. [ADR-017: Frontend sin creación dinámica de permisos](#adr-017-frontend-sin-creación-dinámica-de-permisos)
18. [ADR-018: Schema-driven y tipos en runtime](#adr-018-schema-driven-y-tipos-en-runtime)
19. [ADR-019: Multi-tenant por proyecto](#adr-019-multi-tenant-por-proyecto)
20. [ADR-020: Observabilidad y manejo de errores](#adr-020-observabilidad-y-manejo-de-errores)

---

## ADR-001: Microservicios reales y mono-repo

**Estado:** Aceptado.

**Contexto:** Se necesitaba clarificar si el sistema es un modular monolith o microservicios reales.

**Decisión:** El CMS es **microservicios reales** en runtime y datos. Cada servicio (Core, Identity, Authorization, Publishing, Indexing) es un proceso independiente con su propio Dockerfile y puerto. La única parte "monolítica" es el **repositorio** (mono-repo): una solución .sln con todos los proyectos.

**Consecuencias:**
- Despliegue independiente por servicio.
- Base de datos separada por servicio (una instancia PostgreSQL puede albergar varias bases: `ioda_core`, `ioda_identity`, etc.).
- Comunicación solo por red (HTTP, RabbitMQ); no hay llamadas en proceso.
- Ventaja de mono-repo: refactors transversales, convenciones compartidas, builds coordinados.

**Referencias:** `docs/CONSULTORIA/architecture/modular-monolith-vs-microservicios.md`, `ai/memory/project.context.md`.

---

## ADR-002: Clean Architecture y DDD por servicio

**Estado:** Aceptado.

**Contexto:** Necesidad de mantener dominios acotados, testabilidad y separación de responsabilidades en cada servicio.

**Decisión:** Cada servicio sigue **Clean Architecture** con capas: **Domain** (entidades, value objects, interfaces de repositorio, excepciones de dominio) → **Application** (casos de uso, CQRS, DTOs, validadores, interfaces de aplicación) → **Infrastructure** (persistencia, mensajería, clientes HTTP) → **API** (controllers, middleware). Se aplica **DDD** en el dominio (agregados, entidades, value objects, especificaciones). **SOLID** es obligatorio; dominio no depende de nadie.

**Consecuencias:**
- Reglas de dependencia estrictas: API → Infrastructure → Application → Domain.
- Un cambio en infraestructura (p. ej. otro ORM) no afecta al dominio.
- Controllers solo orquestan; no contienen lógica de negocio.
- Namespaces: `IODA.{Service}.{Layer}.{Feature?}`.

**Referencias:** `docs/000-FASE_DE_CREACION_INICIAL/docs/CONVENTIONS.md`, `ai/memory/project.context.md`.

---

## ADR-003: Base de datos por servicio

**Estado:** Aceptado.

**Contexto:** Evitar acoplamiento por datos compartidos entre servicios.

**Decisión:** Cada microservicio tiene **su propia base de datos** (o esquema lógico separado). En desarrollo se usa una instancia PostgreSQL con bases distintas (`ioda_core`, `ioda_identity`, `ioda_authorization`, `ioda_publishing`). Indexing usa Elasticsearch. No hay tablas compartidas ni acceso directo entre BDs de distintos servicios.

**Consecuencias:**
- Consistencia de datos solo dentro de un servicio; entre servicios se asume consistencia eventual.
- Migraciones y esquemas independientes por servicio.
- Escalado y backup por servicio.
- No se permite "joins" entre datos de Core e Identity; la integración es vía APIs o eventos.

**Referencias:** `ai/memory/project.context.md`, ADR-001.

---

## ADR-004: Comunicación HTTP + mensajería (event-driven)

**Estado:** Aceptado.

**Contexto:** Los servicios deben colaborar sin acoplamiento síncrono fuerte y permitir reacción asíncrona a hechos de dominio.

**Decisión:** Comunicación **dual**:
- **HTTP síncrono** para operaciones request/response (Identity → Authorization para permisos efectivos; Publishing → Core para publicar contenido; frontend → APIs).
- **Mensajería asíncrona** (RabbitMQ) para **eventos de dominio**: publicación de hechos (ContentCreated, ContentPublished, UserRegistered, etc.) y consumo por otros servicios sin acoplamiento directo.

**Consecuencias:**
- No se asume orden de llegada de eventos; handlers deben ser idempotentes cuando sea crítico.
- Latencia y disponibilidad: si Authorization no responde, Identity no puede incluir permisos en el JWT (se documenta y mitiga con NoOp clients).
- Eventos son fire-and-forget; no hay respuesta en la cola.

**Referencias:** `docs/000-FASE_DE_CREACION_INICIAL/docs/EVENTS.md`, `ai/memory/project.context.md`.

---

## ADR-005: Eventos versionados e inmutables

**Estado:** Aceptado.

**Contexto:** Evolución de contratos de eventos sin romper consumidores existentes.

**Decisión:** Todos los eventos siguen un **contrato versionado**: nombre con sufijo `V{Version}` (ej. `ContentCreatedEventV1`), interfaz `IEvent` con `EventId`, `OccurredAt`, `Version`, `EventType`. Eventos **inmutables** (records con `init`). **Versionado:** agregar propiedades opcionales no rompe; eliminar o cambiar tipo de propiedades exige nueva versión (V2). Los consumidores pueden soportar varias versiones en paralelo. Deprecación: publicar ambas versiones un tiempo, notificar, luego retirar la antigua.

**Consecuencias:**
- Compatibilidad hacia atrás en mensajería.
- Catálogo de eventos en `IODA.Shared.Contracts.Events` como fuente de verdad.
- No incluir datos sensibles ni entidades completas en eventos; solo datos necesarios para el consumidor.

**Referencias:** `docs/000-FASE_DE_CREACION_INICIAL/docs/EVENTS.md`.

---

## ADR-006: MassTransit para RabbitMQ

**Estado:** Aceptado.

**Contexto:** Necesidad de una librería de mensajería libre (sin licencias de pago en producción) para publicar/consumir en RabbitMQ.

**Decisión:** Usar **MassTransit 8.x** (Apache 2.0). No adoptar NServiceBus (licencia comercial en producción). EasyNetQ (MIT) queda como alternativa posible si se prefiere cliente más simple solo-RabbitMQ. No usar MassTransit 9+ en el contexto evaluado por posibles cambios de licencia.

**Consecuencias:**
- Sin coste de licencia en desarrollo ni producción.
- Ya integrado en Core para publicar eventos; configuración con retry y circuit breaker documentada.
- Refactor posible a EasyNetQ en el futuro si se prioriza dependencia MIT-only.

**Referencias:** `docs/000-FASE_DE_CREACION_INICIAL/docs/EVALUACION_MESSAGING.md`.

---

## ADR-007: Autenticación JWT y autorización por permisos

**Estado:** Aceptado.

**Contexto:** Las APIs deben identificar al usuario y restringir acceso según permisos, no solo “autenticado”.

**Decisión:** **Autenticación:** JWT Bearer emitido por Identity API (login/refresh). Parámetros de validación (issuer, audience, signing key) configurados por servicio. **Autorización:** basada en **permisos** (códigos como `project.edit`, `role.manage`, `content.publish`), no en nombres de rol en el JWT. Las políticas de ASP.NET Core exigen **claims de permiso** (`RequireClaim("permission", "<code>")`). El JWT incluye múltiples claims `permission` con los códigos efectivos del usuario (unión de permisos de todos sus roles). Scope por proyecto (multi-tenant lógico); validación obligatoria por ProjectId donde aplique.

**Consecuencias:**
- Identity debe obtener permisos efectivos de Authorization (HTTP) al emitir token; si Authorization no está configurado, se usa NoOp y el JWT sale sin permisos.
- Core, Publishing, Authorization protegen endpoints con policies 1:1 con permisos del catálogo.
- Frontend y otros clientes no envían "rol" para autorización; el backend decide por claims.

**Referencias:** `docs/CONSULTORIA/architecture/autorizacion-apis.md`, `docs/003-FASE_MEJORAS_PERMISOS/`, `docs/CONSULTORIA/faqs/roles-y-regla-acceso-todos-endpoints.md`.

---

## ADR-008: Permisos centralizados en código (catálogo)

**Estado:** Aceptado.

**Contexto:** Evitar permisos arbitrarios creados desde la UI que no tengan correspondencia con políticas ni con un modelo coherente.

**Decisión:** Los permisos del sistema se definen en un **catálogo en código** (p. ej. `PermissionCatalog` en Authorization.Application). No existe endpoint público para **crear** permisos (POST /permissions eliminado). Un **seeder** inserta los permisos del catálogo en BD al arrancar si faltan. La API expone solo **GET /permissions** (lista de solo lectura). Al asignar permisos a un rol, se valida que cada permiso pertenezca al catálogo.

**Consecuencias:**
- Una única fuente de verdad para códigos de permiso; las policies y el JWT se alinean con el catálogo.
- Nuevos permisos requieren cambio de código y despliegue.
- Frontend solo puede asignar permisos existentes a roles; no puede crear nuevos permisos.

**Referencias:** `docs/003-FASE_MEJORAS_PERMISOS/PLAN_DE_MIGRACION_PERMISOS_CENTRALIZADOS.md`, `docs/003-FASE_MEJORAS_PERMISOS/BACKEND.md`.

---

## ADR-009: JWT con claims de permisos efectivos

**Estado:** Aceptado.

**Contexto:** Las APIs necesitan autorizar por permiso sin llamar a Authorization en cada request.

**Decisión:** En **login** y **refresh**, Identity llama a Authorization API (`GET users/{userId}/effective-permissions`) y obtiene la lista de códigos de permiso del usuario (unión de roles asignados). Esos códigos se incluyen en el JWT como claims de tipo `permission` (un claim por código). Las políticas de cada API exigen `RequireClaim("permission", "<code>")`. Si Identity no tiene configurado `AuthorizationApi:BaseUrl`, usa `NoOpEffectivePermissionsClient` y el JWT se emite sin claims de permiso (acceso denegado en endpoints protegidos por permiso).

**Consecuencias:**
- Latencia adicional en login/refresh por la llamada HTTP a Authorization.
- Si se revocan permisos a un usuario, el cambio no se refleja hasta que renueve el token (mitigar con TTL corto y/o refresh que reemita con nuevos permisos).
- Frontend debe invalidar caché de permisos al refrescar sesión.

**Referencias:** `docs/003-FASE_MEJORAS_PERMISOS/`, `docs/004-MEJORAS_COMUNICACION/README.md`.

---

## ADR-010: Primer usuario SuperAdmin en backend

**Estado:** Aceptado.

**Contexto:** El primer usuario registrado debe tener todos los privilegios (SuperAdmin) sin depender de que el frontend cree permisos/roles.

**Decisión:** Tras el **primer registro**, Identity llama a Authorization API (`POST bootstrap-first-user` con userId). Authorization, si no existe ninguna regla de acceso (`AccessRuleRepository.CountAsync() == 0`), crea la regla que asigna el rol **SuperAdmin** al usuario. El rol SuperAdmin y todos los permisos del catálogo se crean por **seeders** al arrancar Authorization (PermissionSeeder, SuperAdminRoleSeeder). No hay bypass de autorización: el primer usuario tiene los mismos controles; obtiene permisos porque se le asigna el rol SuperAdmin. El frontend puede seguir teniendo un flujo de "setup" por si el backend no ejecutó bootstrap (ej. Identity sin BaseUrl), pero la fuente de verdad es el backend.

**Consecuencias:**
- Requiere configuración Identity ↔ Authorization (BaseUrl, ServiceApiKey) para que el bootstrap se ejecute.
- Si el bootstrap falla (401, 409), el primer usuario no tendrá permisos hasta que se configure correctamente o se use el flujo de frontend (modo bootstrap en Authorization permite crear rol/regla cuando hay 0 reglas).

**Referencias:** `docs/003-FASE_MEJORAS_PERMISOS/`, `docs/CONSULTORIA/recommendations/alcance-superadmin.md`, `docs/004-MEJORAS_COMUNICACION/`.

---

## ADR-011: Usuario actual (actor) desde JWT, no desde body

**Estado:** Aceptado (recomendación; implementación pendiente de verificar en todos los endpoints).

**Contexto:** Los comandos que llevan “quién actuó” (CreatedBy, UpdatedBy, PublishedBy) no deben aceptar ese valor del body sin validar, para evitar suplantación y asegurar auditoría fiable.

**Decisión:** El **actor** (userId que ejecuta la acción) se obtiene siempre del **JWT** (claim `sub`) en la capa API. No se aceptan ni se confían los campos `CreatedBy`/`UpdatedBy`/etc. enviados en el body; se ignoran o se documentan como no utilizados. Los controllers leen el userId del `ClaimsPrincipal` y lo pasan al comando. Opcionalmente un servicio o extensión centraliza la lectura para no duplicar en cada controller.

**Consecuencias:**
- Auditoría fiable; no se puede suplantar a otro usuario en metadatos.
- El frontend no puede “actuar en nombre de” otro usuario sin un flujo explícito de impersonación (con privilegios Admin).
- Contratos de API pueden dejar de exponer campos de auditoría editables o documentarlos como ignorados.

**Referencias:** `docs/CONSULTORIA/recommendations/uso-usuario-actual-jwt.md`, `docs/CONSULTORIA/analysis/autorizacion-y-usuario-actual.md`.

---

## ADR-012: Convenciones de código y nomenclatura

**Estado:** Aceptado.

**Contexto:** Mantener consistencia en nombres, estructura y patrones en todo el código.

**Decisión:** **Namespaces:** `IODA.{Service}.{Layer}.{Feature?}`. **Archivos:** una clase por archivo; nombre del archivo = nombre de la clase. **Interfaces:** prefijo `I` + PascalCase. **Campos privados:** `_camelCase`. **Constantes:** `UPPER_SNAKE_CASE`. **Métodos y clases:** PascalCase. **Commits:** formato `<type>(<scope>): <subject>` (feat, fix, refactor, docs, test, chore, perf, style). **Ramas:** main, develop, feature/{name}, fix/{name}, hotfix/{name}, release/{version}. Excepciones de dominio heredan de `DomainException`; el middleware las traduce a respuestas HTTP (404, 400, etc.) y ProblemDetails.

**Consecuencias:**
- Onboarding y búsqueda de código predecibles.
- Integración con herramientas de análisis y generación de documentación.

**Referencias:** `docs/000-FASE_DE_CREACION_INICIAL/docs/CONVENTIONS.md`.

---

## ADR-013: CQRS con MediatR y FluentValidation

**Estado:** Aceptado.

**Contexto:** Separar lectura y escritura, validación centralizada y pipeline de comportamientos.

**Decisión:** **CQRS** en la capa Application: Commands (escritura) y Queries (lectura) como requests de MediatR. Handlers implementan `IRequestHandler<TRequest, TResponse>`. **FluentValidation** para validación de commands/queries; **ValidationBehavior** en el pipeline de MediatR ejecuta validadores antes del handler y lanza `ValidationException` en caso de error (mapeada a 400 con detalles por propiedad). No hay lógica de dominio compleja en handlers más allá de orquestación y delegación al dominio/infra.

**Consecuencias:**
- Validación consistente y mensajes de error estructurados (ProblemDetails con `errors`).
- Fácil añadir comportamientos (logging, auditoría, transacciones) en el pipeline.

**Referencias:** `docs/000-FASE_DE_CREACION_INICIAL/docs/CONVENTIONS.md`.

---

## ADR-014: API-First, DTOs y ProblemDetails

**Estado:** Aceptado.

**Contexto:** Contratos explícitos, respuestas de error consistentes y no exponer entidades de dominio.

**Decisión:** **API-First:** las APIs son el producto; contratos (OpenAPI/Swagger) se respetan. **DTOs** para todas las respuestas y requests; nunca exponer entidades de dominio. Uso de **records** para DTOs inmutables. Errores con **ProblemDetails** (`application/problem+json`): ValidationException → 400 con `errors` por propiedad; excepciones de dominio → 404/400 según tipo; excepciones no mapeadas → 500. Middleware compartido (`ErrorHandlingMiddleware`) en cada API con mapeo configurable por servicio. Versionado de APIs y compatibilidad hacia atrás cuando sea posible.

**Consecuencias:**
- Frontend y otros consumidores reciben errores estructurados y traducibles.
- Cambios en el dominio no obligan a romper contratos si los DTOs se mantienen.

**Referencias:** `docs/000-FASE_DE_CREACION_INICIAL/docs/CONVENTIONS.md`, `ai/memory/project.context.md`, shared `IODA.Shared.Api`.

---

## ADR-015: Consistencia eventual entre servicios

**Estado:** Aceptado.

**Contexto:** Con base de datos por servicio y comunicación por red/eventos, no se puede garantizar consistencia fuerte entre servicios.

**Decisión:** **Dentro de un agregado/servicio:** consistencia fuerte. **Entre microservicios:** **consistencia eventual**. Los eventos son asíncronos; no se asume orden de llegada. Los handlers deben ser idempotentes cuando el efecto no sea idempotente por naturaleza. Se documenta qué flujos son eventualmente consistentes (ej. contenido publicado → indexado, contenido creado → notificación).

**Consecuencias:**
- UIs y APIs no deben asumir que un dato escrito en un servicio está ya visible en otro; puede haber retraso.
- Diseño de flujos que toleren retrasos (polling, mensajes de “procesando”, etc.) cuando sea necesario.

**Referencias:** `ai/memory/project.context.md`, `docs/CONSULTORIA/architecture/principios-cms.md`.

---

## ADR-016: Modo bootstrap en Authorization API

**Estado:** Aceptado.

**Contexto:** El primer usuario debe poder crear rol y regla cuando aún no tiene permisos en el JWT (huevo y gallina).

**Decisión:** En Authorization API, la policy **Admin** (protege CRUD de roles, reglas, asignación de permisos) se satisface con un **handler personalizado** que permite acceso en dos casos: (1) **Modo bootstrap:** no existe ninguna regla de acceso (`IAccessRuleRepository.CountAsync() == 0`); (2) **Normal:** el usuario tiene el claim `permission` con valor `role.manage`. Así, el primer usuario (o el frontend en su nombre) puede llamar a POST /roles, POST /rules, etc. cuando la BD está vacía; una vez creada la primera regla, solo quienes tengan `role.manage` pueden seguir gestionando.

**Consecuencias:**
- No hace falta un bypass “primer usuario” por JWT; el criterio es solo “0 reglas”.
- Tras el primer setup, cualquier intento de crear roles/reglas sin el permiso correcto devuelve 403.

**Referencias:** `docs/003-BUGFIXS/BACKEND.md`, `docs/004-MEJORAS_COMUNICACION/README.md`.

---

## ADR-017: Frontend sin creación dinámica de permisos

**Estado:** Aceptado.

**Contexto:** Alinear el frontend con el modelo de permisos centralizados y evitar creación arbitraria de permisos desde la UI.

**Decisión:** El frontend **no crea permisos**. Eliminar formulario y llamadas a POST /permissions en RolesPermissionsPage y en el flujo de primer usuario (RegisterPage). El frontend solo consume **GET /permissions** para listar permisos asignables a roles. La pestaña “Permisos” puede eliminarse o mostrarse como solo lectura (lista del catálogo). Tras cambios de permisos (asignación a rol, reglas), el frontend debe **refrescar el token** (refresh) e **invalidar la caché** de permisos para que la UI refleje los nuevos claims.

**Consecuencias:**
- Coherencia con el catálogo en backend.
- Mejoras de UX documentadas en 005-MEJORAS_FRONTEND (ocultar/eliminar pestaña Permisos, proteger rol SuperAdmin, etc.).

**Referencias:** `docs/003-FASE_MEJORAS_PERMISOS/FRONTEND.md`, `docs/005-MEJORAS_FRONTEND/`.

---

## ADR-018: Schema-driven y tipos en runtime

**Estado:** Aceptado.

**Contexto:** El CMS debe soportar tipos de contenido definidos por el usuario sin recompilar el core.

**Decisión:** El sistema es **schema-driven**: los tipos de contenido se definen en **runtime** (ContentSchema con FieldDefinitions). El core no depende de tipos concretos (Article, Video, etc.); valida y persiste contenido según el esquema asociado. Los esquemas tienen versionado; la validación usa el schema activo. Campos con label, slug (único dentro del schema), tipo y reglas de validación. Default fields (title, teaser, image, content) pueden sugerirse al crear un schema pero son editables/eliminables antes de guardar.

**Consecuencias:**
- Flexibilidad para múltiples tipos de contenido sin despliegues.
- Complejidad en validación y en evolución de esquemas (requerimientos 006-SCHEME-N-SITECONFIG).
- Contenido almacenado como diccionario de campos + schemaId/version.

**Referencias:** `ai/memory/project.context.md`, `docs/006-SCHEME-N-SITECONFIG/REQUERIMIENTOS.md`, `docs/CONSULTORIA/architecture/principios-cms.md`.

---

## ADR-019: Multi-tenant por proyecto

**Estado:** Aceptado.

**Contexto:** Aislamiento de datos y permisos por cliente/organización (proyecto).

**Decisión:** El modelo de datos y el acceso están **delimitados por proyecto** (y opcionalmente por entorno, sitio, schema, estado de contenido). Las reglas de acceso (AccessRule) pueden tener ProjectId, EnvironmentId, SchemaId, ContentStatus opcionales; si son null, el rol es global. Las APIs que manejan contenido exigen ProjectId en la ruta o en el contexto y validan que el usuario tenga permiso en ese ámbito. Multi-tenant es **lógico** (mismo despliegue, datos separados por proyecto), no necesariamente físico.

**Consecuencias:**
- Escalado y facturación por proyecto posibles.
- Queries y permisos deben filtrar siempre por proyecto donde aplique.

**Referencias:** `docs/CONSULTORIA/architecture/principios-cms.md`, `docs/000-FASE_DE_CREACION_INICIAL/docs/FASE_3_ACCESS_RULES.md`.

---

## ADR-020: Observabilidad y manejo de errores

**Estado:** Aceptado.

**Contexto:** Errores trazables, logs estructurados y comportamiento predecible en fallos.

**Decisión:** Cada microservicio incluye **logging estructurado**, **métricas** y **trazas**; **CorrelationId** entre servicios cuando se propague. **Errores nunca silenciosos**: excepciones se capturan en middleware y se devuelven como ProblemDetails; eventos fallidos deben ser trazables (logs, reintentos, dead-letter si aplica). Excepciones de dominio se mapean a códigos HTTP concretos (404, 400, 409); ValidationException a 400 con detalle por campo. En integración HTTP entre servicios (ej. Publishing → Core), los errores de la API llamada se propagan (CoreApiException) y se reexponen al cliente con el mismo status y cuerpo cuando tenga sentido.

**Consecuencias:**
- Depuración y monitoreo coherentes.
- Contratos de error documentados (Swagger, docs).

**Referencias:** `ai/memory/project.context.md`, `docs/000-FASE_DE_CREACION_INICIAL/docs/CONVENTIONS.md`, `docs/000-FASE_DE_CREACION_INICIAL/docs/EVENTS.md` (integración cross-service).

---

## Resumen por tema

| Tema | ADRs |
|------|------|
| Arquitectura general | 001, 002, 003, 004, 015, 018, 019 |
| Eventos y mensajería | 004, 005, 006 |
| Seguridad y permisos | 007, 008, 009, 010, 011, 016, 017 |
| Código y API | 012, 013, 014, 020 |

---

**Mantenimiento:** Al tomar nuevas decisiones arquitectónicas relevantes, añadir un nuevo ADR con el mismo formato (contexto, decisión, consecuencias) y actualizar el índice y el resumen. Incluir referencias a documentos del repo cuando existan.
