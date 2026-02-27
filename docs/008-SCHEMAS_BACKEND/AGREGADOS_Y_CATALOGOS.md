# Agregados vs Entidades de Catálogo

## Definiciones

- **AggregateRoot**: Entidad raíz de un agregado que garantiza consistencia transaccional, publica eventos de dominio, y controla el ciclo de vida de entidades relacionadas.
- **Entidad de Catálogo**: Entidad simple que representa un valor de referencia (lookup), con operaciones CRUD básicas y sin reglas de negocio complejas.

## Criterio aplicado en IODA

### Hierarchy y Tag: Entidades de Catálogo

**Hierarchy** (`src/Services/Core/IODA.Core.Domain/Entities/Hierarchy.cs`) y **Tag** (`src/Services/Core/IODA.Core.Domain/Entities/Tag.cs`) son entidades de catálogo por:

1. **Ciclo de vida simple**: Solo requieren operaciones CRUD básicas por proyecto.
2. **Sin eventos de dominio**: No publican eventos cuando se crean, actualizan o eliminan.
3. **Sin transacciones complejas**: El borrado se maneja en la capa de aplicación (desasignar relaciones con contenidos) sin necesidad de atomicidad transaccional.
4. **Relaciones unidireccionales**: Pertenecen a Project, pero no tienen entidades dependientes que requieran consistencia transaccional.
5. **Sin reglas de negocio complejas**: No hay invariantes que mantener entre múltiples entidades.

### Content y ContentSchema: AggregateRoot

**Content** y **ContentSchema** sí son AggregateRoot porque:

1. **Content** tiene entidades relacionadas: Blocks, Versions, relaciones con Tags, Hierarchies, Sites.
2. **Consistencia transaccional**: Al crear/actualizar un contenido, todas las operaciones deben ejecutarse atómicamente.
3. **Eventos de dominio**: Publican eventos como `ContentCreated`, `ContentPublished`, etc.
4. **Reglas de negocio complejas**: Validación de campos contra schema, control de versiones, publicación, ordenamiento de bloques.

## Cuándo podría reconsiderarse

Si en el futuro se requieren reglas como:
- "Al borrar una Hierarchy, desasignar todas las ContentHierarchy en la misma transacción"
- "Al borrar un Tag, eliminar automáticamente las relaciones con contenidos"

Se podría orquestar desde un servicio de aplicación (comando DeleteHierarchyCommand) sin necesidad de convertir Hierarchy en AggregateRoot.

## Referencias

- Entidad `Hierarchy`: `src/Services/Core/IODA.Core.Domain/Entities/Hierarchy.cs`
- Entidad `Tag`: `src/Services/Core/IODA.Core.Domain/Entities/Tag.cs`
- Entidad `Content`: `src/Services/Core/IODA.Core.Domain/Entities/Content.cs`
- Entidad `ContentSchema`: `src/Services/Core/IODA.Core.Domain/Entities/ContentSchema.cs`
