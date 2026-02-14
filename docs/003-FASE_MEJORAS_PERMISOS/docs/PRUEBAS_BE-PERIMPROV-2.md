# Pruebas BE-PERIMPROV-2 — Fase 2: JWT con permisos y policies por permiso

Rama: `feature/BE_PERIMPROV-2/JWT-permissions-n-policies-x-permission`

---

## 2.1 Integración Identity → Authorization (permisos efectivos)

Identity obtiene los permisos efectivos del usuario desde el servicio Authorization en Login y Refresh. Authorization expone un endpoint para ello; Identity llama con un cliente HTTP (opcionalmente con API key de servicio).

### 2.1.1 Endpoint de permisos efectivos (Authorization API)

**GET** `/api/authorization/users/{userId}/effective-permissions`

- **Respuesta 200:** JSON array de strings (códigos de permiso), sin duplicados. Ejemplo: `["content.read","content.edit","role.manage"]`.
- **Autenticación:** JWT Bearer **o** header `X-Service-Api-Key` con el valor configurado en `Authorization:ServiceApiKey` (servicio Identity).
- **401:** Si no se envía JWT ni API key válida.

**Pasos para probar:**

1. Arrancar la Authorization API (puerto típico 5271).
2. Tener al menos un usuario con reglas de acceso y roles con permisos asignados (p. ej. crear rol, asignar permisos con `POST roles/{id}/permissions`, crear regla con `POST rules` para un `userId`).
3. **Con JWT:** Obtener un token de Identity para ese usuario (o cualquier usuario con rol Admin) y llamar:
   ```bash
   curl -s -X GET "http://localhost:5271/api/authorization/users/<USER_ID>/effective-permissions" \
     -H "Authorization: Bearer <JWT>"
   ```
   **Esperado:** 200 y array de códigos (p. ej. `["content.create","content.edit"]`). Si el usuario no tiene reglas/permisos, array vacío `[]`.
4. **Con API key (servicio a servicio):** Configurar en Authorization API `Authorization:ServiceApiKey` (p. ej. en User Secrets o appsettings) con un valor secreto. Llamar sin JWT:
   ```bash
   curl -s -X GET "http://localhost:5271/api/authorization/users/<USER_ID>/effective-permissions" \
     -H "X-Service-Api-Key: <SERVICE_API_KEY>"
   ```
   **Esperado:** 200 y mismo array de códigos.
5. **Sin credenciales:** Llamar sin `Authorization` ni `X-Service-Api-Key`. **Esperado:** 401 Unauthorized.

### 2.1.2 Cliente en Identity (Login / Refresh)

Identity debe tener configurado `AuthorizationApi:BaseUrl` (URL base del API de Authorization, p. ej. `http://localhost:5271`). Opcional: `AuthorizationApi:ServiceApiKey` para enviar en `X-Service-Api-Key`.

- Si **no** se configura `BaseUrl`, Identity usa `NoOpEffectivePermissionsClient` (devuelve lista vacía; Login/Refresh siguen funcionando).
- Si se configura `BaseUrl`, Identity usa el cliente HTTP y en Login y Refresh obtiene los permisos efectivos antes de generar el access token (en 2.2 esos códigos se incluirán en el JWT).

**Pasos para probar:**

1. Configurar en Identity (User Secrets o appsettings.Development):
   - `AuthorizationApi:BaseUrl` = `http://localhost:5271` (o la URL donde corre Authorization).
   - Opcional: `AuthorizationApi:ServiceApiKey` = mismo valor que `Authorization:ServiceApiKey` en Authorization.
2. Arrancar Identity y Authorization; asegurar que el usuario de prueba tiene al menos una regla de acceso con rol que tenga permisos.
3. **Login:** Hacer login con ese usuario. **Esperado:** 200 y access token (aún sin claims de permisos en 2.1; en 2.2 se validará que el JWT incluya los códigos).
4. **Refresh:** Usar el refresh token. **Esperado:** 200 y nuevo access token.
5. Si Authorization API no está disponible o devuelve error, el cliente debe degradar sin romper Login/Refresh (lista vacía y log de advertencia).

### 2.1.3 Resumen de configuración

| Servicio     | Clave                         | Descripción |
|-------------|--------------------------------|-------------|
| Authorization | `Authorization:ServiceApiKey` | Valor opcional; si está definido, el endpoint `effective-permissions` acepta header `X-Service-Api-Key` con este valor. |
| Identity    | `AuthorizationApi:BaseUrl`    | URL base del API de Authorization (ej. `http://localhost:5271`). Si vacío, no se llama al servicio. |
| Identity    | `AuthorizationApi:ServiceApiKey` | Opcional; se envía en `X-Service-Api-Key` al llamar a Authorization. |

---

## 2.2 (Próximo) Incluir permisos en JWT y policies por permiso

Cuando se implemente 2.2, añadir aquí pasos para comprobar que el access token incluye los códigos de permiso (claim) y que las policies por permiso en Authorization y otros servicios rechazan/autorizan correctamente.
