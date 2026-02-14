# Evaluación: librerías de mensajería (RabbitMQ) sin licencias de pago

## Requisito

Usar librerías **libres y sin licencias comerciales** para publicar/consumir mensajes en RabbitMQ, en desarrollo y en producción.

---

## Opciones evaluadas

### 1. MassTransit 8.x (actual)

| Aspecto | Detalle |
|--------|--------|
| **Licencia** | Apache 2.0 – libre en dev y producción |
| **RabbitMQ** | Transport oficial, estable |
| **Uso en IODA** | Solo publicar eventos (`IEventPublisher` → `IPublishEndpoint.Publish`) |
| **Estado** | Ya integrado en Core (8.3.4); sin coste ni restricciones |

**Conclusión:** Cumple el requisito. No hay motivo técnico ni de licencia para cambiarlo.

---

### 2. NServiceBus

| Aspecto | Detalle |
|--------|--------|
| **Licencia** | Comercial. Gratis solo en **desarrollo**; en **producción** se requiere licencia de pago (Particular Software). |
| **RabbitMQ** | Transport oficial (NServiceBus.RabbitMQ). |
| **Restricciones** | No impone límites técnicos si no hay licencia (sigue funcionando), pero el uso en producción sin licencia no cumple los términos del producto. |

**Conclusión:** **No cumple** el requisito de “sin licencias de paga” en producción. No se recomienda para este proyecto si se quiere evitar cualquier licencia comercial.

---

### 3. EasyNetQ

| Aspecto | Detalle |
|--------|--------|
| **Licencia** | MIT – totalmente libre |
| **RabbitMQ** | Enfocado solo en RabbitMQ; API sencilla (Pub/Sub, Request/Response). |
| **Esfuerzo** | Habría que sustituir la implementación de `IEventPublisher`: usar EasyNetQ en lugar de MassTransit, nueva configuración en DI y conexión a RabbitMQ. La interfaz `IEventPublisher` puede mantenerse. |
| **Ecosistema** | Menos transports y menos “framework” que MassTransit; suficiente para publicar eventos. |

**Conclusión:** **Alternativa válida** si se prefiere una librería MIT-only o más simple. Implica refactor de infraestructura de mensajería, sin beneficio claro frente a MassTransit 8 en nuestro caso de uso actual.

---

## Recomendación

- **Mantener MassTransit 8.x** como solución de mensajería:
  - Licencia Apache 2.0, sin coste ni obligación de licencia comercial en ningún entorno.
  - Ya integrado, estable y adecuado para publicar eventos.
  - Evita trabajo de migración y posibles regresiones.

- **No adoptar NServiceBus** si el objetivo es no depender de licencias de pago: en producción sí requiere licencia comercial.

- **EasyNetQ** queda como opción de respaldo si en el futuro se quiere:
  - Una dependencia explícitamente MIT, o
  - Un cliente más simple y solo-RabbitMQ,

  a cambio de un refactor acotado en la capa de infraestructura de mensajería.

---

## Resumen

| Librería      | Licencia (prod) | ¿Cumple requisito? | Acción recomendada   |
|---------------|------------------|---------------------|------------------------|
| MassTransit 8 | Apache 2.0       | Sí                  | **Mantener**           |
| NServiceBus   | Comercial        | No                  | No usar para este fin |
| EasyNetQ      | MIT              | Sí                  | Alternativa posible    |

**Decisión:** Seguir con **MassTransit 8.3.4** y documentar en el proyecto que no se usan versiones 9+ (licencia comercial) ni NServiceBus en producción sin licencia.
