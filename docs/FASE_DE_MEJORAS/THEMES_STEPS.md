# Theme Frontend – Pasos y plan (Fase de mejoras)

Este documento concentra las tareas relativas al **frontend de temas**: renderizado de contenido para sitios públicos (o preview), separado del panel de administración del CMS.

**Principio:** El Admin genera contenido. Los temas **solo** lo renderizan.

**Referencias:** `docs/FASE_DE_MEJORAS/PLAN_DE_TRABAJO.md`.

---

## 1. Separación Admin vs Themes

- [ ] **Admin Panel:** React + TypeScript (existente en `frontend/`)
- [ ] **Themes:** Tecnología distinta (p. ej. Handlebars + CSS + JS), carpeta o repositorio separado
- [ ] **Deploy independiente:** Themes desplegables en CDN / hosting estático sin depender del build del Admin
- [ ] **Contrato común:** Contenido expuesto como JSON (API pública o por sitio/entorno) para que los temas consuman

---

## 2. Estructura de temas

- [ ] Definir estructura estándar, por ejemplo:

```
themes/
├── default/
│   ├── templates/     # Plantillas por tipo de contenido o página
│   ├── partials/      # Fragmentos reutilizables
│   ├── assets/        # CSS, imágenes, JS
│   └── theme.json     # Metadatos del tema (nombre, versión, autor)
```

- [ ] Convención de nombres para templates (p. ej. por `contentType` o por ruta)
- [ ] Documentar cómo se resuelve qué template usar para cada contenido o página

---

## 3. Tecnologías de render

- [ ] **Motor de plantillas:** Handlebars (o similar) para renderizar HTML a partir de JSON de contenido
- [ ] **Estilos:** CSS (o preprocesador) sin lógica de CMS
- [ ] **Scripts:** JS ligero si hace falta (navegación, acordeones, etc.), sin duplicar lógica del CMS
- [ ] **Datos:** Inyectados desde API (contenido publicado, metadatos de sitio, etc.)

---

## 4. API o contrato para temas (Content Delivery API)

La **Content Delivery API** (definida en BACKEND_STEPS, sección 9 — plan complementario) es la API que los temas consumen: solo contenido **publicado**, read-only y cacheable.

- [ ] **Consumir Content Delivery API** (o endpoint público equivalente): contenido por dominio/sitio, por slug, por id, listados por tipo; relaciones entre contenidos según contrato del backend.
- [ ] Definir contrato mínimo en frontend: estructura del JSON (p. ej. title, contentType, fields, publishedAt) según lo expuesto por la API.
- [ ] Considerar versionado de API para temas (v1, v2) si evoluciona el modelo.
- [ ] Sin autenticación de CMS ni escritura; respuestas estables para cache y CDN.

---

## 5. Registro y asociación de temas (Admin)

Estas tareas son de **UI en el CMS** (Admin) pero afectan al ciclo de vida de los temas.

- [ ] **Registrar tema en Admin:** Nombre, ruta o paquete, versión (según modelo elegido: carpeta, npm, upload)
- [ ] **Asociar tema a sitio:** En la gestión de sitios, selector de tema activo por sitio (o por sitio+entorno)
- [ ] **Versionar temas:** Identificador de versión (theme.json o similar) y política de actualización
- [ ] **Cambiar tema sin tocar contenido:** Al cambiar el tema asociado a un sitio, el contenido sigue siendo el mismo; solo cambia la presentación

*(Parte del “registro” puede ser solo configuración en backend/base de datos si los temas se despliegan por canal propio.)*

---

## 6. Build y despliegue de temas

- [ ] **Build (si aplica):** Script o pipeline para compilar/minificar assets (CSS, JS) y dejar plantillas listas para el runtime
- [ ] **Despliegue:** A CDN o servidor estático; URL base configurable por sitio para cargar assets del tema
- [ ] **Servidor de render (si aplica):** Si el render es server-side (Node, etc.), definir cómo recibe el JSON y qué devuelve (HTML, fragmentos)

---

## 7. Preview desde el Admin (opcional)

- [ ] **Vista previa de contenido con un tema:** En el Admin, botón “Ver con tema” que abra el contenido renderizado con el tema asociado al sitio/entorno actual (iframe o nueva pestaña con URL de preview)
- [ ] Puede requerir endpoint de preview en backend (contenido draft con token temporal) – coordinar con BACKEND_STEPS

---

## 8. Orden sugerido

1. **Contrato y API:** Content Delivery API en backend (BACKEND_STEPS §9); en temas, definir consumo y estructura JSON esperada.
2. **Estructura y tecnología:** Estructura de carpetas, Handlebars (o elegido), ejemplo de tema “default” que consuma JSON.
3. **Asociación tema–sitio:** En el Admin (y en backend si aplica), poder elegir tema por sitio.
4. **Registro y versionado:** Cómo se registran y versionan temas en el sistema.
5. **Build y deploy:** Automatización para temas que lo necesiten.
6. **Preview:** Si se desea vista previa desde el Admin.

---

**Última actualización:** 2026-01-24  
**Origen:** PLAN_DE_TRABAJO (sección Temas y separación Admin vs Themes), [PLAN_COMPLEMENTARIO.md](./PLAN_COMPLEMENTARIO.md) (Content Delivery API).
