# Cómo probar el Media Service (Fase 2.2)

Guía para probar la **API de Media** del Core: subida de archivos, listado, metadatos y descarga/preview.

**Referencia:** `docs/FASE_DE_MEJORAS/BACKEND_STEPS.md` (sección 2.2 Media Service).

---

## 1. Prerrequisitos

- **Conexión a PostgreSQL:** La migración y la Core API usan la connection string de configuración. Si no usas `appsettings.json` con el usuario correcto, define la variable de entorno **antes** de ejecutar la migración o levantar la API (en bash/zsh):
  ```bash
  export ConnectionStrings__DefaultConnection="Host=localhost;Database=ioda_core;Username=postgres;Password=postgres;Include Error Detail=true"
  ```
  Ajusta `Username` y `Password` si en tu entorno usas otros valores. En Windows (PowerShell): `$env:ConnectionStrings__DefaultConnection="Host=localhost;Database=ioda_core;Username=postgres;Password=postgres;Include Error Detail=true"`.
- **Core API** con la migración `AddMediaItems` aplicada:
  ```bash
  dotnet ef database update --project src/Services/Core/IODA.Core.Infrastructure --startup-project src/Services/Core/IODA.Core.API
  ```
- **Identity API** levantada para obtener JWT (igual que en COMO_PROBAR_BACKEND_FASE_1).
- **Proyecto existente** en Core (un `projectId` válido).

### Si aparece "relation projects already exists"

Significa que la base `ioda_core` **ya tiene** las tablas de la migración inicial (p. ej. de una fase anterior), pero EF no tiene registrada esa migración en `__EFMigrationsHistory`. Hay que marcar la migración inicial como aplicada y luego aplicar solo `AddMediaItems`:

1. Conéctate a PostgreSQL (psql, DBeaver, etc.) a la base `ioda_core` y ejecuta:
   ```sql
   INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
   VALUES ('20260201033638_InitialCreate', '9.0.0');
   ```
2. Vuelve a ejecutar:
   ```bash
   dotnet ef database update --project src/Services/Core/IODA.Core.Infrastructure --startup-project src/Services/Core/IODA.Core.API
   ```
   Así solo se aplicará la migración `AddMediaItems` (tabla `media_items`).

---

## 2. Configuración opcional

En **appsettings.json** (o **appsettings.Development.json**) de Core API puedes definir:

```json
"Media": {
  "StoragePath": "C:\\ioda-media"
}
```

- Si **no** configuras `Media:StoragePath` (o `Media:RootPath`), los archivos se guardan en un directorio temporal del sistema (p. ej. `%TEMP%\ioda-media` en Windows o `/var/folders/.../ioda-media` en macOS).
- Para desarrollo local suele bastar con el valor por defecto. Para producción conviene usar una ruta fija y con espacio suficiente.

---

## 3. Obtener JWT y variables de entorno

Igual que en COMO_PROBAR_BACKEND_FASE_1:

1. Login en Identity: `POST /api/auth/login` con email y password.
2. Copiar el `accessToken` de la respuesta.
3. En Swagger Core: **Authorize** → pegar el token (solo el valor, sin "Bearer").

Para **curl** o **.http**:

```bash
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."   # tu accessToken
BASE=http://localhost:5269
PROJECT_ID=...   # GUID de un proyecto existente (GET /api/projects)
USER_ID=...      # GUID del usuario (p. ej. el que devuelve Identity en login)
```

---

## 4. Subir un archivo (POST media)

### 4.1 Desde Swagger

1. **POST** `api/projects/{projectId}/media`
2. En el body elegir **form-data** (no raw JSON).
3. Añadir campos:
   - **file** (type File): seleccionar un archivo (imagen, PDF, etc.).
   - **createdBy** (string): GUID del usuario (requerido).
   - **displayName** (string, opcional): nombre para mostrar (si no se envía se usa el nombre del archivo).
4. Ejecutar. Respuesta **201 Created** con el DTO del media creado (id, publicId, fileName, contentType, sizeBytes, storageKey, version, metadata, createdAt, createdBy).

### 4.2 Con curl

```bash
# Sustituir PROJECT_ID y USER_ID
curl -X POST "$BASE/api/projects/$PROJECT_ID/media" \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@/ruta/a/mi-imagen.png" \
  -F "createdBy=$USER_ID" \
  -F "displayName=Logo principal"
```

Respuesta: JSON con el media recién creado. Anotar el **id** para los siguientes pasos.

---

## 5. Listar media del proyecto (GET media)

### 5.1 Desde Swagger

- **GET** `api/projects/{projectId}/media?page=1&pageSize=20`

Respuesta paginada:

```json
{
  "items": [
    {
      "id": "...",
      "publicId": "med_...",
      "projectId": "...",
      "fileName": "mi-imagen.png",
      "displayName": "Logo principal",
      "contentType": "image/png",
      "sizeBytes": 12345,
      "storageKey": "...",
      "version": 1,
      "metadata": null,
      "createdAt": "2026-01-24T...",
      "createdBy": "..."
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

### 5.2 Con curl

```bash
curl -s "$BASE/api/projects/$PROJECT_ID/media?page=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"
```

---

## 6. Obtener metadatos de un media (GET media/{mediaId})

- **GET** `api/projects/{projectId}/media/{mediaId}`

Devuelve el mismo DTO que en el listado (un solo ítem). Útil para el **MediaPicker** o el selector del CMS una vez conocido el id.

**Importante:** `{mediaId}` debe ser el **`id`** (GUID) del ítem, tal como devuelve el POST o el listado (p. ej. `961b85d9-bb88-4f2e-9dac-b3480b8c19bd`). **No** uses `storageKey`, `publicId` ni el nombre del archivo; si usas otro valor la API devuelve 404.

```bash
MEDIA_ID=...   # el campo "id" (GUID) de la respuesta del POST o del listado, p. ej. 961b85d9-bb88-4f2e-9dac-b3480b8c19bd
curl -s "$BASE/api/projects/$PROJECT_ID/media/$MEDIA_ID" \
  -H "Authorization: Bearer $TOKEN"
```

---

## 7. Descargar o previsualizar el archivo (GET media/{mediaId}/file)

- **GET** `api/projects/{projectId}/media/{mediaId}/file`  
  Devuelve el archivo en stream (Content-Type según el archivo). Ideal para **preview** en el navegador o en un `<img src="...">` si el frontend usa la URL con JWT (o un proxy).

- **GET** `api/projects/{projectId}/media/{mediaId}/file?download=true`  
  Igual pero con header `Content-Disposition: attachment` para forzar **descarga**.

**Importante:** `{mediaId}` debe ser el **`id`** (GUID) del media, no el `storageKey` ni la ruta del archivo. Ejemplo de URL correcta: `.../media/961b85d9-bb88-4f2e-9dac-b3480b8c19bd/file`. Si usas algo como `.../media/ff2223f42e1b.../961b85d9..._imagen.png/file` obtendrás 404.

### 7.1 Probar en el navegador

Con Swagger, al ejecutar **GET .../file** la respuesta es el binario; en Swagger UI puedes abrir el enlace en nueva pestaña (si la API está en localhost) y ver la imagen o descargar el archivo.

### 7.2 Con curl (guardar archivo)

```bash
curl -s "$BASE/api/projects/$PROJECT_ID/media/$MEDIA_ID/file" \
  -H "Authorization: Bearer $TOKEN" \
  -o descarga.png
```

Con `?download=true` para indicar descarga explícita:

```bash
curl -s "$BASE/api/projects/$PROJECT_ID/media/$MEDIA_ID/file?download=true" \
  -H "Authorization: Bearer $TOKEN" \
  -o descarga.png
```

---

## 8. Resumen de endpoints (Media)

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `api/projects/{projectId}/media` | Subir archivo (form-data: file, createdBy, displayName opcional) |
| GET | `api/projects/{projectId}/media` | Listar media del proyecto (paginado: page, pageSize) |
| GET | `api/projects/{projectId}/media/{mediaId}` | Metadatos de un media |
| GET | `api/projects/{projectId}/media/{mediaId}/file` | Stream del archivo (preview) |
| GET | `api/projects/{projectId}/media/{mediaId}/file?download=true` | Stream con descarga forzada |

Todos requieren **JWT** (Authorization: Bearer &lt;token&gt;).

---

## 9. Uso desde el CMS frontend (MediaPicker / selector)

- **Listado para selector:** `GET api/projects/{projectId}/media?page=1&pageSize=20` → mostrar items en una galería o lista.
- **Preview:** `GET api/projects/{projectId}/media/{mediaId}/file` como `src` de una imagen (si el frontend envía el JWT en la petición o usa un proxy que lo añade).
- **Al guardar contenido:** guardar en el campo de tipo “media” o “reference” el **id** (GUID) o el **publicId** del media elegido, según el contrato del schema.

La **versión** actual es 1 por archivo; el **versionado** de media (subir una nueva versión del mismo ítem) se puede añadir en una iteración posterior.

---

## 10. Errores habituales

| Código | Causa |
|--------|--------|
| 400 | No se envió `file` o `createdBy`, o archivo vacío. |
| 401 | Falta JWT o token inválido. |
| 404 | `projectId` o `mediaId` no existe, o el media no pertenece al proyecto. |
| 413 | Archivo demasiado grande (límite por defecto 50 MB en el controlador). |

---

**Última actualización:** 2026-01-24  
**Relacionado:** BACKEND_STEPS.md (sección 2.2), COMO_PROBAR_BACKEND_FASE_1.md (JWT y prerrequisitos), COMO_PROBAR_SITES_FASE_4.md.
