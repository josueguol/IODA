# 🔔 Contratos de Eventos - IODA CMS

## 📑 Tabla de Contenidos

- [1. Filosofía de Eventos](#1-filosofía-de-eventos)
- [2. Convenciones de Eventos](#2-convenciones-de-eventos)
- [3. Estructura de Eventos](#3-estructura-de-eventos)
- [4. Catálogo de Eventos](#4-catálogo-de-eventos)
- [5. Versionado de Eventos](#5-versionado-de-eventos)
- [6. Manejo de Eventos](#6-manejo-de-eventos)
- [7. Integración cross-service y errores (Fase de mejoras)](#7-integración-cross-service-y-errores-fase-de-mejoras)

---

## 1. Filosofía de Eventos

### 1.1 Principios

- ✅ **Eventos Inmutables**: Una vez publicado, un evento no puede cambiar
- ✅ **Eventos Asíncronos**: No esperan respuesta
- ✅ **Eventos de Dominio**: Representan hechos que ocurrieron
- ✅ **Bajo Acoplamiento**: Los servicios no se conocen entre sí, solo eventos
- ✅ **Orden No Garantizado**: Los eventos pueden llegar en cualquier orden

### 1.2 Event-Driven Architecture

```
┌─────────────────┐
│   CMS Core      │
│   Service       │
└────────┬────────┘
         │ Publica: ContentCreatedV1
         ↓
┌─────────────────┐
│   RabbitMQ      │
│   (Event Bus)   │
└────────┬────────┘
         │ Distribuye a suscriptores
         ├────────────────┬─────────────────┐
         ↓                ↓                 ↓
┌────────────────┐ ┌────────────────┐ ┌────────────────┐
│  Publishing    │ │  Indexing      │ │  Authorization │
│  Service       │ │  Service       │ │  Service       │
└────────────────┘ └────────────────┘ └────────────────┘
```

---

## 2. Convenciones de Eventos

### 2.1 Nomenclatura

**Formato:** `{Aggregate}{PastTenseVerb}V{Version}`

**Reglas:**
- Usa **PascalCase**
- El verbo debe estar en **pasado** (algo que ya ocurrió)
- Incluye **versión** al final
- Usa sufijo **Event** en la clase (opcional pero recomendado)

```csharp
// ✅ Correcto
ContentCreatedEventV1
ContentUpdatedEventV1
SchemaValidatedEventV1
ContentPublishedEventV1
UserAuthenticatedEventV1

// ❌ Incorrecto
CreateContent          // No es pasado
content_created        // No es PascalCase
ContentCreate          // No es pasado
ContentCreated         // Falta versión
```

### 2.2 Namespaces

Todos los eventos residen en `IODA.Shared.Contracts.Events.V{Version}`

```csharp
namespace IODA.Shared.Contracts.Events.V1;

public record ContentCreatedEventV1(
    Guid EventId,
    DateTime OccurredAt,
    Guid ContentId,
    string Title,
    string ContentType);
```

---

## 3. Estructura de Eventos

### 3.1 Clase Base

```csharp
// IODA.Shared.Contracts/Events/IEvent.cs
namespace IODA.Shared.Contracts.Events;

public interface IEvent
{
    /// <summary>
    /// Identificador único del evento
    /// </summary>
    Guid EventId { get; }
    
    /// <summary>
    /// Momento en que ocurrió el evento (UTC)
    /// </summary>
    DateTime OccurredAt { get; }
    
    /// <summary>
    /// Versión del evento
    /// </summary>
    int Version { get; }
    
    /// <summary>
    /// Nombre del tipo de evento
    /// </summary>
    string EventType { get; }
}
```

### 3.2 Clase Base Abstracta

```csharp
// IODA.Shared.Contracts/Events/EventBase.cs
namespace IODA.Shared.Contracts.Events;

public abstract record EventBase : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public abstract int Version { get; }
    public abstract string EventType { get; }
}
```

### 3.3 Metadatos Opcionales

```csharp
// IODA.Shared.Contracts/Events/EventMetadata.cs
namespace IODA.Shared.Contracts.Events;

public record EventMetadata(
    string? CorrelationId = null,
    string? CausationId = null,
    string? UserId = null,
    string? Source = null,
    Dictionary<string, string>? CustomProperties = null);
```

---

## 4. Catálogo de Eventos

### 4.1 CMS Core Service Events

#### ContentCreatedEventV1

```csharp
namespace IODA.Shared.Contracts.Events.V1;

/// <summary>
/// Se dispara cuando se crea un nuevo contenido
/// </summary>
public record ContentCreatedEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(ContentCreatedEventV1);
    
    /// <summary>
    /// ID único del contenido
    /// </summary>
    public required Guid ContentId { get; init; }
    
    /// <summary>
    /// Título del contenido
    /// </summary>
    public required string Title { get; init; }
    
    /// <summary>
    /// Tipo de contenido (Article, Video, Gallery, etc.)
    /// </summary>
    public required string ContentType { get; init; }
    
    /// <summary>
    /// Estado inicial del contenido
    /// </summary>
    public required string Status { get; init; }
    
    /// <summary>
    /// ID del proyecto al que pertenece
    /// </summary>
    public required Guid ProjectId { get; init; }
    
    /// <summary>
    /// ID del entorno (dev, staging, prod)
    /// </summary>
    public required Guid EnvironmentId { get; init; }
    
    /// <summary>
    /// ID del usuario que creó el contenido
    /// </summary>
    public required Guid CreatedBy { get; init; }
    
    /// <summary>
    /// Campos dinámicos del contenido
    /// </summary>
    public Dictionary<string, object>? Fields { get; init; }
    
    /// <summary>
    /// Metadatos adicionales del evento
    /// </summary>
    public EventMetadata? Metadata { get; init; }
}
```

#### ContentUpdatedEventV1

```csharp
namespace IODA.Shared.Contracts.Events.V1;

/// <summary>
/// Se dispara cuando se actualiza un contenido existente
/// </summary>
public record ContentUpdatedEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(ContentUpdatedEventV1);
    
    public required Guid ContentId { get; init; }
    public required Guid VersionId { get; init; }
    public required int VersionNumber { get; init; }
    public required string Title { get; init; }
    public required string Status { get; init; }
    public required Guid UpdatedBy { get; init; }
    public Dictionary<string, object>? Fields { get; init; }
    public Dictionary<string, object>? ChangedFields { get; init; }
    public EventMetadata? Metadata { get; init; }
}
```

#### ContentDeletedEventV1

```csharp
namespace IODA.Shared.Contracts.Events.V1;

/// <summary>
/// Se dispara cuando se elimina un contenido
/// </summary>
public record ContentDeletedEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(ContentDeletedEventV1);
    
    public required Guid ContentId { get; init; }
    public required string ContentType { get; init; }
    public required Guid DeletedBy { get; init; }
    public required string Reason { get; init; }
    public bool IsSoftDelete { get; init; }
    public EventMetadata? Metadata { get; init; }
}
```

#### SchemaCreatedEventV1

```csharp
namespace IODA.Shared.Contracts.Events.V1;

/// <summary>
/// Se dispara cuando se crea un nuevo esquema de contenido
/// </summary>
public record SchemaCreatedEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(SchemaCreatedEventV1);
    
    public required Guid SchemaId { get; init; }
    public required string SchemaName { get; init; }
    public required string SchemaType { get; init; }
    public required Guid ProjectId { get; init; }
    public required Guid CreatedBy { get; init; }
    public List<FieldDefinitionDto>? Fields { get; init; }
    public EventMetadata? Metadata { get; init; }
}

public record FieldDefinitionDto(
    string Name,
    string Type,
    bool IsRequired,
    object? DefaultValue,
    Dictionary<string, object>? ValidationRules);
```

#### SchemaUpdatedEventV1

```csharp
namespace IODA.Shared.Contracts.Events.V1;

/// <summary>
/// Se dispara cuando se actualiza un esquema existente
/// </summary>
public record SchemaUpdatedEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(SchemaUpdatedEventV1);
    
    public required Guid SchemaId { get; init; }
    public required string SchemaName { get; init; }
    public required int SchemaVersion { get; init; }
    public required Guid UpdatedBy { get; init; }
    public List<FieldDefinitionDto>? AddedFields { get; init; }
    public List<string>? RemovedFields { get; init; }
    public List<FieldDefinitionDto>? ModifiedFields { get; init; }
    public EventMetadata? Metadata { get; init; }
}
```

---

### 4.2 Identity Service Events

#### UserRegisteredEventV1

```csharp
namespace IODA.Shared.Contracts.Events.V1;

/// <summary>
/// Se dispara cuando un nuevo usuario se registra
/// </summary>
public record UserRegisteredEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(UserRegisteredEventV1);
    
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string Username { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required DateTime RegisteredAt { get; init; }
    public EventMetadata? Metadata { get; init; }
}
```

#### UserAuthenticatedEventV1

```csharp
namespace IODA.Shared.Contracts.Events.V1;

/// <summary>
/// Se dispara cuando un usuario se autentica exitosamente
/// </summary>
public record UserAuthenticatedEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(UserAuthenticatedEventV1);
    
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string IpAddress { get; init; }
    public required string UserAgent { get; init; }
    public required DateTime AuthenticatedAt { get; init; }
    public EventMetadata? Metadata { get; init; }
}
```

#### UserDeactivatedEventV1

```csharp
namespace IODA.Shared.Contracts.Events.V1;

/// <summary>
/// Se dispara cuando se desactiva un usuario
/// </summary>
public record UserDeactivatedEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(UserDeactivatedEventV1);
    
    public required Guid UserId { get; init; }
    public required Guid DeactivatedBy { get; init; }
    public required string Reason { get; init; }
    public required DateTime DeactivatedAt { get; init; }
    public EventMetadata? Metadata { get; init; }
}
```

---

### 4.3 Authorization Service Events

#### RoleCreatedEventV1

```csharp
namespace IODA.Shared.Contracts.Events.V1;

/// <summary>
/// Se dispara cuando se crea un nuevo rol
/// </summary>
public record RoleCreatedEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(RoleCreatedEventV1);
    
    public required Guid RoleId { get; init; }
    public required string RoleName { get; init; }
    public required string Description { get; init; }
    public required Guid CreatedBy { get; init; }
    public List<string>? Permissions { get; init; }
    public EventMetadata? Metadata { get; init; }
}
```

#### PermissionGrantedEventV1

```csharp
namespace IODA.Shared.Contracts.Events.V1;

/// <summary>
/// Se dispara cuando se otorga un permiso a un usuario o rol
/// </summary>
public record PermissionGrantedEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(PermissionGrantedEventV1);
    
    public required Guid TargetId { get; init; }
    public required string TargetType { get; init; } // "User" o "Role"
    public required string Permission { get; init; }
    public required string Resource { get; init; }
    public required Guid GrantedBy { get; init; }
    public Guid? ProjectId { get; init; }
    public Guid? EnvironmentId { get; init; }
    public EventMetadata? Metadata { get; init; }
}
```

#### PermissionRevokedEventV1

```csharp
namespace IODA.Shared.Contracts.Events.V1;

/// <summary>
/// Se dispara cuando se revoca un permiso
/// </summary>
public record PermissionRevokedEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(PermissionRevokedEventV1);
    
    public required Guid TargetId { get; init; }
    public required string TargetType { get; init; }
    public required string Permission { get; init; }
    public required string Resource { get; init; }
    public required Guid RevokedBy { get; init; }
    public required string Reason { get; init; }
    public EventMetadata? Metadata { get; init; }
}
```

---

### 4.4 Publishing Service Events

#### ContentPublishedEventV1

```csharp
namespace IODA.Shared.Contracts.Events.V1;

/// <summary>
/// Se dispara cuando un contenido se publica
/// </summary>
public record ContentPublishedEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(ContentPublishedEventV1);
    
    public required Guid ContentId { get; init; }
    public required Guid VersionId { get; init; }
    public required string Title { get; init; }
    public required string ContentType { get; init; }
    public required Guid PublishedBy { get; init; }
    public required DateTime PublishedAt { get; init; }
    public Guid? ScheduledPublishId { get; init; }
    public EventMetadata? Metadata { get; init; }
}
```

#### ContentUnpublishedEventV1

```csharp
namespace IODA.Shared.Contracts.Events.V1;

/// <summary>
/// Se dispara cuando se despublica un contenido
/// </summary>
public record ContentUnpublishedEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(ContentUnpublishedEventV1);
    
    public required Guid ContentId { get; init; }
    public required Guid UnpublishedBy { get; init; }
    public required string Reason { get; init; }
    public required DateTime UnpublishedAt { get; init; }
    public EventMetadata? Metadata { get; init; }
}
```

#### WorkflowStateChangedEventV1

```csharp
namespace IODA.Shared.Contracts.Events.V1;

/// <summary>
/// Se dispara cuando un contenido cambia de estado en un workflow
/// </summary>
public record WorkflowStateChangedEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(WorkflowStateChangedEventV1);
    
    public required Guid ContentId { get; init; }
    public required Guid WorkflowId { get; init; }
    public required string PreviousState { get; init; }
    public required string NewState { get; init; }
    public required Guid ChangedBy { get; init; }
    public string? Comment { get; init; }
    public EventMetadata? Metadata { get; init; }
}
```

---

### 4.5 Indexing Service Events

#### ContentIndexedEventV1

```csharp
namespace IODA.Shared.Contracts.Events.V1;

/// <summary>
/// Se dispara cuando un contenido se indexa exitosamente
/// </summary>
public record ContentIndexedEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(ContentIndexedEventV1);
    
    public required Guid ContentId { get; init; }
    public required string ContentType { get; init; }
    public required string IndexName { get; init; }
    public required DateTime IndexedAt { get; init; }
    public EventMetadata? Metadata { get; init; }
}
```

#### ContentRemovedFromIndexEventV1

```csharp
namespace IODA.Shared.Contracts.Events.V1;

/// <summary>
/// Se dispara cuando un contenido se elimina del índice
/// </summary>
public record ContentRemovedFromIndexEventV1 : EventBase
{
    public override int Version => 1;
    public override string EventType => nameof(ContentRemovedFromIndexEventV1);
    
    public required Guid ContentId { get; init; }
    public required string IndexName { get; init; }
    public required string Reason { get; init; }
    public required DateTime RemovedAt { get; init; }
    public EventMetadata? Metadata { get; init; }
}
```

---

## 5. Versionado de Eventos

### 5.1 Estrategia de Versionado

**Principio:** Nunca rompas compatibilidad con versiones antiguas

#### ✅ Cambios Permitidos (sin nueva versión)

- Agregar nuevas propiedades opcionales
- Agregar nuevos métodos auxiliares
- Mejorar documentación

```csharp
// V1 original
public record ContentCreatedEventV1 : EventBase
{
    public required Guid ContentId { get; init; }
    public required string Title { get; init; }
}

// V1 extendido (compatible)
public record ContentCreatedEventV1 : EventBase
{
    public required Guid ContentId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; } // ✅ Opcional, no rompe
}
```

#### ❌ Cambios que Requieren Nueva Versión

- Eliminar propiedades
- Cambiar tipos de propiedades
- Hacer requerida una propiedad opcional
- Cambiar el nombre de propiedades

```csharp
// V1
public record ContentCreatedEventV1 : EventBase
{
    public required Guid ContentId { get; init; }
    public required string Title { get; init; }
}

// ❌ INCORRECTO - Rompe compatibilidad
public record ContentCreatedEventV1 : EventBase
{
    public required Guid Id { get; init; } // Cambió nombre
    public required string Heading { get; init; } // Cambió nombre
}

// ✅ CORRECTO - Nueva versión
public record ContentCreatedEventV2 : EventBase
{
    public required Guid Id { get; init; }
    public required string Heading { get; init; }
}
```

### 5.2 Coexistencia de Versiones

Los servicios deben soportar múltiples versiones simultáneamente:

```csharp
// Subscriber soporta ambas versiones
public class ContentEventHandler
{
    public async Task Handle(ContentCreatedEventV1 @event)
    {
        // Lógica para V1
    }
    
    public async Task Handle(ContentCreatedEventV2 @event)
    {
        // Lógica para V2
    }
}
```

### 5.3 Deprecación de Eventos

Proceso:

1. **Crear nueva versión** (V2)
2. **Publicar ambas versiones** en paralelo durante 3-6 meses
3. **Notificar deprecación** de V1
4. **Monitorear uso** de V1
5. **Eliminar V1** cuando no haya consumidores

---

## 6. Manejo de Eventos

### 6.1 Publicación de Eventos (Publisher)

```csharp
// IODA.Shared.Infrastructure/Messaging/IEventPublisher.cs
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(
        TEvent @event, 
        CancellationToken cancellationToken = default) 
        where TEvent : IEvent;
        
    Task PublishBatchAsync<TEvent>(
        IEnumerable<TEvent> events, 
        CancellationToken cancellationToken = default) 
        where TEvent : IEvent;
}

// Uso en el servicio
public class ContentService
{
    private readonly IEventPublisher _eventPublisher;
    
    public async Task CreateContentAsync(CreateContentCommand command)
    {
        var content = Content.Create(/* ... */);
        await _repository.AddAsync(content);
        
        var @event = new ContentCreatedEventV1
        {
            ContentId = content.Id,
            Title = content.Title,
            ContentType = content.ContentType,
            Status = content.Status.ToString(),
            ProjectId = content.ProjectId,
            EnvironmentId = content.EnvironmentId,
            CreatedBy = command.UserId,
            Fields = content.Fields
        };
        
        await _eventPublisher.PublishAsync(@event);
    }
}
```

### 6.2 Consumo de Eventos (Subscriber)

```csharp
// Uso de MassTransit
public class ContentPublishedEventConsumer 
    : IConsumer<ContentPublishedEventV1>
{
    private readonly ILogger<ContentPublishedEventConsumer> _logger;
    private readonly ISearchIndexer _searchIndexer;
    
    public ContentPublishedEventConsumer(
        ILogger<ContentPublishedEventConsumer> logger,
        ISearchIndexer searchIndexer)
    {
        _logger = logger;
        _searchIndexer = searchIndexer;
    }
    
    public async Task Consume(ConsumeContext<ContentPublishedEventV1> context)
    {
        var @event = context.Message;
        
        _logger.LogInformation(
            "Processing ContentPublished event for Content {ContentId}",
            @event.ContentId);
        
        try
        {
            await _searchIndexer.IndexContentAsync(@event.ContentId);
            
            _logger.LogInformation(
                "Successfully indexed content {ContentId}",
                @event.ContentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to index content {ContentId}", 
                @event.ContentId);
            throw; // MassTransit manejará el retry
        }
    }
}
```

### 6.3 Configuración de RabbitMQ con MassTransit

```csharp
// Program.cs o DependencyInjection.cs
services.AddMassTransit(x =>
{
    // Registrar todos los consumers del assembly
    x.AddConsumers(typeof(ContentPublishedEventConsumer).Assembly);
    
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("ioda");
            h.Password("ioda_dev_password");
        });
        
        // Configurar endpoints automáticamente
        cfg.ConfigureEndpoints(context);
        
        // Configuración de retry
        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        
        // Circuit Breaker
        cfg.UseCircuitBreaker(cb =>
        {
            cb.TrackingPeriod = TimeSpan.FromMinutes(1);
            cb.TripThreshold = 15;
            cb.ActiveThreshold = 10;
            cb.ResetInterval = TimeSpan.FromMinutes(5);
        });
    });
});
```

---

## 📊 Diagrama de Flujo de Eventos

### Ejemplo: Creación y Publicación de Contenido

```
1. User Request
   ↓
2. CMS Core API
   ├─ CreateContent
   ├─ Save to DB
   └─ Publish: ContentCreatedV1
      ↓
3. RabbitMQ distributes to:
   ├─ Publishing Service
   │  └─ Validates workflow
   │     └─ Publish: WorkflowStateChangedV1
   │
   ├─ Authorization Service
   │  └─ Creates default permissions
   │
   └─ Audit Service (future)
      └─ Logs activity

4. User Publishes Content
   ↓
5. Publishing Service
   ├─ Validates content
   ├─ Changes state
   └─ Publish: ContentPublishedV1
      ↓
6. RabbitMQ distributes to:
   ├─ Indexing Service
   │  └─ Indexes in Elasticsearch
   │     └─ Publish: ContentIndexedV1
   │
   └─ Notification Service (future)
      └─ Sends notifications
```

---

## 📚 Mejores Prácticas

### ✅ DO

1. **Eventos Inmutables**: Usa `record` con `init`
2. **Propiedad Required**: Marca como `required` todo lo esencial
3. **UTC Timestamps**: Siempre usa `DateTime.UtcNow`
4. **Idempotencia**: Diseña handlers para ser idempotentes
5. **Logging**: Registra cada evento publicado y consumido
6. **Versionado**: Versiona desde el principio

### ❌ DON'T

1. **No Publiques Entidades**: Solo datos necesarios
2. **No Esperes Respuestas**: Los eventos son fire-and-forget
3. **No Falles Silenciosamente**: Lanza excepciones si algo falla
4. **No Uses Datos Sensibles**: Nunca passwords, tokens, etc.
5. **No Rompas Compatibilidad**: Crea nueva versión si es necesario

---

## 7. Integración cross-service y errores (Fase de mejoras)

### 7.1 Llamadas HTTP entre servicios

Cuando un servicio llama a otro por HTTP (ej. **Publishing** → **Core** para publicar contenido), los errores de la API llamada se propagan de forma estructurada:

- **CoreApiException** (en `IODA.Publishing.Application.Exceptions`): Encapsula la respuesta de error de Core (status code y cuerpo `ProblemDetails`). El cliente HTTP no usa `EnsureSuccessStatusCode()`; lee el cuerpo en caso de error y lanza `CoreApiException` con los detalles.
- **Publishing API**: El `ErrorHandlingMiddleware` captura `CoreApiException` y reexpone el `ProblemDetails` de Core en la respuesta (ajustando título si hace falta). Así el cliente del BFF/frontend recibe los mismos códigos y mensajes de validación que devolvería Core.

### 7.2 Consumidores opcionales de eventos del Core

- **ContentCreatedEventV1 / ContentUpdatedEventV1**: El servicio **Publishing** puede consumir estos eventos (p. ej. con MassTransit) para sincronizar estado o workflows. Es opcional y está previsto en futuras iteraciones (ver `BACKEND_STEPS.md` sección 3.3).
- **Contratos**: Los eventos definidos en este documento siguen siendo la fuente de verdad. Si se añaden consumidores, mantener idempotencia y no asumir orden de llegada.

---

**Última actualización:** 2026-01-24
