# Plan: Flujo de instalación nueva (primer usuario → SuperAdmin operativo)

Referencia: [README.md](./README.md) — Problemas P8, P9 y flujo general.

---

## Estado actual del flujo

```
/register → POST /auth/register (isFirstUser: true)
         → POST /auth/login (JWT sin permisos si Identity no tiene AuthorizationApi configurado)
         → setupSuperAdmin(userId):
              GET /authorization/users/{userId}/rules → si ya hay reglas, salir
              GET /authorization/permissions → obtener IDs de permisos del catálogo
              POST /authorization/roles (crear "SuperAdmin") → si falla, buscar existente
              POST /authorization/roles/{roleId}/permissions (asignar todos)
              POST /authorization/rules (crear regla userId → SuperAdmin)
         → refreshSession() → POST /auth/refresh (nuevo JWT, idealmente con permisos)
         → navigate('/') → Home
/home     → loadProjects() → GET /api/projects (sin page/pageSize)
              Si 403 → onUnauthorized → logout → login (el usuario no entiende qué pasó)
              Si 400 → mensaje "Parámetros de búsqueda no válidos"
```

### Problemas en el flujo

1. **GET /permissions devuelve vacío o falla:** Si la Authorization API no tiene seeders ejecutados o la llamada falla con 403, `setupSuperAdmin` no puede asignar permisos al rol y el SuperAdmin queda sin permisos. El frontend muestra "No hay permisos en el sistema".

2. **refreshSession() no trae permisos:** Si Identity no tiene `AuthorizationApi:BaseUrl` configurado, el refresh devuelve un JWT sin claims `permission` (usa `NoOpEffectivePermissionsClient`). El frontend no tiene forma de saber esto.

3. **GET /projects sin page/pageSize:** `loadProjects()` llama a `coreApi.getProjects()` sin argumentos. La URL resultante es `api/projects` sin query string. El backend tiene valores por defecto (page=1, pageSize=20), pero el validador exige `Page >= 1`; si en algún entorno el binding interpreta la ausencia como 0, el validador lanza 400.

4. **Core client hace logout en 403:** `onUnauthorized` en `core-api.ts` desloguea tanto en 401 como en 403. El SuperAdmin sin permisos en JWT es redirigido a login sin mensaje contextual.

---

## Plan paso a paso

### Paso 1 — Llamar siempre con page y pageSize explícitos ✅

**Qué:** En `loadProjects()` dentro de `context-store.ts`, pasar siempre `{ page: 1, pageSize: 20 }` a `coreApi.getProjects()`.

**Por qué:** Evita que la ausencia de query string produzca 400 por validación en algún entorno. Es una corrección defensiva y mínima.

**Cómo:**
1. En `context-store.ts`, cambiar `coreApi.getProjects()` por `coreApi.getProjects({ page: 1, pageSize: 20 })`.

**Archivos:** `context-store.ts`.

---

### Paso 2 — No desloguear en 403 en el cliente Core ✅

**Qué:** En `core-api.ts`, el callback `onUnauthorized` no debe hacer logout ni redirigir cuando `reason === '403'`. Solo debe actuar en `'401'`.

**Por qué:** Un 403 significa "sin permiso" (no "sesión inválida"). Desloguear al usuario impide que vea un mensaje contextual y pueda actuar (cerrar sesión manualmente, contactar admin, etc.). En el caso del primer usuario sin permisos, la UI debería mostrar "No tienes permiso para ver proyectos" y ofrecerle cerrar sesión e iniciar de nuevo.

**Cómo:**
1. En `core-api.ts`, cambiar `onUnauthorized` para que solo llame a `logout()` + `redirect` cuando `reason === '401'`.
2. Cuando es `'403'`, no hacer nada; el error se propaga al `catch` del store y se muestra mediante `getProjectsErrorMessage(e)` → "No tienes permiso para ver proyectos."

**Archivos:** `core-api.ts`.

---

### Paso 3 — Mejorar el mensaje de error en Home para el primer usuario ✅

**Qué:** Cuando el primer usuario llega a Home y GET /projects devuelve 403 o 400, mostrar un mensaje claro y una acción concreta.

**Por qué:** El usuario necesita entender qué pasa y qué puede hacer (p. ej. "Tu sesión no tiene permisos aún; cierra sesión e inicia sesión de nuevo").

**Cómo:**
1. En `context-store.ts` ya existe `getProjectsErrorMessage(e)` que distingue 400 y 403. Verificar que funciona correctamente.
2. En `HomePage.tsx`, tras mostrar el `ErrorBanner` con el mensaje de proyectos:
   - Si el error es de permisos (403), mostrar un texto: "Si acabas de registrarte como primer usuario, cierra sesión e inicia sesión de nuevo para actualizar tus permisos." con un botón "Cerrar sesión e iniciar de nuevo".
   - Si el error es 400, mostrar: "Error de parámetros de búsqueda. Recarga la página o contacta soporte."
3. (Opcional) Marcar en sessionStorage que se acaba de hacer el setup del primer usuario, para personalizar el mensaje solo en ese caso y no mostrarlo a usuarios normales sin permisos.

**Archivos:** `HomePage.tsx`, opcionalmente `RegisterPage.tsx` (para marcar sessionStorage).

---

### Paso 4 — Validar que setupSuperAdmin tiene permisos para asignar ✅

**Qué:** Dentro de `setupSuperAdmin`, si GET /permissions devuelve una lista vacía, mostrar un warning al usuario o loguearlo en consola con un mensaje claro de que los seeders no se han ejecutado.

**Por qué:** Si el catálogo de permisos no existe en la BD (seeders no ejecutados), el rol SuperAdmin se crea pero sin permisos. El usuario ve "No hay permisos en el sistema" después. Es importante que el flujo de setup lo comunique.

**Cómo:**
1. Después de `authorizationApi.getPermissions()`, si `allPermIds.length === 0`:
   - Mostrar un `setSetupStep('⚠ No se encontraron permisos en el sistema. Verifica que la Authorization API esté en ejecución.')`.
   - Continuar el flujo (no bloquear), ya que el rol se puede crear igual y los permisos se asignarán cuando los seeders corran.
2. En el catch de `getPermissions`, si el status es 403: loguearlo como warning pero continuar (el modo bootstrap debería permitir las siguientes operaciones).

**Archivos:** `RegisterPage.tsx` → `setupSuperAdmin`.

---

### Paso 5 — Reintento de refreshSession con pausa ✅

**Qué:** Si el primer `refreshSession()` tras setup falla, reintentar una vez tras 1-2 segundos.

**Por qué:** Entre que se crea la regla en Authorization y el refresh en Identity, puede haber latencia de red o de escritura. Un reintento con pausa cubre ese caso sin complicar el flujo.

**Cómo:**
1. Tras el primer catch de `refreshSession()`:
   - `await new Promise(r => setTimeout(r, 1500))`.
   - Reintentar `refreshSession()`.
2. Si el segundo intento también falla, marcar en sessionStorage para que Home muestre el mensaje del Paso 3.

**Archivos:** `RegisterPage.tsx` → `handleSubmit` (bloque "Actualizando sesión con permisos").

---

### Paso 6 — (Mejora futura) Verificación de salud post-setup ✅

**Qué:** Tras completar setupSuperAdmin + refreshSession, verificar que el JWT del store incluye permisos antes de navegar a Home.

**Por qué:** Si el JWT sigue sin permisos tras el refresh, el usuario va a ver errores en Home. Mejor advertirlo aquí.

**Cómo:**
1. Tras el refresh exitoso, decodificar el JWT del store (p. ej. con una función `decodeJwtPayload`) y comprobar si tiene al menos un claim `permission`.
2. Si no tiene permisos:
   - Mostrar un mensaje: "Tu sesión se creó pero el token no incluye permisos. Esto puede deberse a la configuración del backend. Puedes continuar pero algunas funciones estarán limitadas."
   - Ofrecer botón "Continuar de todos modos" que navega a Home y "Cerrar sesión" que redirige a login.
3. Si tiene permisos: navegar a Home normalmente.

**Archivos:** `RegisterPage.tsx`. Utilidad `decodeJwtPayload` en `shared/`.

---

## Resumen de pasos

| Paso | Acción | Prioridad | Estado |
|------|--------|-----------|--------|
| 1 | GET /projects siempre con page y pageSize | Alta | ✅ (004: pageSize 50) |
| 2 | No desloguear en 403 en Core client | Alta | ✅ (004) |
| 3 | Mensaje claro en Home para el primer usuario | Alta | ✅ |
| 4 | Warning si setupSuperAdmin no encuentra permisos | Media | ✅ |
| 5 | Reintento de refreshSession con pausa + sessionStorage | Media | ✅ |
| 6 | Verificación de JWT post-setup (mejora futura) | Baja | ✅ |

---

## Diagrama del flujo mejorado

```
/register
  ├─ POST /auth/register → isFirstUser: true
  ├─ POST /auth/login → JWT (posiblemente sin permisos)
  ├─ setupSuperAdmin(userId)
  │    ├─ GET /authorization/users/{userId}/rules → si hay reglas, salir
  │    ├─ GET /authorization/permissions → si vacío: warning (Paso 4)
  │    ├─ POST /authorization/roles (crear SuperAdmin)
  │    ├─ POST /authorization/roles/{id}/permissions (asignar)
  │    └─ POST /authorization/rules (regla usuario → SuperAdmin)
  ├─ refreshSession() → nuevo JWT con permisos (si Identity tiene AuthorizationApi configurado)
  │    └─ Si falla: reintentar tras 1.5s (Paso 5); si sigue fallando: marcar sessionStorage
  ├─ (Paso 6 opcional) Verificar claims en JWT
  └─ navigate('/')

/home
  ├─ loadProjects({ page: 1, pageSize: 20 }) (Paso 1)
  ├─ Si 403 → NO logout (Paso 2) → mensaje en UI + botón "Cerrar sesión e iniciar de nuevo" (Paso 3)
  ├─ Si 200 → lista de proyectos (vacía al principio)
  └─ SuperAdmin puede crear proyecto, entorno, sitio, schemas, contenido, usuarios ✓
```
