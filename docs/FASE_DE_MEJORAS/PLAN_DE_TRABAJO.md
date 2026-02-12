# 🧩 Panel de Administración – CMS Genérico (Schema-Driven)

## 0. Responsabilidades y documentos de pasos

El trabajo de esta fase de mejoras está repartido en **tres ámbitos**, cada uno con su propio documento de tareas:

| Ámbito | Documento | Contenido principal |
|--------|-----------|---------------------|
| **Backend** | [BACKEND_STEPS.md](./BACKEND_STEPS.md) | Mejoras Core ✅; Fase 6 (Schema Validation, Media), Sitios; API Gateway/BFF, Content Delivery API, Audit Log, Event Tracing, Feature Flags (plan complementario); tests opcional al final. |
| **CMS Frontend** | [CMS_STEPS.md](./CMS_STEPS.md) | Deuda técnica (herencia, listas/referencias, Media), flujo de acceso, dashboard, roles/permisos UI, Schema Designer, sitios; integración BFF y feature flags (plan complementario). |
| **Theme Frontend** | [THEMES_STEPS.md](./THEMES_STEPS.md) | Estructura temas, Handlebars, contrato JSON, Content Delivery API (plan complementario), registro y asociación tema–sitio, build/deploy, preview. |

La **deuda técnica** de la fase de creación inicial (docs en `FASE_DE_CREACION_INICIAL/`) y las **extensiones del plan complementario** ([PLAN_COMPLEMENTARIO.md](./PLAN_COMPLEMENTARIO.md)) están integradas en estos tres archivos. Antes de codificar, conviene revisar el plan completo y el documento correspondiente al ámbito en el que se trabaje.

---

## 1. Objetivo del Panel de Administración

Construir un **Admin Panel modular, escalable y orientado a flujos**, capaz de:

* Guiar al usuario paso a paso (login → contexto → trabajo)
* Administrar proyectos, entornos, sitios y contenido
* Diseñar esquemas dinámicos
* Gestionar roles y permisos
* Visualizar información mediante dashboards con drill-down
* Separar claramente **gestión (admin)** de **renderizado (themes)**

---

## 2. Flujo de Acceso Mejorado (Access Flow)

### Flujo obligatorio (en orden)

1. **Login**

   * Si el usuario no está autenticado
   * Token JWT requerido

2. **Panel de Proyectos**

   * Listar proyectos disponibles
   * Crear nuevo proyecto (si tiene permiso)
   * Seleccionar proyecto activo

3. **Panel de Entornos**

   * Seleccionar entorno:

     * local
     * qa
     * staging
     * prod
   * El entorno define:

     * reglas
     * workflows
     * permisos
     * publicación

4. **Selección de Sitio**

   * Cada proyecto puede tener múltiples sitios
   * El sitio define:

     * dominio
     * tema
     * estructura
     * contenido visible

5. **Dashboard**

   * Entrada principal de trabajo
   * Métricas, accesos rápidos y navegación contextual

👉 **Nada es accesible sin contexto completo (Proyecto + Entorno + Sitio)**

---

## 3. Gestión de Sitios

### Funcionalidades

* [ ] Crear sitio
* [ ] Asignar:

  * Dominio (example.com)
  * Subdominio (blog.example.com)
  * Subruta (example.com/blog)
* [ ] Asociar tema
* [ ] Activar / desactivar sitio
* [ ] Asignar usuarios al sitio

### Reglas clave

* Un sitio pertenece a un proyecto
* Un sitio puede existir en varios entornos
* El contenido se filtra por sitio + entorno

---

## 4. Routing – Hash-Based Client-Side Routing

### Decisión técnica

* Uso de **hash routing** (`/#/dashboard`)
* Evita dependencia de server-side routing
* Compatible con:

  * CDN
  * S3
  * GitHub Pages
  * Cualquier hosting estático

### Beneficios

* No requiere configuración especial del servidor
* Ideal para Admin Panels
* Cero conflictos con backends desacoplados

---

## 5. Usuarios y Autenticación

### Super Admin

* El **primer usuario registrado** es automáticamente:

  * `SuperAdmin`
* Capacidades:

  * Control total del sistema
  * Puede habilitar / deshabilitar auto-registro
  * Puede crear proyectos
  * Puede asignar roles

---

### Registro de Usuarios

#### Modos disponibles

* [ ] Auto-registro habilitado (por defecto)
* [ ] Auto-registro deshabilitado (solo invitaciones)
* [ ] Creación manual por SuperAdmin

#### Datos básicos de usuario

* Email
* Nombre
* Roles
* Proyectos asignados

---

## 6. Roles y Permisos (Admin UI)

### Alcance de permisos

Los permisos pueden definirse por:

* Proyecto
* Entorno
* Sitio
* Tipo de contenido
* Estado del contenido

### Funcionalidades UI

* [ ] Crear roles
* [ ] Asignar permisos granulares
* [ ] Asignar roles a usuarios
* [ ] Visualización clara de permisos efectivos
* [ ] Prevención de acciones no permitidas

⚠️ **El frontend solo refleja permisos, el backend decide.**

---

## 7. Dashboard con Drill-Down

### Dashboard Principal

Widgets sugeridos:

* Contenido por estado
* Actividad reciente
* Publicaciones recientes
* Errores de validación
* Usuarios activos

---

### Drill-Down (profundo y útil)

Ejemplo:

* Click en “Contenido en Review”
  → Lista filtrada
  → Click en tipo “Video”
  → Lista de videos
  → Click en uno
  → Editor directo

👉 El drill-down **no navega pantallas nuevas**, refina contexto.

---

## 8. Diseño de Componentes (Schema-Driven UI)

### Principio clave

> **Los componentes no representan pantallas, representan campos y bloques.**

### Tipos de componentes

* Input
* Textarea
* RichText
* MediaPicker
* ReferenceSelector
* List / Repeater
* Custom blocks

Cada componente:

* Se renderiza por esquema
* Aplica validaciones dinámicas
* No conoce el dominio

---

## 9. Diseñador de Schemas (Schema Designer)

### Funcionalidades clave

* [ ] Crear tipo de contenido
* [ ] Agregar campos dinámicamente
* [ ] Definir:

  * tipo
  * requerido
  * validaciones
  * UI hint
* [ ] Ordenar campos
* [ ] Previsualizar formulario

---

### Componentes en Schemas (Page Builder)

Permitir que un schema defina **estructuras de página**:

Ejemplo:

* Homepage
* Landing
* Section
* Custom page

Mediante:

* Bloques
* Layouts
* Componentes reutilizables

👉 Recomendación:
**Schema + Blocks**, no WYSIWYG libre.

---

## 10. Temas (Themes)

### Principio

> **El Admin genera contenido.
> Los temas SOLO lo renderizan.**

---

### Estructura de Themes

```
themes/
├── default/
│   ├── templates/
│   ├── partials/
│   ├── assets/
│   └── theme.json
```

---

### Tecnologías

* Handlebars (render)
* CSS
* JS (sin lógica CMS)
* Datos inyectados desde API

---

### Registro de Themes

* [ ] Registrar tema en Admin
* [ ] Asociar tema a sitio
* [ ] Versionar temas
* [ ] Cambiar tema sin tocar contenido

---

## 11. Separación Admin vs Themes

* Admin Panel → React + TypeScript
* Themes → Handlebars + CSS + JS
* Carpetas separadas
* Deploy independiente
* Contrato común: **JSON de contenido**

---

## 12. Prompt para Editor de Código con IA

```
Actúa como un arquitecto frontend senior especializado en React y TypeScript.

Estoy desarrollando un panel de administración para un CMS schema-driven y distribuido.

Necesito:
- Flujo de acceso guiado (login → proyecto → entorno → sitio → dashboard)
- Hash-based routing
- Gestión de usuarios, roles y permisos
- Dashboard con drill-down
- Componentes UI compatibles con esquemas dinámicos
- Diseñador de schemas
- Separación total entre Admin Panel y Themes
- Sistema de temas basado en Handlebars, CSS y JS

Diseña la arquitectura del frontend, los módulos, el routing y los componentes siguiendo buenas prácticas enterprise.
```

---

## 13. Nota Final

Este Admin Panel **no es un CRUD**.
Es una **plataforma de orquestación de contenido y contexto**.

Si este panel se hace bien:

* El CMS se vuelve usable
* El producto se vuelve vendible
* La arquitectura se justifica

---

## 14. Extensiones arquitectónicas y capas de madurez (Plan complementario)

Estas capas **complementan** el plan sin introducir nuevos dominios de negocio: refuerzan gobernanza, experiencia del frontend y operación. La implementación es incremental. Las tareas concretas están repartidas en **BACKEND_STEPS**, **CMS_STEPS** y **THEMES_STEPS**.

### 14.1 API Gateway / Backend For Frontend (BFF)

- **Objetivo:** Centralizar el acceso del CMS Admin a los microservicios (Core, Identity, Access Rules, Publishing), reduciendo acoplamiento.
- **Responsabilidades:** Agregar y orquestar llamadas; validar contexto (Proyecto, Entorno, Sitio); normalizar respuestas; manejo centralizado de errores; verificación JWT.
- **NO debe:** Persistir datos, lógica de negocio profunda, reemplazar servicios existentes.
- **Beneficios:** Frontend desacoplado de microservicios; permisos evaluados en un solo punto.

### 14.2 Content Delivery API (lectura pública)

- **Objetivo:** Exponer contenido **publicado** para Themes, portales, apps e integraciones. Solo lectura, cacheable, sin dependencia del CMS Admin.
- **Características:** Solo estado `Published`; resolver por dominio/sitio/sección/tipo/slug; relaciones entre contenidos; JSON limpio y estable.
- **NO debe:** Autenticación del CMS, escritura, publicación.
- **Relación con Themes:** Los temas consumen esta API (o un endpoint público equivalente); ver THEMES_STEPS.

### 14.3 Observabilidad y auditoría

- **Audit Log Service:** Registrar quién hizo qué, cuándo, en qué proyecto/entorno/sitio, sobre qué entidad (login/logout, CRUD contenido, publicación, cambios de permisos/esquemas).
- **Event Tracing:** `correlationId` por flujo, trazabilidad entre microservicios, asociado a eventos RabbitMQ; facilita debugging distribuido y diagnóstico de errores.

### 14.4 Feature Flags y configuración dinámica

- **Objetivo:** Habilitar/deshabilitar funcionalidades sin redeploy (auto-registro, workflows avanzados, campos experimentales, funcionalidades por entorno).
- **Alcance:** Flags por Proyecto, Entorno, Sitio; evaluadas en backend y frontend. Fase avanzada u opcional.

### 14.5 Orden de implementación recomendado (complementario)

1. API Gateway / BFF (mínimo viable)
2. Content Delivery API
3. Audit Logs básicos
4. Event tracing
5. Feature flags (opcional / fase avanzada)

---

## 15. Listas de tareas por ámbito

Las tareas concretas (checklists, orden sugerido y dependencias) están en:

* **[BACKEND_STEPS.md](./BACKEND_STEPS.md)** – Backend. Incluye API Gateway/BFF, Content Delivery API, Audit Log, Event Tracing, Feature Flags (del plan complementario). **Avance:** Sección 1 (Mejoras en Core API) completada.
* **[CMS_STEPS.md](./CMS_STEPS.md)** – CMS Frontend (Admin Panel). Incluye integración con BFF y feature flags en UI cuando existan.
* **[THEMES_STEPS.md](./THEMES_STEPS.md)** – Theme Frontend. Incluye consumo de Content Delivery API (o endpoint público de contenido).

Este documento (PLAN_DE_TRABAJO.md) describe la visión, los requisitos y las extensiones arquitectónicas; los pasos ejecutables se detallan en esos tres archivos.
