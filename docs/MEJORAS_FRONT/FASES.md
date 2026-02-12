# IODA CMS — Plan de Trabajo (Tareas Ejecutables)

> **Decisión de alcance:**
> Esta iteración implementa el flujo guiado como **wizard dentro de `HomePage.tsx`**.
> Las rutas dedicadas por paso quedan **fuera del scope actual**.

---

## Estado de Contexto (referencia lógica)

Definir y usar estos estados de forma consistente en UI y guards:

* `EMPTY` → sin proyecto
* `PROJECT_SELECTED`
* `PROJECT_ENV_SELECTED`
* `FULL_CONTEXT` (proyecto + entorno + sitio)

---

## A. Wizard de setup en HomePage

* [x] **HomePage como wizard real (una sola vista por paso)**
  Archivo: `frontend/src/app/pages/HomePage.tsx`

  * `EMPTY` → mostrar **Paso 1: Seleccionar / Crear Proyecto**
  * `PROJECT_SELECTED` → mostrar **Paso 2: Seleccionar / Crear Entorno**
  * `PROJECT_ENV_SELECTED` → mostrar **Paso 3: Seleccionar / Crear Sitio**
  * `FULL_CONTEXT` → mostrar **Dashboard**
* [x] **Eliminar render simultáneo de pasos y dashboard**
* [x] **Dashboard solo visible en `FULL_CONTEXT`**

  * Condición explícita con `currentProjectId + currentEnvironmentId + currentSiteId`

---

## B. Guards y reglas de acceso

* [x] **Definir `RequireFullContext`**

  * Requiere: proyecto + entorno + sitio
  * Archivo: `frontend/src/app/components/RequireFullContext.tsx`
* [x] **Aplicar guards en rutas de acción**

  * Dashboard → `RequireFullContext`
  * Contenido / Crear contenido / Publicar → decidido: **requieren `FULL_CONTEXT`** (proyecto + entorno + sitio)
* [x] **Redirección automática al paso correcto**

  * Si falta proyecto → Paso 1
  * Si falta entorno → Paso 2
  * Si falta sitio → Paso 3
  * Archivo: `frontend/src/app/App.tsx`

---

## C. Limpieza y consistencia del contexto

* [x] **Al cambiar proyecto**

  * Limpiar entorno y sitio
  * `setProject()` limpia `currentEnvironmentId`, `currentSiteId`, `environments` y `sites`.
* [x] **Al cambiar entorno**

  * Limpiar sitio
  * `setEnvironment()` ahora limpia `currentSiteId`, `sites` y `setStoredSiteId(null)` antes de recargar sitios filtrados por entorno.
* [x] **Reflejar limpieza en UI inmediatamente**

  * Zustand re-renderiza automáticamente todos los componentes suscritos (`AppLayout`, `HomePage`, `RequireFullContext`) al actualizar el estado con `set()`.
  * El wizard vuelve al Paso 3 si se limpia el sitio; `RequireFullContext` redirige a `/` si se pierde contexto.
* [x] **Evitar estados intermedios inválidos**

  * `rehydrate()` ahora valida la cascada: sin proyecto no restaura entorno ni sitio; sin entorno no restaura sitio. Limpia valores huérfanos de sessionStorage.
  * `loadProjects()` ahora aplica la misma cascada: si el entorno guardado es inválido limpia también el sitio; filtra sitios por entorno al validar.

---

## D. Top Bar (layout persistente)

Archivo: `frontend/src/app/components/AppLayout.tsx`

* [x] **Mantener Top Bar siempre visible al estar autenticado**

  * `AppLayout` envuelve todas las rutas protegidas.
* [x] **Eliminar selects de proyecto / entorno / sitio del Top Bar**

  * Los 3 `<select>` fueron eliminados. La selección de contexto ahora vive solo en el wizard (HomePage).
* [x] **Mostrar solo resumen de contexto**

  * Se muestra `Proyecto / Entorno / Sitio` como texto en la barra.
  * Si no hay contexto: "Sin contexto — configurar" con link al wizard.
  * Botón **"Cambiar"** navega a `/` para volver al wizard.
* [x] **Menú de usuario (dropdown)**

  * Muestra nombre/email del usuario.
  * Dropdown con email y botón "Cerrar sesión" en rojo.
* [x] **Menú "Administración" (dropdown)**

  * Schemas (`/admin/schemas`)
  * Sitios (`/sites`)
  * Roles y permisos (`/admin/roles`)
  * Usuarios (`/admin/users`) — protegido con `Can permission="user.list"`

---

## E. Buscador global

* [x] **Definir comportamiento explícito**

  * Visible **solo en `FULL_CONTEXT`** — el formulario de búsqueda se renderiza condicionalmente con `{hasFullContext && ...}`.
  * Si no hay contexto completo: el buscador no aparece en la barra.
* [x] **Evitar búsquedas sin contexto válido**

  * El input de búsqueda solo se monta cuando hay contexto completo. La ruta `/search` también requiere `RequireFullContext`.

---

## F. Acciones del CMS y visibilidad

* [x] **Ocultar o deshabilitar acciones del CMS fuera de `FULL_CONTEXT`**

  * Los links de Contenido, Crear contenido y Publicar solo se renderizan cuando `hasFullContext === true`.
  * Sitios, Schemas, Roles y Usuarios están en el dropdown "Admin" (siempre accesible, son acciones de administración no atadas al contexto de contenido).
* [x] **Nunca mostrar acciones que no se puedan ejecutar**

  * Los links de CMS solo aparecen con contexto completo. Los permisos (`Can`) controlan la visibilidad de Publicar y Usuarios.

---

## G. Dashboard como panel de acciones

Archivo: `frontend/src/app/pages/HomePage.tsx`

* [x] **Mover accesos principales del header a widgets**

  * El Top Bar (Fase D) ya no tiene links de acción redundantes. El Dashboard es el panel principal de acciones.
  * Widgets organizados en dos secciones: **Contenido** y **Administración**.
* [x] **Widgets controlados por permisos (`Can(permission)`)**

  * Contenido → accesible a todos
  * Crear contenido → accesible a todos
  * Publicar → `Can permission="content.publish"`
  * Búsqueda → accesible a todos
  * Diseñador de schemas → accesible a todos
  * Gestión de sitios → accesible a todos
  * Roles y permisos → accesible a todos
  * Usuarios → `Can permission="user.list"`
* [x] **Widget "Contexto actual"**

  * Muestra proyecto / entorno / sitio con nombres legibles.
  * Cada fila tiene botón **"Cambiar"** que limpia el nivel correspondiente:
    * Cambiar sitio → `setSite(null)` → wizard muestra Paso 3
    * Cambiar entorno → `setEnvironment(null)` → cascada limpia sitio → wizard muestra Paso 2
    * Cambiar proyecto → `setProject(null)` → cascada limpia entorno+sitio → wizard muestra Paso 1
  * **Pendiente:** hoy el contexto se muestra en un `<p>` simple. Falta convertirlo en un widget con acciones "Cambiar proyecto/entorno/sitio".

---

## H. UX y consistencia visual

* [x] **Unificar loaders y estados vacíos**

  * Se reutilizan `LoadingSpinner` / `ErrorBanner` en todas las páginas.
* [ ] **Accesibilidad en wizard**

  * `label` + `htmlFor`
  * Navegación por teclado
  * **Pendiente:** los `<label>` del wizard no usan `htmlFor`/`id`. Falta vincularlos.
* [x] **Contraste correcto en tema claro/oscuro**

  * Implementado con variables CSS `--page-text`, `--input-bg`, etc. en `index.css` y en todas las páginas/componentes.

---

## I. QA — Criterios de aceptación

* [x] Login → siempre entra a layout autenticado + wizard
* [x] Nunca se muestra Dashboard sin `FULL_CONTEXT`
* [x] No se pueden ejecutar acciones sin contexto válido (guards redirigen)
* [x] Cambio de proyecto limpia entorno y sitio
* [x] Cambio de entorno limpia sitio
* [x] Permisos controlan visibilidad (no solo bloqueo) — Publicar y Usuarios protegidos con `Can`; links CMS ocultos sin contexto
* [x] Redirecciones siempre llevan al paso correcto

---

## Resumen de pendientes (orden sugerido de ejecución)

| # | Fase | Tarea clave | Esfuerzo |
|---|------|-------------|----------|
| 1 | **H** | Accesibilidad: `label htmlFor` + navegación teclado | Bajo |

---

## Flujo final esperado

```text
Login
  ↓
Layout autenticado (Top Bar)
  ↓
Wizard de contexto (Proyecto → Entorno → Sitio)
  ↓
Dashboard (widgets de acción)
```
