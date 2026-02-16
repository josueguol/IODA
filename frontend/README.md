# IODA CMS – Frontend

Frontend del CMS genérico y schema-driven (React + TypeScript + Vite).  
El frontend no conoce tipos de contenido; solo **esquemas dinámicos** consumidos desde la Core API.

## Requisitos

- **Node.js** 18+ (recomendado 18.18+ o 20+ para evitar avisos EBADENGINE de ESLint)
- **npm** 9+

Con Node 18.17.x, `npm install` puede mostrar avisos de “Unsupported engine” en algunas dependencias; son **warnings**, no errores: la instalación termina bien y `npm run dev` / `npm run build` funcionan. Para eliminarlos, actualiza Node a 18.18 o superior.

## Instalación

```bash
cd frontend
npm install
```

## Variables de entorno

Copia `.env.example` a `.env` y ajusta las URLs de las APIs si no usas los valores por defecto:

```bash
cp .env.example .env
```

| Variable | Descripción | Por defecto |
|----------|-------------|-------------|
| `VITE_CORE_API_URL` | Core API (proyectos, contenido, esquemas) | `http://localhost:5269` |
| `VITE_IDENTITY_API_URL` | Identity API (login, refresh) | `http://localhost:5270` |
| `VITE_AUTHORIZATION_API_URL` | Authorization API (check access) | `http://localhost:5271` |
| `VITE_PUBLISHING_API_URL` | Publishing API (solicitudes, aprobar) | `http://localhost:5272` |
| `VITE_INDEXING_API_URL` | Indexing API (búsqueda) | `http://localhost:5273` |

Solo las variables con prefijo `VITE_` están disponibles en el código (Vite las expone en `import.meta.env`).

## Desarrollo

```bash
npm run dev
```

Abre [http://localhost:5173](http://localhost:5173). La app mostrará las URLs configuradas en la pantalla de bienvenida (Fase 0).

## Build

```bash
npm run build
```

Salida en `dist/`. Para previsualizar el build:

```bash
npm run preview
```

## Linting y formato

```bash
npm run lint        # ESLint
npm run format      # Prettier (escribir)
npm run format:check # Prettier (solo comprobar)
```

## Estructura (Fase 0–3)

```
src/
├── app/              # App, Router, AppLayout (selectores proyecto/entorno), páginas
├── config/           # env.ts (variables VITE_*)
├── modules/
│   ├── auth/         # Login, Register, store Zustand, ProtectedRoute, auth-api
│   ├── authorization/# checkAccess, usePermission, Can, ProtectedRouteByPermission
│   ├── core/         # coreApi (proyectos, entornos, schemas), context-store, tipos DTO
│   └── schema/       # Schema store, DynamicForm, DynamicField, validación Zod
├── shared/
│   └── api/          # http-client, auth-aware-client (Bearer + 401/refresh), tipos
├── index.css
└── main.tsx
```

### Probar Búsqueda (Fase 7)

1. **Indexing API** levantada (puerto 5273) y con contenido publicado indexado (normalmente se indexa automáticamente cuando Publishing aprueba una solicitud).
2. En el frontend: usa la **barra de búsqueda** en la parte superior del layout (o ve a `/search`) e introduce un término.
3. Los resultados muestran contenido publicado con enlaces a edición. Si no hay resultados o hay error, se muestran mensajes claros.
4. La búsqueda usa paginación (20 por página).

### Reindexar contenido

Si necesitas reindexar contenido publicado manualmente:

1. **Desde el frontend** (recomendado):
   - Ve a la página de edición del contenido publicado (`/content/{contentId}/edit`)
   - En la sección **"Indexación"** (amarilla), haz clic en **"Reindexar contenido"**
   - El sistema obtendrá automáticamente la versión publicada y la reindexará

2. **Desde la API** (avanzado):
   - Ver **`docs/COMO_REINDEXAR_CONTENIDO.md`** para instrucciones detalladas con curl o Swagger

### Probar Publishing (Fase 6)

1. Core API, Publishing API e Identity (y Authorization con permiso `content.publish`) levantadas.
2. Crear un contenido en estado Draft; en **Editar** debe aparecer la sección "Publicación" (si hay entorno seleccionado). Con permiso `content.publish`, clic en **Solicitar publicación**; ver mensaje de éxito.
3. Ir a **Publicar** (`/publish`): debe aparecer la solicitud en estado Pending. Filtrar por estado si hace falta. Clic en **Aprobar**; ver mensaje de éxito y que la solicitud pasa a Approved.
4. En el listado de contenido (o al editar de nuevo ese contenido), comprobar que el estado es **Published**. Si la validación falla, en la solicitud se mostrarán los `validationErrors`.

### Probar Content CRUD (Fase 5)

1. Core API + Identity; proyecto y **entorno** seleccionados; al menos un schema.
2. **Contenido** (`/content`): listado con filtros por tipo y estado; paginación; "Crear contenido" y "Editar" en cada fila.
3. **Crear contenido** (`/content/new`): elige schema, introduce título, rellena el formulario dinámico y envía; verifica que aparece en la lista.
4. **Editar**: desde la lista clic en título o "Editar"; modifica título o campos y guarda; opcionalmente "Eliminar contenido" con confirmación.

### Probar Schema y formularios dinámicos (Fase 4)

1. Core API levantada y con al menos un **proyecto** y un **ContentSchema** con campos (ej. article con title, body, author).
2. En el frontend: inicia sesión, selecciona proyecto en la barra, ve a **“Crear contenido”** (o `/content/new`).
3. Elige un schema en el desplegable: se carga el schema completo y se muestra un formulario generado dinámicamente (inputs según `fieldType`: string, number, boolean, date, richtext, json, etc.).
4. Rellena título y campos y envía: se crea el contenido en Core (Fase 5) y se redirige al listado. Las validaciones se aplican vía Zod.

### Probar Contexto – Proyecto y entorno (Fase 3)

1. Levanta **Core API** (y Identity para estar logueado):
   ```bash
   dotnet run --project src/Services/Core/IODA.Core.API/IODA.Core.API.csproj
   ```
   Core suele escuchar en **http://localhost:5269**. Asegúrate de tener al menos un proyecto (y opcionalmente entornos) en la base de datos del Core.
2. En el frontend, tras iniciar sesión, en la barra superior aparecen los selectores **Proyecto** y **Entorno**.
3. Elige un proyecto: se cargan sus entornos y puedes elegir uno. El contexto se guarda en `sessionStorage` y se restaura al recargar.
4. Otros módulos pueden leer `currentProjectId` y `currentEnvironmentId` desde `useContextStore()` para las llamadas a Core.

### Probar Authorization (Fase 2)

1. Levanta **Identity API** y **Authorization API** (ver `docs/COMO_PROBAR_FASE_2.md` y `docs/COMO_PROBAR_FASE_3.md`).
2. **Asignar permisos**: el CMS no tiene pantalla para roles/permisos; se configuran en la **Authorization API** (Swagger). Sigue **`docs/COMO_ASIGNAR_PERMISOS_CMS.md`** para crear los permisos `content.edit` y `content.publish`, un rol con esos permisos y asignar el rol a tu usuario (UserId de Identity).
3. En el frontend: inicia sesión. En la home verás el botón "Solo si tienes content.edit" solo si tienes ese permiso.
4. El enlace "Ir a Publicar (requiere content.publish)" lleva a `/publish`; si no tienes `content.publish` serás redirigido a `/forbidden`.

### Probar Auth (Fase 1)

1. **Levanta Identity API y Authorization API** (si no, verás "ERR_CONNECTION_REFUSED" al hacer login o al comprobar permisos como `project.create`):
   ```bash
   # Desde la raíz del repo (ioda)
   dotnet run --project src/Services/Identity/IODA.Identity.API/IODA.Identity.API.csproj
   dotnet run --project src/Services/Authorization/IODA.Authorization.API/IODA.Authorization.API.csproj
   ```
   - Identity suele escuchar en **http://localhost:5270**, Authorization en **http://localhost:5271** (revisa `launchSettings.json` de cada proyecto).
   - Si usas **Docker** para las APIs, los puertos en el host son 5002 (Identity) y 5003 (Authorization). En `frontend/.env` define `VITE_IDENTITY_API_URL=http://localhost:5002` y `VITE_AUTHORIZATION_API_URL=http://localhost:5003`.
   - Tras cambiar `.env`, reinicia `npm run dev`.
2. En el frontend: `npm run dev` → abre http://localhost:5173.
3. Serás redirigido a `/login`. Crea una cuenta en "Crear cuenta" o inicia sesión si ya tienes usuario.
4. Tras login correcto irás a la home; "Cerrar sesión" limpia la sesión y vuelve a `/login`.
5. Recarga la página estando logueado: la sesión se restaura con el refresh token (rehydrate).

## Plan de fases

Ver **FRONTEND_NEXT_STEPS.md** en la raíz del repo para el plan paso a paso (Fase 0 → Auth → Authorization → Contexto → Schema → Content → Publishing → opcional Indexing).

## Referencias

- Backend: ver `NEXT_STEPS.md` y `docs/COMO_PROBAR_FASE_*.md` para levantar las APIs.
- Plan frontend: `FRONTENT_PLAN_DE_TRABAJO.md`.
