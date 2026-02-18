# Plan de trabajo fullstack — 006 Scheme y SiteConfig

**Referencias obligatorias:**  
- `ai/agents/architect.agent.md` (rol y reglas de actuación)  
- `ai/memory/project.context.md` (identidad del sistema y reglas no negociables)  
- `ai/memory/decisions.log.md` (ADRs; no contradecir decisiones aceptadas)  
- `docs/006-SCHEME-N-SITECONFIG/REQUERIMIENTOS.md` (requisitos funcionales)

**Alcance del documento:** Decisiones arquitectónicas, impacto por capa, contratos y flujos. Orden de implementación y paquetes de trabajo Backend / Frontend. No incluye implementación detallada (código).

---

## 1. Validación frente a contexto y ADRs

### 1.1 Coherencia con project.context.md

| Principio / regla | Cómo se respeta en este plan |
|-------------------|------------------------------|
| Schema-driven, tipos en runtime | Req 1 y 2 extienden el modelo de schema y contenido sin fijar tipos concretos en Core. |
| Headless, API-First | Contratos (Core API, Content Delivery API) se definen antes; Themes consumen API. |
| Microservicios, BD por servicio | Cambios de modelo y eventos solo en Core (y en Indexing/Publishing vía eventos/HTTP). No se comparten tablas. |
| DDD, Clean Architecture | Nuevas entidades/value objects en Domain; aplicación en Application; persistencia en Infrastructure. Controllers solo orquestan. |
| No lógica de dominio en Controllers | Validación de slug, resolución de URLs, auditoría, etc. en Domain/Application. |
| Autorización en todo endpoint | Nuevos o modificados endpoints siguen validando ProjectId y permisos (ADR-007, ADR-019). |
| Eventos versionados | Cualquier nuevo evento (ej. cambios que afecten Indexing/Publishing) tendrá versión e inmutabilidad (ADR-005). |
| Multi-tenant por proyecto | Relaciones contenido–sitio, jerarquías y URLs siguen acotadas por ProjectId/EnvironmentId/SiteId (ADR-019). |

### 1.2 Coherencia con decisions.log.md

| ADR | Implicación en 006 |
|-----|--------------------|
| ADR-011 (actor desde JWT) | Req 2 (createdBy, updatedBy): origen de verdad es el JWT en la API, no el body. Contratos no exponen CreatedBy/UpdatedBy editables por el cliente. |
| ADR-014 (API-First, DTOs) | Nuevos campos y recursos se exponen vía DTOs; no se exponen entidades. ProblemDetails para errores. |
| ADR-015 (consistencia eventual) | Contenido compartido entre sitios, publicación por sitio e indexación se consideran eventualmente consistentes; no garantizar lectura inmediata entre servicios. |
| ADR-018 (schema-driven, label/slug) | Req 1 refina FieldDefinition (label + slug, unicidad de slug por schema, default fields sugeridos). No contradice; concreta el modelo. |
| ADR-019 (multi-tenant por proyecto) | Req 4 (contenido–sitio) y Req 5 (URLs por sitio) refuerzan el ámbito proyecto/entorno/sitio. |

No se introducen cambios de lenguaje, frameworks alternativos ni ruptura de contratos sin estrategia de versionado (project.context: no breaking changes sin versionado).

---

## 2. Impacto en el dominio (Core)

### 2.1 Bounded context y agregados afectados

- **Content Schema (agregado):** FieldDefinition pasa a tener **Label** (visible) y **Slug** (técnico, único en el schema, kebab-case). Lista de default fields (title, teaser, image, content) como sugerencia al crear schema; no son obligatorios. El agregado garantiza unicidad de slug entre campos del mismo schema y reglas de formato.
- **Content (agregado):** Metadata de auditoría (createdAt, updatedAt, createdBy, updatedBy) y relación con versionado existente. Opción: valor object **ContentAudit** o campos en la entidad según si se versionan con el snapshot o no (ver sección 3). Relaciones opcionales: **ParentContentId** (jerarquía), **Tags** (etiquetas), **CategoryIds** (categorías). Asociación **Content–Site**: N:M (un contenido puede estar en varios sitios del mismo entorno/proyecto).
- **Site (agregado existente):** Configuración de **URL pattern** por sitio (template con placeholders: slug, createdAt, section, custom fields). Valor object o entidad **SiteUrlConfig** según preferencia de modelado.
- **Conceptos nuevos en dominio:** SlugUniquenessPerSchema (invariante), ContentHierarchy (regla anti-ciclos), ContentSiteAssignment (reglas de consistencia por entorno).

### 2.2 Reglas de dominio a respetar

- Unicidad de slug entre FieldDefinitions del mismo ContentSchema (validación en agregado o en aplicación antes de persistir).
- Jerarquía de contenido: no ciclos (ParentContentId no puede llevar a un descendiente); validación en comando o servicio de dominio.
- Contenido–sitio: ámbito por proyecto/entorno; un contenido no puede asignarse a un sitio de otro proyecto/entorno.
- URLs: resolución en publicación o en lectura; slug/URL único por sitio (o por sitio+locale si se añade después) para evitar colisiones.

---

## 3. Cambios en modelos (resumen conceptual)

### 3.1 FieldDefinition (Req 1)

- **Label:** string, visible en UI.
- **Slug:** string, técnico, kebab-case, único dentro del schema. Autogenerado desde label pero editable antes de guardar.
- Validación de slug: regex/reglas en dominio (kebab-case, sin espacios, sin caracteres especiales). Unicidad validada en backend al crear/actualizar schema (evitar colisiones en updates: comparar con slugs existentes en la versión del schema que se está editando).

### 3.2 Contenido – Auditoría (Req 2)

- **createdAt**, **updatedAt**: timestamps (UTC). Decisión: **metadata técnica** no versionada en el snapshot de contenido; se actualizan en la entidad raíz y en cada versión se puede guardar “version number” y “at”.
- **createdBy**, **updatedBy**: GUID de usuario. Origen: **siempre desde JWT** (ADR-011); no aceptar en body. Forman parte del snapshot versionado si se desea auditoría por versión; si no, solo en la entidad actual.
- **version**: ya existe; integrar con createdAt/updatedAt/createdBy/updatedBy para que cada versión lleve su propio “quién y cuándo” si se requiere trazabilidad por versión.

### 3.3 Contenido – Jerarquías y etiquetas (Req 3)

- **ParentContentId** (nullable): FK a Content del mismo proyecto/entorno/schema o sin restricción de schema según diseño. Regla anti-ciclos en aplicación o dominio.
- **Tags:** colección de strings o entidad Tag (nombre normalizado). N:M Content–Tag si Tag es entidad; si es valor, array en contenido.
- **Categorías:** N:M Content–Category; Category como entidad por proyecto (opcional en fase inicial).

### 3.4 Contenido – Sitios (Req 4)

- **ContentSite** (asociación N:M): ContentId, SiteId, opcionalmente IsPrimary o orden. Un contenido puede estar en uno o más sitios; publicación por sitio puede depender de esta tabla (publicado “en” cada sitio por separado o una sola publicación con lista de sitios).

### 3.5 Site – Configuración de URL (Req 5)

- **UrlTemplate** (o equivalente): string con placeholders, ej. `/{slug}`, `/{section}/{createdAt:yyyy/MM}/{slug}`. Placeholders definidos: slug, createdAt (con formato), section, y campos custom por nombre. Configurable por sitio; evaluado en publicación o en resolución de ruta (Content Delivery API).

---

## 4. Cambios en servicios (Backend – Core)

### 4.1 Capa Application (CQRS, MediatR, FluentValidation)

- **Commands/Queries nuevos o modificados:**  
  - Create/Update ContentSchema: aceptar campos con Label + Slug; validar unicidad de slug en el schema (y en la versión en edición).  
  - Create/Update Content: no recibir createdBy/updatedBy en body; inyectar desde JWT en controller.  
  - Opcionales: SetContentParent, AssignContentToSites, SetContentTags, PublishContentToSites (si publicación pasa a ser por sitio).  
  - GetContentByUrl(siteId, path): query para Content Delivery API que resuelve URL según template del sitio.

- **Validadores:**  
  - Slug format (kebab-case, etc.).  
  - Unicidad de slug entre campos del mismo schema (consulta a repositorio o dominio).  
  - No ciclo en ParentContentId (recorrido de ancestros o constraint en BD).

### 4.2 Capa Domain

- Value objects o invariantes: **Slug** (formato + posible unicidad en contexto), **ContentAudit** (timestamps + userIds).  
- Reglas: anti-ciclo en jerarquía; asignación contenido–sitio dentro del mismo proyecto/entorno.

### 4.3 Capa Infrastructure

- Persistencia: tablas o columnas nuevas según modelos anteriores (ContentSite, Tag, Category, campos en ContentSchema/Content, Site.UrlTemplate, etc.).  
- Migraciones EF sin breaking changes para datos existentes: columnas nullable o con valores por defecto donde corresponda.

### 4.4 Eventos (ADR-005)

- Si se añaden nuevos eventos (ej. ContentParentChanged, ContentSitesAssigned, ContentTagged), definirlos en **IODA.Shared.Contracts.Events** con versión (V1) e inmutabilidad.  
- Actualizar consumidores (Indexing, Publishing) si los eventos afectan sus modelos; mantener compatibilidad hacia atrás (nuevas propiedades opcionales o nuevos eventos V2).

---

## 5. Cambios en base de datos (Core)

- **ContentSchema / FieldDefinition:** columnas Label y Slug (o refactor de nombre existente a Slug); índice único (SchemaId, Slug) para FieldDefinition (por versión de schema si se versionan campos).  
- **Content:** columnas CreatedAt, UpdatedAt, CreatedBy, UpdatedBy (si no existen); ParentContentId (nullable FK); tabla ContentTag (ContentId, TagId o TagValue según modelo); tabla ContentSite (ContentId, SiteId); tabla Category y ContentCategory si se implementan categorías.  
- **Site:** columna o tabla de configuración UrlTemplate (string o JSON).  
- Migraciones: compatibilidad hacia atrás (defaults, nullables); no eliminar columnas sin deprecación y versión de API.

---

## 6. Impacto en publicación (Publishing service)

- **Flujo actual:** Publicación de contenido (estado, versión) y posible notificación a Indexing.  
- **Cambios posibles:**  
  - Si la publicación pasa a ser “por sitio”: evento o estado que indique “publicado en sitios [ids]” o publicación única con lista de sitios; Publishing y Core deben estar alineados por contrato.  
  - Resolución de URL: puede vivir en Core (GetContentByUrl) y Publishing solo cambia estado; o Publishing calcula/guarda la URL final al publicar (según dónde se decida evaluar el template).  
- **Eventos:** ContentPublishedEvent actual puede extenderse con opcionales (SiteIds, ResolvedUrl) en nueva versión si hace falta; no romper V1.

---

## 7. Impacto en Content Delivery API

- **Contratos:**  
  - Listado/obtención de contenido: incluir campos de auditoría (createdAt, updatedAt, createdBy, updatedBy) según política de exposición (solo timestamps, o también IDs de usuario para resolución en theme). Decisión: mínimo createdAt/updatedAt; createdBy/updatedBy opcionales o con nivel de detalle configurable (ADR-014, DTOs).  
  - Soporte de jerarquía: endpoint o campo `parentId` / `children` en DTOs para estructuras anidadas.  
  - Etiquetas y categorías: listas en el DTO de contenido.  
  - Sitios: si un contenido está en varios sitios, la Delivery API puede filtrar por siteId y devolver solo contenido asignado a ese sitio; o exponer lista de siteIds en el contenido.  
  - **Resolución de URL:** endpoint tipo `GET /sites/{siteId}/content/by-path?path=...` que use el motor de URLs del sitio y devuelva el contenido correspondiente. Cache por (siteId, path) según estrategia definida.

- **Themes (Handlebars/HTML/CSS/JS):** consumen la Delivery API; no cambian stack. Solo se documentan nuevos campos y endpoints.

---

## 8. Riesgos técnicos

| Riesgo | Mitigación |
|--------|------------|
| Colisiones de slug entre sitios (Req 4) | Definir ámbito de unicidad: slug único por (SiteId, ContentType) o global por sitio. Validación en comando y en resolución de URL. |
| Cambios retroactivos de URL (Req 5) | Versionar configuración de URL por sitio; al cambiar template, definir política: redirecciones 301, o “solo para nuevo contenido”. Documentar en contrato. |
| Consultas de árbol profundo (Req 3) | Límite de profundidad o materialización de path; índices por ParentContentId; evitar N+1 en Delivery API (DTO con árbol acotado o paginación). |
| Compatibilidad hacia atrás | Campos nuevos nullable o con default; eventos con propiedades opcionales; API versionada si hay breaking changes (v2 de rutas). |

---

## 9. Recomendación de orden de implementación

Orden sugerido para no romper compatibilidad y repartir trabajo Backend/Frontend.

### Fase 1 — Schema: default fields + label/slug (Req 1)

- **Backend (Core):**  
  - Modelo FieldDefinition: Label, Slug; validación de formato y unicidad de slug en schema.  
  - Default fields sugeridos al crear schema (constante o configuración en aplicación).  
  - Migraciones; endpoints Create/Update schema que acepten label/slug.  
- **Frontend (Admin):**  
  - UI de creación/edición de schema: sugerir default fields (title, teaser, image, content); cada campo con label y slug editables; autogenerar slug desde label; validación en cliente y mensajes de error desde backend (400 con detalle).  
- **Contratos:** DTOs de schema con label y slug; documentar en OpenAPI.  
- **Dependencias:** ninguna crítica. Permite preparar Req 2 y 3 en el mismo modelo de contenido.

### Fase 2 — Metadata y auditoría (Req 2)

- **Backend (Core):**  
  - Campos CreatedAt, UpdatedAt, CreatedBy, UpdatedBy en Content (y en snapshot de versión si se decide así). Origen de CreatedBy/UpdatedBy desde JWT en controller (ADR-011).  
  - Ajustar Create/Update Content para no aceptar auditoría en body; rellenar desde ClaimsPrincipal.  
- **Frontend (Admin):**  
  - Mostrar createdAt/updatedAt (y opcionalmente createdBy/updatedBy si se expone) en detalle de contenido; no enviar estos campos en create/update.  
- **Content Delivery API:**  
  - Incluir en DTOs los campos de auditoría acordados (mínimo createdAt, updatedAt); opcional createdBy/updatedBy según política.  
- **Themes:** pueden usar fechas para “publicado el”, “actualizado el”; sin cambio de stack.

### Fase 3 — Jerarquías y etiquetas (Req 3)

- **Backend (Core):**  
  - ParentContentId en Content; regla anti-ciclos en comando.  
  - Modelo de Tags (tabla Tag + ContentTag o array en contenido).  
  - Opcional: Category + ContentCategory.  
  - Queries: obtener árbol de contenido (con límite de profundidad) o lista plana con parentId.  
- **Frontend (Admin):**  
  - Selector de padre (árbol o combo); selector de etiquetas (autocompletado o lista); opcional categorías.  
- **Content Delivery API:**  
  - DTOs con parentId, children (o ids de hijos), tags, categoryIds. Endpoint de árbol por proyecto/entorno/schema si se necesita.  
- **Eventos:** si se notifica a Indexing/Publishing por cambios de jerarquía o tags, definir eventos V1 con campos opcionales o nuevos eventos.

### Fase 4 — Multi-sitio y compartición (Req 4)

- **Backend (Core):**  
  - Tabla ContentSite; comandos para asignar/desasignar contenido a sitios; validación de sitio en mismo proyecto/entorno.  
  - Publicación: decidir si “publicar” es global o por sitio; si es por sitio, estado o tabla Publication por (ContentId, SiteId).  
- **Frontend (Admin):**  
  - UI para asignar contenido a uno o más sitios (checkboxes o multi-select por sitio del proyecto/entorno).  
- **Content Delivery API:**  
  - Filtrar contenido por siteId; listar sitios en los que está un contenido si aplica.  
- **Riesgo:** conflictos de slug entre sitios; definir unicidad (por sitio, por sitio+contentType) e implementar validación.

### Fase 5 — Sistema flexible de URLs (Req 5)

- **Backend (Core):**  
  - Modelo SiteUrlConfig / UrlTemplate por sitio; placeholders definidos (slug, createdAt, section, custom fields).  
  - Motor de resolución: dado siteId y path, resolver a ContentId (y versión) según template y datos del contenido.  
  - Cache de resolución (por siteId + path) con invalidación al publicar/despublicar o al cambiar template.  
  - Estrategia para cambios retroactivos: documentar (solo nuevos vs. recálculo masivo) y versionar configuración si hace falta.  
- **Frontend (Admin):**  
  - Configuración de plantilla de URL por sitio (ej. selector de patrón o editor de template con placeholders).  
- **Content Delivery API:**  
  - Endpoint `GET /sites/{siteId}/content/by-path?path=...` que use el motor de resolución y devuelva el contenido (y metadatos de auditoría según Fase 2).  
- **Themes:** consumen by-path para generar rutas; sin cambio de stack.

---

## 10. Paquetes de trabajo por rol (resumen)

| Fase | Backend (Core) | Frontend (Admin) | Contratos / Delivery API |
|------|----------------|------------------|---------------------------|
| 1 | FieldDefinition label/slug; default fields; validación unicidad | UI schema: label, slug, default fields | DTOs schema; OpenAPI |
| 2 | Auditoría en Content; actor desde JWT | No enviar auditoría; mostrar en UI | DTOs contenido con auditoría |
| 3 | ParentContentId; Tags (y opc. Category); anti-ciclos | Selector padre; selector tags | DTOs con parentId, tags, tree |
| 4 | ContentSite; publicación por sitio (si aplica) | Asignar contenido a sitios | Filtro por siteId; lista sitios |
| 5 | UrlTemplate; motor resolución; cache | Config URL por sitio | GET by-path; documentar placeholders |

---

## 11. Recomendación final

- Implementar en el orden **1 → 2 → 3 → 4 → 5** para apoyar cada requisito en el anterior (schemas con slug, luego auditoría, luego jerarquía/tags, luego multi-sitio, luego URLs).  
- Respetar en todo momento: **Clean Architecture** (lógica en Domain/Application), **ADR-011** (actor desde JWT), **ADR-014** (DTOs, ProblemDetails), **ADR-018/019** (schema-driven, multi-tenant por proyecto).  
- Cualquier cambio que rompa contratos existentes debe ir con **versionado de API** (p. ej. v2 de rutas o query param `api-version`) y documentación de compatibilidad hacia atrás.  
- Eventos nuevos o modificados: **versionados e inmutables** (ADR-005); publicar en IODA.Shared.Contracts.Events.

Este plan queda como referencia para el equipo fullstack; la implementación concreta (clases, métodos, pruebas) se detalla en tareas por fase en Backend y Frontend sin contradecir este documento ni los ADRs.
