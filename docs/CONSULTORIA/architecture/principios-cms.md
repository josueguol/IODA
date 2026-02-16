# Principios del CMS: lista revisada y ampliada

**Contexto:** El proyecto se define como un CMS genérico, extensible y desacoplado. Esta es la lista de principios rectores revisada desde el rol de consultor de arquitectura, con correcciones y principios adicionales a considerar.

---

## 1. Lista actual (con corrección)

La lista que tenías:

- **Schema-driven** (tipos de contenido definidos en runtime)
- **Headless**
- **Microservicios**
- **100% SOLID**
- **Orientado a eventos**
- **Escalable y enterprise-ready**
- ~~Schema-Driven~~ *(duplicado; se deja una sola mención)*

Son todos válidos y coherentes con el sistema. Solo conviene quitar la repetición de "Schema-Driven".

---

## 2. Principios que ya están implícitos en el plan

En la sección "Principios rectores" y en la arquitectura del repo aparecen:

- **DDD (Domain Driven Design)**
- **Clean Architecture**
- **Event-Driven Architecture**

Recomendación: **elevarlos a la lista principal** de principios del proyecto. Son decisiones estructurales que condicionan todo el diseño; no son solo “patrones de implementación”. Quedaría explícito que el CMS es Schema-driven, Headless, Microservicios, SOLID, Orientado a eventos, Escalable y enterprise-ready **y** que se construye con DDD, Clean Architecture y Event-Driven Architecture.

---

## 3. Principios adicionales a considerar

| Principio | Descripción breve | Por qué añadirlo |
|-----------|-------------------|-------------------|
| **API-First / Contract-First** | Las APIs son el producto; contratos (OpenAPI, versionado) se definen y respetan desde el diseño. | En un CMS headless las APIs son la interfaz principal. Definir contratos primero reduce acoplamiento entre frontend y servicios y facilita evolución y versionado. |
| **Security by design** | Seguridad integrada desde el diseño (Identity, Authorization, permisos, JWT), no como capa añadida después. | Ya tienes Identity, Authorization, permisos y JWT. Declararlo como principio refuerza que nuevos features incluyan autorización y auditoría desde el inicio. |
| **Observabilidad** | Logs estructurados, métricas y trazas como parte del diseño operativo. | Ya está en la infra (logs, métricas, trazas). Subirlo a “principio” asegura que cada servicio y cada flujo crítico se diseñen con observabilidad desde el principio. |
| **Versionado y compatibilidad hacia atrás** | Estrategia clara para APIs, esquemas y eventos: versionado semántico, deprecación y evolución sin romper consumidores. | Schema-driven + eventos + headless implica evolución de esquemas, payloads y APIs. Sin principio explícito es fácil introducir cambios breaking. |
| **Multi-tenant / Alcance por proyecto** | El modelo de datos y el acceso están delimitados por proyecto (y opcionalmente por entorno/sitio). | Proyectos, entornos y sitios ya existen en el dominio. Declararlo como principio deja claro el modelo de aislamiento y escalado (por tenant/proyecto). |
| **Extensibilidad** | Puntos de extensión bien definidos (validadores de esquema, reglas de publicación, integraciones) sin recompilar el core. | Coincide con “extensible” en la visión. Hacerlo principio obliga a definir extension points (plugins, interfaces públicas) y evita que “extensible” quede en marketing. |
| **Eventual consistency (donde aplique)** | Donde haya flujos asíncronos (eventos, colas), se asume consistencia eventual y se documenta. | Orientado a eventos implica mensajería y procesamiento asíncrono. Declararlo evita suposiciones de consistencia fuerte donde no la hay y guía el diseño de UIs y APIs. |

No es obligatorio adoptar los siete a la vez. Los más alineados con lo que ya haces y con “enterprise-ready” son: **API-First**, **Security by design**, **Observabilidad** y **Versionado/compatibilidad**. **Multi-tenant**, **Extensibilidad** y **Eventual consistency** son opcionales pero recomendables según el roadmap.

---

## 4. Lista unificada sugerida

Versión consolidada para documentación y visión del producto:

**Principios de producto y arquitectura**

- Schema-driven (tipos de contenido definidos en runtime)
- Headless
- Microservicios
- 100% SOLID
- Orientado a eventos (event-driven)
- Escalable y enterprise-ready
- DDD (Domain Driven Design)
- Clean Architecture
- API-First / Contract-First
- Security by design
- Observabilidad (logs, métricas, trazas)
- Versionado y compatibilidad hacia atrás

**Opcionales según alcance**

- Multi-tenant / Alcance por proyecto
- Extensibilidad (extension points definidos)
- Eventual consistency (donde aplique)

---

## Referencias

- Plan de trabajo: `docs/000-FASE_DE_CREACION_INICIAL/PLAN_DE_TRABAJO.md`
- Convenciones y eventos: `docs/000-FASE_DE_CREACION_INICIAL/docs/CONVENTIONS.md`, `EVENTS.md`
