# Tareas Frontend — Bugfixs primer usuario y 403 en roles

Responsable: **Equipo Frontend**.  
Referencia: [README.md](./README.md).

---

## Diagnóstico

- Tras registrar al primer usuario, el frontend hace login y ejecuta `setupSuperAdmin(userId)`: crea permisos (si no existen), rol SuperAdmin, asigna permisos al rol y crea la regla de acceso usuario–SuperAdmin.
- El **JWT que se obtiene en ese primer login no incluye permisos** (porque o bien el bootstrap en backend no se ejecutó, o bien Identity no está configurado para pedir permisos a Authorization). Ese mismo JWT se sigue usando en las peticiones siguientes.
- Al ir a "Roles" y pulsar "Crear rol", se envía POST /api/authorization/roles con ese JWT. El backend exige el claim `permission` con valor `role.manage` → como el token no lo tiene, devuelve **403** → el cliente puede interpretar 403 como "no autorizado" y desloguear al usuario (redirigir a login).
- Aunque el backend permita el modo bootstrap (cuando no hay reglas), el primer usuario podrá crear el rol y asignarse SuperAdmin desde la UI, pero el **JWT que tiene en memoria seguirá sin permisos** hasta que se renueve (refresh o nuevo login).

---

## Tareas Frontend

### 1. Refresco de token tras setup del primer usuario (prioritario)

- [ ] **1.1** Tras completar con éxito `setupSuperAdmin` en `RegisterPage` (después de crear rol, asignar permisos y crear la regla de acceso para el primer usuario), forzar la obtención de un **nuevo access token** que incluya los permisos:
  - Opción A: Llamar al endpoint de **refresh token** con el refresh token actual y reemplazar en el store el access token (y expiry) por los devueltos.
  - Opción B: Hacer **login de nuevo** con email/password (ya disponibles en ese flujo) y guardar el nuevo resultado en el store.
- [ ] **1.2** Así las siguientes peticiones (lista de proyectos, crear rol, etc.) usarán un JWT que ya incluye los permisos efectivos (si Identity está configurado con AuthorizationApi:BaseUrl y el usuario tiene la regla SuperAdmin).
- [ ] **1.3** Invalidar la caché de permisos del módulo de autorización tras actualizar el token (p. ej. `invalidatePermissionCache()`), para que los hooks que usen permisos no sigan con datos obsoletos.
- **Archivos típicos:** `RegisterPage.tsx` (flujo post-registro primer usuario); módulo de auth (refresh o login); store de auth.
- **Riesgo:** Bajo. Solo afecta al flujo del primer usuario tras el setup.

### 2. Manejo de 403 en llamadas a Authorization API

- [ ] **2.1** Revisar el comportamiento actual cuando POST /api/authorization/roles (o createRole) devuelve **403**: si se está haciendo logout o redirección a login de forma automática (p. ej. en un interceptor o en el cliente HTTP al recibir 403), valorar no desloguear cuando la respuesta sea 403 en endpoints concretos de "gestión" (crear rol, crear permiso, etc.) y en su lugar mostrar un mensaje al usuario ("No tienes permiso para realizar esta acción" o "Tu sesión no tiene permisos actualizados; intenta cerrar sesión y volver a entrar").
- [ ] **2.2** Así se evita que el usuario sea deslogueado al pulsar "Crear rol" cuando su JWT aún no tiene el permiso; si el backend ya tiene modo bootstrap, la petición podrá seguir y, tras refrescar el token (tarea 1), la siguiente vez el JWT ya tendrá permisos.
- **Archivos típicos:** Cliente HTTP que llama a Authorization API (p. ej. `authorization-api.ts` o el que centralice respuestas 401/403); posiblemente store de auth.
- **Riesgo:** Bajo. Mejora la UX y evita deslogueos inesperados.

### 3. GET /projects y mensaje de error

- [ ] **3.1** Si GET /api/projects devuelve **400** o **403**, mostrar en la UI un mensaje claro según el código (p. ej. "No tienes permiso para ver proyectos" para 403, o "Parámetros de búsqueda no válidos" para 400), en lugar de un error genérico o deslogueo.
- [ ] **3.2** Tras aplicar la tarea 1 (refresco de token tras setup), comprobar que, con el nuevo token (con permisos), GET /projects devuelve 200 cuando el usuario tiene el permiso `project.edit` (o el que exija el backend).
- **Riesgo:** Bajo.

---

## Orden recomendado

1. Implementar **refresco de token tras setupSuperAdmin** (1.1–1.3).
2. Ajustar **manejo de 403** en llamadas a Authorization (2.1–2.2) para no desloguear y mostrar mensaje adecuado.
3. Revisar **mensajes de error** para GET /projects (3.1–3.2).
