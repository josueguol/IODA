# Backend – Pasos y deuda técnica (Fase de mejoras)

Este documento concentra las tareas pendientes del **backend** heredadas de la Fase de creación inicial y las mejoras asociadas al plan del panel de administración.

**Referencias:** `docs/FASE_DE_CREACION_INICIAL/NEXT_STEPS.md`, `docs/FASE_DE_CREACION_INICIAL/PLAN_DE_TRABAJO.md`.

---

## 1. Mejoras en Core API

- [x] **Endpoint Environment:** CreateEnvironmentCommand + POST `api/projects/{projectId}/environments`, GET por id `api/projects/{projectId}/environments/{environmentId}`; LIST ya existía. Validator y repositorio con AddAsync/GetByIdAsync.
- [x] **Paginación estándar:** GET `api/projects` devuelve `PagedResultDto<ProjectDto>` con `page`, `pageSize` (content ya estaba paginado; schemas se mantiene como listado).
- [x] **Health Check RabbitMQ:** Añadido cuando `RabbitMQ:Enabled` es true (AspNetCore.HealthChecks.RabbitMQ, connection string desde config).
- [x] **Proteger Core API con JWT:** Autenticación JwtBearer (mismo SecretKey/Issuer/Audience que Identity), `[Authorize]` en Projects, Content y Schemas; Swagger Bearer definido.

---

### 1.5 Herencia de Schemas

- [x] **ParentSchemaId en ContentSchema:** Columna opcional `parent_schema_id` (FK auto-referencial a `content_schemas`) que permite que un schema herede campos de otro.
- [x] **Resolución de campos heredados:** `GetSchemaByIdQueryHandler` recorre la cadena de herencia (padre → abuelo → …) y devuelve `InheritedFields` en el DTO. Los campos propios del hijo prevalecen sobre los del padre (override por nombre).
- [x] **Protección contra ciclos:** El handler usa un `HashSet<Guid>` para evitar bucles infinitos en herencia circular.
- [x] **Migración:** `AddSchemaInheritance` (columna `parent_schema_id`, FK con `OnDelete: SetNull`).
- [x] **CreateSchemaCommand:** Acepta `ParentSchemaId` opcional; valida que el padre exista antes de crear.

---

## 2. Servicios opcionales

### 2.1 Schema Validation Service

- [x] **Validación centralizada por esquema:** `ISchemaValidationService` valida el diccionario de campos contra el esquema en Create/Update de contenido (CreateContentCommandHandler, UpdateContentCommandHandler).
- [x] **Reglas reutilizables:** Por tipo (string/text/richtext, number/integer, boolean, date/datetime, enum, json/reference) y por reglas en `ValidationRules` (minLength, maxLength, pattern, min, max, allowedValues para enum).
- [x] **Evolución sin romper contenido existente:** Solo se validan los campos definidos en el esquema; los campos extra en el payload se ignoran. La validación se aplica solo en create/update, no a contenido ya persistido.
- Implementación: `IODA.Core.Application/Services/SchemaValidationService.cs`, `Interfaces/ISchemaValidationService.cs`; excepción `SchemaValidationException` en Domain; errores devueltos como 400 con ProblemDetails (errors por campo).

### 2.2 Media Service

- [x] **Subida de archivos:** POST `api/projects/{projectId}/media` (multipart: file, createdBy, displayName opcional). Almacenamiento local configurable (`Media:StoragePath`).
- [x] **Versionado de media:** Campo `Version` en entidad (actualmente 1 por ítem); ampliable con tabla de versiones en el futuro.
- [x] **Metadatos:** fileName, displayName, contentType, sizeBytes, metadata (JSON), createdAt, createdBy; DTO expuesto en list/get.
- [ ] Integración con CDN (opcional, sustituir `IMediaStorage` por implementación CDN).
- [x] **API para CMS frontend:** List paginado, get por id, stream de archivo (preview/descarga). Ver [COMO_PROBAR_MEDIA_FASE_2.md](./docs/COMO_PROBAR_MEDIA_FASE_2.md).

---

## 3. Mejoras opcionales por servicio

### 3.1 Identity Service

- [x] **Setup Status endpoint:** `GET /api/auth/setup-status` (público) devuelve `{ hasUsers, selfRegistrationEnabled }`. Permite al frontend detectar si es la primera ejecución.
- [x] **Detección de primer usuario:** `RegisterResultDto` incluye `isFirstUser: bool`. El handler comprueba si hay usuarios existentes antes de registrar.
- [x] **Auto-registro configurable:** `SelfRegistration:Enabled` en `appsettings.json` (por defecto `true`). Si está deshabilitado, el endpoint de registro devuelve 403 (excepto para el primer usuario, que siempre puede registrarse).
- [x] **IUserRepository.AnyAsync:** Nuevo método para verificar existencia de usuarios sin cargar datos.
- [ ] Integrar proveedor externo (OAuth/OpenID) – opcional

### 3.2 Authorization Service

- [ ] Consumir eventos de Identity (ej. MassTransit consumer para UserLoggedInEventV1) – opcional

### 3.3 Publishing Service

- [ ] Workflows configurables (WorkflowDefinition en futuras iteraciones) – opcional
- [ ] Consumir eventos del CMS Core (MassTransit consumer para ContentCreated/ContentUpdated) – opcional
- [x] **Mejorar log cuando Core devuelve 400 en publish:** `CorePublishClient` lee `ProblemDetails` de Core API cuando hay error (400, 404, etc.) y lanza `CoreApiException` con los detalles. `ApprovePublicationCommandHandler` captura la excepción, guarda los errores en `PublicationRequest.ValidationErrors` y rechaza la solicitud. `ErrorHandlingMiddleware` de Publishing expone los `ProblemDetails` de Core en la respuesta HTTP. Los logs incluyen el `ProblemDetails` completo para debugging.

### 3.4 Indexing Service

- [ ] Sin deuda explícita; opcional: más filtros o facetas en búsqueda

---

## 4. Soporte para Sitios (si se requiere en Admin)

Para que el CMS frontend pueda gestionar “Sitios” y el flujo Proyecto → Entorno → Sitio:

- [x] **Domain Core:** Entidad Site (o equivalente), relación Project → Sites, Environment
- [x] **API Core:** CRUD de sitios por proyecto (o por proyecto+entorno según modelo)
- [x] **Contrato:** Dominio, subdominio, subruta, tema asociado, activo
- [x] **Filtrado de contenido:** Opcionalmente por sitio si se añade al modelo de contenido

**Implementado:**
- Entidad `Site` con relaciones a `Project` y `Environment` (opcional)
- Campos: `Domain`, `Subdomain`, `Subpath`, `ThemeId`, `IsActive`
- Repository `ISiteRepository` con métodos para búsqueda por proyecto, proyecto+entorno, y por dominio
- Comandos: `CreateSiteCommand`, `UpdateSiteCommand`, `DeleteSiteCommand`, `ActivateSiteCommand`, `DeactivateSiteCommand`
- Queries: `GetSiteByIdQuery`, `ListSitesByProjectQuery`, `ListSitesByProjectAndEnvironmentQuery`
- API Controller `SitesController` con endpoints CRUD bajo `/api/projects/{projectId}/sites`
- Validadores FluentValidation para creación y actualización
- Migración `AddSitesTable` creada

**Filtrado por sitio:** La entidad `Content` tiene `SiteId` opcional. Al crear contenido se puede asignar un sitio (`CreateContentRequest.SiteId`). Listado y contenido publicado aceptan query param `siteId`: `GET api/projects/{projectId}/content?siteId={siteId}` y `GET api/projects/{projectId}/environments/{environmentId}/content/published?siteId={siteId}`. El dominio incluye `AssignToSite(Guid?)` para futuras APIs de asignación.

---

## 5. Revisión y convenciones

- [x] Revisar y ajustar documentación (CONVENTIONS.md, EVENTS.md)
- [x] Refinar contratos de eventos si hay cambios en consumidores
- [x] Añadir ejemplos en [COMO_PROBAR_BACKEND_FASE_1.md](./docs/COMO_PROBAR_BACKEND_FASE_1.md) (y fases posteriores) donde falten

**Realizado:**
- **CONVENTIONS.md:** Añadida sección 10 (Fase de mejoras): entidades Site y MediaItem, excepciones SchemaValidationException, MediaItemNotFoundException, SiteNotFoundException, respuestas ProblemDetails y CoreApiException, ISchemaValidationService e IMediaStorage.
- **EVENTS.md:** Añadida sección 7 (Integración cross-service): CoreApiException y propagación de ProblemDetails (Publishing → Core), consumidores opcionales de ContentCreated/ContentUpdated.
- **COMO_PROBAR:** En COMO_PROBAR_BACKEND_FASE_1.md añadida sección 8 con tabla de guías (Media Fase 2, Sites Fase 4) y referencias cruzadas; en COMO_PROBAR_MEDIA_FASE_2 y COMO_PROBAR_SITES_FASE_4 actualizado el pie con enlaces a las otras guías.

---

## 6. Tests (opcional / al final)

Los tests no son prerrequisito para las mejoras funcionales. Pueden abordarse al final o cuando se priorice calidad/regresión. Por ahora **DevOps no está considerado** en este plan.

### 6.1 Unit tests (opcional)

- [ ] **Domain (Core):** Entidades, value objects, reglas de negocio (Content, ContentStatus, Slug, etc.)
- [ ] **Domain (Identity):** User, RefreshToken, excepciones
- [ ] **Domain (Authorization):** Permission, Role, AccessRule
- [ ] **Application (Core):** Handlers de comandos y queries, validadores FluentValidation
- [ ] **Application (Identity):** LoginCommand, RegisterCommand, RefreshTokenCommand
- [ ] **Application (Authorization):** CheckAccessQuery, comandos de roles/permisos/rules
- [ ] **Application (Publishing):** RequestPublication, ApprovePublication, RejectPublication
- [ ] **Application (Indexing):** SearchContentQuery, IndexContentCommand

### 6.2 Integration tests (opcional)

- [ ] **Core API:** Endpoints (projects, content, schemas, publish/unpublish), middleware, health
- [ ] **Identity API:** register, login, refresh, errores
- [ ] **Authorization API:** check, roles, permissions, rules
- [ ] **Publishing API:** requests, approve, reject (con Core API mock o test)
- [ ] **Indexing API:** search, index, remove (con Elasticsearch test o NoOp)

---

## 8. API Gateway / Backend For Frontend (BFF) — Plan complementario

- [ ] Crear proyecto/servicio BFF que orqueste llamadas a Core, Identity, Access Rules y Publishing.
- [ ] Validar contexto (Proyecto, Entorno, Sitio) en el BFF; rechazar peticiones sin contexto válido.
- [ ] Normalizar respuestas y manejo centralizado de errores (códigos y mensajes consistentes).
- [ ] Verificación JWT en el BFF; no duplicar lógica de negocio ni persistencia.
- [ ] Documentar endpoints del BFF y su mapeo a microservicios.

---

## 9. Content Delivery API (lectura pública) — Plan complementario

- [ ] Definir API de solo lectura para contenido **publicado** (estado Published).
- [ ] Resolución por dominio, sitio, sección, tipo de contenido y slug; soporte de relaciones entre contenidos.
- [ ] Respuestas JSON estables y cacheables; sin autenticación de CMS ni escritura.
- [ ] Documentar contrato (schemas, ejemplos) para consumo por Themes y portales.

---

## 10. Observabilidad y auditoría — Plan complementario

- [ ] **Audit Log Service:** Registrar eventos (login/logout, CRUD contenido, publicación, cambios de permisos/esquemas) con quién, cuándo, proyecto/entorno/sitio y entidad afectada.
- [ ] **Event Tracing:** Asignar `correlationId` por flujo; propagar en llamadas entre servicios y en mensajes RabbitMQ para trazabilidad distribuida.

---

## 11. Feature Flags y configuración dinámica — Plan complementario (opcional / fase avanzada)

- [ ] Servicio o módulo de feature flags evaluables por Proyecto, Entorno y Sitio.
- [ ] Endpoint o integración para que backend y frontend evalúen flags (sin redeploy para activar/desactivar funcionalidades).

---

## 12. Orden sugerido

1. ~~**Mejoras Core** (Environment endpoint, paginación, health RabbitMQ, JWT)~~ ✅ **Completado.**
2. **Media Service** si el CMS necesita subida/preview de archivos.
3. **Schema Validation Service** si se quiere validación centralizada.
4. **Sitios** cuando el producto lo exija y el modelo esté definido.
5. **API Gateway/BFF** (plan complementario, mínimo viable).
6. **Content Delivery API** (plan complementario).
7. **Observabilidad** (Audit Log + Event Tracing, plan complementario).
8. **Feature Flags** (plan complementario, opcional / fase avanzada).
9. **Revisión y convenciones** (documentación, eventos).
10. **Opcionales** por servicio (proveedor externo, eventos, workflows) según prioridad.
11. **Tests** (unit + integración) – opcional, al final, cuando se priorice regresión.

---

**Última actualización:** 2026-01-24  
**Origen:** Deuda técnica de Fase de creación inicial + plan de mejoras del panel de administración + [PLAN_COMPLEMENTARIO.md](./PLAN_COMPLEMENTARIO.md).

**Avance:** Sección 1 (Mejoras en Core API) completada en esta fase. Sección 1.5 (Herencia de Schemas) completada. Sección 3.1 Identity (Setup Status, SuperAdmin, Auto-registro) completada.
