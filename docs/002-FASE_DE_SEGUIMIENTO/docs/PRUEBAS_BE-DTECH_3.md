# Guía de pruebas — Fase 3 Backend (Optimización y refactorización avanzada)

Rama: `fix/BE-DTECH-3/advanced-optimization-and-refactoring`  
Objetivo: validar TreatWarningsAsErrors, refactor de SchemaValidationService, documentación de GetFile público y convención unificada 400/409.

---

## Resumen de cambios (Fase 3)

| Item | Descripción |
|------|-------------|
| 3.1 | `TreatWarningsAsErrors: true` en `Directory.Build.props`; correcciones en Shared y NoWarn progresivos (CS1591, IDE0011, etc.) por tipo de proyecto. |
| 3.2 | `SchemaValidationService` refactorizado a estrategias por tipo: `IFieldValidator` + implementaciones (String, Number, Boolean, Date, Enum, Default). |
| 3.3 | `MediaController.GetFile` con `[AllowAnonymous]` documentado en código (decisión: público para img/video y CDN; si se requiere privacidad, usar signed URLs). |
| 3.4 | Convención unificada en `IODA.Shared.Api.ExceptionMappingConvention`: `ArgumentException` → 400; `InvalidOperationException` con mensaje "already exists" → 409, resto → 400. Aplicada en Core, Authorization, Publishing, Indexing. |

---

## Requisitos previos

- Mismo entorno que Fase 1 (PostgreSQL, User Secrets o variables de entorno, JWT compartido). Ver `PRUEBAS_BE-DTECH_1.md` para configuración de APIs y bases de datos.
- Para probar endpoints que requieren JWT: obtener token vía Identity (login/register) y usar header `Authorization: Bearer <token>`.

---

## 1. Build con TreatWarningsAsErrors (3.1)

Desde la raíz del repositorio:

```bash
dotnet build src/Services/Core/IODA.Core.API/IODA.Core.API.csproj
dotnet build src/Services/Authorization/IODA.Authorization.API/IODA.Authorization.API.csproj
dotnet build src/Services/Publishing/IODA.Publishing.API/IODA.Publishing.API.csproj
dotnet build src/Services/Indexing/IODA.Indexing.API/IODA.Indexing.API.csproj
```

**Esperado:** Compilación correcta, 0 advertencias, 0 errores. Si se introduce un nuevo warning en proyectos con `TreatWarningsAsErrors`, el build fallará.

Opcional — build de toda la solución (si existe):

```bash
dotnet build IODA.sln
```

---

## 2. Convención 400 / 409 (3.4)

Comprobar que las excepciones de “conflicto” (recurso ya existe) devuelven **409 Conflict** y el resto de argumentos/operaciones inválidas **400 Bad Request**.

### 2.1 Core API — esquema duplicado → 409

1. Crear un proyecto y un esquema de contenido (por ejemplo tipo `"Article"`) con un token válido.
2. Intentar crear **otro** esquema en el mismo proyecto con el mismo `schemaType` (p. ej. `"Article"`).

**Esperado:** Respuesta **409 Conflict** con `Title: "Conflict"` y mensaje indicando que el tipo ya existe. Cuerpo en `application/problem+json`.

Ejemplo (reemplazar `PROJECT_ID`, `JWT` y ajustar host/puerto):

```bash
# Primera creación → 201
curl -s -X POST "http://localhost:5000/api/projects/PROJECT_ID/schemas" \
  -H "Authorization: Bearer JWT" -H "Content-Type: application/json" \
  -d '{"schemaType":"Article","schemaName":"Article","description":"","fields":[]}'

# Segunda creación mismo tipo → 409
curl -s -X POST "http://localhost:5000/api/projects/PROJECT_ID/schemas" \
  -H "Authorization: Bearer JWT" -H "Content-Type: application/json" \
  -d '{"schemaType":"Article","schemaName":"Article","description":"","fields":[]}'
```

### 2.2 Authorization API — rol duplicado → 409

1. Crear un rol (p. ej. `"Editor"`) con token válido.
2. Intentar crear de nuevo un rol con el mismo nombre.

**Esperado:** **409 Conflict** con mensaje de conflicto (rol ya existe).

---

## 3. MediaController.GetFile — acceso anónimo (3.3)

El endpoint `GET /api/projects/{projectId}/media/{mediaId}/file` está marcado con `[AllowAnonymous]` de forma intencional.

**Comprobación:**

1. Subir un media al proyecto (POST con JWT) y anotar `projectId` y `mediaId`.
2. Llamar al endpoint del archivo **sin** cabecera `Authorization`:

```bash
curl -s -o /dev/null -w "%{http_code}" "http://localhost:5000/api/projects/PROJECT_ID/media/MEDIA_ID/file"
```

**Esperado:** **200** (si el media existe y el proyecto coincide). Si el media no existe o no pertenece al proyecto: **404**.

La decisión de diseño está documentada en el XML del método en `MediaController.cs` y en `docs/FASE_DE_SEGUIMIENTO/BACKEND.md` (Fase 3.3).

---

## 4. Validación de contenido contra esquema (3.2)

Tras el refactor a `IFieldValidator`, la validación de contenido (create/update) debe seguir comportándose igual.

**Comprobación rápida:**

1. Crear un esquema con un campo requerido (p. ej. string `title` requerido).
2. Crear contenido **sin** enviar ese campo o con tipo incorrecto.

**Esperado:** **400** con detalle de validación de esquema (ProblemDetails con `errors` por campo), coherente con el comportamiento anterior al refactor.

---

## 5. Tests unitarios (opcional)

Si existen tests para `SchemaValidationService` o para los handlers que usan validación de esquema, ejecutarlos tras el refactor:

```bash
dotnet test --no-build --filter "FullyQualifiedName~SchemaValidation|FullyQualifiedName~CreateContent|FullyQualifiedName~UpdateContent"
```

Ajustar el filtro según los nombres de los proyectos y clases de test del repositorio.

---

## Criterios de aceptación Fase 3

- [ ] Build de los APIs (Core, Authorization, Publishing, Indexing) con **0 errores y 0 advertencias** (TreatWarningsAsErrors activo).
- [ ] Crear esquema duplicado (mismo `schemaType` en el mismo proyecto) → **409 Conflict**.
- [ ] Crear rol duplicado en Authorization → **409 Conflict**.
- [ ] `GET .../media/{id}/file` sin Authorization → **200** cuando el media existe (acceso público documentado).
- [ ] Validación de contenido contra esquema (campos requeridos/tipos) sigue devolviendo **400** con errores por campo.
