# 008 – Tareas para agente de desarrollo Fullstack

Desglose técnico de las tareas derivadas de la auditoría (AUDITORIA_ESQUEMAS_BACKEND.md). Cada tarea indica **qué hacer**, **en qué archivos** y **contratos exactos** para que no quede duda.

**Orden recomendado:** H.1 → C.1 → C.2 → M.1 → M.2 → L.1 (H.1 no depende de bloques; C.2 depende de C.1).

---

## H.1 – Campo Order en Content (identidad estructural)

**Objetivo:** Permitir ordenar hermanos (hijos del mismo padre) de forma explícita y editable.

### Backend

1. **Dominio – Content**
   - **Archivo:** `src/Services/Core/IODA.Core.Domain/Entities/Content.cs`
   - Añadir propiedad: `public int Order { get; private set; }` (0-based o 1-based; definir convención y mantenerla).
   - En el constructor privado que recibe todos los parámetros: añadir parámetro `int order` y asignar `Order = order`.
   - En `Content.Create(...)`: añadir parámetro `int? order = null`. Si `order` es null, no asignar aquí (se asignará en el handler con lógica de “siguiente disponible”).
   - Añadir método: `public void SetOrder(int order)` que asigne `Order = order` y actualice `UpdatedAt`.
   - **Importante:** El agregado no debe calcular “siguiente Order”;
     el **handler** obtendrá `max(Order)+1` del repositorio cuando no se pase order.

2. **Repositorio – siguiente Order**
   - **Archivo:** `src/Services/Core/IODA.Core.Domain/Repositories/IContentRepository.cs`
   - Añadir: `Task<int> GetNextOrderForParentAsync(Guid? parentContentId, CancellationToken cancellationToken = default);`
   - **Archivo:** `src/Services/Core/IODA.Core.Infrastructure/Persistence/Repositories/ContentRepository.cs`
   - Implementar: si `parentContentId` es null, filtrar `ParentContentId == null`; si no, filtrar `ParentContentId == parentContentId`. Devolver `Max(Order) + 1` o 0 si no hay filas. Usar `_context.Contents.AsNoTracking()` y proyección a int para no cargar entidades.

3. **Persistencia**
   - **Archivo:** `src/Services/Core/IODA.Core.Infrastructure/Persistence/Configurations/ContentConfiguration.cs`
   - Añadir: `builder.Property(c => c.Order).HasColumnName("order").IsRequired();`
   - Crear migración EF Core: columna `order` (int, NOT NULL, default 0). Para datos existentes, en la migración asignar un valor (p. ej. por `CreatedAt` o Id) para que no queden duplicados; por ejemplo `UPDATE contents SET "order" = sub.rn FROM (SELECT id, ROW_NUMBER() OVER (PARTITION BY parent_content_id ORDER BY created_at, id) - 1 AS rn FROM contents) AS sub WHERE contents.id = sub.id;` (ajustar nombre de columna si usan snake_case).
   - Añadir índice compuesto: `builder.HasIndex(c => new { c.ParentContentId, c.Order }).HasDatabaseName("ix_contents_parent_order");` (útil para listar hijos ordenados).

4. **Comandos y handlers**
   - **CreateContentCommand** (`src/Services/Core/IODA.Core.Application/Commands/Content/CreateContentCommand.cs`): añadir parámetro opcional `int? Order = null`.
   - **CreateContentCommandHandler:** antes de `Content.Create(...)`, si `request.Order == null`, llamar a `_unitOfWork.Contents.GetNextOrderForParentAsync(request.ParentContentId, cancellationToken)` y usar ese valor; si no null, usar `request.Order`. Pasar ese `int` al constructor de Content (Content.Create debe aceptar order y pasarlo al constructor privado).
   - **UpdateContentCommand** y **UpdateContentRequest:** añadir `int? Order = null`. Si viene valor, en el handler llamar a `content.SetOrder(order.Value)` (y validar que no rompa unicidad si se desea; opcionalmente reordenar hermanos en la misma transacción).
   - **Comando nuevo (opcional pero recomendado):** `SetContentOrderCommand(ContentId, int Order)` y handler que llame a `content.SetOrder(Order)` y opcionalmente ajuste Order de otros hermanos para evitar huecos (o dejar huecos y solo garantizar orden relativo).

5. **DTOs y API**
   - **ContentDto** (`src/Services/Core/IODA.Core.Application/DTOs/ContentDto.cs`): añadir `int Order` al record.
   - **ContentListItemDto:** añadir `int Order`.
   - **ContentMappings** (`.../Mappings/ContentMappings.cs`): en `ToDto` y `ToListItemDto` mapear `content.Order`.
   - **CreateContentRequest** (ContentController): añadir `int? Order = null`.
   - **UpdateContentRequest:** añadir `int? Order = null`.
   - **ContentController:** pasar `request.Order` al CreateContentCommand y al UpdateContentCommand.

6. **Listado de hijos**
   - **IContentRepository.GetChildrenAsync:** debe devolver ordenado por `Order` (y como desempate por `Id` o `CreatedAt`). **Archivo:** `ContentRepository.cs`, en `GetChildrenAsync` cambiar `.OrderBy(c => c.Title)` a `.OrderBy(c => c.Order).ThenBy(c => c.Id)`.
   - Si existe endpoint explícito “hijos de un contenido”, asegurar que use este método y que la respuesta esté ordenada por Order.

7. **Migración de datos**
   - En la migración que añade la columna `order`, asignar valores únicos por `(parent_content_id)` como se indicó arriba (p. ej. ROW_NUMBER sobre created_at, id) para que no falle ningún índice único si se añade uno en el futuro.

### Frontend

1. **Tipos**
   - **Archivo:** `frontend/src/modules/core/types.ts`
   - En interfaz `Content` y `ContentListItem`: añadir `order: number`.

2. **API**
   - **Archivo:** `frontend/src/modules/core/api/core-api.ts`
   - En `createContent` body: añadir `order?: number | null`.
   - En `updateContent` body: añadir `order?: number | null`.

3. **UI (listado y formularios)**
   - Donde se listen hijos de un contenido (árbol o lista): ordenar por `order` (y desempate por id). Si se muestra una tabla, mostrar la columna Order si aporta valor.
   - En formulario de crear/editar contenido: campo opcional “Orden” (número) que se envíe como `order` en create/update.
   - Opcional: pantalla o modal “Reordenar hijos” que llame a update con distintos `order` para cada hijo (o endpoint dedicado SetOrder si se implementa).

### Criterio de aceptación

- Contenidos con el mismo `parentContentId` (o null) tienen `order` distinto o igual según se defina (un valor por fila; desempate por id).
- GET contenido y GET list devuelven `order`.
- Crear contenido sin `order` asigna el siguiente disponible para ese padre.
- Actualizar `order` cambia la posición en el listado de hijos.
- Frontend muestra y permite editar orden donde se listen hijos.

---

## C.1 – Capa de composición por bloques (ContentBlock)

**Objetivo:** Entidad Block, relación Content → Blocks (1:N), persistencia, comandos y API.

### Backend

1. **Entidad ContentBlock**
   - **Archivo nuevo:** `src/Services/Core/IODA.Core.Domain/Entities/ContentBlock.cs`
   - Propiedades: `Guid Id`, `Guid ContentId`, `string BlockType` (ej. "hero", "text", "image"), `int Order` (posición dentro del contenido), `Dictionary<string, object> Payload` (JSONB; datos del bloque). No heredar de AggregateRoot; es entidad hija del agregado Content.
   - Constructor privado para EF. Factory estático: `ContentBlock.Create(Guid contentId, string blockType, int order, Dictionary<string, object>? payload)` que valide blockType no vacío y asigne Id = Guid.NewGuid(), Payload = payload ?? new().
   - Métodos: `SetOrder(int order)`, `UpdatePayload(Dictionary<string, object> payload)` (y actualizar si el agregado expone una colección de bloques y lógica de reordenación).

2. **Content – colección de bloques**
   - **Archivo:** `src/Services/Core/IODA.Core.Domain/Entities/Content.cs`
   - Añadir: `private readonly List<ContentBlock> _blocks = [];` y `public IReadOnlyCollection<ContentBlock> Blocks => _blocks.AsReadOnly();`
   - Métodos en Content (opcional, si se quiere lógica en el agregado): `AddBlock(ContentBlock block)`, `RemoveBlock(Guid blockId)`, `ReorderBlocks(IReadOnlyList<Guid> blockIdsInOrder)` (actualizar Order de cada bloque según la lista). Si se prefiere, la reordenación puede vivir solo en aplicación (handler que actualice bloques uno a uno).
   - Los bloques se eliminan en cascada al borrar Content (configurar en EF).

3. **Persistencia**
   - **Archivo nuevo:** `src/Services/Core/IODA.Core.Infrastructure/Persistence/Configurations/ContentBlockConfiguration.cs`
   - Tabla `content_blocks`. PK `Id`. FK `content_id` → `contents(id)` ON DELETE CASCADE. Columnas: `id` (uuid), `content_id` (uuid), `block_type` (string, max 100), `order` (int), `payload` (jsonb). Índice `ix_content_blocks_content_id_order` en (content_id, order).
   - **CoreDbContext:** `public DbSet<ContentBlock> ContentBlocks => Set<ContentBlock>();` y aplicar configuración.
   - **ContentConfiguration:** `HasMany(c => c.Blocks).WithOne().HasForeignKey(b => b.ContentId).OnDelete(DeleteBehavior.Cascade);` y `Navigation(c => c.Blocks).HasField("_blocks");` (ajustar si ContentBlock tiene nav a Content).
   - Crear migración EF Core.

4. **Repositorio**
   - No es obligatorio un IContentBlockRepository si siempre se cargan bloques con Content. En `ContentRepository.GetByIdAsync` (y donde se necesite contenido con bloques): `.Include(c => c.Blocks)` y ordenar bloques por Order. Si se prefiere consultar bloques sueltos: interfaz `IContentBlockRepository` con GetByContentId, Add, Update, Delete, y registrar en UoW.

5. **DTOs**
   - **Archivo:** `src/Services/Core/IODA.Core.Application/DTOs/ContentBlockDto.cs` (nuevo)
   - Record: `ContentBlockDto(Guid Id, Guid ContentId, string BlockType, int Order, Dictionary<string, object> Payload)`.
   - **ContentDto:** añadir `IReadOnlyList<ContentBlockDto> Blocks` (ordenado por Order).
   - **ContentMappings:** en ToDto, mapear `content.Blocks.OrderBy(b => b.Order).Select(b => new ContentBlockDto(b.Id, b.ContentId, b.BlockType, b.Order, new Dictionary<string, object>(b.Payload))).ToList()`.

6. **Comandos y handlers**
   - **Añadir bloque:** `AddContentBlockCommand(ContentId, BlockType, int Order, Dictionary<string, object>? Payload)`. Handler: cargar Content (con Blocks), crear ContentBlock, añadir a Content (o guardar vía repositorio de bloques), guardar. Devolver BlockId (Guid).
   - **Actualizar bloque:** `UpdateContentBlockCommand(BlockId, Dictionary<string, object>? Payload, int? Order)`. Handler: cargar bloque y contenido; actualizar payload/order; guardar.
   - **Eliminar bloque:** `RemoveContentBlockCommand(BlockId)`. Handler: cargar bloque, verificar ContentId, borrar bloque (y guardar).
   - **Reordenar bloques:** `ReorderContentBlocksCommand(ContentId, IReadOnlyList<Guid> BlockIdsInOrder)`. Handler: cargar Content con Blocks; para cada bloque, asignar Order según índice en la lista; guardar.
   - CreateContentCommand y UpdateContentCommand: opcionalmente aceptar `IReadOnlyList<ContentBlockDto>? Blocks` para crear/actualizar contenido y bloques en la misma transacción (menos round-trips; implementar si se desea UX de “guardar contenido y bloques a la vez”).

7. **API**
   - **ContentController** (o controller dedicado Blocks):
     - `POST api/projects/{projectId}/content/{contentId}/blocks` → body: `{ blockType, order, payload }` → devuelve blockId.
     - `PUT api/projects/{projectId}/content/{contentId}/blocks/{blockId}` → body: `{ payload?, order? }`.
     - `DELETE api/projects/{projectId}/content/{contentId}/blocks/{blockId}`.
     - `POST api/projects/{projectId}/content/{contentId}/blocks/reorder` → body: `{ blockIds: Guid[] }` (orden deseado).
   - GET contenido por id ya debe devolver `ContentDto` con `Blocks` poblado (mapeo anterior).

### Frontend

1. **Tipos**
   - **Archivo:** `frontend/src/modules/core/types.ts`
   - Añadir: `ContentBlock { id: string; contentId: string; blockType: string; order: number; payload: Record<string, unknown> }`.
   - En `Content`: añadir `blocks: ContentBlock[]`.

2. **API**
   - **Archivo:** `frontend/src/modules/core/api/core-api.ts`
   - Añadir: `addContentBlock(projectId, contentId, body: { blockType: string; order: number; payload?: Record<string, unknown> })` → POST …/content/{contentId}/blocks, devuelve blockId.
   - `updateContentBlock(projectId, contentId, blockId, body: { payload?: Record<string, unknown>; order?: number })` → PUT …/content/…/blocks/{blockId}.
   - `removeContentBlock(projectId, contentId, blockId)` → DELETE.
   - `reorderContentBlocks(projectId, contentId, body: { blockIds: string[] })` → POST …/blocks/reorder.
   - getContent ya devolverá `blocks` en el objeto Content.

3. **UI**
   - En la pantalla de edición de contenido (o detalle): sección “Bloques” que liste `content.blocks` ordenados por `order`.
   - Botones/acciones: Añadir bloque (selector de tipo + formulario mínimo para payload), Editar bloque (payload/orden), Eliminar, Reordenar (drag-and-drop o flechas). Las acciones llaman a los endpoints anteriores.
   - Tipos de bloque: al menos un catálogo estático en front (hero, text, image, etc.) o obtenido de API; el backend en C.2 validará contra AllowedBlockTypes del schema.

### Criterio de aceptación

- Un contenido puede tener N bloques; cada bloque tiene Id, ContentId, BlockType, Order, Payload.
- API CRUD de bloques (crear, actualizar, eliminar, reordenar) y GET contenido devuelve blocks ordenados.
- Frontend muestra y edita bloques del contenido.

---

## C.2 – Allowed block configuration en ContentSchema

**Objetivo:** Que cada ContentSchema defina qué tipos de bloque puede tener un contenido; validar al añadir/actualizar bloques.

### Backend

1. **Modelo de datos – AllowedBlockTypes**
   - Opción A (recomendada): columna JSONB en `content_schemas`, p. ej. `allowed_block_types`. Estructura: lista de objetos `{ "blockType": "hero", "minOccurrences": 0, "maxOccurrences": 1 }` (min/max opcionales; si no se envían, sin límite).
   - Opción B: tabla `schema_allowed_block_types` (SchemaId, BlockType, MinOccurrences, MaxOccurrences). Más normalizado, más tablas.
   - **Archivo:** `src/Services/Core/IODA.Core.Domain/Entities/ContentSchema.cs`
   - Añadir propiedad: `public IReadOnlyList<AllowedBlockTypeRule> AllowedBlockTypes { get; private set; }` (o tipo equivalente; si es JSONB, usar clase/record serializable: BlockType string, MinOccurrences int?, MaxOccurrences int?).
   - En Create y en método de actualización (si existe): aceptar y asignar lista de reglas.

2. **Persistencia**
   - **ContentSchemaConfiguration:** mapear `AllowedBlockTypes` a columna jsonb `allowed_block_types` con conversión a lista de reglas (o configurar tabla si se eligió Opción B). Crear migración (añadir columna o tabla).

3. **Validación al añadir/actualizar bloques**
   - **Archivo nuevo o existente:** servicio o validador `IBlockAllowedBySchemaValidator` (o dentro del handler): dado ContentId y BlockType (y opcionalmente lista actual de bloques del contenido), cargar Content + Schema, comprobar que Schema.AllowedBlockTypes contenga BlockType; si hay min/max, comprobar que el conteo actual de bloques de ese tipo + el nuevo no exceda max ni quede por debajo de min al eliminar.
   - Llamar a este validador en:
     - AddContentBlockCommandHandler (antes de añadir).
     - UpdateContentBlockCommandHandler si se cambia BlockType (si se permite cambiar tipo; si no, no aplica).
     - RemoveContentBlockCommandHandler si se quiere validar min (al quitar uno, que no quede por debajo de min).
   - Si no está permitido: lanzar excepción de dominio o aplicación (p. ej. `BlockTypeNotAllowedException`) con mensaje claro.

4. **API y DTOs de Schema**
   - **ContentSchemaDto** y **CreateContentSchemaCommand**: añadir `IReadOnlyList<AllowedBlockTypeRuleDto>? AllowedBlockTypes` (BlockType, MinOccurrences?, MaxOccurrences?). En create/update de schema, persistir esta lista.
   - **GetSchema** y **ListSchemas**: devolver `allowedBlockTypes` en el DTO.

### Frontend

1. **Tipos**
   - En tipo ContentSchema (y CreateSchemaRequest si aplica): `allowedBlockTypes?: { blockType: string; minOccurrences?: number; maxOccurrences?: number }[]`.
   - Al cargar schema para edición de contenido, usar `allowedBlockTypes` para restringir el selector de “tipo de bloque” al añadir (solo mostrar tipos permitidos).

2. **Schema designer**
   - En la pantalla de creación/edición de schema: sección “Bloques permitidos” donde se listen los tipos permitidos y opcionalmente min/max por tipo. Añadir/quitar tipos y guardar con el schema.

### Criterio de aceptación

- ContentSchema tiene lista de reglas (tipo de bloque + min/max opcional).
- Al añadir un bloque a un contenido, si el BlockType no está en AllowedBlockTypes del schema del contenido, la API devuelve error.
- Si hay maxOccurrences, no se puede añadir más bloques de ese tipo por encima del máximo.
- Frontend solo ofrece tipos permitidos al añadir bloque y muestra la configuración en el schema.

---

## M.1 – Política explícita ContentType vs SchemaType

**Objetivo:** Documentar y, si aplica, implementar regla que evite desincronización entre Content.ContentType y ContentSchema.SchemaType.

### Backend

1. **Documentación**
   - **Archivo:** `docs/008-SCHEMAS_BACKEND/POLITICA_CONTENT_TYPE.md` (nuevo) o sección en AUDITORIA.
   - Redactar: “ContentType en Content es una copia inmutable del Schema.SchemaType en el momento de creación del contenido. No se permite cambiar SchemaType de un ContentSchema que tenga al menos un Content asociado.” O bien: “Si en el futuro se permite cambiar SchemaType, se debe ejecutar un proceso (comando o script) que actualice Content.ContentType de todos los contenidos de ese schema.”

2. **Regla de negocio (opcional)**
   - **Archivo:** `src/Services/Core/IODA.Core.Application/Commands/Schemas/UpdateContentSchemaCommand.cs` (o el que actualice schema): si existe comando “UpdateSchema” que pueda cambiar SchemaType, añadir comprobación: si el nuevo SchemaType es distinto del actual, consultar si existe algún Content con ese SchemaId; si existe, lanzar InvalidOperationException con mensaje que remita a la política.
   - Si no existe actualización de SchemaType hoy, dejar solo la documentación.

3. **Lectura “effective content type” (opcional)**
   - En **GetContentByIdQueryHandler** o en el mapeo a DTO: opcionalmente exponer un campo `EffectiveContentType` que sea `content.Schema?.SchemaType ?? content.ContentType` para que el cliente pueda usar siempre el tipo “oficial” del schema. No sustituir ContentType en el DTO; añadir solo si se desea.

### Frontend

- Ningún cambio obligatorio. Si se añade `effectiveContentType` en el DTO, añadir al tipo TypeScript y usarlo en UI si aporta (p. ej. mostrar “tipo efectivo” en detalle).

### Criterio de aceptación

- Documento de política existe y describe inmutabilidad de ContentType y regla sobre cambio de SchemaType.
- Si hay actualización de schema por tipo, la aplicación rechaza cambiar SchemaType cuando hay contenidos usando ese schema (o se documenta el proceso de migración).

---

## M.2 – Unificar FieldName y Slug en FieldDefinition

**Objetivo:** Usar Slug como única clave técnica; eliminar o deprecar FieldName en lectura/escritura de contenido y validación.

### Backend

1. **Dominio**
   - **Archivo:** `src/Services/Core/IODA.Core.Domain/Entities/ContentSchema.cs` (clase FieldDefinition)
   - Mantener propiedad `FieldName` por compatibilidad con datos existentes pero marcar como `[Obsolete("Use Slug as the technical key")]` o documentar que es legacy.
   - En **SchemaValidationService** y cualquier sitio que use `fieldDef.FieldName` para buscar en `Fields`: cambiar a `fieldDef.Slug`. Comprobar: `SchemaValidationService.Validate` usa `fieldDef.FieldName` en `TryGetValue(fieldDef.FieldName, ...)` y en errores; cambiar a `fieldDef.Slug`.
   - En **FieldDefinition.Create** y en persistencia: seguir rellenando FieldName = Slug (para datos ya guardados y nuevos) hasta que se migren datos y se elimine FieldName en una versión posterior.

2. **API y DTOs**
   - **FieldDefinitionDto** (o equivalente): exponer `slug` como la clave técnica; si se expone `fieldName`, marcarla obsoleta o no usarla en nuevos contratos. Los clientes deben usar `slug` como clave en `content.fields[slug]`.

3. **Migración de datos**
   - Si en BD existen FieldDefinition con FieldName != Slug, script o migración que actualice FieldName = Slug donde difieran (y actualizar contenido Fields: renombrar claves de FieldName a Slug en el JSONB). Esto puede ser una migración de datos en PostgreSQL: actualizar `field_definitions` y luego un script que, por cada contenido, reemplace en `fields` las claves viejas por Slug según su schema. Detallar en comentario de la tarea si se hace en este sprint.

### Frontend

1. **Tipos**
   - En `FieldDefinition`: usar `slug` como clave para referenciar el campo en `content.fields`. No usar `fieldName` en lógica nueva.

2. **Formularios de contenido**
   - Al construir el payload de create/update content, usar las claves de `content.fields` como slugs (las que vienen del schema como `field.slug`). Al leer schema, usar `field.slug` para acceder a `content.fields[field.slug]`.

### Criterio de aceptación

- Validación de contenido usa Slug para leer/escribir Fields.
- DTOs y API recomiendan Slug como clave técnica; FieldName no se usa en flujos nuevos.
- Documentación o comentarios indican que FieldName está deprecado y se eliminará en una versión futura.

---

## L.1 – Documentar criterio Hierarchy/Tag como no-AggregateRoot

**Objetivo:** Dejar explícito por qué Hierarchy y Tag no son AggregateRoot y cuándo podría reconsiderarse.

### Backend / Documentación

1. **Archivo:** `docs/008-SCHEMAS_BACKEND/AGREGADOS_Y_CATALOGOS.md` (nuevo) o sección en AUDITORIA.
   - Texto: “Hierarchy y Tag son entidades de catálogo asociadas a Project; no son AggregateRoot. Su ciclo de vida es más simple (CRUD por proyecto) y no requieren eventos de dominio ni transacciones complejas como Content. Content y ContentSchema son agregados. Si en el futuro se exigen reglas como ‘al borrar una Hierarchy desasignar todas las ContentHierarchy en la misma transacción’, se puede orquestar desde un servicio de aplicación (comando DeleteHierarchyCommand) sin necesidad de convertir Hierarchy en agregado raíz.”
   - Incluir referencia a `IODA.Core.Domain/Entities/Hierarchy.cs` y `Tag.cs`.

### Frontend

- Ninguno.

### Criterio de aceptación

- Documento existe y explica el criterio de agregados vs entidades de catálogo para Hierarchy y Tag.

---

## Resumen de archivos a tocar (por tarea)

| Tarea | Backend (principales) | Frontend (principales) |
|-------|------------------------|-------------------------|
| H.1   | Content.cs, IContentRepository, ContentRepository, ContentConfiguration, migración, CreateContentCommand/Handler, UpdateContentCommand/Handler, ContentDto, ContentListItemDto, ContentMappings, ContentController, CreateContentRequest, UpdateContentRequest | types.ts (Content, ContentListItem), core-api.ts (create/update body), pantallas lista/forma contenido (order) |
| C.1   | ContentBlock.cs (nuevo), Content.cs (Blocks), ContentBlockConfiguration, CoreDbContext, ContentConfiguration, ContentDto + ContentBlockDto, ContentMappings, comandos/handlers bloques, ContentController o BlocksController | types.ts (ContentBlock, Content.blocks), core-api.ts (blocks CRUD + reorder), UI edición contenido (bloques) |
| C.2   | ContentSchema.cs (AllowedBlockTypes), ContentSchemaConfiguration, migración, validador bloque vs schema, AddContentBlockCommandHandler, ContentSchemaDto, CreateContentSchemaCommand, get/list schema | types.ts (schema.allowedBlockTypes), Schema designer UI (bloques permitidos), selector tipo de bloque en contenido |
| M.1   | docs/008/.../POLITICA_CONTENT_TYPE.md, opcional: UpdateContentSchemaCommand/Handler, GetContentById DTO effectiveContentType | opcional: tipo effectiveContentType |
| M.2   | ContentSchema.cs FieldDefinition (obsolete FieldName), SchemaValidationService (usar Slug), FieldDefinitionDto, migración datos si FieldName != Slug | types.ts y formularios: usar slug como clave en fields |
| L.1   | docs/008/.../AGREGADOS_Y_CATALOGOS.md | — |

---

**Referencia:** `docs/008-SCHEMAS_BACKEND/AUDITORIA_ESQUEMAS_BACKEND.md`
