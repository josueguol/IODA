# Decisiones aprobadas – 009 Schemas Corrections

Fecha: 2026-02-27
Estado: Aprobadas por negocio/producto

## 1) Slug

Decisión:

- El frontend envía `slug`.
- El backend valida y normaliza.
- Si `slug` viene vacío/null, backend hace fallback desde `title`.

Validación backend requerida:

- Caracteres permitidos: letras, números, `-`, `_`.
- Sin espacios.
- Normalización sugerida: lowercase + trim + colapso de separadores repetidos.

Resultado esperado:

- Usuario puede personalizar slug.
- Backend asegura consistencia y seguridad del formato.

---

## 2) Sección principal

Decisión:

- El contenido tendrá una sección principal opcional y única.
- Si la sección asignada es hija, al filtrar por la sección padre el contenido debe aparecer.

Implementación recomendada:

- Mantener jerarquías existentes.
- Marcar una relación primaria (`is_primary`) para la sección principal.
- Resolver filtro por padre incluyendo descendientes en query backend.

---

## 3) URLs por sitio

Decisión funcional:

- La URL final se construye con:
  - base URL configurada del sitio (`https://dominio[/subpath]`)
  - + ruta de contenido por slug.
- Ejemplo:
  - `https://otrositio.com/portal1/una-nota-publicada/`

- No se usará por ahora configuración dinámica de URI tipo plantilla.
- El default es por slug del contenido.

Modelo recomendado para cumplir sharing y personalización futura:

- Tabla dedicada para rutas de publicación por sitio y contenido (ej. `content_site_urls`).
- Unicidad por sitio+ruta publicada (evita colisiones dentro del sitio).
- Permite:
  - default por slug
  - override por sitio compartido cuando se requiera personalización.

---

## 4) Site owner / sites shared

Decisión:

- No crear conceptos duplicados.
- `site_owner` se mapea a `SiteId` del contenido.
- `sites_shared` se mapea a relación `ContentSites`.
- En frontend se mostrará explícitamente owner y shared.

---

## 5) Capacidades existentes que se preservan (obligatorio)

- Versionado de contenido.
- Auditoría (`createdBy`, `updatedBy`, `publishedBy`).
- Timestamps (`created_at`, `updated_at`, `published_at`).
- Estado de contenido (`status`).

---

## 6) Gate de salida de diseño

Se autoriza pasar a implementación cuando:

- Contratos backend/frontend actualizados con estas decisiones.
- Plan de migración definido.
- Criterios QA definidos para validación de rutas, filtros por sección y no regresión de versionado/auditoría.
