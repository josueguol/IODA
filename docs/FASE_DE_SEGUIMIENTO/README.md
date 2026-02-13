# Fase de Seguimiento — Tareas por Responsable

Este directorio contiene las tareas derivadas del **Diagnóstico Técnico del CMS** ([DIAGNOSTICO_TECNICO_CMS.md](../DIAGNOSTICO_TECNICO_CMS.md)), organizadas por **responsable** (Backend, Frontend, DevOps/Infraestructura, Testing/QA, Documentación).

## Resumen del diagnóstico

| Aspecto | Valoración |
|--------|------------|
| **Estado general** | Riesgoso |
| **Deuda técnica** | Alta |
| **Recomendación** | No desplegar en producción sin abordar seguridad y tests |

**Riesgos principales:** APIs sin autorización (Authorization, Publishing, Indexing), ausencia de tests, violaciones de Clean Architecture en Identity, CORS permisivo, secretos en `appsettings.json`, middleware de errores duplicado.

---

## Plan por fases

Las tareas están alineadas con las tres fases del plan de acción del diagnóstico:

- **Fase 1 (1–2 sprints):** Correcciones críticas — seguridad, autorización, CORS, Identity, secretos.
- **Fase 2 (2–3 sprints):** Mejoras estructurales — tests, middleware compartido, excepciones de dominio, validadores, DTOs, extensiones JWT/CORS.
- **Fase 3:** Optimización y refactorización — TreatWarningsAsErrors, refactor SchemaValidationService, health checks, signed URLs, documentación.

---

## Documentos por responsable

| Documento | Responsable | Descripción |
|-----------|-------------|-------------|
| [BACKEND.md](./BACKEND.md) | Backend / API | Autorización, Identity, Publishing, Core, middleware, validadores, excepciones, DTOs. |
| [FRONTEND.md](./FRONTEND.md) | Frontend | Configuración CORS/orígenes, env, consumo de APIs protegidas. |
| [DEVOPS_INFRAESTRUCTURA.md](./DEVOPS_INFRAESTRUCTURA.md) | DevOps / Infra | Docker, docker-compose, variables de entorno, health checks, secretos en despliegue. |
| [TESTING_QA.md](./TESTING_QA.md) | QA / Backend | Proyectos de test, unit/integration, cobertura, CI. |
| [DOCUMENTACION.md](./DOCUMENTACION.md) | Tech Lead / Arquitectura | ARCHITECTURE.md, CONVENTIONS.md, documentación de decisiones. |

---

## Orden sugerido de ejecución

1. **Fase 1:** Backend (autorización + Identity + validación startup + secretos) + DevOps (CORS y secretos en despliegue) + Frontend (orígenes y tokens).
2. **Fase 2:** Backend (middleware compartido, excepciones, validadores, DTOs, extensiones) + Testing (proyectos y casos críticos) + Documentación (convenciones).
3. **Fase 3:** Backend (refactors opcionales, MediaController) + DevOps (health checks, depends_on) + Documentación (arquitectura).

Actualizar este README y los documentos de tareas cuando se cierren ítems o cambien prioridades.
