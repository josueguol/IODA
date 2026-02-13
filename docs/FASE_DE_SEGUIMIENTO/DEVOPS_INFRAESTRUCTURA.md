# Tareas DevOps e Infraestructura

Responsable: **Equipo DevOps / Infraestructura**.  
Referencia: [DIAGNOSTICO_TECNICO_CMS.md](../DIAGNOSTICO_TECNICO_CMS.md).

---

## Fase 1: Correcciones críticas (1–2 sprints)

### 1.1 Secretos en despliegue

- [ ] **docker-compose:** Las contraseñas y `Jwt__SecretKey` en `environment` en texto plano son aceptables solo para desarrollo local. Para producción (o entornos compartidos): usar Docker secrets o un vault (ej. Azure Key Vault, HashiCorp Vault) e inyectar variables en el compose o en el orquestador.
- [ ] Documentar cómo se inyectan en cada entorno (local, CI, staging, producción) las variables: `ConnectionStrings__DefaultConnection`, `Jwt__SecretKey`, RabbitMQ, `Media__StoragePath`.

---

### 1.2 CORS en despliegue

- [ ] Asegurar que en los pipelines o configuración de despliegue (Kubernetes, Docker Compose, etc.) las variables o config que definen “orígenes permitidos” para CORS estén definidas y no queden en `AllowAnyOrigin` en producción.
- [ ] Coordinar con Frontend la lista de orígenes por entorno.

---

### 1.3 Media.StoragePath en contenedores

- [ ] En Core API, `Media.StoragePath` en appsettings puede ser una ruta absoluta local que no existe en contenedor. Configurar en despliegue por **variable de entorno** o **volumen** montado, de modo que el directorio exista y sea persistente si se usan múltiples réplicas o reinicios.

---

## Fase 2: Mejoras (2–3 sprints)

### 2.1 Docker Compose — dependencias entre servicios

- [ ] En el `docker-compose` que incluya PostgreSQL y RabbitMQ: declarar `depends_on` de los APIs que los usan hacia postgres/rabbitmq.
- [ ] Donde sea posible, usar condición de tipo `service_healthy` (healthcheck de postgres/rabbitmq) para que los APIs no arranquen hasta que las dependencias estén listas y se reduzcan fallos al levantar el stack completo.

---

### 2.2 Health checks

- [ ] Definir health checks (endpoints o comprobaciones) para los servicios que usen base de datos y/o colas (Core, Identity, Authorization, Publishing, Indexing según corresponda).
- [ ] Exponer en cada API un endpoint de health (ej. `/health`) que compruebe conexión a BD y opcionalmente a RabbitMQ/Elasticsearch.
- [ ] En Dockerfile o docker-compose, usar estos health checks para `depends_on` con condición `service_healthy` y para orquestadores (Kubernetes liveness/readiness).

---

## Fase 3: Optimización

### 3.1 Dockerfiles

- [ ] Opcional: estandarizar los cinco Dockerfiles (Core, Identity, Authorization, Publishing, Indexing) en un Dockerfile base o script común para reducir duplicación (ej. healthcheck, usuario no root, pasos de seguridad).
- [ ] Los Dockerfiles actuales (multi-stage, SDK 9.0, aspnet 9.0) son correctos; esta tarea es de mantenibilidad.

---

## Criterios de aceptación

- En producción no se usan secretos en texto plano en el compose; se usan secrets o vault.
- Media.StoragePath configurado por variable/volumen y válido en contenedor.
- CORS en producción restringido y alineado con Frontend.
- Si se usa compose con postgres/rabbitmq, los APIs dependen de ellos y, si es posible, de su estado “healthy”.
- Health checks definidos y usados donde corresponda en despliegue.
