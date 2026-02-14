# 📋 Convenciones y Estándares de Código - IODA CMS

## 📑 Tabla de Contenidos

- [1. Nomenclatura](#1-nomenclatura)
- [2. Estructura de Proyectos](#2-estructura-de-proyectos)
- [3. Principios SOLID](#3-principios-solid)
- [4. Patrones de Diseño](#4-patrones-de-diseño)
- [5. Manejo de Errores](#5-manejo-de-errores)
- [6. Validaciones](#6-validaciones)
- [7. DTOs y Contratos](#7-dtos-y-contratos)
- [8. Tests](#8-tests)
- [9. Git y Commits](#9-git-y-commits)
- [10. Fase de mejoras (adiciones al Core)](#10-fase-de-mejoras-adiciones-al-core)

---

## 1. Nomenclatura

### 1.1 Namespaces

Sigue el patrón: `IODA.{Service}.{Layer}.{Feature?}`

```csharp
// ✅ Correcto
namespace IODA.Core.Domain.Entities;
namespace IODA.Core.Application.UseCases.Content;
namespace IODA.Core.Infrastructure.Persistence.Repositories;

// ❌ Incorrecto
namespace IODA.Core.Content; // No especifica capa
namespace Core.Domain; // Falta prefijo IODA
```

### 1.2 Clases y Archivos

- **Una clase por archivo**
- **El nombre del archivo debe coincidir con el nombre de la clase**
- Usa **PascalCase** para clases

```csharp
// Archivo: Content.cs
public class Content 
{
    // ...
}

// Archivo: IContentRepository.cs
public interface IContentRepository
{
    // ...
}
```

### 1.3 Interfaces

Siempre inicia con `I` seguido de PascalCase

```csharp
// ✅ Correcto
public interface IContentRepository { }
public interface IEventPublisher { }
public interface ISchemaValidator { }

// ❌ Incorrecto
public interface ContentRepository { }
public interface EventPublisherInterface { }
```

### 1.4 Métodos

Usa **PascalCase** y verbos que describan la acción

```csharp
// ✅ Correcto
public Task<Content> GetContentByIdAsync(Guid id);
public void PublishContent(Content content);
public bool ValidateSchema(ContentSchema schema);

// ❌ Incorrecto
public Task<Content> content_by_id(Guid id); // snake_case
public void publish(Content content); // No específico
public bool Check(ContentSchema schema); // Verbo ambiguo
```

### 1.5 Variables y Parámetros

Usa **camelCase**

```csharp
// ✅ Correcto
var contentId = Guid.NewGuid();
string contentType = "Article";
int maxRetries = 3;

// ❌ Incorrecto
var ContentId = Guid.NewGuid(); // PascalCase
string content_type = "Article"; // snake_case
```

### 1.6 Campos Privados

Usa **_camelCase** (guion bajo + camelCase)

```csharp
public class ContentService
{
    // ✅ Correcto
    private readonly IContentRepository _contentRepository;
    private readonly ILogger<ContentService> _logger;
    
    // ❌ Incorrecto
    private readonly IContentRepository contentRepository; // Sin _
    private readonly ILogger<ContentService> m_logger; // Notación húngara
}
```

### 1.7 Constantes

Usa **UPPER_SNAKE_CASE**

```csharp
// ✅ Correcto
public const int MAX_CONTENT_SIZE = 10485760; // 10MB
public const string DEFAULT_CONTENT_TYPE = "Article";
public const string CONTENT_CREATED_EVENT = "ContentCreated";

// ❌ Incorrecto
public const int maxContentSize = 10485760; // camelCase
public const string DefaultContentType = "Article"; // PascalCase
```

---

## 2. Estructura de Proyectos

### 2.1 Organización por Capas (Clean Architecture)

```
IODA.{Service}.Domain/
├── Entities/
│   ├── Content.cs
│   ├── ContentVersion.cs
│   └── ContentSchema.cs
├── ValueObjects/
│   ├── Slug.cs
│   ├── ContentStatus.cs
│   └── Identifier.cs
├── Repositories/
│   └── IContentRepository.cs
├── Events/
│   ├── ContentCreatedDomainEvent.cs
│   └── ContentUpdatedDomainEvent.cs
├── Exceptions/
│   ├── ContentNotFoundException.cs
│   └── InvalidSchemaException.cs
└── Specifications/
    └── PublishedContentSpecification.cs

IODA.{Service}.Application/
├── UseCases/
│   ├── Content/
│   │   ├── Create/
│   │   │   ├── CreateContentCommand.cs
│   │   │   ├── CreateContentCommandHandler.cs
│   │   │   └── CreateContentCommandValidator.cs
│   │   └── Get/
│   │       ├── GetContentQuery.cs
│   │       └── GetContentQueryHandler.cs
├── DTOs/
│   ├── ContentDto.cs
│   └── ContentSchemaDto.cs
├── Interfaces/
│   ├── IContentService.cs
│   └── IEventPublisher.cs
├── Validators/
│   └── ContentDtoValidator.cs
└── Behaviors/
    ├── ValidationBehavior.cs
    └── LoggingBehavior.cs

IODA.{Service}.Infrastructure/
├── Persistence/
│   ├── Configurations/
│   │   ├── ContentConfiguration.cs
│   │   └── ContentSchemaConfiguration.cs
│   ├── Repositories/
│   │   └── ContentRepository.cs
│   └── {Service}DbContext.cs
├── Messaging/
│   ├── RabbitMQ/
│   │   ├── RabbitMQConfiguration.cs
│   │   └── RabbitMQEventPublisher.cs
│   └── EventHandlers/
│       └── ContentCreatedEventHandler.cs
├── External/
│   └── (APIs externas, servicios de terceros)
└── DependencyInjection.cs

IODA.{Service}.API/
├── Controllers/
│   └── ContentController.cs
├── Middleware/
│   ├── ErrorHandlingMiddleware.cs
│   └── RequestLoggingMiddleware.cs
├── Filters/
│   └── ValidationFilter.cs
├── Extensions/
│   └── ServiceCollectionExtensions.cs
├── appsettings.json
├── appsettings.Development.json
├── Dockerfile
└── Program.cs
```

### 2.2 Reglas de Dependencia

```
┌────────────────────┐
│       API          │ ───┐
└────────────────────┘    │
         ↓                │
┌────────────────────┐    │
│  Infrastructure    │ ───┤ Dependen de →
└────────────────────┘    │
         ↓                │
┌────────────────────┐    │
│   Application      │ ───┤
└────────────────────┘    │
         ↓                │
┌────────────────────┐    │
│     Domain         │ ←──┘
└────────────────────┘
   (No depende de nadie)
```

**Reglas:**
- ✅ API puede referenciar Application, Infrastructure y Domain
- ✅ Infrastructure puede referenciar Application y Domain
- ✅ Application puede referenciar Domain
- ❌ Domain NO puede referenciar a nadie (solo .NET Base Class Library)

---

## 3. Principios SOLID

### 3.1 Single Responsibility Principle (SRP)

**Una clase, un propósito**

```csharp
// ❌ Incorrecto - Hace demasiado
public class ContentManager
{
    public void CreateContent(Content content) { }
    public void SendEmail(string to, string subject) { }
    public void LogToDatabase(string message) { }
    public void ValidateSchema(ContentSchema schema) { }
}

// ✅ Correcto - Responsabilidad única
public class ContentService
{
    private readonly IContentRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    
    public async Task CreateContentAsync(Content content)
    {
        await _repository.AddAsync(content);
        await _eventPublisher.PublishAsync(new ContentCreated(content.Id));
    }
}

public class EmailService { }
public class LoggingService { }
public class SchemaValidator { }
```

### 3.2 Open/Closed Principle (OCP)

**Abierto para extensión, cerrado para modificación**

```csharp
// ✅ Correcto - Extensible sin modificar
public interface IFieldValidator
{
    bool CanValidate(FieldDefinition field);
    ValidationResult Validate(object value, FieldDefinition field);
}

public class StringFieldValidator : IFieldValidator
{
    public bool CanValidate(FieldDefinition field) 
        => field.Type == FieldType.String;
    
    public ValidationResult Validate(object value, FieldDefinition field)
    {
        // Validación específica para strings
    }
}

// Puedes agregar nuevos validadores sin tocar el código existente
public class EmailFieldValidator : IFieldValidator { }
public class UrlFieldValidator : IFieldValidator { }
```

### 3.3 Liskov Substitution Principle (LSP)

**Los subtipos deben ser sustituibles por sus tipos base**

```csharp
// ✅ Correcto
public interface IContentRepository
{
    Task<Content?> GetByIdAsync(Guid id);
}

public class PostgresContentRepository : IContentRepository
{
    // Respeta el contrato: puede devolver null si no encuentra
    public async Task<Content?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Contents.FindAsync(id);
    }
}

// ❌ Incorrecto - Lanza excepción cuando no debería
public class BadContentRepository : IContentRepository
{
    public async Task<Content?> GetByIdAsync(Guid id)
    {
        var content = await _dbContext.Contents.FindAsync(id);
        if (content == null)
            throw new Exception(); // ❌ Cambia el contrato
        return content;
    }
}
```

### 3.4 Interface Segregation Principle (ISP)

**Interfaces pequeñas y específicas**

```csharp
// ❌ Incorrecto - Interface demasiado grande
public interface IContentRepository
{
    Task<Content> GetByIdAsync(Guid id);
    Task<IEnumerable<Content>> GetAllAsync();
    Task AddAsync(Content content);
    Task UpdateAsync(Content content);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Content>> SearchAsync(string query);
    Task<int> CountAsync();
    Task<bool> ExistsAsync(Guid id);
}

// ✅ Correcto - Interfaces segregadas
public interface IContentReader
{
    Task<Content?> GetByIdAsync(Guid id);
    Task<IEnumerable<Content>> GetAllAsync();
}

public interface IContentWriter
{
    Task AddAsync(Content content);
    Task UpdateAsync(Content content);
    Task DeleteAsync(Guid id);
}

public interface IContentSearcher
{
    Task<IEnumerable<Content>> SearchAsync(string query);
}
```

### 3.5 Dependency Inversion Principle (DIP)

**Depende de abstracciones, no de concreciones**

```csharp
// ❌ Incorrecto - Depende de implementación concreta
public class ContentService
{
    private readonly PostgresContentRepository _repository;
    
    public ContentService()
    {
        _repository = new PostgresContentRepository(); // ❌ new = acoplamiento
    }
}

// ✅ Correcto - Depende de abstracción
public class ContentService
{
    private readonly IContentRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    
    // Inyección de dependencias
    public ContentService(
        IContentRepository repository,
        IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }
}
```

---

## 4. Patrones de Diseño

### 4.1 Repository Pattern

```csharp
// Domain/Repositories/IContentRepository.cs
public interface IContentRepository
{
    Task<Content?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Content>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Content> AddAsync(Content content, CancellationToken cancellationToken = default);
    Task UpdateAsync(Content content, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

// Infrastructure/Persistence/Repositories/ContentRepository.cs
public class ContentRepository : IContentRepository
{
    private readonly CoreDbContext _context;
    
    public ContentRepository(CoreDbContext context)
    {
        _context = context;
    }
    
    public async Task<Content?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Contents
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
    
    // ... otras implementaciones
}
```

### 4.2 CQRS Pattern (Command Query Responsibility Segregation)

```csharp
// Commands (escritura)
public record CreateContentCommand(
    string Title,
    string ContentType,
    Dictionary<string, object> Fields) : IRequest<Guid>;

public class CreateContentCommandHandler 
    : IRequestHandler<CreateContentCommand, Guid>
{
    private readonly IContentRepository _repository;
    
    public async Task<Guid> Handle(
        CreateContentCommand command, 
        CancellationToken cancellationToken)
    {
        var content = Content.Create(
            command.Title, 
            command.ContentType, 
            command.Fields);
            
        await _repository.AddAsync(content, cancellationToken);
        return content.Id;
    }
}

// Queries (lectura)
public record GetContentByIdQuery(Guid Id) : IRequest<ContentDto>;

public class GetContentByIdQueryHandler 
    : IRequestHandler<GetContentByIdQuery, ContentDto>
{
    private readonly IContentRepository _repository;
    
    public async Task<ContentDto> Handle(
        GetContentByIdQuery query, 
        CancellationToken cancellationToken)
    {
        var content = await _repository.GetByIdAsync(
            query.Id, 
            cancellationToken);
            
        return content.ToDto();
    }
}
```

### 4.3 Specification Pattern

```csharp
// Domain/Specifications/Specification.cs
public abstract class Specification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();
    
    public bool IsSatisfiedBy(T entity)
    {
        var predicate = ToExpression().Compile();
        return predicate(entity);
    }
}

// Domain/Specifications/PublishedContentSpecification.cs
public class PublishedContentSpecification : Specification<Content>
{
    public override Expression<Func<Content, bool>> ToExpression()
    {
        return content => content.Status == ContentStatus.Published;
    }
}

// Uso
var publishedSpec = new PublishedContentSpecification();
var publishedContents = await _repository
    .FindAsync(publishedSpec.ToExpression());
```

---

## 5. Manejo de Errores

### 5.1 Excepciones de Dominio

```csharp
// Domain/Exceptions/DomainException.cs
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    
    protected DomainException(string message, Exception innerException) 
        : base(message, innerException) { }
}

// Domain/Exceptions/ContentNotFoundException.cs
public class ContentNotFoundException : DomainException
{
    public Guid ContentId { get; }
    
    public ContentNotFoundException(Guid contentId) 
        : base($"Content with ID '{contentId}' was not found.")
    {
        ContentId = contentId;
    }
}

// Domain/Exceptions/InvalidSchemaException.cs
public class InvalidSchemaException : DomainException
{
    public IEnumerable<string> Errors { get; }
    
    public InvalidSchemaException(IEnumerable<string> errors) 
        : base("Schema validation failed.")
    {
        Errors = errors;
    }
}
```

### 5.2 Middleware de Manejo Global

```csharp
// API/Middleware/ErrorHandlingMiddleware.cs
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    
    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            ContentNotFoundException notFound => 
                (StatusCodes.Status404NotFound, new 
                { 
                    error = "NotFound", 
                    message = notFound.Message,
                    contentId = notFound.ContentId 
                }),
                
            InvalidSchemaException invalidSchema => 
                (StatusCodes.Status400BadRequest, new 
                { 
                    error = "ValidationError", 
                    message = invalidSchema.Message,
                    errors = invalidSchema.Errors 
                }),
                
            DomainException domain => 
                (StatusCodes.Status400BadRequest, new 
                { 
                    error = "DomainError", 
                    message = domain.Message 
                }),
                
            _ => 
                (StatusCodes.Status500InternalServerError, new 
                { 
                    error = "InternalServerError", 
                    message = "An unexpected error occurred." 
                })
        };
        
        _logger.LogError(exception, "Error occurred: {Message}", exception.Message);
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        
        await context.Response.WriteAsJsonAsync(response);
    }
}
```

---

## 6. Validaciones

### 6.1 FluentValidation

```csharp
// Application/Validators/CreateContentCommandValidator.cs
public class CreateContentCommandValidator : AbstractValidator<CreateContentCommand>
{
    public CreateContentCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .MaximumLength(200)
            .WithMessage("Title must not exceed 200 characters.");
            
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .WithMessage("ContentType is required.");
            
        RuleFor(x => x.Fields)
            .NotNull()
            .WithMessage("Fields cannot be null.")
            .Must(fields => fields.Count > 0)
            .WithMessage("At least one field is required.");
    }
}
```

### 6.2 Validation Behavior (Pipeline)

```csharp
// Application/Behaviors/ValidationBehavior.cs
public class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }
        
        var context = new ValidationContext<TRequest>(request);
        
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            
        var failures = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToList();
            
        if (failures.Any())
        {
            throw new ValidationException(failures);
        }
        
        return await next();
    }
}
```

---

## 7. DTOs y Contratos

### 7.1 DTOs

```csharp
// Application/DTOs/ContentDto.cs
public record ContentDto(
    Guid Id,
    string Title,
    string ContentType,
    string Status,
    Dictionary<string, object> Fields,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

// Conversión usando extension methods
public static class ContentExtensions
{
    public static ContentDto ToDto(this Content content)
    {
        return new ContentDto(
            content.Id,
            content.Title,
            content.ContentType,
            content.Status.ToString(),
            content.Fields,
            content.CreatedAt,
            content.UpdatedAt);
    }
}
```

### 7.2 Records vs Classes

**Usa `record` para DTOs inmutables:**

```csharp
// ✅ Correcto - Inmutable, value-based equality
public record ContentDto(
    Guid Id,
    string Title,
    string ContentType);

// ❌ Menos ideal para DTOs
public class ContentDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string ContentType { get; set; }
}
```

---

## 8. Tests

### 8.1 Nomenclatura de Tests

Patrón: `{MethodName}_{Scenario}_{ExpectedResult}`

```csharp
public class ContentServiceTests
{
    [Fact]
    public async Task CreateContent_WithValidData_ReturnsContentId()
    {
        // Arrange
        var repository = new Mock<IContentRepository>();
        var service = new ContentService(repository.Object);
        
        // Act
        var contentId = await service.CreateContentAsync(
            "Test Title", 
            "Article", 
            new Dictionary<string, object>());
        
        // Assert
        contentId.Should().NotBeEmpty();
    }
    
    [Fact]
    public async Task GetContentById_WithNonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        var repository = new Mock<IContentRepository>();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Content?)null);
        var service = new ContentService(repository.Object);
        
        // Act
        var act = () => service.GetContentByIdAsync(Guid.NewGuid());
        
        // Assert
        await act.Should().ThrowAsync<ContentNotFoundException>();
    }
}
```

### 8.2 Estructura AAA (Arrange, Act, Assert)

```csharp
[Fact]
public async Task Example_Test()
{
    // Arrange - Preparar el escenario
    var content = Content.Create("Title", "Article", new());
    var repository = new Mock<IContentRepository>();
    repository.Setup(r => r.GetByIdAsync(content.Id, default))
        .ReturnsAsync(content);
    
    // Act - Ejecutar la acción
    var result = await repository.Object.GetByIdAsync(content.Id);
    
    // Assert - Verificar el resultado
    result.Should().NotBeNull();
    result!.Title.Should().Be("Title");
}
```

---

## 9. Git y Commits

### 9.1 Formato de Commits

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:**
- `feat`: Nueva funcionalidad
- `fix`: Corrección de bug
- `refactor`: Refactorización de código
- `docs`: Cambios en documentación
- `test`: Agregar o modificar tests
- `chore`: Tareas de mantenimiento
- `perf`: Mejoras de rendimiento
- `style`: Cambios de formato (no afectan la lógica)

**Ejemplos:**

```bash
feat(core): add content versioning support

Implements automatic versioning for content updates.
Each update creates a new version entry in the database.

Closes #123

---

fix(identity): correct JWT expiration time

The token was expiring immediately instead of after 60 minutes.

---

refactor(shared): extract event contracts to separate project

Moves all event definitions to IODA.Shared.Contracts
for better reusability across services.
```

### 9.2 Ramas

```
main            - Producción estable
develop         - Integración de features
feature/{name}  - Nueva funcionalidad
fix/{name}      - Corrección de bug
hotfix/{name}   - Corrección urgente en producción
release/{version} - Preparación de release
```

**Ejemplo:**
```bash
git checkout -b feature/content-versioning
git checkout -b fix/jwt-expiration
git checkout -b hotfix/critical-security-issue
```

---

## 10. Fase de mejoras (adiciones al Core)

### 10.1 Entidades y agregados añadidos

- **Site** (`IODA.Core.Domain.Entities.Site`): Agregado para sitios por proyecto; relación con `Project` y opcionalmente `Environment`. Campos: `Domain`, `Subdomain`, `Subpath`, `ThemeId`, `IsActive`.
- **MediaItem** (`IODA.Core.Domain.Entities.MediaItem`): Agregado para archivos subidos; pertenece a un `Project`; almacenamiento abstracto vía `IMediaStorage`.

### 10.2 Excepciones de dominio adicionales

Todas heredan de `DomainException`; el middleware las convierte en respuestas HTTP consistentes:

| Excepción | HTTP | Uso |
|-----------|------|-----|
| `SchemaValidationException` | 400 | Validación de contenido contra esquema (Create/Update content). |
| `MediaItemNotFoundException` | 404 | Media no encontrado (get/stream). |
| `SiteNotFoundException` | 404 | Sitio no encontrado (CRUD sitios). |

### 10.3 Respuestas de error en API (ProblemDetails)

Las APIs exponen errores con `application/problem+json` y `ProblemDetails`:

- **ValidationException** (FluentValidation): 400, `errors` por propiedad.
- **SchemaValidationException**: 400, `errors` por campo del esquema.
- **ContentNotFoundException**, **MediaItemNotFoundException**, **SiteNotFoundException**: 404.
- **DomainException** genérica: 400.
- **ArgumentException**, **InvalidOperationException**: 400.

En servicios que llaman a otros (ej. Publishing → Core), se usa `CoreApiException` para propagar el `ProblemDetails` de la API llamada y reexponerlo en la respuesta (manteniendo códigos y mensajes coherentes).

### 10.4 Servicios de aplicación

- **ISchemaValidationService**: Valida diccionario de campos contra `ContentSchema` (reglas por tipo y `ValidationRules`). Usado en `CreateContentCommandHandler` y `UpdateContentCommandHandler`.
- **IMediaStorage**: Abstracción de almacenamiento de archivos (implementación local: `LocalMediaStorage` con `Media:StoragePath`).

---

## 📚 Referencias

- [Clean Architecture (Robert C. Martin)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design (Eric Evans)](https://martinfowler.com/tags/domain%20driven%20design.html)
- [Microsoft .NET Guidelines](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)

---

**Última actualización:** 2026-01-24
