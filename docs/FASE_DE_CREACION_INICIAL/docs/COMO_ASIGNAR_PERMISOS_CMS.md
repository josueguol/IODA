# Cómo asignar permisos al usuario en el CMS (content.edit, content.publish)

El **frontend del CMS no tiene pantalla para gestionar roles ni permisos**. Esa configuración se hace en la **Authorization API** (vía Swagger o llamadas HTTP). El CMS solo **consulta** si el usuario tiene un permiso (por ejemplo `content.publish`) y muestra u oculta botones y rutas.

---

## Permisos que usa el CMS

| Permiso          | Uso en el CMS |
|------------------|----------------|
| `content.edit`   | Botón de ejemplo en la Home; puedes usarlo para editar contenido. |
| `content.publish`| Ruta **Publicar** (`/publish`), botón **Solicitar publicación** en la edición de contenido. |

Si no tienes `content.publish` asignado, no verás el enlace "Publicar" en la barra ni el botón "Solicitar publicación" al editar un contenido en Draft.

---

## Pasos para asignar content.publish (y content.edit)

Necesitas:

1. **Authorization API** levantada (puerto 5271 en local).
2. **UserId** de tu usuario (GUID que devuelve Identity al hacer login o al registrar).

### Opción A: Usar Swagger (recomendado)

1. Abre **http://localhost:5271/swagger** (o el puerto que uses).

2. **Crear el permiso `content.publish`** (y si quieres, `content.edit`):
   - **POST /api/authorization/permissions**
   - Body ejemplo para publicar:
     ```json
     { "code": "content.publish", "description": "Solicitar y aprobar publicación" }
     ```
   - Body ejemplo para editar:
     ```json
     { "code": "content.edit", "description": "Editar contenido" }
     ```
   - Anota el **GUID** que devuelve cada llamada (por ejemplo `PERMISSION_PUBLISH_ID`, `PERMISSION_EDIT_ID`).

3. **Listar permisos** (por si ya existen):
   - **GET /api/authorization/permissions**
   - Si `content.publish` o `content.edit` ya están creados, anota sus `id`.

4. **Crear un rol** (o usar uno existente):
   - **POST /api/authorization/roles**
   - Body: `{ "name": "Publisher", "description": "Puede publicar contenido" }`
   - Anota el **roleId**.
   - Para dar también edición, puedes crear un rol "Editor" o añadir ambos permisos al mismo rol.

5. **Asignar permisos al rol**:
   - **POST /api/authorization/roles/{roleId}/permissions**
   - Sustituye `{roleId}` por el GUID del rol.
   - Body (array de GUIDs de permisos):
     ```json
     { "permissionIds": [ "GUID-de-content.publish", "GUID-de-content.edit" ] }
     ```
   - Usa los `id` que obtuviste en el paso 2 (o de GET permissions).

6. **Asignar el rol a tu usuario**:
   - Necesitas tu **UserId** (GUID). Puedes verlo en la respuesta de login de Identity, o en el frontend (por ejemplo en la barra del CMS suele mostrarse el usuario; el `userId` está en el store de auth).
   - **POST /api/authorization/rules**
   - Body:
     ```json
     { "userId": "TU-USER-ID-AQUI", "roleId": "GUID-DEL-ROL" }
     ```

7. En el **frontend**, cierra sesión y vuelve a entrar (o espera un minuto por la caché de permisos). Deberías ver el enlace "Publicar" y el botón "Solicitar publicación" si asignaste `content.publish`.

### Opción B: Script con curl

Sustituye `USER_ID` por tu UserId real (GUID de Identity).

```bash
BASE=http://localhost:5271
USER_ID=00000000-0000-0000-0000-000000000000   # ← Sustituir por tu UserId

# 1. Crear permisos (si no existen; si ya existen, usa GET /permissions y anota los id)
curl -s -X POST "$BASE/api/authorization/permissions" \
  -H "Content-Type: application/json" \
  -d '{"code":"content.publish","description":"Solicitar y aprobar publicación"}'
# Anotar GUID → PERM_PUBLISH_ID

curl -s -X POST "$BASE/api/authorization/permissions" \
  -H "Content-Type: application/json" \
  -d '{"code":"content.edit","description":"Editar contenido"}'
# Anotar GUID → PERM_EDIT_ID

# 2. Crear rol Publisher
curl -s -X POST "$BASE/api/authorization/roles" \
  -H "Content-Type: application/json" \
  -d '{"name":"Publisher","description":"Puede publicar contenido"}'
# Anotar GUID → ROLE_ID

# 3. Asignar permisos al rol (sustituir ROLE_ID, PERM_PUBLISH_ID, PERM_EDIT_ID)
curl -s -X POST "$BASE/api/authorization/roles/ROLE_ID/permissions" \
  -H "Content-Type: application/json" \
  -d '{"permissionIds":["PERM_PUBLISH_ID","PERM_EDIT_ID"]}'

# 4. Asignar rol al usuario
curl -s -X POST "$BASE/api/authorization/rules" \
  -H "Content-Type: application/json" \
  -d "{\"userId\":\"$USER_ID\",\"roleId\":\"ROLE_ID\"}"

# 5. Comprobar
curl -s -X POST "$BASE/api/authorization/check" \
  -H "Content-Type: application/json" \
  -d "{\"userId\":\"$USER_ID\",\"permissionCode\":\"content.publish\"}"
# Debe devolver: {"allowed":true}
```

---

## Cómo obtener tu UserId

- **Identity API**: al hacer **login** (POST /api/auth/login), la respuesta incluye el usuario; en muchos contratos viene un campo `userId` (GUID).
- **Frontend**: una vez logueado, en las herramientas de desarrollo (DevTools) puedes inspeccionar el estado de la app (por ejemplo el store de Zustand) o añadir temporalmente un `console.log(useAuthStore.getState().user)` para ver `userId`.

---

## Errores frecuentes

- **409 Conflict** al crear permiso/rol: el código o nombre ya existe. Usa **GET /api/authorization/permissions** y **GET /api/authorization/roles** para obtener los `id` y asígnelos al rol y a la regla.
- **404** en assign permissions o rules: el `roleId` o `permissionId` no existe; comprueba que los GUIDs son los correctos.
- **El botón no aparece** en el CMS: el frontend cachea el resultado de “check” unos 60 segundos. Cierra sesión y vuelve a entrar, o espera un minuto y recarga.

---

## Resumen

Los permisos **no se asignan desde el CMS**, sino desde la **Authorization API** (Swagger o API). Pasos mínimos:

1. Crear permiso `content.publish` (y opcionalmente `content.edit`).
2. Crear un rol y asignarle esos permisos.
3. Crear una regla (POST /api/authorization/rules) con tu **UserId** y el **roleId**.

Después de eso, el CMS mostrará las opciones que dependen de `content.publish` (y de `content.edit` si las usas).
