# Pruebas FE-PERIMPROV-2 — Fase 2: Leer permisos desde JWT (preparación opcional)

Rama: `feature/FE_PERIMPROV-2/read-JWT-permissions`

---

## Objetivo

Comprobar que el frontend lee los permisos del access token (JWT) y los usa para ocultar/mostrar UI cuando no hay contexto, reduciendo llamadas a `checkAccess`. Con contexto (p. ej. `projectId`, `environmentId`) se sigue usando `checkAccess`.

---

## 2.1 Utilidad de parsing del JWT

**Archivo:** `frontend/src/modules/authorization/utils/jwt-permissions.ts`

- **Claim esperado:** tipo `permission`, valor = código (ej. `content.edit`). Varios permisos en el payload como `"permission": ["code1", "code2"]` o múltiples claims del mismo tipo según serialización del backend.

### Pasos para probar

1. **Token sin claims de permiso:** Pasar un JWT válido que no tenga la clave `permission` en el payload. **Esperado:** `parsePermissionsFromAccessToken(token)` devuelve `[]`.
2. **Token con un permiso (string):** Payload con `"permission": "content.edit"`. **Esperado:** `["content.edit"]`.
3. **Token con varios permisos (array):** Payload con `"permission": ["content.edit", "role.manage"]`. **Esperado:** array con esos códigos.
4. **Token null o vacío:** **Esperado:** `[]`.
5. **Token malformado (no JWT):** **Esperado:** `[]` (sin lanzar).

**Sugerencia:** Tests unitarios con mocks de token (payload base64url del segmento central del JWT).

---

## 2.2 Hook useJwtPermissions

**Archivo:** `frontend/src/modules/authorization/hooks/useJwtPermissions.ts`

- Devuelve la lista de códigos de permiso extraídos del `accessToken` actual del store.

### Pasos para probar

1. **Usuario no autenticado:** `accessToken` null. **Esperado:** `useJwtPermissions()` devuelve `[]`.
2. **Usuario autenticado con JWT sin permisos:** Token sin claim `permission`. **Esperado:** `[]`.
3. **Usuario autenticado con JWT con permisos:** Tras login con backend que emite permisos en el JWT (Fase 2 BE), **esperado:** array con los códigos del token (ej. `["content.edit", "content.publish"]`).
4. **Tras refresh:** Tras renovar el token, el hook debe devolver los permisos del **nuevo** token (mismo valor que devuelve el backend en el nuevo access token).

---

## 2.3 usePermission: sin contexto usa JWT primero

**Comportamiento:** Cuando se llama `usePermission(permissionCode)` **sin** `context` (o con contexto vacío):

- Si el permiso está en la lista obtenida del JWT, se devuelve `{ allowed: true, loading: false }` **sin llamar** a `POST /api/authorization/check`.
- Si no está en el JWT, se hace la llamada a `checkAccess` como antes (compatibilidad con backends que aún no emiten permisos en el JWT).

Cuando se llama `usePermission(permissionCode, context)` **con** contexto (p. ej. `projectId`, `environmentId`):

- Siempre se llama a `checkAccess` (el backend resuelve el ámbito).

### Pasos para probar

1. **Backend con JWT con permisos (Identity + Authorization Fase 2):**
   - Login con usuario que tenga permiso `content.publish` en el JWT.
   - En una pantalla que use `<Can permission="content.publish">` o `usePermission("content.publish")` **sin** contexto.
   - **Esperado:** El contenido protegido se muestra y en la pestaña Network **no** aparece una petición a `POST .../api/authorization/check` para ese permiso (o al menos se reduce: primera vez podría ir por caché).
2. **Mismo permiso con contexto:** `usePermission("content.publish", { projectId: "..." })`. **Esperado:** Sí aparece petición a `checkAccess` (con `projectId` en el body).
3. **Permiso no en el JWT:** Usuario cuyo token no incluye `role.manage`. **Esperado:** Se llama a `checkAccess`; si el backend devuelve `allowed: false`, la UI no muestra la acción.
4. **Usuario sin permisos en el JWT (backend antiguo):** Token sin claims `permission`. **Esperado:** Comportamiento igual que antes: se usa `checkAccess` para decidir; la UI no se rompe.

---

## 2.4 Componentes Can y ProtectedRouteByPermission

No cambian de API; siguen usando `usePermission`. Comprobar que:

- Las pantallas que usan `<Can permission="...">` o `ProtectedRouteByPermission` sin contexto se comportan igual que antes cuando el JWT tiene permisos (y dejan de hacer una llamada a check por permiso cuando el permiso está en el JWT).
- Con contexto, la ruta o el contenido condicional siguen dependiendo de la respuesta de `checkAccess`.

---

## 2.5 Resumen de archivos tocados

| Archivo | Cambio |
|--------|--------|
| `authorization/utils/jwt-permissions.ts` | Nuevo: `parsePermissionsFromAccessToken`, `JWT_PERMISSION_CLAIM_TYPE` |
| `authorization/hooks/useJwtPermissions.ts` | Nuevo: hook que devuelve permisos del JWT |
| `authorization/hooks/usePermission.ts` | Sin contexto: usa JWT primero; con contexto: checkAccess |
| `authorization/index.ts` | Export de `useJwtPermissions` |

---

## Dependencia con backend

- Para aprovechar la reducción de llamadas a `checkAccess`, el backend debe estar en **Fase 2** (Identity emitiendo claims `permission` en el JWT). Ver [PRUEBAS_BE-PERIMPROV-2.md](./PRUEBAS_BE-PERIMPROV-2.md).
- Si el backend aún no emite permisos en el JWT, el frontend sigue funcionando: en ese caso `usePermission` sin contexto acabará usando `checkAccess` como antes.
