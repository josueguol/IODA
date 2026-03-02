# Tareas por agente – 009 Schemas Corrections

Estado: Aprobado para ejecución (fase implementación)

## 1) Gobernanza Técnica (perfil unificado)

### Tareas

1. Registrar decisiones ya confirmadas en `DECISIONES_APROBADAS.md` y validar que el diseño técnico las respete.

2. Definir impacto de contratos:
- Endpoints de create/update/list content.
- DTOs afectados.
- Compatibilidad y versionado.

3. Autorizar ADR si aplica:
- Registrar decisión y razones en `ai/memory/decisions.log.md`.

### Entregables

- Documento de decisiones aprobadas.
- Lista de contratos y migraciones aprobadas.

---

## 2) Fullstack (Backend + Frontend)

### Backend – tareas

1. Dominio/persistencia:
- Implementar soporte de `section` principal (opcional, única).
- Implementar tabla de rutas por sitio (owner/shared) con unicidad por sitio+ruta.
- Mantener default de publicación por slug con override por sitio cuando aplique.
- Mantener `status`, `timestamps`, versionado y auditoría.

2. Application/API:
- Ajuste comandos create/update content.
- Incorporar `slug` enviado por frontend (validación + fallback desde title).
- Incluir `section` y `tags` en contratos si faltan.
- Query de filtro por sección padre con expansión de hijas.

3. Migraciones:
- Crear migraciones con índices/constraints.
- Script de backfill para datos existentes (si aplica).

### Frontend – tareas

1. Formularios:
- Campo `slug` editable con prenormalización.
- `section` principal y `tags`.
- Gestión de URLs por sitio owner/shared.

2. Listados/filtros:
- Filtro por sección padre y resultados de hijas.

3. Tipos y API client:
- Sincronizar tipos TS y contratos backend.

### Entregables

- PR o set de commits con backend+frontend.
- Evidencia de build + smoke tests.

---

## 3) QA Tester Gatekeeper

### Tareas

1. Validar funcionalidad principal:
- create/update/publish content con campos nuevos.
- URL final por sitio owner y sitio compartido.

2. Validar reglas:
- unicidad URL por sitio.
- filtros por sección padre.
- tags/section opcionales.

3. Validar no regresión:
- versionado.
- created/updated/published timestamps.
- createdBy/updatedBy/publishedBy.
- status lifecycle.

4. Validar contratos:
- request/response alineados y sin romper clientes existentes.

### Decisión de gate

- Emitir: `Aprobado`, `Aprobado con condiciones`, `Rechazado`.
- Si aprobado y autorizado, preparar commit/push final.

---

## Checklist de aceptación global

- [ ] Campos base definidos y consistentes con dominio actual.
- [ ] No se rompe versionado/auditoría/status.
- [ ] URL default por slug (`/{slug}`) operativa.
- [ ] URL única por sitio.
- [ ] Soporte de compartir contenido con URL por sitio compartido.
- [ ] Filtro por sección padre incluye hijas.
- [ ] QA gatekeeper aprueba con evidencia.
