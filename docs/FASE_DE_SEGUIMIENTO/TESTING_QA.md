# Tareas Testing y QA

Responsable: **Equipo QA / Backend (tests automatizados)**.  
Referencia: [DIAGNOSTICO_TECNICO_CMS.md](../DIAGNOSTICO_TECNICO_CMS.md).

---

## Contexto

El diagnóstico indica **0% de cobertura**: no existe ningún proyecto `*Tests` o `*.UnitTests` en la solución. `Directory.Build.props` ya define paquetes para proyectos que contengan "Test" en el nombre, pero no hay proyectos de test. Esto impide refactorizar con seguridad y aumenta el riesgo de regresiones.

---

## Fase 2: Introducción de tests (2–3 sprints)

### 2.1 Estructura de proyectos de test

- [ ] Añadir proyectos de test para al menos **Core** e **Identity** (ej. `IODA.Core.UnitTests`, `IODA.Core.IntegrationTests`, `IODA.Identity.UnitTests`, `IODA.Identity.IntegrationTests`).
- [ ] Referenciar los proyectos de aplicación y dominio correspondientes; usar las mismas versiones de paquetes que el resto de la solución (ya preparado en Directory.Build.props para proyectos con "Test" en el nombre).
- [ ] Incluir estos proyectos en la solución (`IODA.sln`).

---

### 2.2 Unit tests — prioridad

- [ ] **Handlers críticos:** Unit tests para CreateContentCommandHandler, Publish/Unpublish, ApprovePublicationCommandHandler (Publishing), Login/Register (Identity), CheckAccessQuery (Authorization). Usar mocks para repositorios y dependencias externas.
- [ ] **Validadores:** Unit tests para los FluentValidators de comandos de Content, Schemas, Sites y, cuando exista, UploadMediaCommandValidator.
- [ ] **SchemaValidationService (Core):** Unit tests que cubran los distintos tipos (string, number, boolean, date, enum, etc.) y casos límite (null, formato inválido).
- [ ] **Dominio (opcional pero recomendable):** Tests para entidades y value objects con lógica (ej. Content, ContentSchema) donde haya reglas de negocio o eventos de dominio.

---

### 2.3 Integration tests

- [ ] **API:** Tests de integración que llamen a los endpoints de Core e Identity (y opcionalmente Authorization, Publishing, Indexing) con cliente HTTP, validando respuestas 200/400/401/404 según el caso.
- [ ] **Persistencia:** Tests que usen una base de datos real o en contenedor (Testcontainers) para repositorios de Core e Identity, verificando que los handlers persisten y leen correctamente.
- [ ] Usar **Testcontainers** para PostgreSQL (y opcionalmente RabbitMQ/Elasticsearch) en los integration tests para no depender de instancias compartidas.

---

### 2.4 Cobertura y CI

- [ ] Configurar **coverlet** (o equivalente) para generar cobertura de código en los proyectos de test.
- [ ] Definir un **umbral mínimo** de cobertura en CI (ej. 60–70% en módulos críticos) y hacer que el pipeline falle si no se cumple.
- [ ] Integrar la ejecución de tests y la generación de cobertura en el pipeline de CI (Azure DevOps, GitHub Actions, etc.).

---

## Priorización sugerida (módulos críticos)

1. CreateContentCommandHandler, SchemaValidationService.
2. Publish/Unpublish, ApprovePublicationCommandHandler.
3. Login/Register, GetSetupStatus (cuando se mueva a Application).
4. CheckAccessQuery (Authorization).
5. Validadores de comandos y repositorios principales.

---

## Criterios de aceptación

- Al menos Core e Identity tienen proyectos de test (unit + integration) incluidos en la solución.
- Handlers y validadores críticos listados tienen tests automatizados.
- Cobertura generada en CI con umbral mínimo configurado.
- Integration tests con Testcontainers (o alternativa documentada) para BD (y opcionalmente cola/búsqueda).
