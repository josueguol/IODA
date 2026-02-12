# 🖥️ Frontend CMS – Próximos pasos (plan paso a paso)

Documento de referencia para implementar el **frontend del CMS genérico y schema-driven** de forma incremental, con fases probables y alineadas al backend ya construido.

---

## 1. Objetivo y principios

- **Frontend desacoplado**: no conoce tipos de contenido, solo **esquemas dinámicos**.
- **Arquitectura modular** y feature-based.
- **Renderizado dinámico** de formularios desde esquemas.
- **Gestión de permisos** vía Authorization API (solo UI; el backend manda).
- **Integración con múltiples microservicios** (Identity, Authorization, Core, Publishing, Indexing).
- **Código limpio y escalable** (TypeScript, React, buenas prácticas).

---

## 2. Backend de referencia (URLs y contratos)

| Servicio        | Puerto local (dev) | Puerto Docker | Base path / uso principal                          |
|-----------------|--------------------|---------------|----------------------------------------------------|
| **Core API**    | 5269               | 5001          | `/api/projects`, `/api/projects/{id}/content`, schemas, publish |
| **Identity API**| 5270               | 5002          | `/api/auth/register`, `/api/auth/login`, `/api/auth/refresh`     |
| **Authorization API** | 5271        | 5003          | `/api/authorization/check`, roles, permissions, rules          |
| **Publishing API**    | 5272        | 5004          | `/api/publishing/requests`, approve, reject                       |
| **Indexing API**     | 5273        | 5005          | `/api/indexing/search`, index, remove                           |

- **Autenticación**: JWT (access + refresh). Identity devuelve `accessToken`, `refreshToken`, `expiresAt`.
- **Autorización**: `POST /api/authorization/check` con `userId`, `permissionCode`, opcionalmente `projectId`, `environmentId`, `schemaId`, `contentStatus`.
- **Core**: proyectos, entornos (Environment vía DB/API según implementación), esquemas por proyecto, contenido CRUD y publish/unpublish por proyecto.

Toda la implementación del frontend debe **adaptarse a estos servicios** (mismas URLs, mismos cuerpos de petición/respuesta).

---

## 3. Stack técnico (recomendado)

- **Lenguaje**: TypeScript (strict).
- **Framework**: React 18+.
- **Build**: Vite.
- **Estado global**: Zustand (o Redux Toolkit si se prefiere).
- **Formularios**: React Hook Form + Zod (validación alineada a esquemas).
- **UI**: Headless/Radix + Tailwind CSS (o CSS Modules).
- **Rutas**: React Router v6.
- **HTTP**: fetch o axios con interceptors (JWT, refresh, errores centralizados).
- **Variables de entorno**: `VITE_*` para base URLs de cada API.

---

## 4. Fases de implementación (paso a paso)

Cada fase termina con **criterios de “listo”** y **cómo probar** antes de seguir.

---

### Fase 0 – Fundamentos del proyecto frontend

**Objetivo:** Proyecto React + TypeScript + Vite con estructura de carpetas modular, clientes HTTP base y configuración de entornos.

**Tareas:**

- [x] Crear proyecto con `npm create vite@latest frontend -- --template react-ts` (o equivalente).
- [x] Configurar ESLint, Prettier, TypeScript strict.
- [x] Definir estructura de carpetas (ver sección 5).
- [x] Añadir variables de entorno (ej. `VITE_CORE_API_URL`, `VITE_IDENTITY_API_URL`, `VITE_AUTHORIZATION_API_URL`, `VITE_PUBLISHING_API_URL`) y un módulo `config` o `env` que las exporte.
- [x] Crear **cliente HTTP base** (fetch/axios) con:
  - Base URL configurable por servicio.
  - Interceptor de respuesta para errores 401/403 y formato de errores (ProblemDetails si aplica).
- [x] Documentar en un `README.md` del frontend cómo instalar dependencias y arrancar en dev.

**Criterios de “listo”:**

- `npm install` y `npm run dev` levantan la app.
- Cambiar una `VITE_*` y usarla en un componente de prueba (ej. mostrar la URL de Core en pantalla) confirma que la config se lee bien.

**Cómo probar:**

- Ejecutar `npm run dev`, abrir la app en el navegador y comprobar que no hay errores de compilación ni de runtime.
- Añadir temporalmente un `<p>{import.meta.env.VITE_CORE_API_URL}</p>` y verificar que muestra la URL configurada.

---

### Fase 1 – Módulo Auth (Identity API)

**Objetivo:** Login, JWT, refresh token y logout; sesión persistida de forma segura (ej. memoria + opcional httpOnly cookie en futuras iteraciones).

**Tareas:**

- [x] Definir tipos TypeScript para login request/response y refresh (alineados a Identity API).
- [x] Implementar **Auth API client**: `login(email, password)`, `refresh(refreshToken)`, `register(...)` si se usa.
- [x] Crear **store de auth** (Zustand o similar): `user`, `accessToken`, `refreshToken`, `expiresAt`, `isAuthenticated`, acciones `login`, `logout`, `refreshSession`.
- [x] Implementar **interceptor HTTP** que:
  - Añade `Authorization: Bearer <accessToken>` a las peticiones que vayan a APIs que requieran JWT.
  - Ante 401, intenta refresh con el refresh token; si falla, hace logout y redirige a login.
- [x] Pantalla de **Login** (formulario email/password, llamada a Identity API, guardar tokens y usuario en el store).
- [x] **Logout**: limpiar store y redirigir a login.
- [x] Persistencia opcional: guardar solo refresh token en `localStorage` (o sesión) para rehidratar al recargar; obtener nuevo access token al iniciar la app si hay refresh token válido.

**Criterios de “listo”:**

- Con Identity API levantada, el usuario puede hacer login y ver una pantalla post-login (ej. “Dashboard” o “Home”).
- Al recargar, si el refresh token es válido, la sesión se restaura sin volver a introducir contraseña.
- Logout limpia estado y redirige a login.

**Cómo probar:**

- Levantar Identity API (ver `docs/COMO_PROBAR_FASE_2.md`).
- En el frontend: registrar usuario (si hay pantalla) o usar credenciales existentes; hacer login; comprobar que se recibe JWT y que una ruta protegida es accesible.
- Forzar 401 (ej. token caducado o borrar access token en devtools) y comprobar que se intenta refresh y, si falla, redirección a login.

---

### Fase 2 – Módulo Authorization (Access Rules)

**Objetivo:** Usar la Authorization API para decidir en la UI qué mostrar/ocultar (botones, rutas, menús) según permisos; sin lógica de negocio, solo hints de UI.

**Tareas:**

- [x] Definir tipos para `CheckAccess` request/response (userId, permissionCode, projectId?, environmentId?, schemaId?, contentStatus?).
- [x] Implementar **Authorization API client**: `checkAccess(params)` (cliente auth-aware con JWT).
- [x] Crear hook `usePermission(permissionCode, context?)` que llama a `checkAccess` (con userId del store de auth y contexto opcional) y devuelve `{ allowed, loading, error }`.
- [x] Crear componente **Can** (ej. `<Can permission="content.edit" projectId={...}>...</Can>`) que renderiza children solo si `allowed === true`.
- [x] Opcional: cachear resultados de check por (userId, permissionCode, context) para evitar llamadas repetidas en la misma sesión (cache 60s, `invalidatePermissionCache()`).
- [x] Protección de rutas: componente o guard que consulte `usePermission` para la ruta (ej. “content.edit”) y redirija a “sin permiso” o a home si no hay acceso.

**Criterios de “listo”:**

- En una pantalla de prueba, un botón “Solo si tienes content.edit” solo se muestra cuando el usuario tiene ese permiso en el backend.
- Una ruta protegida con “content.publish” redirige o muestra “Sin permiso” si el usuario no tiene el permiso.

**Cómo probar:**

- Tener Authorization API y Identity API levantadas; usuario con rol que tenga `content.edit` (o el permiso que uses).
- Asignar/revocar reglas de acceso en Authorization API y comprobar que el frontend muestra u oculta el botón/ruta según el resultado de `check`.

---

### Fase 3 – Contexto (proyecto y entorno) y clientes Core

**Objetivo:** El usuario elige “proyecto” y “entorno”; el resto de la app usa ese contexto. Clientes HTTP para Projects y, si existe, Environments.

**Tareas:**

- [x] **Store de contexto** (Zustand): `currentProjectId`, `currentEnvironmentId`, `setProject`, `setEnvironment`, lista de proyectos/entornos en memoria.
- [x] **Core API client** (módulo `modules/core`): `getProjects()`, `getProject(id)`, `getEnvironments(projectId)` (alineado a Core API).
- [x] Selector de **proyecto** en layout (`AppLayout`): dropdown desde Core; al elegir, guardar en store y cargar entornos.
- [x] Selector de **entorno** en el mismo layout; mismo patrón.
- [x] Persistir en `sessionStorage` el último proyecto/entorno elegido para rehidratar al recargar.

**Criterios de “listo”:**

- Al arrancar la app (con usuario logueado), se listan proyectos desde Core y el usuario puede elegir uno.
- El resto de módulos pueden leer `currentProjectId` y `currentEnvironmentId` del store para las llamadas API.

**Cómo probar:**

- Con Core API levantada y al menos un proyecto creado, abrir el frontend, elegir proyecto y comprobar que el store actualiza y que las peticiones posteriores a Core usan ese `projectId` (ej. en URLs o en body).

---

### Fase 4 – Módulo Schema y motor de formularios dinámicos

**Objetivo:** Obtener esquemas desde Core (por proyecto), cachearlos y renderizar formularios dinámicos a partir de `FieldDefinition` (tipos, validaciones, etc.).

**Tareas:**

- [x] **Core API client** (schemas): `getSchemas(projectId)`, `getSchema(projectId, schemaId)` (alineado a `GET /api/projects/{projectId}/schemas`, `GET .../schemas/{schemaId}`).
- [x] Definir tipos TypeScript para **ContentSchema** y **FieldDefinition** en `modules/core/types.ts` (nombre, tipo, required, validationRules, etc.) según el contrato del Core.
- [x] **Schema store** (`useSchemaStore`): `loadSchemas(projectId)`, `loadSchema(projectId, schemaId)`, cache en memoria, `getSchemaSync(projectId, schemaId)`.
- [x] **Dynamic Form Engine**:
  - Mapeo **tipo de campo → componente UI**: string/richtext/text → input o textarea, number/integer → input number, boolean → checkbox, date/datetime → input date/datetime-local, enum/reference → input text, json → textarea (en `DynamicField.tsx`).
  - React Hook Form con `Controller`; validaciones desde esquema (required, min/max, pattern) vía Zod en `field-validation.ts` (`buildZodSchema`).
- [x] Componente **DynamicForm**: props `projectId`, `schemaId`, `defaultValues?`, `onSubmit`; resuelve schema con store, construye formulario y envía valores al submit. Página **Crear contenido** (`/content/new`) con selector de schema y DynamicForm; payload mostrado en pantalla (Fase 5 enviará a Core).

**Criterios de “listo”:**

- Dado un proyecto con al menos un schema (ej. “Article” con campos title, body, publishedAt), la pantalla de “crear contenido” muestra un formulario generado a partir del schema (campos correctos, tipos correctos).
- Validaciones required y tipos básicos funcionan (no enviar si hay errores).

**Cómo probar:**

- Crear en Core un ContentSchema con varios FieldDefinitions (string, number, boolean, enum, date).
- En el frontend, seleccionar ese proyecto y abrir “Crear contenido” para ese schema; comprobar que se listan los campos y que al enviar se construye el payload correcto (para la siguiente fase).

---

### Fase 5 – Módulo Content (CRUD genérico)

**Objetivo:** Listar, crear, editar y eliminar contenido genérico por proyecto y schema; usar el Dynamic Form para crear/editar.

**Tareas:**

- [x] **Core API client** (content): `getContentList`, `getContent`, `createContent`, `updateContent`, `deleteContent` (alineado a ContentController). Backend: añadido endpoint `DELETE /api/projects/{projectId}/content/{contentId}`.
- [x] Pantalla **lista de contenido** (`/content`): filtros por schema (dropdown desde getSchemas), por estado (Draft/Published si aplica); tabla o cards con columnas dinámicas (al menos id, slug, status, schema).
- [x] Pantalla **crear contenido** (`/content/new`): selector de schema → DynamicForm con ese schema → onSubmit llama a `createContent` con projectId y body (slug, schemaId, fields según Core).
- [x] Pantalla **editar contenido** (`/content/:contentId/edit`): cargar contenido por id, rellenar DynamicForm con valores actuales, onSubmit llama a `updateContent`.
- [x] Eliminar: botón o acción “eliminar” con confirmación y llamada a `deleteContent`.
- [x] Mostrar **estado** del contenido (Draft/Published) y, si aplica, versión; en la siguiente fase se enlazará con Publishing.

**Criterios de “listo”:**

- El usuario puede listar contenido de un proyecto (filtrando por schema), crear un nuevo contenido rellenando el formulario dinámico, editar un contenido existente y eliminarlo.
- Los datos se persisten correctamente en Core (comprobar en Core API o en BD).

**Cómo probar:**

- Con Core API levantada, crear un proyecto y un schema; desde el frontend crear varios contenidos, editarlos, listarlos y eliminar uno; verificar en Swagger de Core o en la BD que los datos son correctos.

---

### Fase 6 – Módulo Publishing (flujo de estados)

**Objetivo:** Mostrar estado de publicación del contenido y permitir solicitar publicación, aprobar y rechazar usando la Publishing API.

**Tareas:**

- [x] **Publishing API client**: `requestPublication(contentId, projectId, environmentId, requestedBy)`, `approvePublication(requestId, approvedBy)`, `rejectPublication(requestId, rejectedBy, reason?)`, `getPublicationRequests(contentId?, status?)`.
- [x] En la pantalla de **detalle/edición de contenido**: mostrar estado actual (Draft/Published) y, si el usuario tiene permiso, botones “Solicitar publicación” / “Aprobar” / “Rechazar” según el estado y las reglas de negocio (UI según permisos con `usePermission`).
- [x] Pantalla **“Solicitudes de publicación”**: listar `getPublicationRequests`, filtrar por contentId o status (Pending, Approved, Rejected); acciones Aprobar/Rechazar con formulario (approvedBy; rejectedBy + reason).
- [x] Tras aprobar: Core recibe la llamada desde Publishing y publica el contenido; opcionalmente mostrar mensaje de éxito y actualizar estado del contenido en UI (refetch o actualización optimista).
- [x] Mostrar **historial o feedback** de validación si Publishing devuelve mensajes (ej. “Contenido no válido por…”); mostrarlos en la UI.

**Criterios de “listo”:**

- Desde el frontend se puede solicitar la publicación de un contenido (Draft), ver la solicitud en “Solicitudes de publicación” y, con un usuario con permiso, aprobar o rechazar.
- Tras aprobar, el contenido pasa a Published en Core (comprobar en Core o en lista de contenido).
- Los botones de publicar/aprobar/rechazar se muestran u ocultan según permisos (Authorization).

**Cómo probar:**

- Tener Core API y Publishing API levantadas; Identity y Authorization configurados con usuarios y roles/permisos adecuados.
- Crear contenido en estado Draft, solicitar publicación, aprobar desde la UI y verificar que el contenido queda Published en Core.

---

### Fase 7 – Integración opcional con Indexing y refinamientos

**Objetivo:** Búsqueda de contenido publicado (si se usa Indexing) y mejoras transversales (manejo de errores, loading, accesibilidad).

**Tareas:**

- [x] **Indexing API client** (`modules/indexing`): `search({ q?, page?, pageSize?, contentType? })` (GET `/api/indexing/search`).
- [x] Pantalla **búsqueda** (`/search`): resultados con enlace a edición del contenido; paginación. Barra de búsqueda en `AppLayout` que redirige a `/search?q=...`.
- [x] Refinamientos: **LoadingSpinner** y **ErrorBanner** en `shared/components`; usados en SearchPage y ContentListPage. Mensajes consistentes.
- [ ] Opcional: **Media module** (pendiente; requiere Media API en backend).

**Criterios de “listo”:**

- Si Indexing API está disponible, la búsqueda devuelve resultados y la UI los muestra.
- Errores de API se muestran de forma uniforme; las pantallas principales muestran loading mientras cargan datos.

**Cómo probar:**

- Con Indexing API levantada y contenido publicado indexado, usar la búsqueda y comprobar que los resultados coinciden con el contenido.
- Simular error de red o 500 y comprobar que se muestra un mensaje claro al usuario.

---

## 5. Estructura de carpetas sugerida

```
frontend/
├── src/
│   ├── app/                    # Bootstrap, router, layout raíz
│   │   ├── App.tsx
│   │   ├── router.tsx
│   │   └── layout/
│   ├── modules/
│   │   ├── auth/               # Login, logout, store auth
│   │   ├── authorization/      # usePermission, Can, guards
│   │   ├── core/               # Core API (proyectos, entornos, schemas), store contexto, tipos DTO
│   │   ├── schema/             # Schema store (cache), DynamicForm, DynamicField, field-validation (Zod)
│   │   ├── content/            # (páginas en app/pages) Lista, crear, editar, eliminar contenido
│   │   ├── publishing/         # publishingApi, tipos PublicationRequest (solicitudes, aprobar, rechazar)
│   │   ├── indexing/           # indexingApi, tipos SearchResult (búsqueda de contenido publicado)
│   │   └── search/             # (páginas en app/pages) Pantalla de búsqueda
│   ├── shared/
│   │   ├── api/                # Cliente HTTP base, interceptors
│   │   ├── components/         # LoadingSpinner, ErrorBanner, componentes reutilizables
│   │   ├── hooks/              # Hooks genéricos (useApi, useDebounce…)
│   │   └── types/              # Tipos globales
│   ├── config/                 # env, constantes
│   └── main.tsx
├── public/
├── index.html
├── package.json
├── tsconfig.json
├── vite.config.ts
└── README.md
```

Cada módulo puede seguir una convención interna, por ejemplo: `api/`, `components/`, `hooks/`, `store/`, `types/`.

---

## 6. Orden recomendado y dependencias

- **Fase 0** es obligatoria primero.
- **Fase 1 (Auth)** es prerequisito de todas las demás (las APIs requieren JWT salvo login/register).
- **Fase 2 (Authorization)** puede hacerse en paralelo o justo después de Fase 1; recomendable antes de mostrar botones/ rutas condicionadas por permisos.
- **Fase 3 (Contexto)** es necesaria antes de Fase 4 y 5 (schemas y contenido son por proyecto).
- **Fase 4 (Schema + Dynamic Form)** es prerequisito de Fase 5 (Content usa el formulario dinámico).
- **Fase 5 (Content)** es prerequisito de Fase 6 (Publishing actúa sobre contenido existente).
- **Fase 6 (Publishing)** puede probarse en cuanto Content y Authorization estén listos.
- **Fase 7** es opcional y se puede hacer en cualquier momento una vez Core y opcionalmente Indexing estén disponibles.

Resumen: **0 → 1 → (2 en paralelo o después) → 3 → 4 → 5 → 6 → 7 (opcional)**.

---

## 7. Cómo usar este documento

- **Antes de codar cada fase:** leer las tareas y los criterios de “listo” de esa fase.
- **Al terminar cada fase:** comprobar “Cómo probar” y marcar las tareas como hechas en este documento (cambiar `[ ]` por `[x]`).
- **Si el backend cambia:** actualizar la sección 2 (URLs, contratos) y los clientes API correspondientes en el frontend.
- **Para priorizar:** si hay poco tiempo, el mínimo viable es Fase 0 + 1 + 3 + 4 + 5 (proyecto, auth, contexto, formularios dinámicos y CRUD de contenido); Fase 2 y 6 añaden permisos y flujo de publicación.

---

**Última actualización:** 2026-01-24  
**Referencias:** `FRONTENT_PLAN_DE_TRABAJO.md`, `NEXT_STEPS.md`, `docs/COMO_PROBAR_FASE_*.md`
