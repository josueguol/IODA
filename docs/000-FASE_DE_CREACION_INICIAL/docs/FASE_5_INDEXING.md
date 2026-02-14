# Fase 5 – Indexing Service

## Estado

✅ **Indexing Service implementado** (Elasticsearch para búsqueda de contenido publicado, consumidores MassTransit para ContentPublished/ContentUnpublished).

Guía detallada de ejecución y pruebas: **COMO_PROBAR_FASE_5.md**.

---

## Estructura

- **IODA.Indexing.Domain** – IndexedContentDocument (ValueObject)
- **IODA.Indexing.Application** – IContentIndexer (IndexAsync, RemoveAsync, SearchAsync); IndexContentCommand, RemoveFromIndexCommand; SearchContentQuery; FluentValidation; MediatR
- **IODA.Indexing.Infrastructure** – ElasticsearchContentIndexer (Elastic.Clients.Elasticsearch 8.x), NoOpContentIndexer; ContentPublishedEventV1Consumer, ContentUnpublishedEventV1Consumer (MassTransit); Elasticsearch/RabbitMQ opcionales
- **IODA.Indexing.API** – IndexingController (search, index, remove); ErrorHandlingMiddleware; Swagger; Dockerfile; servicio en docker-compose (puerto 5005)

---

## Flujo

1. **Indexación automática**: Core API emite `ContentPublishedEventV1` al publicar contenido. Indexing consume el evento (MassTransit) e indexa el documento en Elasticsearch (ContentId, VersionId, Title, ContentType, PublishedAt, Fields opcionales).
2. **Desindexación automática**: Si se emite `ContentUnpublishedEventV1`, Indexing consume y elimina el documento del índice.
3. **Búsqueda**: `GET /api/indexing/search?q=...&page=1&pageSize=20&contentType=...` → devuelve SearchResultDto (Total, Items).
4. **Indexar manual**: `POST /api/indexing/index` con body (ContentId, VersionId, Title, ContentType, PublishedAt, Fields opcional) → indexa sin depender del evento.
5. **Eliminar del índice manual**: `DELETE /api/indexing/index/{contentId}` → elimina del índice.

---

## Configuración

- **Elasticsearch:Enabled** – `true` para usar Elasticsearch; `false` usa NoOpContentIndexer (búsqueda vacía, index/remove no hacen nada).
- **Elasticsearch:Url** – URL del nodo Elasticsearch (ej. `http://localhost:9200`).
- **Elasticsearch:IndexName** – Nombre del índice (por defecto `ioda-published-content`).
- **RabbitMQ:Enabled** – `true` para suscribirse a ContentPublished/ContentUnpublished; `false` no registra consumidores.
- **RabbitMQ:Host**, **VirtualHost**, **Username**, **Password** – Conexión a RabbitMQ.

No se usa base de datos PostgreSQL en Indexing.

---

## Cómo ejecutar

### 1. Elasticsearch y RabbitMQ (opcional)

- **Elasticsearch**: puerto 9200 (local o Docker).
- **RabbitMQ**: para recibir eventos de publicación; mismo virtual host que Core/Publishing (`ioda_cms`).

### 2. Arrancar la API

**Opción A – Desde la solución**
```bash
dotnet run --project src/Services/Indexing/IODA.Indexing.API/IODA.Indexing.API.csproj
```
- Swagger: **http://localhost:5273/swagger**

**Opción B – Con Docker**
```bash
docker compose --profile services up -d ioda-indexing-api
```
- Swagger: **http://localhost:5005/swagger**
- En Docker, Elasticsearch y RabbitMQ deben estar en la misma red (Elasticsearch__Url, RabbitMQ__Host).

---

## Documentación relacionada

- **COMO_PROBAR_FASE_5.md** – Pasos para probar Indexing API (Elasticsearch, RabbitMQ, búsqueda, indexación manual).
- **EVENTS.md** – Contratos ContentPublishedEventV1, ContentUnpublishedEventV1.
- **NEXT_STEPS.md** – Estado global del proyecto y próximos pasos.
