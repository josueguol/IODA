# Fase 4 – Publishing Service

## Estado

✅ **Publishing Service implementado** (solicitudes de publicación, validación de contenido, aprobación y llamada al Core API para publicar).

Guía detallada de ejecución y pruebas: **COMO_PROBAR_FASE_4.md**.

---

## Estructura

- **IODA.Publishing.Domain** – PublicationRequest (Pending, Approved, Rejected), IPublicationRequestRepository, PublicationRequestNotFoundException
- **IODA.Publishing.Application** – RequestPublicationCommand, ApprovePublicationCommand, RejectPublicationCommand; GetPublicationRequestsQuery; IContentValidator, ICorePublishClient; FluentValidation; MediatR
- **IODA.Publishing.Infrastructure** – PublishingDbContext (PostgreSQL), PublicationRequestRepository, CorePublishClient (HttpClient al Core API), ContentValidator (valida título, estado, campos)
- **IODA.Publishing.API** – PublishingController (requests, approve, reject, list); ErrorHandlingMiddleware; Swagger; Dockerfile; servicio en docker-compose (puerto 5004)

---

## Flujo

1. **Solicitar publicación**: `POST /api/publishing/requests` con ContentId, ProjectId, EnvironmentId, RequestedBy → se crea PublicationRequest (Pending).
2. **Aprobar**: `POST /api/publishing/requests/{requestId}/approve` con ApprovedBy → Publishing valida contenido (llama a Core API para obtener contenido; comprueba título, estado no Published, campos presentes). Si es válido, llama a Core API `POST /api/projects/{projectId}/content/{contentId}/publish` con PublishedBy. Core publica y emite ContentPublishedEventV1. Publishing marca la solicitud como Approved.
3. **Rechazar**: `POST /api/publishing/requests/{requestId}/reject` con RejectedBy y opcional Reason → marca la solicitud como Rejected.
4. **Listar solicitudes**: `GET /api/publishing/requests?contentId=...&status=...` → lista PublicationRequestDto.

---

## Configuración

- **ConnectionStrings:DefaultConnection** – PostgreSQL (`ioda_publishing`).
- **CoreApi:BaseUrl** – URL base del Core API (ej. `http://localhost:5001` en local; en Docker `http://ioda-core-api:8080`).

---

## Cómo ejecutar

### 1. Base de datos

```bash
createdb -U postgres ioda_publishing
export ConnectionStrings__DefaultConnection="Host=localhost;Database=ioda_publishing;Username=postgres;Password=postgres;Include Error Detail=true"
dotnet ef database update --project src/Services/Publishing/IODA.Publishing.Infrastructure/IODA.Publishing.Infrastructure.csproj --startup-project src/Services/Publishing/IODA.Publishing.API/IODA.Publishing.API.csproj
```

### 2. Arrancar la API

**Opción A – Desde la solución**
```bash
dotnet run --project src/Services/Publishing/IODA.Publishing.API/IODA.Publishing.API.csproj
```
- Swagger: **http://localhost:5272/swagger**

**Opción B – Con Docker**
```bash
docker compose --profile services up -d ioda-publishing-api
```
- Swagger: **http://localhost:5004/swagger**
- Asegúrate de que **Core API** esté accesible (CoreApi__BaseUrl en Docker: `http://ioda-core-api:8080`).

---

## Consumir eventos del CMS Core (opcional)

- **ContentCreatedEventV1**, **ContentUpdatedEventV1**: se pueden consumir con MassTransit para auditar o actualizar estado; por ahora no está implementado. Añadir consumer en Infrastructure cuando se requiera.

---

## Documentación relacionada

- **COMO_PROBAR_FASE_4.md** – Guía paso a paso (DB, migraciones, Core API, Docker, ejemplos).
- **NEXT_STEPS.md** – Estado del proyecto y próximos pasos.
