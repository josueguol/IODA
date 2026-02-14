# Análisis: Autorización y usuario actual en las APIs

**Fecha:** 2025-02-13  
**Alcance:** Backend (Core, Identity, Authorization, Publishing, Indexing).  
**Objetivo:** Diagnosticar cómo está implementada la autorización y el uso del “usuario actual” en los comandos.

---

## 1. Resumen

| Pregunta | Respuesta |
|----------|-----------|
| ¿Autorización implementada como...? | **Attributes + Policy** (ASP.NET Core). No middleware global de autorización; no validación manual en handlers. |
| ¿Claims-based? | **Sí.** JWT con `sub` (userId), `email`, `jti` y `ClaimTypes.Role` (Admin, Editor, etc.). |
| ¿Quién fija CreatedBy / UpdatedBy / PublishedBy? | **El cliente**, vía body. Ningún API obtiene el usuario actual del JWT para estos campos. |
| ¿Autorización a nivel de recurso (RBAC contextual)? | **No.** Core e Indexing solo exigen `[Authorize]` (usuario autenticado). No se consulta Authorization API para “¿puede este usuario editar este proyecto?”. |

---

## 2. Modelo de autorización actual

### 2.1 Por servicio

- **Identity:** Emite JWT (`sub`, `email`, roles). Endpoints públicos (login, register, setup) + algún endpoint con `[Authorize]`.
- **Authorization:** `[Authorize]` en el controller; políticas `Admin` para CRUD de roles/permisos/reglas. Endpoint `POST /check` recibe `UserId` en el body (cualquier usuario autenticado puede preguntar por cualquier otro usuario).
- **Publishing:** `[Authorize(Policy = "Editor")]` (Editor o Admin). Coherente con el dominio.
- **Core:** `[Authorize]` sin políticas. Cualquier token válido puede crear/actualizar/publicar contenido en cualquier proyecto.
- **Indexing:** `[Authorize]` sin políticas. Cualquier token válido puede indexar y buscar.

### 2.2 Políticas definidas

- **Authorization API:** `options.AddPolicy("Admin", policy => policy.RequireRole("Admin"))`.
- **Publishing API:** `options.AddPolicy("Editor", policy => policy.RequireRole("Editor", "Admin"))`.
- **Core e Indexing:** `AddAuthorization()` sin políticas; solo autenticación.

### 2.3 Usuario actual en comandos

En Core (y flujos que llaman a Core):

- `CreateContentRequest.CreatedBy`, `UpdateContentRequest.UpdatedBy`, `PublishContentRequest.PublishedBy`, `UnpublishContentRequest.UnpublishedBy` son **Guid** que vienen del **body**.
- El controller no lee `User` (JWT) ni reemplaza estos valores.
- Los handlers confían en lo que reciben; no hay comprobación “solo el propio usuario puede actuar como sí mismo”.

El JWT ya incluye `sub` (userId). Ese valor no se usa para rellenar ni validar los campos de auditoría.

---

## 3. Conclusiones del análisis

1. **Autorización:** Implementación por **attributes y policy**, claims-based (roles en JWT). Correcto a nivel de “quién puede entrar al endpoint”.
2. **Inconsistencia de políticas:** Core e Indexing no distinguen roles; cualquier autenticado tiene el mismo acceso que un Editor/Admin en esos servicios.
3. **Usuario actual:** No hay “current user from JWT” en los comandos. Los IDs de auditoría son **client-supplied**, lo que invalida la confianza en trazas de “quién hizo qué” y permite suplantación en auditoría.
4. **Autorización a nivel de recurso:** No existe. No se usa el servicio Authorization (p. ej. `CheckAccess`) antes de operar sobre un proyecto/contenido concreto.

Este documento se complementa con la recomendación en `docs/recommendations/uso-usuario-actual-jwt.md`.
