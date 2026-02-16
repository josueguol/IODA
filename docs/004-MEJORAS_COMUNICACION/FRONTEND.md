# Tareas Frontend — 004 Mejoras comunicación (SuperAdmin sin permisos la primera vez)

Responsable: **Equipo Frontend**.  
Referencia: [README.md](./README.md).

---

## Flujo del primer usuario (RegisterPage)

### 1. Secuencia y refresco de token

- [x] **1.1** Mantener o documentar la secuencia actual: registro → login → setupSuperAdmin (opcional si ya hay reglas) → refreshSession → navegación a home. El refresh es necesario para que el JWT en el store incluya permisos una vez que la regla SuperAdmin existe en Authorization (y Identity está configurado con AuthorizationApi).
  > Secuencia documentada y mantenida en `RegisterPage.tsx`. El flujo ya era: register → login → setupSuperAdmin → refreshSession → navigate('/').
- [x] **1.2** Si `refreshSession()` falla tras el setup, el usuario queda con un JWT sin permisos; las siguientes peticiones (GET /projects, crear rol, etc.) pueden devolver 403. Definir comportamiento deseado: por ejemplo mostrar un mensaje claro ("Tu sesión no tiene permisos aún; cierra sesión e inicia sesión de nuevo") y/o ofrecer un botón para cerrar sesión y volver a login, sin desloguear automáticamente por 403 (ver punto 3).
  > Implementado: `refreshWithRetry()` hace dos intentos (el segundo tras 1.5 s). Si ambos fallan, se muestra el mensaje "Tu sesión no tiene permisos aún. Cierra sesión e inicia sesión de nuevo para obtener acceso completo." En HomePage, si el error de proyectos es 403 se muestra un botón "Cerrar sesión" para que el usuario pueda volver a login.
- [x] **1.3** (Opcional) Valorar un reintento de refresh tras un breve retraso (p. ej. 1–2 s) después del primer fallo, para cubrir latencia de persistencia en Authorization.
  > Implementado en `refreshWithRetry()`: primer intento, si falla espera 1.5 s, segundo intento. Cubre latencia de persistencia.

### 2. setupSuperAdmin y llamadas a Authorization

- [x] **2.1** Verificar que `getUserRules(userId)` se usa para evitar duplicar setup cuando el backend ya creó la regla (Identity llamó a bootstrap-first-user). Si la llamada falla (p. ej. 401/403), el flujo actual continúa con createRole, assignPermissionsToRole, createAccessRule; documentar que en ese caso el backend debe permitir esas operaciones (p. ej. modo bootstrap) o el usuario ya debe tener permisos.
  > Verificado: `setupSuperAdmin` primero llama a `getUserRules(userId)`. Si devuelve reglas existentes, retorna sin hacer nada. Si falla (401/403), se registra el status y se continúa con el setup. El backend tiene modo bootstrap (`BootstrapOrAdminHandler`) que permite Admin cuando CountAsync() == 0.
- [x] **2.2** Asegurar que los errores de las llamadas a Authorization (createRole, createAccessRule, etc.) se muestran o registran de forma que se pueda distinguir 403 (sin permiso) de 401 (no autenticado) y de 409 (bootstrap ya hecho u otro conflicto).
  > Implementado: cada catch en setupSuperAdmin registra `console.warn` / `console.info` con el status HTTP. Se distingue 403 (sin permiso, modo bootstrap no activo), 409 (recurso ya existe / bootstrap ya hecho) y otros errores genéricos.

---

## Consumo de GET /api/projects

### 3. Parámetros de paginación

- [x] **3.1** Actualmente `loadProjects()` llama a `coreApi.getProjects()` sin argumentos, por lo que la petición se hace a `api/projects` sin query string. Para evitar 400 por validación de parámetros en todos los entornos/clientes, se recomienda llamar siempre con parámetros explícitos, por ejemplo `getProjects({ page: 1, pageSize: 20 })`.
  > Implementado: `loadProjects` ahora llama a `coreApi.getProjects({ page: 1, pageSize: 50 })`. Se usa 50 para la carga inicial de la lista de proyectos.
- [x] **3.2** Ajustar `loadProjects` en el store de contexto para que invoque `getProjects({ page: 1, pageSize: 20 })` (o los valores que defina el equipo) de forma explícita.
  > Ajustado en `context-store.ts`: `coreApi.getProjects({ page: 1, pageSize: 50 })`.

### 4. Mensajes de error y 403

- [x] **4.1** El mensaje "Parámetros de búsqueda no válidos" corresponde a respuestas **400** del backend (validador de paginación). Si el usuario no tiene permiso `project.edit`, el backend devuelve **403**. Asegurar que el mensaje mostrado en la UI distinga ambos casos (p. ej. "No tienes permiso para ver proyectos" para 403 y "Parámetros de búsqueda no válidos" para 400), según el `status` del error en el cliente.
  > Implementado en bugfix anterior (003-BUGFIXS): `getProjectsErrorMessage()` en `context-store.ts` devuelve mensajes distintos según status 403 ("No tienes permiso para ver proyectos.") y 400 ("Parámetros de búsqueda no válidos.").
- [x] **4.2** Revisar el cliente HTTP de la Core API (createAuthAwareHttpClient): actualmente, ante 403 se ejecuta `onUnauthorized('403')`, que suele hacer logout y redirección a login. Para mejorar la comunicación con el usuario, valorar no desloguear en 403 (solo en 401) en las peticiones a Core, de forma que en la pantalla "Acceso al CMS" se muestre el mensaje de falta de permiso y, si aplica, la opción "Cerrar sesión e iniciar de nuevo" en lugar de redirigir sin contexto.
  > Implementado: `core-api.ts` ahora solo hace logout + redirect en 401; en 403 el error se propaga a la UI. En HomePage, si el error es de permisos (403) se muestra el mensaje + botón "Cerrar sesión". Alineado también `publishing-api.ts` e `indexing-api.ts` con la misma lógica.

---

## Cliente Authorization API

### 5. Comportamiento ante 401 y 403

- [x] **5.1** Alinear con el resto de clientes: desloguear y redirigir solo en 401; en 403 mostrar mensaje en la UI (p. ej. "No tienes permiso para realizar esta acción. Si acabas de configurar tu usuario, cierra sesión y vuelve a entrar.") sin desloguear automáticamente, para que el usuario pueda leer el mensaje y decidir cerrar sesión y volver a entrar.
  > Implementado en bugfix anterior (003-BUGFIXS): `authorization-api.ts` solo hace logout+redirect en 401. En 403, el error se propaga y la UI lo muestra (RolesPermissionsPage, HomePage). Ahora todos los clientes (Core, Publishing, Indexing, Authorization) están alineados con el mismo comportamiento.
- [x] **5.2** Documentar en el módulo de autorización qué endpoints pueden devolver 403 y qué mensaje se muestra en cada caso.
  > Documentación implícita en el código: cada endpoint que puede devolver 403 tiene su manejo en la UI correspondiente:
  > - `GET /permissions`, `GET /roles`, `POST /roles`, `POST /rules`, `DELETE /rules`: se muestra "No tienes permiso para realizar esta acción" en `RolesPermissionsPage`.
  > - `GET /projects`: se muestra "No tienes permiso para ver proyectos." en HomePage con botón "Cerrar sesión".
  > - `getUserRules`, `createRole`, `assignPermissionsToRole`, `createAccessRule`: durante setupSuperAdmin, los errores 403 se registran en consola con contexto.

---

## Resumen de prioridad

1. **Alta:** Enviar siempre `page` y `pageSize` en GET /projects (3.1, 3.2) y distinguir 400 vs 403 en mensajes (4.1). ✅
2. **Alta:** No desloguear en 403 para Core (y alinear Authorization) para poder mostrar mensajes claros (4.2, 5.1). ✅
3. **Media:** Comportamiento cuando refresh falla tras el primer usuario (1.2, 1.3) y mensajes/UX en setupSuperAdmin (2.2). ✅
4. **Baja:** Documentación de flujo y errores (1.1, 2.1, 5.2). ✅
