# CMS Frontend (Admin Panel) – Pasos y deuda técnica (Fase de mejoras)

Este documento concentra las tareas pendientes del **frontend del CMS (panel de administración)**: deuda técnica de la Fase de creación inicial e ítems del plan del panel de administración que corresponden al admin (React + TypeScript).

**Referencias:** `docs/FASE_DE_CREACION_INICIAL/FRONTEND_NEXT_STEPS.md`, `docs/FASE_DE_CREACION_INICIAL/FRONTENT_PLAN_DE_TRABAJO.md`, `docs/FASE_DE_MEJORAS/PLAN_DE_TRABAJO.md`.

---

## 1. Deuda técnica heredada (Fase de creación inicial)

### 1.1 Módulo Schema

- [x] **Resolver herencia de esquemas:** Backend expone `parentSchemaId` e `inheritedFields` en ContentSchemaDto. Frontend: SchemaDesignerPage permite seleccionar schema padre al crear; DynamicForm combina campos heredados + propios para formularios de creación/edición. Protección contra ciclos en backend.
- [x] Cache y metadatos ya cubiertos (getSchemaSync, store)

### 1.2 Dynamic Form Engine

- [x] **Listas y referencias:** Campo tipo `list` (textarea un valor por línea → array); tipo `reference` (input texto); tipo `media` con MediaPicker
- [x] **Campos complejos:** MediaPicker cuando fieldType es `media` (galería + subida); Rich text sigue como textarea (editor enriquecido opcional después)
- [x] Resto de tipos básicos ya implementados (string, number, boolean, date, enum, json)

### 1.3 Publishing Module

- [x] **Historial de cambios:** Backend expone `GET /api/projects/{projectId}/content/{contentId}/versions` (lista todas las versiones ordenadas desc). Frontend: sección colapsable "Historial de versiones" en EditContentPage con badge de estado, fecha, campos expandibles por versión y botón "Restaurar esta versión".

### 1.4 Navegación / Contexto

- [x] **Selección de sitio:** Selector de sitio en layout (Proyecto → Entorno → Sitio); persistencia en sessionStorage; carga de sitios por proyecto (opcional por entorno)
- [x] Proyecto y entorno ya implementados con persistencia en sessionStorage

### 1.5 Media Module

- [x] **Subida de media:** coreApi.uploadMedia (multipart); MediaPicker con input file
- [x] **Preview:** Galería de miniaturas (imágenes vía URL de file); no-imagen muestra nombre
- [x] **Selector desde librería:** MediaPicker lista media del proyecto y permite elegir uno (o subir nuevo)
- [x] **Metadatos:** DTO MediaItem (fileName, displayName, contentType, etc.) expuesto; UI muestra nombre/archivo seleccionado

---

## 2. Flujo de acceso mejorado (Access Flow)

Objetivo: guiar al usuario paso a paso (login → contexto → trabajo). Nada accesible sin contexto completo.

- [x] **Panel de Proyectos:** Paso 1 en Home: listar proyectos, crear nuevo (si tiene permiso `project.create`), seleccionar proyecto activo
- [x] **Panel de Entornos:** Paso 2 en Home: selección explícita de entorno; opción “Crear entorno” (POST Core API)
- [x] **Selección de Sitio:** Sitio opcional en barra superior (ya implementado en Fase 1); no bloquea el flujo
- [x] **Dashboard:** Tras proyecto + entorno se muestra Dashboard en Home: contexto actual, accesos rápidos (Contenido, Crear contenido, Publicar, Búsqueda)
- [x] **Bloquear acceso:** Componente `RequireContext` redirige a "/" si faltan proyecto o entorno; rutas /content, /content/new, /content/:id/edit, /publish, /search protegidas

---

## 3. Routing *(opcional según despliegue)*

- [x] **Hash-based routing** (`/#/dashboard`, `/#/content`, etc.) si se decide por hosting estático (CDN, S3, GitHub Pages) y evitar dependencia de server-side routing
- [x] Documentar decisión (hash vs browser history) según despliegue

---

## 4. Usuarios y autenticación (Admin) *(opcional / al final)*

- [x] **Super Admin:** Primer usuario registrado se detecta como SuperAdmin (backend `GET /api/auth/setup-status`, `RegisterResultDto.IsFirstUser`). Frontend auto-crea rol SuperAdmin con todos los permisos y asigna acceso al usuario.
- [x] **Auto-registro:** Configuración `SelfRegistration:Enabled` en `appsettings.json` de Identity. Backend rechaza registro (403) si está deshabilitado (excepto primer usuario). Frontend oculta link de registro y muestra mensaje si está deshabilitado.
- [x] **Creación manual de usuarios** por admin (UI que consuma Identity API)
- [x] **Listado de usuarios** (GET /api/auth/users) y formulario de alta (email, contraseña, nombre) en `/admin/users`
- [x] Permisos `user.list` y `user.create`: crear en Roles y permisos y asignar a un rol para ver/crear usuarios. Roles y proyectos asignados se gestionan en **Roles y permisos** (reglas de acceso por userId).

---

## 5. Roles y permisos (Admin UI)

- [x] **Crear roles** desde la UI (consumiendo Authorization API)
- [x] **Asignar permisos granulares** a roles
- [x] **Asignar roles a usuarios** (reglas de acceso con contexto: proyecto, entorno, schema, estado)
- [x] **Visualización clara de permisos efectivos** (por proyecto, entorno, sitio, tipo de contenido, estado)
- [x] Prevención de acciones no permitidas (deshabilitar botones/rutas según check)
- El backend sigue siendo la fuente de verdad; el frontend solo refleja permisos.

---

## 6. Dashboard con drill-down *(opcional / después del flujo base)*

- [ ] **Dashboard principal:** Widgets (contenido por estado, actividad reciente, publicaciones recientes, errores de validación, usuarios activos)
- [ ] **Drill-down:** Click en “Contenido en Review” → lista filtrada → por tipo “Video” → lista de videos → click en uno → editor. Refinar contexto sin multiplicar pantallas.

---

## 7. Diseño de componentes (Schema-Driven UI)

- [x] **MediaPicker** cuando exista Media API (integrado en DynamicField para tipo `media`; usa Core API media)
- [x] **ReferenceSelector** para campos referencia (convención: `referenceContentType`/`referenceSchemaId` en validationRules; lista contenido del proyecto)
- [x] **List / Repeater** para campos lista (ListRepeater: añadir/quitar ítems; sustituye textarea “un valor por línea”)
- [ ] **Custom blocks** si los esquemas los definen *(opcional / al final)*
- Principio: componentes por campo/bloque, render por esquema, validaciones dinámicas.

---

## 8. Diseñador de Schemas (Schema Designer) *(opcional / depende de Core API)*

- [x] **Crear tipo de contenido** desde la UI (llamando a Core API POST /api/projects/{projectId}/schemas)
- [x] **Agregar campos dinámicamente** (nombre, tipo, requerido, ayuda; validaciones/UI hint opcionales vía validationRules)
- [x] **Ordenar campos** (botones ↑/↓ en el diseñador; displayOrder enviado al crear)
- [x] **Previsualizar formulario** generado por el schema (vista previa con labels y placeholders según campos)
- [x] Core API expone creación de schemas; edición/borrado no implementados en backend (solo crear desde UI)

---

## 9. Componentes en Schemas (Page Builder) *(opcional / al final)*

- [ ] Permitir que un schema defina **estructuras de página** (Homepage, Landing, Section, Custom page) mediante bloques y layouts
- [ ] Enfoque **Schema + Blocks**, no WYSIWYG libre
- [ ] Puede requerir extensión del modelo en Core (bloques, layouts) – coordinar con BACKEND_STEPS

---

## 10. Gestión de Sitios (UI) *(opcional / al final; depende de backend Sitios)*

Depende de que el backend exponga Sitios (ver BACKEND_STEPS).

- [x] Crear sitio (dominio, subdominio, subruta)
- [x] Asociar tema a sitio
- [x] Activar / desactivar sitio
- [ ] Asignar usuarios al sitio (si el modelo lo soporta)

---

## 11. Mensajes y UX

- [ ] **Mensajes “sin permiso”** consistentes en toda la app (reutilizar o extender patrones de Forbidden / Can)
- [ ] Mantener uso de LoadingSpinner y ErrorBanner en pantallas principales
- [ ] Accesibilidad básica (labels, contraste, teclado) donde falte

---

## 12. Integración con API Gateway / BFF — Plan complementario

Cuando el backend exponga un BFF (ver BACKEND_STEPS, sección 8):

- [ ] **Consumir el BFF** en lugar de llamar directamente a Core, Identity, Access Rules y Publishing desde el Admin.
- [ ] Mantener un único punto de entrada para autenticación y contexto (JWT, proyecto, entorno, sitio).
- [ ] Ajustar manejo de errores según las respuestas normalizadas del BFF.

---

## 13. Feature flags en UI — Plan complementario *(opcional / fase avanzada)*

Si el backend implementa Feature Flags (BACKEND_STEPS, sección 11):

- [ ] Consumir endpoint o contexto de flags (por proyecto/entorno/sitio) para mostrar u ocultar funcionalidades (auto-registro, workflows experimentales, campos opcionales).
- [ ] Evitar redeploy del frontend para activar/desactivar features cuando el backend lo soporte.

---

## 14. Orden sugerido

### Prioritario (flujo y deuda cercana)

1. **Deuda técnica cercana:** Herencia de esquemas (si backend da soporte), listas/referencias en formularios, Media module cuando exista Media API.
2. **Flujo de acceso:** Reforzar proyecto → entorno como flujo guiado; añadir dashboard como entrada.
3. **Roles y permisos en UI:** Crear/asignar roles y permisos consumiendo Authorization API.
4. **Mensajes y UX:** Permisos consistentes, LoadingSpinner/ErrorBanner, accesibilidad básica.

### Opcional o al final

5. **Dashboard y drill-down:** Widgets y navegación contextual (cuando se priorice métricas).
6. **Hash-based routing:** Solo si se decide hosting estático (CDN, S3, etc.).
7. **Usuarios y autenticación (Admin):** Super Admin, auto-registro, creación manual de usuarios.
8. **Schema Designer:** Cuando Core exponga CRUD de schemas o se defina el contrato.
9. **Sitios y selección de sitio:** Cuando el backend soporte Sitios.
10. **Integración BFF:** Consumir API Gateway/BFF cuando exista (plan complementario).
11. **Page Builder / Bloques:** Tras definir modelo y API con backend.
12. **Feature flags en UI:** Cuando el backend exponga flags (plan complementario, opcional).
13. **Historial de cambios (Publishing), Custom blocks:** Según necesidad del producto.

---

**Última actualización:** 2026-01-24  
**Origen:** FRONTEND_NEXT_STEPS, FRONTENT_PLAN_DE_TRABAJO, PLAN_DE_TRABAJO, [PLAN_COMPLEMENTARIO.md](./PLAN_COMPLEMENTARIO.md).
