# Cómo probar lo ya creado (Fase 5 – Indexing)

Guía para ejecutar y probar el **Indexing API** (búsqueda en Elasticsearch, indexación automática vía eventos y manual).

---

## 1. Prerrequisitos

- **.NET 9 SDK**
- **Elasticsearch** (puerto 9200) – opcional: si `Elasticsearch:Enabled=false`, se usa NoOp (búsqueda vacía).
- **RabbitMQ** (opcional) – para consumir ContentPublishedEventV1 y ContentUnpublishedEventV1. Si `RabbitMQ:Enabled=false`, no se registran consumidores.
- **Core API** y **Publishing API** – si quieres probar el flujo completo: publicar contenido en Core vía Publishing y que Indexing indexe al recibir el evento.

---

## 2. Configuración

Revisa **`src/Services/Indexing/IODA.Indexing.API/appsettings.json`** y **appsettings.Development.json**:

- **Elasticsearch:Enabled** – `true` para usar Elasticsearch; `false` para NoOp.
- **Elasticsearch:Url** – `http://localhost:9200` (local) o `http://elasticsearch:9200` (Docker).
- **Elasticsearch:IndexName** – `ioda-published-content`.
- **RabbitMQ:Enabled** – `true` para consumir eventos; `false` para no suscribirse.
- **RabbitMQ:Host**, **VirtualHost**, **Username**, **Password** – conexión a RabbitMQ.

---

## 3. Ejecutar la API

### Opción A: Sin Elasticsearch ni RabbitMQ (NoOp)

En **appsettings.Development.json** pon:
```json
"Elasticsearch": { "Enabled": "false" },
"RabbitMQ": { "Enabled": "false" }
```

```bash
cd /Users/josuegolivares/desarrollo/csharp/ioda
dotnet run --project src/Services/Indexing/IODA.Indexing.API/IODA.Indexing.API.csproj
```

- Swagger: **http://localhost:5273/swagger**
- **GET /api/indexing/search** devolverá `{ "total": 0, "items": [] }`.
- **POST /api/indexing/index** y **DELETE /api/indexing/index/{contentId}** no fallan pero no persisten nada.

### Opción B: Con Elasticsearch (sin RabbitMQ)

1. Levanta Elasticsearch (por ejemplo con Docker: `docker run -d -p 9200:9200 -e "discovery.type=single-node" docker.elastic.co/elasticsearch/elasticsearch:8.15.0` o tu versión).
2. En appsettings.Development.json: `Elasticsearch:Enabled=true`, `RabbitMQ:Enabled=false`.
3. Arranca la API como arriba.
4. **POST /api/indexing/index** con un body de ejemplo:
   ```json
   {
     "contentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
     "versionId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
     "title": "Mi artículo",
     "contentType": "Article",
     "publishedAt": "2026-01-24T12:00:00Z",
     "fields": null
   }
   ```
5. **GET /api/indexing/search?q=artículo** debería devolver ese documento.
6. **DELETE /api/indexing/index/3fa85f64-5717-4562-b3fc-2c963f66afa6** lo quita del índice.

### Opción C: Con Elasticsearch y RabbitMQ (flujo completo)

1. Levanta **Elasticsearch** y **RabbitMQ** (mismo virtual host que Core/Publishing, ej. `ioda_cms`).
2. En appsettings.Development.json: `Elasticsearch:Enabled=true`, `RabbitMQ:Enabled=true`, y Host/User/Password de RabbitMQ.
3. Arranca **Core API**, **Publishing API** e **Indexing API**.
4. Crea contenido en Core, solicita publicación en Publishing y aprueba. Core emite **ContentPublishedEventV1**. Indexing lo consume e indexa en Elasticsearch.
5. **GET /api/indexing/search** listará el contenido publicado indexado.

---

## 4. Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | /api/indexing/search | Búsqueda: query params `q`, `page`, `pageSize`, `contentType` |
| POST | /api/indexing/index | Indexar manualmente (body: ContentId, VersionId, Title, ContentType, PublishedAt, Fields) |
| DELETE | /api/indexing/index/{contentId} | Eliminar del índice |

---

## 5. Docker

```bash
docker compose --profile services up -d ioda-indexing-api
```

- Swagger: **http://localhost:5005/swagger**
- Variables de entorno en docker-compose: Elasticsearch__Url, RabbitMQ__Host, etc. Asegúrate de que los servicios Elasticsearch y RabbitMQ estén en la misma red (`local-dev-network` / `ioda-internal`) si los usas.

---

## 6. Solución de problemas

- **"Elasticsearch index failed"**: comprueba que Elasticsearch esté levantado y que Elasticsearch:Url sea correcto. Para ES 8.x puede ser necesario configurar SSL/certificados si usas HTTPS.
- **No se indexa al publicar**: verifica que RabbitMQ esté activo, que Core API y Publishing estén publicando en el mismo exchange/queue que Indexing consume, y que `RabbitMQ:Enabled=true` en Indexing.
- **Búsqueda siempre vacía**: con NoOp es normal. Con Elasticsearch, confirma que el índice existe y que has indexado al menos un documento (manual o por evento).
