# 🧠 CMS Genérico Distribuido (Schema-Driven) – Arquitectura y Plan de Construcción

## 1. Visión General

Este proyecto define un **CMS genérico, extensible y desacoplado**, diseñado bajo los siguientes principios:

* **Schema-driven** (tipos de contenido definidos en runtime)
* **Headless**
* **Microservicios**
* **100% SOLID**
* **Orientado a eventos**
* **Escalable y enterprise-ready**

El sistema permite crear tipos de contenido dinámicos (Video, Article, Gallery, etc.) que **extienden un Content base sin herencia rígida**, usando esquemas almacenados y validados en tiempo de ejecución.

---

## 2. Principios Rectores

* **S (Single Responsibility):** cada servicio cumple un propósito único
* **O (Open/Closed):** nuevos tipos de contenido sin modificar el core
* **L (Liskov):** contratos claros entre servicios
* **I (Interface Segregation):** APIs pequeñas y específicas
* **D (Dependency Inversion):** dependencias vía contratos y eventos

Arquitectura basada en:

* DDD (Domain Driven Design)
* Clean Architecture
* Event-Driven Architecture

---

## 3. Arquitectura General del Sistema

### Servicios principales

* CMS Core Service
* Identity Service
* Access Rules Service
* Publishing Service
* Indexing Service
* Schema Validation Service (opcional)
* Media Service (opcional)

### Infraestructura transversal

* RabbitMQ (event bus)
* API Gateway (opcional)
* PostgreSQL
* Observabilidad (logs, métricas, trazas)

---

## 4. Fase 0 – Fundamentos del Proyecto

### Objetivo

Dejar listo el terreno técnico para que los servicios crezcan sin fricción.

#### Tareas

* [x] Crear repositorio principal (mono-repo o multi-repo)
* [x] Definir convenciones de naming y versionado
* [x] Crear solución base `.sln`
* [x] Definir estructura de carpetas por servicio
* [x] Configurar Docker y Docker Compose
* [x] Configurar RabbitMQ en entorno local (referencia a red externa)
* [x] Definir contratos de eventos (event names + payloads)
* [x] Definir política de versionado de eventos
* [x] Crear Building Blocks compartidos (Entity, ValueObject, Specification, etc.)
* [x] Crear Contratos compartidos (Events V1)
* [x] Documentación completa (CONVENTIONS.md + EVENTS.md)

**Resultado esperado:** ✅ **COMPLETADO**
- Estructura de solución lista con 26 proyectos definidos
- Building Blocks DDD implementados y documentados
- 10+ eventos versionados definidos
- 50+ páginas de documentación enterprise
- Docker Compose configurado para infraestructura

---

## 5. Fase 1 – CMS Core Service (Almacenamiento)

### Responsabilidad

> Almacenar contenido, esquemas y versiones. Nada más.

### Componentes internos

* Domain
* Application
* Infrastructure
* API

### Tareas – Dominio

* [x] Definir entidades base:

  * Project
  * Environment
  * ~~Site~~ (pendiente evolución)
  * ~~Section~~ (pendiente evolución)
  * ~~Category~~ (pendiente evolución)
  * ~~Tag~~ (pendiente evolución)
  * Content
  * ContentVersion
  * ContentSchema
* [x] Definir Value Objects (Slug, Status, Identifier)
* [x] Definir interfaces de repositorio
* [x] Definir eventos de dominio

### Tareas – Esquemas

* [x] Definir modelo de FieldDefinition
* [x] Definir tipos básicos de campo (string, number, boolean, date, enum, json, reference)
* [x] Definir modelo de ContentSchema
* [x] Permitir herencia lógica de esquemas (extends Content) — contenido schema-driven
* [x] Persistir esquemas en base de datos

### Tareas – Persistencia

* [x] Configurar PostgreSQL
* [x] Implementar JSONB para campos dinámicos
* [x] Implementar migraciones
* [x] Implementar versionado automático de contenido

### Tareas – API

* [x] Crear endpoints genéricos:

  * Crear contenido
  * Actualizar contenido
  * Obtener contenido
  * Listar contenido por tipo
  * (además: proyectos, schemas, publicar/despublicar, versiones, contenido publicado)
* [x] Emitir eventos:

  * ContentCreated
  * ContentUpdated
  * SchemaCreated
  * SchemaUpdated
  * ContentPublished / ContentUnpublished

**Resultado esperado:** ✅ **COMPLETADO**
Se puede crear cualquier tipo de contenido definido por esquema y versionarlo.

---

## 6. Fase 2 – Identity Service (Autenticación)

### Responsabilidad

> Saber quién es el usuario.

#### Tareas

* [x] Implementar registro (POST /api/auth/register, email + contraseña + displayName opcional)
* [x] Implementar login (POST /api/auth/login, email + contraseña)
* [x] Generar JWT (access token con Issuer, Audience, ExpirationMinutes)
* [x] Implementar refresh tokens (POST /api/auth/refresh, almacenamiento en BD)
* [ ] Integrar proveedor externo (opcional)
* [x] Emitir eventos de autenticación (UserLoggedInEventV1, IAuthEventPublisher; implementación NoOp, sustituible por MassTransit)
* [x] Dockerfile y servicio en docker-compose (puerto 5002)
* [x] Documentación COMO_PROBAR_FASE_2.md

**Resultado esperado:** ✅ **COMPLETADO**
El sistema identifica usuarios de forma independiente al CMS Core (registro, login, refresh token, JWT). Documentación de pruebas en COMO_PROBAR_FASE_2.md.

---

## 7. Fase 3 – Access Rules Service (Autorización)

### Responsabilidad

> Decidir qué puede hacer cada usuario.

#### Tareas

* [x] Definir modelo de Roles (Role, RolePermission)
* [x] Definir modelo de Permisos (Permission)
* [x] Definir reglas contextuales (AccessRule):

  * por proyecto (ProjectId)
  * por entorno (EnvironmentId)
  * por tipo de contenido (SchemaId)
  * por estado (ContentStatus)
* [x] Exponer API de autorización (check, roles, permissions, rules; CQRS con MediatR)
* [ ] Consumir eventos de Identity (opcional: MassTransit consumer para UserLoggedInEventV1)
* [x] Dockerfile y servicio en docker-compose (puerto 5003)
* [x] Documentación FASE_3_ACCESS_RULES.md y COMO_PROBAR_FASE_3.md

**Resultado esperado:** ✅ **COMPLETADO**
Cualquier servicio puede preguntar: *¿este usuario puede hacer esto aquí?* (POST /api/authorization/check). Guía de pruebas en COMO_PROBAR_FASE_3.md.

---

## 8. Fase 4 – Publishing Service

### Responsabilidad

> Controlar el ciclo de vida del contenido.

#### Tareas

* [x] Definir estados del contenido (PublicationRequest: Pending, Approved, Rejected)
* [ ] Definir workflows configurables (opcional: WorkflowDefinition en futuras iteraciones)
* [x] Validar contenido antes de publicar (IContentValidator, ContentValidator; llama a Core API)
* [ ] Consumir eventos del CMS Core (opcional: MassTransit consumer para ContentCreated/ContentUpdated)
* [x] Emitir eventos de publicación (Core API emite ContentPublishedEventV1 al publicar; Publishing llama a Core API)
* [x] API: solicitar publicación, aprobar (valida + llama Core API publish), rechazar; listar solicitudes
* [x] Dockerfile y servicio en docker-compose (puerto 5004)
* [x] Documentación FASE_4_PUBLISHING.md y COMO_PROBAR_FASE_4.md

**Resultado esperado:** ✅ **COMPLETADO**
El contenido solo se publica si cumple validación y aprobación (Publishing valida y llama a Core API para publicar). Guía de pruebas en COMO_PROBAR_FASE_4.md.

---

## 9. Fase 5 – Indexing Service

### Responsabilidad

> Indexar solo contenido publicado.

#### Tareas

* [x] Integrar motor de búsqueda (Elasticsearch vía Elastic.Clients.Elasticsearch 8.x)
* [x] Definir modelo de indexación (IndexedContentDocument, IndexedContentDoc, IContentIndexer)
* [x] Consumir eventos de publicación (ContentPublishedEventV1, ContentUnpublishedEventV1 con MassTransit)
* [x] Eliminar contenido despublicado del índice (ContentUnpublishedEventV1Consumer, RemoveAsync)
* [x] API: búsqueda (GET search), indexar manual (POST index), eliminar del índice (DELETE index/{contentId})
* [x] NoOp cuando Elasticsearch/RabbitMQ deshabilitados
* [x] Dockerfile y servicio en docker-compose (puerto 5005)
* [x] Documentación FASE_5_INDEXING.md y COMO_PROBAR_FASE_5.md

**Resultado esperado:** ✅ **COMPLETADO**
El buscador refleja exactamente el contenido publicado. Indexación automática vía eventos y API para búsqueda e indexación manual.

---

## 10. Fase 6 – Servicios Opcionales

### Schema Validation Service

* [ ] Validación centralizada por esquema
* [ ] Reglas reutilizables
* [ ] Evolución sin romper contenido existente

### Media Service

* [ ] Subida de archivos
* [ ] Versionado de media
* [ ] Metadatos
* [ ] Integración con CDN

---

## 11. Comunicación entre Servicios

### RabbitMQ

* Eventos inmutables
* Payloads versionados
* Comunicación asíncrona
* Bajo acoplamiento

**Flujo típico:**

1. CMS Core guarda contenido
2. Evento `ContentCreated`
3. Publishing decide estado
4. Evento `ContentPublished`
5. Indexing indexa

---

## 12. Seguridad

* Identity aislado
* Autorización desacoplada
* Tokens verificados por gateway o middleware
* Zero trust entre servicios

---

## 13. Alcances Potenciales

* Multi-tenant
* White-label
* SaaS
* CMS enterprise
* Multicanal
* Integración con IA
* Plugins y extensiones

---

## 14. Prompt para Editor de Código con IA

```
Actúa como un arquitecto senior en C# y .NET.

Estoy construyendo un CMS genérico, distribuido y schema-driven basado en microservicios.

Requisitos:
- 100% SOLID
- Clean Architecture
- DDD
- RabbitMQ
- PostgreSQL + JSONB
- Esquemas dinámicos
- APIs genéricas
- Versionado de contenido
- Servicios separados: Core, Identity, Authorization, Publishing, Indexing

Ayúdame a implementar el servicio indicado siguiendo contratos claros, bajo acoplamiento y principios enterprise.
```

---

## 15. Nota Final

Este CMS es una **plataforma extensible**, no un producto cerrado.
El valor está en la **arquitectura limpia y la evolución segura**.
