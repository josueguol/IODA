# ¿El CMS es modular monolith o microservicios reales?

**Pregunta:** ¿El CMS es modular monolith por ahora o ya es microservicios reales?

---

## Respuesta: **Microservicios reales** (en runtime y datos)

En el estado actual del proyecto, el sistema se comporta como **microservicios reales**, no como modular monolith. La única parte “monolítica” es el **repositorio** (mono-repo).

---

## Criterios que lo sitúan como microservicios

| Criterio | Estado en el proyecto |
|----------|------------------------|
| **Procesos separados** | Cada servicio es un proceso distinto: Core, Identity, Authorization, Publishing, Indexing. Cada uno tiene su propio `Program.cs`, su propio contenedor en `docker-compose` (ioda-core-api, ioda-identity-api, etc.) y su propio puerto. |
| **Despliegue independiente** | Cada servicio tiene su propio Dockerfile y se puede construir y desplegar por separado. No hay un único binario que agrupe todos los dominios. |
| **Base de datos por servicio** | Cada servicio tiene su propia base de datos (mismo servidor PostgreSQL en desarrollo, pero **bases distintas**): `ioda_core`, `ioda_identity`, `ioda_authorization`, `ioda_publishing`. Indexing usa Elasticsearch, sin BD relacional propia para su dominio. No hay una única BD compartida con tablas de todos los dominios. |
| **Comunicación por red** | Los servicios se hablan por HTTP (Identity → Authorization para permisos efectivos; Publishing → Core para publicar) y por mensajería (RabbitMQ). No hay llamadas en proceso ni referencias directas entre “módulos” en un mismo proceso. |

Con esto, a nivel de **runtime** y **datos** el sistema cumple la definición habitual de microservicios: múltiples servicios desplegables, cada uno con su almacenamiento y comunicándose por red.

---

## Lo que podría recordar a “monolith” (y por qué no lo es)

- **Mono-repo (una sola solución .sln):** Todo el código está en un único repositorio y una única solución. Eso es una decisión de **organización del código**, no de arquitectura de ejecución. Sigue habiendo varios ejecutables y varios contenedores. Muchos equipos usan mono-repo para microservicios.
- **Mismo servidor PostgreSQL en desarrollo:** En `docker-compose` todos apuntan a `Host=postgres` pero con `Database=ioda_*` distinto. Es decir, **una instancia, varias bases**. Eso sigue siendo “database per service” a nivel lógico; en producción cada servicio podría tener incluso su propio clúster de BD si se quisiera.

Por tanto: **no es modular monolith**. Un modular monolith sería un **solo** proceso (una sola API o host) con varios módulos/bounded contexts dentro, y típicamente una base de datos compartida o un esquema único. Aquí hay varios procesos y varias bases.

---

## Resumen

| Pregunta | Respuesta |
|----------|-----------|
| ¿Modular monolith? | **No.** No hay un único proceso que agrupe todos los dominios. |
| ¿Microservicios reales? | **Sí.** Procesos separados, base de datos por servicio, comunicación por HTTP y mensajería. |
| ¿Dónde está lo “mono”? | En el **repositorio**: una solución con todos los proyectos (mono-repo). Eso no convierte el sistema en monolith a nivel de ejecución. |

Si en el futuro se empaquetaran todos los servicios en un único proceso (por ejemplo un único host que cargue Core + Identity + Authorization + … como “módulos”), entonces sí se pasaría a un **modular monolith**. Con la configuración actual (contenedores y bases separadas), el CMS se considera **microservicios reales**.
