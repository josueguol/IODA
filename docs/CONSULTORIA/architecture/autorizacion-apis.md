# Arquitectura: Autorización en las APIs

**Estado actual (rama fix/BE-DTECH-1/add-auth-on-apis):** Las APIs exigen JWT en la mayoría de endpoints; el modelo es **claims-based** con **attributes y políticas**.

---

## Cómo está implementada la autorización

- **Mecanismo:** `[Authorize]` y `[Authorize(Policy = "Nombre")]` en controladores de ASP.NET Core. No hay middleware propio de autorización ni comprobaciones manuales dentro de los handlers.
- **Autenticación:** JWT Bearer (`AddJwtBearer`). Parámetros de validación (issuer, audience, signing key) configurados por servicio vía `appsettings` / entorno.
- **Claims relevantes:** `sub` (userId), `email`, `jti`, y `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` (roles: Admin, Editor, etc.).

## Políticas por servicio

| Servicio       | Políticas definidas | Uso en controladores |
|----------------|---------------------|------------------------|
| Identity      | (ninguna en código revisado) | Algunos endpoints `[Authorize]`. |
| Authorization | `Admin` (RequireRole("Admin")) | Controller `[Authorize]`; acciones de CRUD con `[Authorize(Policy = "Admin")]`. |
| Publishing    | `Editor` (RequireRole("Editor", "Admin")) | Controller `[Authorize(Policy = "Editor")]`. |
| Core          | Ninguna            | Solo `[Authorize]` (cualquier autenticado). |
| Indexing      | Ninguna            | Solo `[Authorize]` (cualquier autenticado). |

## Limitaciones conocidas

1. **Core e Indexing:** No distinguen por rol; cualquier usuario con token válido tiene el mismo acceso.
2. **Usuario actual:** Los comandos que llevan “quién actuó” (CreatedBy, UpdatedBy, etc.) reciben ese valor del **body**, no del JWT. Ver `docs/analysis/autorizacion-y-usuario-actual.md` y `docs/recommendations/uso-usuario-actual-jwt.md`.
3. **Autorización a nivel de recurso:** No se consulta al servicio Authorization (p. ej. `CheckAccess`) antes de operar sobre un proyecto/contenido/entorno concreto; solo se comprueba “autenticado” o “rol Editor/Admin”.

## Referencias

- Análisis detallado: `docs/analysis/autorizacion-y-usuario-actual.md`
- Recomendación usuario actual: `docs/recommendations/uso-usuario-actual-jwt.md`
- Diagnóstico general: `docs/DIAGNOSTICO_AUDITORIA_TECNICA.md`
