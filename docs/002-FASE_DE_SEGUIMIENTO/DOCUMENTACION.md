# Tareas Documentación

Responsable: **Tech Lead / Arquitectura**.  
Referencia: [DIAGNOSTICO_TECNICO_CMS.md](../DIAGNOSTICO_TECNICO_CMS.md) y README del proyecto.

---

## Fase 2 y Fase 3

### 2.1 Convenciones

- [ ] Crear o completar **CONVENTIONS.md** (o equivalente en `docs/`) con:
  - Convenciones de commits (si no están ya en README).
  - Nomenclatura (proyectos, namespaces, DTOs, Commands/Queries).
  - Uso de eventos de dominio y mensajería.
  - Reglas de capas (API no depende de Domain para repositorios; excepciones, etc.).
- [ ] Ubicar el documento donde el equipo lo use (raíz del repo o `docs/`); enlazarlo desde el README principal.

---

### 3.1 Arquitectura

- [ ] Crear **ARCHITECTURE.md** (o equivalente) que describa:
  - Servicios (Core, Identity, Authorization, Publishing, Indexing) y sus responsabilidades.
  - Flujo de publicación (Core → Publishing → Indexing) y uso de RabbitMQ.
  - Capas por servicio (Domain, Application, Infrastructure, API) y dependencias permitidas.
  - Decisiones relevantes: CORS, JWT, almacenamiento de media, uso de signed URLs o AllowAnonymous donde aplique.
- [ ] Incluir un diagrama de alto nivel (opcional pero recomendable) y referencias a los documentos de convenciones y eventos (EVENTS.md, etc.).

---

### 3.2 Decisiones de seguridad y despliegue

- [ ] Documentar en ARCHITECTURE.md o en un doc específico:
  - Dónde se gestionan los secretos por entorno (User Secrets, variables de entorno, vault).
  - Comportamiento esperado si falta JWT SecretKey o ConnectionString en no-Development (fallo de arranque).
  - Uso de `[AllowAnonymous]` en MediaController.GetFile y bajo qué condiciones (público vs signed URL/token).

---

## Criterios de aceptación

- CONVENTIONS.md existe y está referenciado; cubre commits, nomenclatura, capas y eventos.
- ARCHITECTURE.md describe servicios, capas, flujos y decisiones de seguridad/despliegue relevantes.
- El README principal enlaza a estos documentos para onboarding y mantenimiento.
