# Política: ContentType vs SchemaType

## Definiciones

- **ContentType**: Campo en la entidad `Content` que almacena el tipo de contenido (ej. "article", "page", "landing"). Es una copia inmutable del `SchemaType` en el momento de creación del contenido.

- **SchemaType**: Campo en la entidad `ContentSchema` que define el tipo técnico del schema (slug único en el proyecto, ej. "article", "page").

## Regla principal

> **ContentType en Content es una copia inmutable del SchemaType en el momento de creación del contenido.**

Cuando se crea un `Content` a partir de un `ContentSchema`, el `ContentType` se copia desde `Schema.SchemaType` y no debe cambiarse después. Esto garantiza:
- Trazabilidad: el contenido siempre sabe qué tipo tenía cuando se creó.
- Integridad: cambiar el tipo de un schema no afecta contenidos existentes.
- Consistencia: el tipo visto por el frontend no depende de cambios futuros en el schema.

## Implicaciones

### 1. Cambio de SchemaType en un Schema

Si en el futuro se permite cambiar `SchemaType` en un `ContentSchema`:
- **No se debe permitir** si existe al menos un `Content` asociado a ese schema.
- Si se requiere cambiar, se debe ejecutar un proceso de migración que actualice `Content.ContentType` de todos los contenidos de ese schema antes de modificar el schema.

### 2. Recomendación de implementación

Al actualizar un schema que pueda cambiar `SchemaType`, implementar la siguiente validación:

```csharp
// En UpdateContentSchemaCommandHandler
var existingContents = await _unitOfWork.Contents.GetBySchemaIdAsync(request.SchemaId);
if (existingContents.Any() && existingSchema.SchemaType != request.NewSchemaType)
{
    throw new InvalidOperationException(
        "No se puede cambiar el SchemaType de un schema que tiene contenidos asociados. " +
        "Elimine los contenidos o migre sus tipos antes de modificar el schema.");
}
```

### 3. EffectiveContentType (opcional)

Si se necesita el tipo "oficial" actual del schema en tiempo de lectura:
- Usar `content.Schema?.SchemaType ?? content.ContentType`
- Esto permite que el frontend siempre muestre el tipo actual del schema, incluso si el contenido se creó con una versión anterior.

## Excepciones

- **No hay excepciones** a la inmutabilidad de ContentType para contenidos existentes.
- Solo aplica a nuevos contenidos si se decide permitir crear contenido con un tipo diferente al del schema (no recomendado).

## Referencias

- Entidad `Content`: `src/Services/Core/IODA.Core.Domain/Entities/Content.cs`
- Entidad `ContentSchema`: `src/Services/Core/IODA.Core.Domain/Entities/ContentSchema.cs`
- Tarea relacionada: `docs/008-SCHEMAS_BACKEND/TAREAS_AGENTE_FULLSTACK.md` (M.1)
