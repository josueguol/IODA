# 🧠 IODA CMS - Schema-Driven Distributed Content Management System

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Architecture](https://img.shields.io/badge/architecture-Clean%20Architecture-green.svg)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
[![DDD](https://img.shields.io/badge/pattern-DDD-orange.svg)](https://martinfowler.com/tags/domain%20driven%20design.html)

## 📖 Descripción

**IODA CMS** es un sistema de gestión de contenidos enterprise, **schema-driven**, **headless** y **distribuido** construido sobre principios SOLID, Clean Architecture y Domain-Driven Design.

### ✨ Características Principales

- 🎯 **Schema-Driven**: Tipos de contenido definidos en runtime sin modificar código
- 🔌 **Headless & API-First**: Separación total entre backend y frontend
- 🏗️ **Microservicios**: Servicios independientes y escalables
- 📦 **SOLID**: 100% adherencia a principios SOLID
- 🎨 **Clean Architecture**: Separación de responsabilidades en capas
- 🌊 **Event-Driven**: Comunicación asíncrona mediante eventos
- 📝 **Versionado de Contenido**: Control total del historial
- 🔐 **Security by Design**: Autenticación y autorización desacopladas
- 🚀 **Enterprise-Ready**: Escalable, mantenible y extensible

---

## 🏛️ Arquitectura

### Servicios

```
┌─────────────────────────────────────────────────────────────┐
│                        API Gateway                          │
│                    (Opcional - Futuro)                      │
└─────────────────────────────────────────────────────────────┘
                              │
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
┌───────▼────────┐   ┌────────▼────────┐   ┌───────▼────────┐
│  CMS Core      │   │    Identity     │   │ Authorization  │
│  Service       │   │    Service      │   │    Service     │
│                │   │                 │   │                │
│ • Content      │   │ • Auth          │   │ • Roles        │
│ • Schemas      │   │ • JWT           │   │ • Permissions  │
│ • Versions     │   │ • Refresh Token │   │ • Policies     │
└────────┬───────┘   └────────┬────────┘   └───────┬────────┘
         │                    │                    │
         │          ┌─────────▼────────┐           │
         │          │    RabbitMQ      │◄──────────┘
         │          │   (Event Bus)    │
         │          └─────────┬────────┘
         │                    │
    ┌────▼──────────┐  ┌──────▼─────────┐
    │  Publishing   │  │   Indexing     │
    │   Service     │  │   Service      │
    │               │  │                │
    │ • Workflows   │  │ • Search       │
    │ • States      │  │ • Elastic      │
    └───────────────┘  └────────────────┘
```

### Estructura de Proyecto (Clean Architecture)

Cada servicio sigue esta estructura:

```
Service/
├── Domain/              # Entidades, Value Objects, Interfaces
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Repositories/    # Interfaces
│   ├── Events/
│   └── Exceptions/
├── Application/         # Casos de uso, DTOs, Validators
│   ├── UseCases/
│   ├── DTOs/
│   ├── Validators/
│   ├── Interfaces/
│   └── Behaviors/
├── Infrastructure/      # Implementaciones, DB, Messaging
│   ├── Persistence/
│   ├── Messaging/
│   ├── External/
│   └── Configuration/
└── API/                 # Controllers, Middleware, Startup
    ├── Controllers/
    ├── Middleware/
    ├── Filters/
    └── Program.cs
```

---

## 🚀 Inicio Rápido

### Prerrequisitos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [PostgreSQL 16](https://www.postgresql.org/)
- [RabbitMQ](https://www.rabbitmq.com/)

### Configuración Local

1. **Clonar el repositorio**

```bash
git clone <repository-url>
cd ioda
```

2. **Levantar infraestructura con Docker**

```bash
docker-compose up -d
```

3. **Restaurar dependencias**

```bash
dotnet restore
```

4. **Ejecutar migraciones**

```bash
# Por cada servicio que use base de datos
cd src/Services/Core/IODA.Core.API
dotnet ef database update
```

5. **Ejecutar servicios**

```bash
# Terminal 1 - CMS Core
cd src/Services/Core/IODA.Core.API
dotnet run

# Terminal 2 - Identity
cd src/Services/Identity/IODA.Identity.API
dotnet run

# ... y así sucesivamente
```

---

## 📚 Documentación

- [Plan de Trabajo](PLAN_DE_TRABAJO.md)
- [Guía de Arquitectura](docs/ARCHITECTURE.md) *(próximamente)*
- [Convenciones de Código](docs/CONVENTIONS.md) *(próximamente)*
- [Guía de Eventos](docs/EVENTS.md) *(próximamente)*

---

## 🛠️ Stack Tecnológico

| Tecnología | Propósito |
|-----------|-----------|
| .NET 8.0 | Framework principal |
| ASP.NET Core | API Web |
| Entity Framework Core | ORM |
| PostgreSQL | Base de datos |
| RabbitMQ | Message Broker |
| MassTransit | Abstracción de mensajería |
| FluentValidation | Validaciones |
| MediatR | CQRS / Mediator Pattern |
| Serilog | Logging estructurado |
| xUnit | Testing |

---

## 🎯 Principios SOLID

### Single Responsibility Principle (SRP)
Cada servicio tiene una única responsabilidad bien definida.

### Open/Closed Principle (OCP)
Nuevos tipos de contenido se agregan mediante esquemas, sin modificar el core.

### Liskov Substitution Principle (LSP)
Contratos claros y respetados entre servicios.

### Interface Segregation Principle (ISP)
APIs específicas y pequeñas por contexto.

### Dependency Inversion Principle (DIP)
Dependencias mediante abstracciones (interfaces y eventos).

---

## 📝 Convenciones de Código

### Nomenclatura

- **Namespaces**: `IODA.{Service}.{Layer}.{Feature}`
- **Clases**: PascalCase
- **Interfaces**: `I` + PascalCase
- **Métodos**: PascalCase
- **Variables**: camelCase
- **Constantes**: UPPER_SNAKE_CASE

### Eventos

Formato: `{Aggregate}{Action}{Version}`

Ejemplos:
- `ContentCreatedV1`
- `SchemaUpdatedV1`
- `ContentPublishedV1`

### Commits

Formato: `type(scope): description`

Tipos:
- `feat`: Nueva funcionalidad
- `fix`: Corrección de bug
- `refactor`: Refactorización
- `docs`: Documentación
- `test`: Tests
- `chore`: Tareas de mantenimiento

---

## 🧪 Testing

```bash
# Ejecutar todos los tests
dotnet test

# Con cobertura
dotnet test /p:CollectCoverage=true
```

---

## 📦 Deployment

*(Documentación próximamente)*

---

## 🤝 Contribución

*(Guidelines próximamente)*

---

## 📄 Licencia

Este proyecto está bajo la licencia MIT.

---

## 👥 Equipo

Desarrollado con 💙 por el equipo IODA

---

## 📞 Soporte

Para preguntas o soporte, por favor abrir un issue en el repositorio.
# IODA


## IA Commands

# ARTCHITECT
```
Crea plan de trabajo para el fullstack de los requerimientos en docs/006-SCHEME-N-SITECONFIG/REQUERIMIENTOS.md
Actúa como definido en ai/agents/architect.agent.md
Antes de responder, valida que tu propuesta no contradiga:
- ai/memory/project.context.md
- ai/memory/decisions.log.md
````

# FULLSTACK
```
Realiza las tareas definidas en docs/006-SCHEME-N-SITECONFIG/PLAN_DE_TRABAJO_FULLSTACK.md
Actúa como definido en ai/agents/fullstack.agent.md
Antes de desarrollar, valida que no violes lo definido en:
- ai/memory/project.context.md
- ai/memory/decisions.log.md
```
