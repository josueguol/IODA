# 🖥️ Frontend para CMS Genérico (Schema-Driven)

## 1. Objetivo del Frontend

Construir un **Frontend desacoplado, dinámico y extensible**, capaz de:

* Renderizar contenido desde esquemas
* Gestionar múltiples servicios backend
* Adaptarse a nuevos tipos de contenido sin cambios de código
* Aplicar reglas de autorización y permisos
* Escalar a nivel enterprise

---

## 2. Stack Recomendado

* Lenguaje: **TypeScript**
* Framework: **React**
* Build tool: **Vite**
* Estado global: Zustand / Redux Toolkit
* Formularios: React Hook Form
* UI Components: Headless UI / Radix
* Estilos: CSS Modules / Tailwind (opcional)
* Autenticación: JWT
* API: REST (base), GraphQL (opcional)

---

## 3. Arquitectura del Frontend

### Principios

* Modular
* Feature-based
* Separación UI / lógica
* Sin conocimiento del dominio backend

Estructura base:

```
frontend/
├── app/
├── modules/
├── shared/
├── services/
├── schemas/
├── auth/
└── infrastructure/
```

---

## 4. Módulos Principales

### 4.1 Auth Module

**Responsabilidad:** Identidad del usuario.

Tareas:

* [x] Login
* [x] Manejo de JWT
* [x] Refresh token
* [x] Guardado seguro de sesión
* [x] Logout

---

### 4.2 Authorization Module

**Responsabilidad:** Control de acceso en UI.

Tareas:

* [x] Resolver permisos por acción
* [x] Ocultar / mostrar acciones
* [x] Validar acceso a rutas
* [x] Sin lógica de negocio

---

### 4.3 Schema Module

**Responsabilidad:** Consumir y cachear esquemas.

Tareas:

* [x] Obtener esquemas desde CMS Core
* [x] Cachear esquemas
* [ ] Resolver herencia de esquemas (pendiente)
* [x] Exponer metadatos a otros módulos (store + getSchemaSync)

---

### 4.4 Dynamic Form Engine

**Responsabilidad:** Renderizar formularios desde esquemas.

Tareas:

* [x] Mapear field types → componentes UI (string, number, boolean, date, richtext, json, enum, reference)
* [x] Aplicar validaciones (Zod desde ValidationRules: required, min/max, pattern)
* [ ] Manejar listas y referencias (referencia como input por ahora)
* [ ] Manejar campos complejos (rich text como textarea; media pendiente)

---

### 4.5 Content Module

**Responsabilidad:** CRUD de contenido.

Tareas:

* [x] Crear contenido genérico (DynamicForm + createContent)
* [x] Editar contenido (DynamicForm + updateContent)
* [x] Versionado visual (estado y vN en UI)
* [x] Listado por tipo (filtro contentType + status)
* [x] Filtros dinámicos (schema, Draft/Published)

---

### 4.6 Publishing Module

**Responsabilidad:** Flujo de estados.

Tareas:

* [x] Mostrar estado actual (Draft/Published en edición de contenido)
* [x] Acciones permitidas (Solicitar publicación en edición; Aprobar/Rechazar en /publish, según permiso)
* [ ] Historial de cambios (pendiente)
* [x] Feedback de validación (validationErrors y rejectionReason en lista de solicitudes)

---

### 4.7 Media Module

**Responsabilidad:** Gestión de archivos.

Tareas:

* [ ] Subida de media
* [ ] Preview
* [ ] Selección desde librería
* [ ] Metadatos

---

### 4.8 Navigation / Context Module

**Responsabilidad:** Contexto del CMS.

Tareas:

* [x] Selección de proyecto
* [x] Selección de entorno
* [ ] Selección de sitio (pendiente)
* [x] Persistencia de contexto

---

## 5. Comunicación con Backend

* API clients desacoplados
* Un client por servicio
* Manejo centralizado de errores
* Retries y timeouts

---

## 6. Seguridad

* Tokens nunca acoplados a lógica
* Validación por middleware
* Autorización solo como UI hint
* Backend siempre manda

---

## 7. Escalabilidad

* Posible micro-frontend
* Plugins de UI
* White-label
* Multi-tenant

---

## 8. Prompt para Editor de Código con IA

```
Actúa como un arquitecto frontend senior especializado en React y TypeScript.

Estoy construyendo el frontend de un CMS genérico y schema-driven.
El frontend no conoce tipos de contenido, solo esquemas dinámicos.

Necesito:
- Arquitectura modular
- Renderizado dinámico de formularios
- Gestión de permisos
- Integración con múltiples microservicios
- Código limpio y desacoplado
- Escalabilidad enterprise

Ayúdame a implementar los módulos indicados siguiendo buenas prácticas modernas.
```

---

## 9. Progreso actual

**Fases completadas (según FRONTEND_NEXT_STEPS.md):**

- **Fase 0** – Fundamentos (proyecto Vite, estructura, env, cliente HTTP, README).
- **Fase 1** – Auth (Identity API: login, register, refresh, store Zustand, rutas protegidas, rehydrate).
- **Fase 2** – Authorization (checkAccess, usePermission, Can, ProtectedRouteByPermission).
- **Fase 3** – Contexto (Core API client: proyectos/entornos; store de contexto; selectores en AppLayout; persistencia en sessionStorage).
- **Fase 4** – Schema y formularios dinámicos (Core API schemas; schema store con cache; DynamicForm + DynamicField con React Hook Form y Zod; página Crear contenido en `/content/new`).
- **Fase 5** – Content CRUD (Core API content: list, get, create, update, delete; lista con filtros y paginación; crear con DynamicForm y entorno; editar y eliminar con confirmación).
- **Fase 6** – Publishing (Publishing API client; en edición de contenido: "Solicitar publicación" si Draft y permiso; pantalla Solicitudes de publicación con listado, filtros, Aprobar/Rechazar; feedback validationErrors/rejectionReason).
- **Fase 7** – Indexing y refinamientos (Indexing API client; pantalla de búsqueda `/search` con resultados y paginación; barra de búsqueda en AppLayout; componentes compartidos LoadingSpinner y ErrorBanner para manejo uniforme de errores y loading).

**Completado:** Todas las fases principales (0-7). Pendiente: Media module (requiere Media API en backend).

---

## 10. Nota Final

Este frontend no es una app CRUD.
Es un **motor de interpretación de esquemas**.
