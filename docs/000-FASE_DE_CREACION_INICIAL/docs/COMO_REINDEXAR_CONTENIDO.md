# Cómo reindexar contenido

La Indexing API permite reindexar contenido publicado manualmente. Esto es útil cuando:
- El contenido no aparece en los resultados de búsqueda
- Necesitas actualizar el índice después de cambios en Elasticsearch
- El evento automático de indexación falló

## Opción 1: Usar el frontend (recomendado)

1. Ve a la página de edición del contenido publicado (`/content/{contentId}/edit`)
2. En la sección **"Indexación"** (amarilla), haz clic en **"Reindexar contenido"**
3. El sistema obtendrá automáticamente la versión publicada y la reindexará

## Opción 2: Usar la API directamente

### Paso 1: Obtener el contenido publicado

Obtén el contenido desde la Core API para conocer sus datos:

```bash
GET /api/projects/{projectId}/content/{contentId}
```

Respuesta ejemplo:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Mi artículo",
  "contentType": "article",
  "currentVersion": 2,
  "publishedAt": "2026-01-24T10:00:00Z",
  "fields": { ... }
}
```

### Paso 2: Obtener la versión publicada

Obtén la versión específica usando `currentVersion`:

```bash
GET /api/projects/{projectId}/content/{contentId}/versions/{currentVersion}
```

Respuesta ejemplo:
```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",  // Este es el VersionId
  "contentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "versionNumber": 2,
  "title": "Mi artículo",
  "fields": { ... },
  "status": "Published",
  ...
}
```

### Paso 3: Reindexar usando Indexing API

Llama al endpoint de reindexación con los datos obtenidos:

```bash
POST /api/indexing/index
Content-Type: application/json
Authorization: Bearer {tu-jwt-token}

{
  "contentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "versionId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "title": "Mi artículo",
  "contentType": "article",
  "publishedAt": "2026-01-24T10:00:00Z",
  "fields": { ... }  // Opcional: campos del contenido
}
```

### Ejemplo completo con curl

```bash
# 1. Obtener contenido
CONTENT_ID="3fa85f64-5717-4562-b3fc-2c963f66afa6"
PROJECT_ID="..."
JWT_TOKEN="..."

CONTENT=$(curl -s -H "Authorization: Bearer $JWT_TOKEN" \
  "http://localhost:5269/api/projects/$PROJECT_ID/content/$CONTENT_ID")

# Extraer currentVersion (requiere jq)
CURRENT_VERSION=$(echo $CONTENT | jq -r '.currentVersion')

# 2. Obtener versión
VERSION=$(curl -s -H "Authorization: Bearer $JWT_TOKEN" \
  "http://localhost:5269/api/projects/$PROJECT_ID/content/$CONTENT_ID/versions/$CURRENT_VERSION")

VERSION_ID=$(echo $VERSION | jq -r '.id')
TITLE=$(echo $CONTENT | jq -r '.title')
CONTENT_TYPE=$(echo $CONTENT | jq -r '.contentType')
PUBLISHED_AT=$(echo $CONTENT | jq -r '.publishedAt')
FIELDS=$(echo $CONTENT | jq -c '.fields')

# 3. Reindexar
curl -X POST \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"contentId\": \"$CONTENT_ID\",
    \"versionId\": \"$VERSION_ID\",
    \"title\": \"$TITLE\",
    \"contentType\": \"$CONTENT_TYPE\",
    \"publishedAt\": \"$PUBLISHED_AT\",
    \"fields\": $FIELDS
  }" \
  "http://localhost:5273/api/indexing/index"
```

## Opción 3: Usar Swagger UI

1. Abre Swagger de la **Indexing API** (`http://localhost:5273/swagger`)
2. Autentícate con tu JWT token (botón "Authorize")
3. Expande `POST /api/indexing/index`
4. Haz clic en "Try it out"
5. Completa el body con los datos del contenido (obtenidos de Core API)
6. Ejecuta la petición

## Notas importantes

- **Solo contenido publicado**: El endpoint de reindexación está diseñado para contenido con `status = "Published"` y `publishedAt != null`
- **VersionId requerido**: Necesitas obtener el `VersionId` de la versión publicada (no solo `currentVersion`)
- **Autenticación**: Todas las llamadas requieren un JWT token válido
- **Indexación automática**: Normalmente, el contenido se indexa automáticamente cuando se publica vía el evento `ContentPublishedEventV1`. La reindexación manual solo es necesaria en casos excepcionales.

## Solución de problemas

### Error 400 Bad Request
- Verifica que el contenido esté publicado (`status = "Published"`)
- Asegúrate de que `publishedAt` no sea `null`
- Verifica que `versionId` corresponda a una versión válida del contenido

### Error 401 Unauthorized
- Verifica que tu JWT token sea válido y no haya expirado
- Asegúrate de incluir el header `Authorization: Bearer {token}`

### El contenido no aparece en búsqueda después de reindexar
- Verifica que Elasticsearch esté corriendo y accesible
- Revisa los logs de la Indexing API para ver si hubo errores
- Verifica que el índice de Elasticsearch exista (`ioda-published-content` por defecto)
