# Project Context – CMS Platform (.NET Microservices)

## 1. System Identity

Este proyecto es una plataforma CMS genérica, extensible y desacoplada.

No es un CMS preconfigurado.
Es una infraestructura de contenido basada en esquemas dinámicos.

El sistema es:

- Schema-driven (tipos definidos en runtime)
- Headless
- Microservicios reales (procesos independientes)
- API-First
- Event-driven
- Enterprise-ready
- Diseñado con DDD y Clean Architecture
- Security by design
- Observability-first

Repositorio: mono-repo.
Ejecución: múltiples procesos independientes.

---

## 2. Architectural Foundations

### Backend Stack
- C#
- .NET (ASP.NET Core)
- Base de datos por servicio
- Comunicación:
  - HTTP síncrono
  - Mensajería asíncrona (event-driven)

### Style
- DDD
- Clean Architecture
- SOLID obligatorio
- Separación estricta de capas

### Service Boundaries
Cada microservicio:

- Tiene su propio dominio
- Tiene su propia base de datos
- No comparte modelos de dominio
- Solo expone contratos vía API o eventos

---

## 3. Domain Model Philosophy

Conceptos centrales del ecosistema:

- Project
- Environment
- Site
- Content Type (runtime schema)
- Content Entry
- Identity
- Roles
- Permissions
- Publication lifecycle

Los tipos de contenido se definen en runtime.
El core no puede depender de tipos concretos.

---

## 4. Non-Negotiable Rules

- No lógica de dominio en Controllers.
- Controllers solo orquestan.
- Dominio no depende de infraestructura.
- Application layer no contiene reglas de negocio complejas.
- No acceso directo entre bases de datos de distintos servicios.
- No breaking changes sin versionado.
- Todo endpoint valida autorización.
- No permisos creados arbitrariamente desde UI.
- Todo evento debe estar versionado.
- No lógica distribuida sin consistencia definida.
- No acoplamiento implícito entre microservicios.

---

## 5. Consistency & Communication Model

Dentro de un agregado:
- Consistencia fuerte.

Entre microservicios:
- Eventual consistency.

Eventos:
- Versionados.
- Inmutables.
- Diseñados como contratos públicos.

No se asume sincronía entre servicios.

---

## 6. Security Model

- Autenticación basada en JWT.
- Autorización basada en roles + permisos.
- Scope por proyecto (multi-tenant lógico).
- Validación obligatoria por ProjectId.
- Seguridad aplicada desde diseño.

---

## 7. API Philosophy

- API-First.
- Contratos explícitos.
- Versionado obligatorio.
- Compatibilidad hacia atrás siempre que sea posible.
- DTOs nunca exponen entidades de dominio.

---

## 8. Observability

Cada microservicio debe incluir:

- Logging estructurado.
- Métricas.
- Trazas distribuidas.
- CorrelationId entre servicios.

Errores nunca silenciosos.
Eventos fallidos deben ser trazables.

---

## 9. Extensibility Model

El sistema debe permitir:

- Validadores de schema.
- Reglas de publicación.
- Integraciones externas.
- Extension points explícitos.

El core no debe modificarse para extender funcionalidad.

---

## 10. Scalability Assumptions

- Escalamiento horizontal por servicio.
- Aislamiento por base de datos.
- Multi-tenant por proyecto.
- Servicios desacoplados por contrato, no por implementación.

---

## 11. Architectural Risks

- Complejidad del schema runtime.
- Evolución de eventos sin versionado claro.
- Permisos mal modelados.
- Acoplamiento accidental vía DTOs compartidos.
- Falsa sensación de consistencia fuerte entre servicios.
