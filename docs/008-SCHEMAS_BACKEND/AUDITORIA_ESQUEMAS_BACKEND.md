# 008 – Auditoría: Esquemas y Backend CMS

**Referencia:** `ai/agents/auditor.agent.md`  
**Objetivo:** Auditar la arquitectura actual del CMS respecto a diseño schema-driven y basado en composición.  
**Fecha:** 2026-02-23

---

## 1. Resumen ejecutivo

El CMS actual tiene **un único modelo raíz (Content)**, **separación clara entre jerarquía (parentId) y taxonomía (Hierarchy/Tag)** y **capa de esquema dinámico (ContentSchema + FieldDefinition + validación)** bien implementada. Las principales carencias son la **ausencia total de la capa de composición por bloques** y la **falta del campo `order` en la identidad estructural** para ordenar hermanos. La redundancia ContentType/SchemaType y la duplicación FieldName/Slug son mejoras de consistencia recomendadas.

---

## 2. Análisis según reglas arquitectónicas

### 2.1 Single Root Model (Modelo raíz único)

**Regla:** Debe existir una única entidad raíz (Node o equivalente). Todos los tipos de contenido (homepage, landing, section, detail, gallery, etc.) deben derivar del mismo modelo. No debe haber múltiples entidades raíz por tipo de página.

**Estado actual:** **CUMPLE**

- **Content** (`IODA.Core.Domain/Entities/Content.cs`) es la única entidad de contenido. No existen entidades separadas por tipo (Homepage, Landing, Section, etc.).
- El tipo se determina por **SchemaId** y se denormaliza en **ContentType** (string) para consultas.
- Todos los contenidos comparten: PublicId, ProjectId, EnvironmentId, SiteId, ParentContentId, SchemaId, Title, Slug, Status, ContentType, Fields (JSONB), timestamps, versionado.

**Violaciones:** Ninguna.

---

### 2.2 Strict Layer Separation (Separación estricta de capas)

**Regla:** El sistema debe separar claramente:
- **Capa de identidad estructural:** id, slug, parentId (opcional), type, status, timestamps, **order**
- **Capa de esquema dinámico:** definiciones de campos declarativas, validaciones, **configuración de bloques permitidos**
- **Capa de composición por bloques:** bloques reutilizables, relación Node → Blocks composicional, bloques no acoplados a tipos de página

#### 2.2.1 Structural Identity Layer

**Estado actual:** **CUMPLE PARCIALMENTE**

| Atributo   | En Content | Notas |
|-----------|------------|--------|
| id        | ✅ Id (Guid) | |
| slug      | ✅ Slug (Value Object) | |
| parentId  | ✅ ParentContentId (opcional) | |
| type      | ✅ ContentType (string) | Denormalizado desde Schema.SchemaType |
| status    | ✅ Status (ContentStatus) | |
| timestamps| ✅ CreatedAt, UpdatedAt, PublishedAt | |
| **order** | ❌ **Ausente** | No hay orden explícito de hermanos (hijos del mismo padre). |

**Violación (severidad Alta):** Falta un campo **Order** (o DisplayOrder/SortOrder) en **Content** para ordenar nodos hermanos dentro del árbol. Hoy el orden solo existe en **FieldDefinition** (DisplayOrder), no en el contenido.

#### 2.2.2 Dynamic Schema Layer

**Estado actual:** **CUMPLE PARCIALMENTE**

- **ContentSchema:** define SchemaName, SchemaType, herencia (ParentSchemaId), versionado, activo/inactivo.
- **FieldDefinition:** FieldName, Label, Slug, FieldType, IsRequired, DefaultValue, HelpText, ValidationRules (JSONB), DisplayOrder.
- **ISchemaValidationService:** valida `Fields` del contenido contra el esquema; hay validadores por tipo (IFieldValidator: string, number, boolean, date, enum, etc.).

**Violación (severidad Crítica):** No existe **configuración de bloques permitidos** en el esquema. La auditoría exige “allowed block configuration” en la capa de esquema; actualmente no hay concepto de “bloques” en el dominio ni en el esquema.

#### 2.2.3 Block Composition Layer

**Estado actual:** **NO IMPLEMENTADA**

- No existe entidad **Block** (o equivalente) en el dominio.
- No hay relación composicional **Content → Blocks** (por ejemplo, una colección de bloques ordenados por posición).
- No hay bloques reutilizables ni desacoplados de tipos de página.

**Violación (severidad Crítica):** Toda la capa de composición por bloques está ausente. Es un requisito explícito del diseño esperado.

---

### 2.3 Hierarchy Rules (Reglas de jerarquía)

**Regla:** La jerarquía debe ser explícita y opcional. Jerarquía (parentId / árbol) no debe mezclarse con clasificación taxonómica. Si existen ambas, deben ser conceptos claramente separados.

**Estado actual:** **CUMPLE**

- **Árbol de contenido:** **Content.ParentContentId** (opcional). Un contenido puede tener un solo padre; null = raíz. Validación anti-ciclos en aplicación (GetAncestorIdsAsync, GetParentIdAsync).
- **Taxonomía / categorías:** **Hierarchy** (árbol propio con ParentHierarchyId) + **ContentHierarchy** (M:N) para clasificar contenido en categorías.
- **Etiquetas:** **Tag** + **ContentTag** (M:N).

Jerarquía de contenido (árbol de páginas) y taxonomía (Hierarchy/Tag) están separados. No hay mezcla de conceptos.

**Violaciones:** Ninguna.

---

### 2.4 Architectural Constraints (Restricciones arquitectónicas)

**Regla:** Sin duplicación de modelos por tipo de página; sin lógica condicional grande por "type"; sin lógica de presentación en modelos de dominio; SOLID; escalable y extensible.

**Estado actual:** **CUMPLE EN SU MAYORÍA**

- **Sin duplicación por tipo:** Un solo modelo Content. ✅
- **Sin lógica condicional masiva por type:** No hay `switch (ContentType)` ni cadenas largas de `if (contentType == ...)` en dominio o aplicación; solo filtros en consultas (p. ej. ListContentByProjectQuery por ContentType). ✅
- **Sin lógica de presentación en dominio:** Content y ContentSchema no conocen UI ni renderizado. ✅
- **SOLID:** Validación por estrategias (IFieldValidator), servicios de validación inyectables, repositorios por agregado. ✅

**Observaciones (severidad Media/Baja):**

- **Redundancia ContentType / SchemaType:** Content almacena `ContentType` (string) que se copia del `Schema.SchemaType` al crear. Si en el futuro se permitiera cambiar el tipo de un schema, habría que mantener consistencia (p. ej. no permitir cambio de tipo o actualizar contenidos). Recomendable documentar que ContentType es una copia inmutable en vida del contenido o derivarla siempre del Schema en lecturas.
- **FieldDefinition: FieldName y Slug:** El código indica que FieldName se mantiene por compatibilidad y coincide con Slug en campos nuevos. Redundancia; a medio plazo unificar en un solo concepto (p. ej. Slug como clave técnica) reduce confusión.

---

## 3. Clasificación de hallazgos por severidad

### Crítico

| ID  | Hallazgo | Ubicación |
|-----|----------|------------|
| C.1 | **Capa de composición por bloques ausente:** No existe entidad Block, ni relación Content → Blocks, ni bloques reutilizables. | Dominio Core |
| C.2 | **Sin “allowed block configuration” en esquema:** ContentSchema no define qué bloques puede contener un tipo de contenido. | ContentSchema, FieldDefinition |

### Alto

| ID  | Hallazgo | Ubicación |
|-----|----------|------------|
| H.1 | **Falta campo Order en la identidad estructural:** No hay forma de ordenar hermanos (hijos del mismo ParentContentId). | Content |

### Medio

| ID  | Hallazgo | Ubicación |
|-----|----------|------------|
| M.1 | **Redundancia ContentType / Schema.SchemaType:** ContentType denormalizado; riesgo de desincronización si las reglas de negocio cambian. | Content, comandos/handlers |
| M.2 | **FieldName y Slug duplicados en FieldDefinition:** Mantener un solo identificador técnico (Slug) simplifica evolución. | FieldDefinition |

### Bajo

| ID  | Hallazgo | Ubicación |
|-----|----------|------------|
| L.1 | **Consistencia de agregados:** Hierarchy/Tag no son AggregateRoot; Content y ContentSchema sí. Aceptable si Hierarchy/Tag son entidades de catálogo; documentar criterio. | Hierarchy, Tag |

---

## 4. Tareas de mejora (backlog)

### 4.1 Crítico – Capa de bloques y esquema

**Tarea C.1 – Diseñar e implementar Block Composition Layer**

1. **Modelo de dominio**
   - Introducir entidad **Block** (o **ContentBlock**): identificador, tipo de bloque, orden/posición, payload (JSONB o entidad tipada por tipo de bloque), ContentId, opcionalmente SchemaId si se quiere restringir por esquema.
   - Relación **Content → Blocks**: 1:N composicional (los bloques no tienen sentido sin el contenido). Eliminación en cascada al borrar contenido.
   - Definir **tipos de bloque** (p. ej. hero, text, image, cta, list) como catálogo o enum extensible por proyecto.

2. **Persistencia**
   - Tabla `content_blocks` (o equivalente) con FK a `contents`, índices por ContentId y orden.
   - Migraciones EF Core.

3. **API y aplicación**
   - Comandos/consultas para añadir, reordenar, actualizar y eliminar bloques de un contenido.
   - DTOs que incluyan la lista de bloques en Content (get by id, list si aplica).

**Criterio de aceptación:** Un contenido puede tener N bloques ordenados; los bloques son reutilizables por tipo y no están acoplados a un tipo de página concreto (homepage, landing, etc.).

---

**Tarea C.2 – Allowed block configuration en ContentSchema**

1. **Esquema**
   - En **ContentSchema** (o en una entidad asociada al schema), añadir **configuración de bloques permitidos**: por ejemplo lista de tipos de bloque permitidos y, opcionalmente, cardinalidad (min/max por tipo).
   - Persistir en tabla de schema o en JSONB en `content_schemas`.

2. **Validación**
   - Al añadir o actualizar bloques de un contenido, validar que el tipo de bloque esté permitido por el ContentSchema del contenido y que se respeten límites (si se definen).

**Criterio de aceptación:** Cada ContentSchema define qué tipos de bloque puede tener un contenido de ese tipo; la aplicación rechaza bloques no permitidos.

---

### 4.2 Alto – Orden de hermanos

**Tarea H.1 – Campo Order en Content (identidad estructural)**

1. **Dominio**
   - Añadir propiedad **Order** (o **SortOrder** / **DisplayOrder**) de tipo `int` en **Content**. Significado: orden entre hermanos (mismo ParentContentId). Mismo padre ⇒ mismo nivel; menor Order ⇒ antes.

2. **Persistencia**
   - Columna en `contents`, índice compuesto (ParentContentId, Order) para consultas ordenadas de hijos.

3. **Lógica de negocio**
   - Al crear contenido: asignar Order = siguiente disponible para ese padre (p. ej. max(Order)+1 o similar).
   - Permitir reordenar (comando “Move content” o “SetOrder”) actualizando Order de los afectados sin romper unicidad.

4. **API**
   - Listado de hijos de un contenido ordenado por Order; DTOs que expongan Order.

**Criterio de aceptación:** Los hijos de un mismo contenido padre se ordenan de forma determinista por Order; la API permite leer y actualizar ese orden.

---

### 4.3 Medio – Consistencia y redundancia

**Tarea M.1 – Política explícita ContentType vs SchemaType**

1. Documentar en contrato de dominio y en esta carpeta (008):
   - Que **ContentType** en Content es una **copia inmutable** del Schema.SchemaType en el momento de creación del contenido.
   - Que no se permite cambiar SchemaType de un schema que ya tiene contenidos, o que si se permite, existe un proceso (comando/script) que actualiza Content.ContentType en todos los contenidos afectados.
2. Opcional: en lecturas, exponer “effective content type” como Schema.SchemaType para evitar desvíos si en el futuro se añaden migraciones de tipo.

**Criterio de aceptación:** Documentación clara y, si aplica, regla de negocio o migración que mantenga ContentType alineado con Schema.

---

**Tarea M.2 – Unificar FieldName y Slug en FieldDefinition**

1. Decidir un único identificador técnico: **Slug** (recomendado).
2. En dominio y persistencia:
   - Usar **Slug** como clave en contenido (Fields[slug]) y en APIs.
   - Marcar **FieldName** como obsoleto o eliminarlo en una versión futura; migrar datos si FieldName y Slug difieren en datos existentes.
3. Actualizar **SchemaValidationService** y cualquier uso de FieldName para que usen Slug.

**Criterio de aceptación:** Un solo campo “clave técnica” por campo del esquema (Slug); sin duplicación semántica con FieldName.

---

### 4.4 Bajo – Documentación de agregados

**Tarea L.1 – Criterio Hierarchy/Tag como no-AggregateRoot**

1. Documentar en 008 o en guía de dominio por qué **Hierarchy** y **Tag** no son AggregateRoot (vida útil manejada por proyecto; no tienen ciclo de vida tan rico como Content).
2. Si en el futuro se exigen reglas de consistencia más fuertes (ej. borrar Hierarchy y desasignar ContentHierarchy en la misma transacción), valorar si convertirlos en agregados o si se mantienen como entidades de catálogo con servicios de aplicación que orquesten cambios.

**Criterio de aceptación:** Criterio documentado y coherente con el resto del bounded context.

---

## 5. Propuesta de diseño de la capa de bloques (resumen)

Para cumplir con la regla de **Block Composition Layer** sin acoplar bloques a tipos de página:

- **Content** sigue siendo el único agregado de contenido; los bloques son **entidades hijas** del agregado Content (o un agregado hijo “ContentBlockList” según preferencia DDD).
- **Block** tiene: Id, ContentId, BlockType (string o enum), Order/Position, Payload (JSONB). No tiene SchemaId en el bloque si la restricción “permitido” se define solo en ContentSchema.
- **ContentSchema** incluye **AllowedBlockTypes** (lista de tipos permitidos y opcionalmente min/max por tipo).
- La **API de contenido** devuelve bloques ordenados por Order; los comandos de creación/actualización de contenido pueden recibir la lista de bloques y validarla contra el schema.

Con esto se cumple: bloques reutilizables, relación Content → Blocks composicional y bloques no atados a un tipo de página concreto (el acople es “qué bloques permite este schema”, no “este tipo de página tiene estos campos fijos”).

---

## 6. Beneficios de las mejoras a largo plazo

- **Orden (Order):** Permite menús, breadcrumbs y listados de hijos predecibles y editables por el usuario sin depender del orden de inserción.
- **Capa de bloques:** Permite páginas construidas por composición (landing, homepage, secciones) sin multiplicar modelos por tipo de página; evolución por nuevos tipos de bloque sin tocar el núcleo del contenido.
- **Allowed block configuration:** Mantiene la gobernanza del esquema (solo los bloques definidos por el proyecto) y evita contenido inválido.
- **Unificación FieldName/Slug y política ContentType/SchemaType:** Menos ambigüedad, menos bugs de evolución y documentación clara para futuros desarrolladores.

---

## 7. Referencias

- `ai/agents/auditor.agent.md` – Reglas de auditoría
- `src/Services/Core/IODA.Core.Domain/Entities/Content.cs`
- `src/Services/Core/IODA.Core.Domain/Entities/ContentSchema.cs`
- `src/Services/Core/IODA.Core.Application/Interfaces/ISchemaValidationService.cs`
- `src/Services/Core/IODA.Core.Application/Services/SchemaValidationService.cs`
- `docs/006-SCHEME-N-SITECONFIG/REQUERIMIENTOS.md` – Contexto de esquemas y sitios
