# 🚀 Fase 1 - CMS Core Service (EN PROGRESO)

## ✅ Progreso Actual

### 1. Domain Layer - COMPLETADO ✅

#### Value Objects (3/3)
- ✅ **Slug** - URL-friendly slugs con validación y normalización
- ✅ **ContentStatus** - Estados de contenido (Draft, InReview, Approved, Published, Archived, Rejected)
- ✅ **Identifier** - IDs públicos con prefijos (ej: cnt_abc123, prj_xyz789)

#### Entidades (6/6)
- ✅ **Project** - Proyectos con entornos
  - Identifier público, slug, activo/inactivo
  - Domain events: `ProjectCreatedDomainEvent`, `ProjectUpdatedDomainEvent`
  
- ✅ **Environment** - Entornos dentro de proyectos (dev, staging, prod)
  - Identifier público, slug, activo/inactivo
  - Relación con Project
  
- ✅ **Content** - Contenido schema-driven ⭐
  - **JSONB fields** para campos dinámicos
  - Versionado automático
  - Estados con transiciones controladas
  - Identifier público, slug
  - Domain events: `ContentCreatedDomainEvent`, `ContentUpdatedDomainEvent`, `ContentStatusChangedDomainEvent`, `ContentPublishedDomainEvent`, `ContentUnpublishedDomainEvent`
  
- ✅ **ContentVersion** - Historial de versiones
  - **JSONB snapshot** de fields
  - Audit trail completo
  - Comments opcionales
  
- ✅ **ContentSchema** - Definición de tipos de contenido
  - Schema-driven: define estructura de Content
  - Versionado de schemas
  - Activo/inactivo
  - Domain events: `SchemaCreatedDomainEvent`, `SchemaUpdatedDomainEvent`
  
- ✅ **FieldDefinition** - Campos dentro de un schema
  - Tipos de campo
  - Requerido/opcional
  - Valores por defecto
  - **JSONB validation rules**
  - Help text
  - Display order

#### Repository Interfaces (4/4)
- ✅ **IProjectRepository** - CRUD + consultas por slug
- ✅ **IContentRepository** - CRUD + consultas avanzadas (por proyecto, environment, tipo, publicados)
- ✅ **IContentSchemaRepository** - CRUD + consultas por proyecto y tipo
- ✅ **IUnitOfWork** - Coordinación de transacciones

#### Domain Events (7 eventos)
- ✅ `ProjectCreatedDomainEvent`
- ✅ `ProjectUpdatedDomainEvent`
- ✅ `ContentCreatedDomainEvent`
- ✅ `ContentUpdatedDomainEvent`
- ✅ `ContentStatusChangedDomainEvent`
- ✅ `ContentPublishedDomainEvent`
- ✅ `ContentUnpublishedDomainEvent`
- ✅ `SchemaCreatedDomainEvent`
- ✅ `SchemaUpdatedDomainEvent`

**Status:** ✅ **Domain Layer compilando sin errores**

---

### 2. Application Layer - COMPLETADO ✅

#### Interfaces
- ✅ **IEventPublisher** - Publicación de integration events (implementado en Infrastructure)

#### DTOs
- ✅ **ProjectDto**, **ContentDto**, **ContentListItemDto**
- ✅ **ContentVersionDto**, **ContentSchemaDto**, **ContentSchemaListItemDto**
- ✅ **FieldDefinitionDto**, **PagedResultDto&lt;T&gt;**

#### Mappings (Extension methods)
- ✅ **ContentMappings** - ToDto, ToListItemDto
- ✅ **ProjectMappings** - ToDto
- ✅ **SchemaMappings** - ToDto, ToListItemDto, FieldDefinition.ToDto

#### Commands y Handlers
- ✅ **CreateProjectCommand** / CreateProjectCommandHandler
- ✅ **CreateContentCommand** / CreateContentCommandHandler (publica ContentCreatedEventV1)
- ✅ **UpdateContentCommand** / UpdateContentCommandHandler (publica ContentUpdatedEventV1)
- ✅ **PublishContentCommand** / PublishContentCommandHandler (publica ContentPublishedEventV1)
- ✅ **UnpublishContentCommand** / UnpublishContentCommandHandler (publica ContentUnpublishedEventV1)
- ✅ **CreateContentSchemaCommand** / CreateContentSchemaCommandHandler (publica SchemaCreatedEventV1)

#### Queries y Handlers
- ✅ **GetContentByIdQuery** / GetContentByIdQueryHandler
- ✅ **ListContentByProjectQuery** / ListContentByProjectQueryHandler (paginado, filtros)
- ✅ **GetPublishedContentQuery** / GetPublishedContentQueryHandler (paginado)
- ✅ **GetContentVersionQuery** / GetContentVersionQueryHandler
- ✅ **GetSchemaByIdQuery** / GetSchemaByIdQueryHandler
- ✅ **ListSchemasByProjectQuery** / ListSchemasByProjectQueryHandler
- ✅ **GetProjectByIdQuery** / GetProjectByIdQueryHandler

#### Validators (FluentValidation)
- ✅ CreateProjectCommandValidator
- ✅ CreateContentCommandValidator
- ✅ UpdateContentCommandValidator
- ✅ PublishContentCommandValidator
- ✅ UnpublishContentCommandValidator
- ✅ CreateContentSchemaCommandValidator (incl. validación de campos y SchemaType regex)

#### Behaviors (Pipeline)
- ✅ **ValidationBehavior&lt;TRequest, TResponse&gt;** - Ejecuta validadores antes del handler
- ✅ **LoggingBehavior&lt;TRequest, TResponse&gt;** - Log de request y tiempo de ejecución

#### DependencyInjection
- ✅ **AddApplication()** - Registra MediatR, FluentValidation, validators y behaviors

**Status:** ✅ **Application Layer compilando sin errores**

---

### 3. Infrastructure Layer - COMPLETADO ✅

#### Persistence
- ✅ **CoreDbContext** - DbContext con DbSets para todas las entidades
- ✅ **CoreDbContextFactory** - Design-time factory para migraciones (usa env ConnectionStrings__DefaultConnection)
- ✅ **Converters**
  - **JsonbDictionaryConverter** - Dictionary&lt;string, object&gt; ↔ JSONB
  - **NullableJsonbDictionaryConverter** - Dictionary? ↔ JSONB (ValidationRules)
  - **JsonObjectConverter** - object? ↔ JSONB (DefaultValue)
  - **ValueObjectConverters** - Slug, ContentStatus, Identifier ↔ string
- ✅ **Entity Configurations**
  - ProjectConfiguration, EnvironmentConfiguration
  - ContentConfiguration (JSONB fields, backing field _versions)
  - ContentVersionConfiguration (JSONB fields)
  - ContentSchemaConfiguration (backing field _fields)
  - FieldDefinitionConfiguration (JSONB default_value, validation_rules)
- ✅ **Repositories**
  - ProjectRepository
  - ContentRepository (Include Versions, filtros por proyecto/entorno/tipo/estado)
  - ContentSchemaRepository (Include Fields ordenados)
- ✅ **UnitOfWork** - Coordina DbContext + transacciones
- ✅ **Migrations** - InitialCreate generada

#### Messaging
- ✅ **MassTransitEventPublisher** - Implementa IEventPublisher usando IPublishEndpoint (RabbitMQ)

#### DependencyInjection
- ✅ **AddInfrastructure(configuration)** - Registra:
  - CoreDbContext (Npgsql, retry, command timeout)
  - UnitOfWork, Repositories (vía UnitOfWork)
  - IEventPublisher → MassTransitEventPublisher
  - MassTransit con RabbitMQ (Host, VirtualHost, Username, Password desde config)

**Status:** ✅ **Infrastructure Layer compilando sin errores**

---

### 4. API Layer - COMPLETADO ✅

#### Controllers
- ✅ **ProjectsController** – POST crear, GET por ID
- ✅ **ContentController** – CRUD, publicar/despublicar, versiones, contenido publicado
- ✅ **SchemasController** – POST crear, GET por ID, GET listar por proyecto

#### Middleware
- ✅ **ErrorHandlingMiddleware** – Captura excepciones y devuelve ProblemDetails:
  - `ValidationException` (FluentValidation) → 400 Bad Request con detalle de errores
  - `ContentNotFoundException` → 404 Not Found
  - `DomainException` → 400 Bad Request
  - `ArgumentException` / `InvalidOperationException` → 400 Bad Request
  - Resto → 500 Internal Server Error (detalle solo en Development)
- ✅ **RequestLoggingMiddleware** – Log de método, path, status code y duración por request

#### Configuration
- ✅ **Program.cs** – Application, Infrastructure, Controllers, Swagger, CORS, middleware, Health Checks
- ✅ **Dependency Injection** – vía AddApplication() y AddInfrastructure()
- ✅ **Swagger/OpenAPI** – v1 en desarrollo
- ✅ **Health Checks** – `AddDbContextCheck<CoreDbContext>` ("database"), endpoint `/health` con JSON (status, checks)
- ✅ **appsettings** – ConnectionStrings, RabbitMQ (con opción Enabled para desarrollo)

**Status:** ✅ **API Layer completo y compilando**

---

## 📊 Métricas Actuales

| Componente | Estado | Archivos | Líneas de Código |
|-----------|--------|----------|------------------|
| **Domain Layer** | ✅ Completo | 11 | ~1,100 |
| **Application Layer** | ✅ Completo | 35+ | ~1,400 |
| **Infrastructure Layer** | ✅ Completo | 20+ | ~1,100 |
| **API Layer** | ✅ Completo | 8+ | ~400 |
| **TOTAL** | 100% (Fase 1) | 70+ | ~4,000 |

---

## 🎯 Highlights del Domain Layer

### 🌟 Schema-Driven Architecture
El diseño permite crear **tipos de contenido dinámicos** sin modificar código:

```csharp
// Ejemplo: Crear un schema "Article"
var articleSchema = ContentSchema.Create(
    projectId,
    "Article",
    "article",
    "Standard blog article",
    new List<FieldDefinition>
    {
        FieldDefinition.Create(schemaId, "body", "RichText", isRequired: true),
        FieldDefinition.Create(schemaId, "author", "String", isRequired: true),
        FieldDefinition.Create(schemaId, "publishDate", "DateTime"),
        FieldDefinition.Create(schemaId, "tags", "Array")
    },
    userId);

// Crear contenido usando ese schema
var article = Content.Create(
    projectId,
    environmentId,
    articleSchema.Id,
    "My First Article",
    "article",
    new Dictionary<string, object>
    {
        ["body"] = "<p>Content here...</p>",
        ["author"] = "John Doe",
        ["publishDate"] = DateTime.UtcNow,
        ["tags"] = new[] { "tech", "dotnet" }
    },
    userId);
```

### 🗄️ JSONB Support
Tres entidades usan **JSONB** de PostgreSQL:

1. **Content.Fields** - Campos dinámicos del contenido
2. **ContentVersion.Fields** - Snapshot de campos por versión
3. **FieldDefinition.ValidationRules** - Reglas de validación customizadas

Esto permite almacenar datos flexibles sin CREATE TABLE por cada tipo de contenido.

### 📝 Versionado Automático
Cada actualización de Content crea automáticamente un `ContentVersion`:

```csharp
content.Update(newTitle, newFields, userId);
// Automáticamente:
// - CurrentVersion++
// - Se crea ContentVersion
// - Se emite ContentUpdatedDomainEvent
```

### 🔐 Estado con Reglas de Negocio
ContentStatus no es un enum simple, es un Value Object con reglas:

```csharp
if (!content.Status.CanBePublished)
{
    throw new InvalidOperationException("Cannot publish");
}

content.Publish(userId);
// Solo funciona si Status == Approved o Published
```

### 📢 Domain Events Ricos
Cada acción importante emite eventos:

```csharp
// Al crear contenido
RaiseDomainEvent(new ContentCreatedDomainEvent(
    Id, Title, ContentType, ProjectId, EnvironmentId));

// Al publicar
RaiseDomainEvent(new ContentPublishedDomainEvent(
    Id, VersionId, Title, ContentType));
```

Estos eventos se convertirán en Integration Events para RabbitMQ.

---

## 🎓 Principios Aplicados

### ✅ SOLID
- **S**: Cada entidad tiene una responsabilidad clara
- **O**: Extensible vía schemas sin modificar código
- **L**: Interfaces de repositorio bien definidas
- **I**: Repositorios segregados por agregado
- **D**: Domain no depende de Infrastructure

### ✅ DDD
- **Entities**: Con identidad (Guid)
- **Value Objects**: Inmutables, comparados por valor
- **Aggregate Roots**: Project, Content, ContentSchema
- **Domain Events**: Comunicación entre agregados
- **Repository Pattern**: Abstracción de persistencia

### ✅ Clean Architecture
- Domain sin dependencias externas
- Solo referencia a BuildingBlocks compartidos
- Lógica de negocio encapsulada en entidades

---

## 🚀 Próximo Paso

**Fase 1 – CMS Core Service completada.** Siguientes opciones según PLAN_DE_TRABAJO:

- **Tests** – Unit tests (Domain, Application), integration tests (API)
- **Fase 2** – Identity Service u otro microservicio
- **Mejoras opcionales** – Endpoint para crear Environment, Health Check de RabbitMQ cuando esté habilitado

---

**Última actualización:** 2026-01-24  
**Status:** Fase 1 ✅ Completada (Domain | Application | Infrastructure | API)
