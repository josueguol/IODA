# IODA CMS — Reordenamiento de Vistas y Flujos

## Objetivo
Reorganizar las vistas y el flujo del CMS para que el acceso sea más claro, guiado y consistente, manteniendo visibles los elementos globales mientras el usuario esté autenticado.

El CMS debe guiar al usuario paso a paso:
**Login → Proyecto → Entorno → Sitio → Dashboard**

---

## Estado actual (referencia visual)
Actualmente:
- El selector de proyecto, entorno y sitio conviven con el dashboard.
- La barra superior ya existe, pero su uso no está completamente alineado con el flujo.
- El dashboard aparece incluso cuando el contexto aún no está completamente definido.

---

## Backlog de tareas (basado en `frontend/`)

Esta lista traduce el **flujo deseado** a cambios concretos en el repo, con referencia a archivos existentes.

### A. Ajustar el flujo guiado (sin crear páginas nuevas)

- [ ] **Home como “wizard” real (una sola vista por paso):** En `frontend/src/app/pages/HomePage.tsx`, renderizar **solo un paso a la vez**:
  - Si **no hay proyecto** → mostrar solo “Paso 1”.
  - Si hay proyecto pero **no hay entorno** → mostrar solo “Paso 2”.
  - Si hay proyecto+entorno pero **no hay sitio** → mostrar solo “Paso 3”.
  - Si hay proyecto+entorno+sitio → mostrar **solo Dashboard**.
- [ ] **Dashboard solo con contexto completo (incluye sitio):** Cambiar la condición actual para que el Dashboard requiera también `currentSiteId`.
  - Archivo: `frontend/src/app/pages/HomePage.tsx`
- [ ] **Definir qué rutas exigen sitio:** Decidir si “Contenido/Crear contenido/Publicar” requieren o no `sitio`.
  - Si **sí**: crear `RequireFullContext` (proyecto+entorno+sitio) y aplicarlo en `frontend/src/app/App.tsx`.
  - Si **no**: mantener `RequireContext` (proyecto+entorno) para contenido, pero el Dashboard seguirá exigiendo sitio.

### B. Top Bar persistente pero alineada al flujo

Actualmente la top bar vive en `frontend/src/app/components/AppLayout.tsx`.

- [ ] **Separar “selector de contexto” del Top Bar:** El plan pide que el Top Bar sea global (marca, búsqueda, config, usuario) y que la **selección** sea parte del wizard.
  - En `AppLayout.tsx`, mover los `<select>` de proyecto/entorno/sitio a un “ContextBar” secundario (debajo del Top Bar) **o** reemplazarlos por un resumen “Contexto actual: …” + acción “Cambiar” que lleve al wizard.
  - Regla: el Top Bar no debe sentirse como el “setup”; el setup vive en las pantallas de selección.
- [ ] **Ocultar/Deshabilitar acciones del CMS mientras no haya contexto completo:** En `AppLayout.tsx`, ocultar (o deshabilitar) accesos como `Contenido`, `Crear contenido`, `Publicar`, `Sitios`, `Schemas`, `Usuarios`, etc. hasta que el contexto esté completo según el flujo (ideal: proyecto+entorno+sitio).
- [ ] **Buscador global (solo cuando aplica):** si la búsqueda requiere contexto, deshabilitar/ocultar el input hasta que el contexto esté completo (o aclarar “Selecciona contexto para buscar”).
- [ ] **Menú de usuario:** Reemplazar el botón “Cerrar sesión” por un dropdown (icono/usuario) con:
  - “Cerrar sesión”
  - (opcional) “Mi perfil”
- [ ] **Menú “Configuración” del CMS (placeholder):** Agregar un dropdown “Configuración” (aunque al inicio sea estático) con links a:
  - “Roles y permisos” (`/admin/roles`)
  - “Usuarios” (`/admin/users`)
  - “Schemas” (`/admin/schemas`)
  - “Sitios” (`/sites`)

### C. Alternativa con rutas dedicadas (si quieres URLs explícitas por paso)

Si se prefiere que cada paso sea una ruta propia (más claro para navegación y QA):

- [ ] **Crear páginas:** `SelectProjectPage`, `SelectEnvironmentPage`, `SelectSitePage` en `frontend/src/app/pages/`.
- [ ] **Actualizar routing:** En `frontend/src/app/App.tsx`:
  - `/` → redirigir al paso correspondiente según contexto.
  - `/setup/project`, `/setup/environment`, `/setup/site`
  - `/dashboard` → solo con contexto completo.
- [ ] **HomePage** quedaría como `/dashboard` (solo widgets).

### D. Detalles UX a cerrar

- [ ] **Consistencia de mensajes/estados:** Unificar “no hay X”, loaders y errores (reusar `LoadingSpinner`/`ErrorBanner`).
- [ ] **Permisos:** Revisar consistencia de `Can(permission)` en crear proyecto/entorno/sitio (ya existe; validar que no haya acciones visibles sin permiso).
- [ ] **Accesibilidad:** En forms del wizard, usar `label` correctamente (ideal `htmlFor` + `id`) y navegación por teclado.
- [ ] **Reglas de limpieza de contexto:** Confirmar en `context-store` que al cambiar proyecto se limpian entorno+sitio, y al cambiar entorno se limpia sitio. Si falta, implementarlo y reflejarlo en UI.
- [ ] **Redirecciones consistentes:** si el usuario entra a una ruta que requiere contexto y no lo tiene, redirigir al paso correcto (no solo a `/`).
  - Archivo: `frontend/src/app/App.tsx` (guards) y/o helper de “resolver siguiente paso” usado por Home.

### E. Dashboard como panel de acciones (widgets)

- [ ] **Convertir accesos del header a widgets del dashboard:** Los links de acción deberían estar en el Dashboard (no “siempre visibles” en la barra).
  - Archivo: `frontend/src/app/pages/HomePage.tsx` (sección Dashboard)
- [ ] **Widgets con permisos:** mostrar/ocultar widgets según permisos (`Can(permission)`), por ejemplo:
  - `Crear contenido`, `Publicar`, `Roles y permisos`, `Usuarios`, `Diseñador de schemas`, `Gestión de sitios`.
- [ ] **Widget “Contexto actual”:** mostrar el contexto seleccionado y una acción “Cambiar” para volver al wizard.

### F. Criterios de aceptación (QA manual)

- [ ] **Login → setup:** al iniciar sesión, siempre se ve layout autenticado + paso 1 (si no hay contexto).
- [ ] **Setup por pasos:** nunca se ven pasos 2/3 si falta el paso previo; no se ve Dashboard hasta tener proyecto+entorno+sitio.
- [ ] **Cambio de contexto:** al cambiar proyecto, se limpian entorno/sitio; al cambiar entorno, se limpia sitio; la UI refleja el nuevo estado.
- [ ] **Permisos:** sin permiso de crear, no aparece el botón; sin permiso de publicar, no aparece widget/link.
- [ ] **Tema claro/oscuro:** contraste correcto en wizard, dashboard y top bar.

## Flujo deseado (alto nivel)

1. Login (sin cambios)
2. Layout autenticado (barra superior persistente)
3. Selección / creación de Proyecto
4. Selección / creación de Entorno
5. Selección / creación de Sitio
6. Dashboard (solo cuando el contexto está completo)

---

## 1. Login
- **Estado:** Correcto, no requiere cambios.
- **Resultado esperado:**  
  Al autenticar, el usuario entra al **layout principal del CMS**, no directamente al dashboard.

---

## 2. Layout autenticado (barra superior persistente)

### Comportamiento
Una vez logueado, **la barra superior debe mostrarse siempre**, independientemente del paso en el que esté el usuario.

### Contenido de la barra (Top Bar)
- Nombre del CMS (ej. `IODA CMS`)
- Buscador global
- Menú desplegable de configuración del CMS
- Icono de usuario
  - Al hacer click:
    - Opción: **Cerrar sesión**

⚠️ Esta barra **debe preservarse mientras el usuario esté logueado**.

---

## 3. Pantalla: Selección o creación de Proyecto

### Cuándo se muestra
- Si el usuario **no ha seleccionado proyecto activo**.

### Contenido principal (centrado)
- Lista de proyectos disponibles
- Opción para **crear proyecto** (solo si tiene permisos)

### Comportamiento
- No se muestra dashboard
- No se muestran widgets de contenido
- Solo el selector de proyecto y acciones relacionadas

---

## 4. Pantalla: Selección o creación de Entorno

### Cuándo se muestra
- Cuando ya hay proyecto seleccionado
- Pero **no hay entorno activo**

### Contenido principal
- Lista de entornos del proyecto (ej. Development, QA, Production)
- Opción para **crear entorno** (si tiene permisos)

### Comportamiento
- Mantener la barra superior
- No mostrar dashboard
- No mostrar opciones de contenido

---

## 5. Pantalla: Selección o creación de Sitio

### Cuándo se muestra
- Cuando hay proyecto y entorno seleccionados
- Pero **no hay sitio activo**

### Contenido principal
- Lista de sitios asociados al proyecto + entorno
- Información del dominio / path
- Opción para **crear sitio** (si tiene permisos)

### Nota
- El sitio puede ser opcional en algunos flujos, pero:
  - **El dashboard solo aparece cuando hay sitio seleccionado**

---

## 6. Dashboard (contexto completo)

### Condición obligatoria
El dashboard **solo debe mostrarse si existe**:
- Proyecto seleccionado
- Entorno seleccionado
- Sitio seleccionado

### Estructura
Dashboard basado en **widgets**, por ejemplo:
- Crear contenido
- Gestionar contenido
- Schemas
- Publicar
- Roles / permisos
- Gestión de sitios

### Objetivo del dashboard
Ser un **panel de acciones**, no un selector de contexto.

---

## Reglas clave de UX

- ❌ No mostrar dashboard sin contexto completo
- ✅ Guiar al usuario paso a paso
- ✅ Mantener siempre visible la barra superior
- ✅ Separar claramente:
  - Selección de contexto
  - Acciones del CMS

---

## Resumen del flujo ideal

```text
Login
  ↓
Layout autenticado (Top Bar)
  ↓
Seleccionar / Crear Proyecto
  ↓
Seleccionar / Crear Entorno
  ↓
Seleccionar / Crear Sitio
  ↓
Dashboard con widgets
