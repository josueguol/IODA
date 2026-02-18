Actúa como un arquitecto de software senior especializado en sistemas distribuidos, CMS enterprise y diseño domain-driven.

Estoy desarrollando un CMS schema-driven, multi-proyecto, multi-entorno y multi-sitio, con:

* Backend desacoplado por servicios
* Admin Panel en React + TypeScript
* Themes separados que consumen una Content Delivery API
* Versionado ya implementado
* Separación entre gestión (Admin) y renderizado (Themes)

Necesito que analices y diseñes los cambios arquitectónicos necesarios para incorporar los siguientes requerimientos, sin romper los principios actuales (SOLID, separación de responsabilidades, multi-entorno, multi-sitio).

---

# REQUERIMIENTO 1 – Default Fields + Slug Editable en Schemas

Cuando se crea un nuevo Schema:

1. Debe sugerir automáticamente al menos estos campos:

   * title (text)
   * teaser (text corto / descripción breve)
   * image (media)
   * content (richtext)

2. Estos campos:

   * Son eliminables
   * Son editables antes de guardar el schema

3. Cada campo debe tener:

   * label visible (ej: "Descripción Corta")
   * slug técnico (ej: "descripcion-corta")

4. El slug:

   * Se autogenera desde el label
   * Es editable antes de crear el campo
   * Debe ser único dentro del schema
   * Debe cumplir reglas (kebab-case, sin espacios, sin caracteres especiales)

Necesito que definas:

* Cómo modelar FieldDefinition (label vs slug)
* Dónde validar unicidad (backend obligatorio)
* Cómo evitar colisiones en updates
* Cómo impacta esto en versionado de schemas

---

# REQUERIMIENTO 2 – Metadata y Control Editorial

Cada contenido debe tener:

* createdAt
* updatedAt
* createdBy
* updatedBy
* version (ya existe versionado)

Necesito:

* Diseño del modelo de auditoría embebido o separado
* Integración con versionado existente
* Estrategia para no romper compatibilidad histórica
* Definir si estos campos son:

  * Campos del dominio
  * Metadata técnica
  * Parte del snapshot versionado

También definir:

* Si deben exponerse en Content Delivery API
* Qué nivel de detalle debe ver el frontend público

---

# REQUERIMIENTO 3 – Jerarquías y Etiquetas

Necesito soportar:

1. Relaciones padre-hijo entre contenidos
2. Estructuras jerárquicas (ej: páginas anidadas)
3. Etiquetas (tags) por contenido
4. Posible clasificación por categorías

Diseña:

* Modelo relacional o agregado para jerarquía
* Estrategia para evitar ciclos
* Estrategia de consultas eficientes (árbol profundo)
* Cómo impacta en publicación
* Cómo impacta en Content Delivery API
* Cómo impacta en multi-sitio

---

# REQUERIMIENTO 4 – Multi-Sitio y Compartición

Cada contenido debe:

* Pertenecer a uno o más sitios
* Poder cambiar de sitio
* Poder compartirse entre sitios
* Mantener consistencia por entorno

Diseña:

* Modelo de asociación contenido ↔ sitio
* Si debe ser many-to-many
* Cómo se comporta la publicación por sitio
* Si la URL debe ser distinta por sitio
* Cómo evitar conflictos de slug entre sitios

---

# REQUERIMIENTO 5 – Sistema Flexible de URLs

Necesito una configuración por sitio para definir rutas de publicación.

Ejemplo:

* Por defecto: /{slug}
* Personalizado: /{section}/{createdAt:yyyy/MM}/{slug}

Requisitos:

* Soporte de placeholders dinámicos:

  * slug
  * createdAt (con formato)
  * section
  * custom fields
* Configurable por sitio
* Evaluado en publicación
* Compatible con Content Delivery API

Diseña:

* Motor de resolución de rutas
* Estrategia de validación
* Estrategia de cache
* Cómo manejar cambios retroactivos
* Cómo versionar URLs

---

# ENTREGABLE ESPERADO

Quiero que estructures la respuesta en:

1. Impacto en el dominio
2. Cambios en modelos
3. Cambios en servicios
4. Cambios en base de datos
5. Impacto en publicación
6. Impacto en Content Delivery API
7. Riesgos técnicos
8. Recomendación de orden de implementación

No quiero soluciones superficiales.
Quiero decisiones arquitectónicas justificadas.

El sistema debe seguir siendo:

* Escalable
* Multi-entorno
* Multi-sitio
* Versionado
* Separado entre Admin y Themes
