# Principios del CMS (estado actualizado)

## 1) Principios rectores vigentes

- Schema-driven (tipos y campos configurables en runtime).
- Headless (API-first como contrato principal).
- Arquitectura por servicios (Core, Identity, Authorization, Publishing, Indexing).
- DDD + Clean Architecture por servicio.
- Event-driven donde aplica (publicacion/indexacion y eventos de dominio/integracion).
- Security by design (JWT, permisos/policies y validacion por servicio).
- Observabilidad y operacion reproducible en contenedores.
- Evolucion incremental por fases con trazabilidad documental.

## 2) Estado arquitectonico actual del proyecto

### Backend

- Servicios principales activos:
  - `Core`: proyectos, entornos, sitios, schemas, contenido, media, jerarquias, etiquetas.
  - `Identity`: autenticacion/sesion y bootstrap de usuario inicial.
  - `Authorization`: roles, permisos y reglas de acceso.
  - `Publishing`: flujo de solicitudes y aprobacion/publicacion.
  - `Indexing`: indexacion para busqueda.
- Patrón de implementacion: API -> Application (MediatR) -> Domain -> Infrastructure.
- Validacion: FluentValidation en comandos y validaciones de dominio.
- Persistencia: EF Core + migraciones por servicio.

### Frontend

- Routing funcional separado por contexto:
  - `/#/content`: listado.
  - `/#/content/create`: creacion.
  - `/#/content/editor`: edicion.
  - `/#/admin/schemas`: listado de schemas.
  - `/#/admin/schemas/design`: disenador de schema.
- UX de contenido estandarizada:
  - Campos del schema a la izquierda.
  - Propiedades a la derecha.
- Orden de campos dinamicos respeta `displayOrder` definido en disenador.

## 3) Decisiones de arquitectura consolidadas (fases 009 y 010)

- `schemaType` es inmutable despues de crear el schema.
- Slugs:
  - Front hace prenormalizacion.
  - Backend valida/normaliza con whitelist (`a-z`, `0-9`, `-`, `_`).
- Campos nativos de contenido:
  - `title` y `slug` son nativos (no se persisten como custom fields).
  - En disenador se modelan como virtuales para ordenar posicion.
- Herencia de schema:
  - Removida del alcance funcional actual (frontend y backend operativo).
- Contenido padre:
  - Removido del frontend y de contratos API de aplicacion.
  - El contenido se opera actualmente como nivel raiz.
- URLs por sitio:
  - Gestion por sitio owner + sitios compartidos.
  - Unicidad por sitio/path.
- Seguridad:
  - Publicacion y operaciones sensibles dependen de JWT + policy/claims.

## 4) Modelo funcional minimo del contenido

### Campos de contenido

- `title` (obligatorio).
- `slug` (obligatorio, editable por usuario, validado por backend).
- `fields` dinamicos del schema (ordenados por `displayOrder`).
- `section` principal opcional via jerarquias.
- `tags` opcionales (multiple).

### Propiedades operativas

- `site_owner` (sitio principal).
- `sites_shared` (sitios adicionales).
- `site_urls` (path por sitio).
- `status` (`Draft`, `Published`, etc).
- `created_at`, `updated_at`, `published_at`.
- trazabilidad por actor: `created_by`, `updated_by`, `published_by`.
- versionado de contenido.

## 5) Restricciones y trade-offs actuales

- No se implementa jerarquia de contenido padre/hijo (decisión de simplificacion funcional).
- Algunos contratos historicos siguen en transicion y pueden coexistir temporalmente durante despliegues.
- El flujo de publicacion depende de consistencia entre claims/permisos y policies por servicio.

## 6) Lineamientos para cambios futuros

- Cualquier cambio estructural debe registrarse con ADR y reflejar:
  - impacto en API contracts,
  - impacto en migraciones,
  - impacto en frontend routing/estado,
  - estrategia de rollout y rollback.
- No introducir nuevos acoplamientos cross-service sin contrato explicito.
- Mantener compatibilidad hacia atras cuando el cambio afecte payloads ya usados por frontend.

## 7) Referencias de avance

- `docs/009-SCHEMAS_CORRECTIONS/`
- `docs/010-FRONTEND-IMPROVEMENTS/`
- `ai/agents/orchestrator.agent.md` (perfil consolidado de gobernanza tecnica)
