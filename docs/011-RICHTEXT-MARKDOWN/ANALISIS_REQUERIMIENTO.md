# Analisis de requerimiento - RichText Markdown con BlockNote

Fecha: 2026-03-07
Marco: `docs/CONSULTORIA/architecture/principios-cms.md` + `ai/agents/orchestrator.agent.md`

## 1. Clasificacion del cambio

- Tipo: Frontend + Contrato de contenido.
- Impacto: Alto (afecta authoring, persistencia de valor de campo y retiro de componente existente).
- Servicios/capas afectadas:
  - Frontend CMS editor (renderer de campos y UX de authoring).
  - Core Content (si se ajusta forma de persistencia/validacion del payload del campo).
  - Integraciones de render headless que consuman el payload canonico del editor nuevo.

## 2. Requerimiento funcional interpretado

1. Crear un nuevo componente de contenido con BlockNote para reemplazar `richtext`.
2. Soportar bloques base: `h2` a `h6`, `quote`, listas, `paragraph`, `code`, `table`, `media`, `embed`, `component module`.
3. Soportar layout multicolumna: 2 y 3 columnas.
4. Permitir formateado por componente/bloque alineado con capacidades de toolbar de BlockNote.
5. Eliminar `richtext` del catalogo de componentes disponibles en schema designer y en editor de contenido.

## 3. Decisiones arquitectonicas necesarias (antes de codificar)

1. Contrato canonico del valor del campo
- Decision aplicada: almacenar `markdown` como formato canonico + `metadata` minima para bloques no triviales (tabla/media/embed/columnas/modulos) cuando markdown puro no sea suficiente.
- Motivo: preserva API-first y compatibilidad headless con serializacion portable.

2. Estrategia de compatibilidad hacia atras
- Decision aplicada: no hay compatibilidad hacia atras en esta fase.
- Motivo: el entorno es de desarrollo, los datos son descartables y se prioriza reducir complejidad.

3. Eliminacion de `richtext`
- Decision aplicada: remover `richtext` de catalogo, runtime y contratos activos en el mismo sprint.
- Motivo: evita mantener rutas legacy que ya no aportan valor en DEV/QA.

4. Seguridad y validacion
- Decision propuesta: sanitizar HTML embebido y validar `embed/media/component module` por allowlist.
- Motivo: `security by design`, no confiar en frontend.

## 4. Impacto tecnico por capa

## Backend (Core)

- Definir/ajustar DTO de campo para soportar tipo `richtexteditor`.
- Validar payload (tamano, estructura minima, reglas de bloques permitidos).
- Versionar contrato si hay cambio breaking en request/response existente.
- No implementar migracion de valores `richtext` legacy en esta fase.

## Frontend (CMS)

- Integrar editor BlockNote y toolbar de formato.
- Implementar bloque multicolumna (2/3) y bloques solicitados.
- Adaptar schema designer para nuevo tipo y eliminar `richtext`.
- Eliminar rendering legacy del runtime.

## QA

- Probar happy path de authoring para cada bloque clave.
- Probar error path (payload invalido, embed no permitido, contenido excesivo).
- Probar permisos de edicion/publicacion y confirmar ausencia de schemas legacy activos.

## 5. Riesgos y mitigaciones

1. Riesgo: perdida de fidelidad entre bloques del editor y serializacion markdown.
- Mitigacion: pruebas de serializacion del flujo nuevo y casos manuales de QA.

2. Riesgo: discrepancia editor vs renderer headless.
- Mitigacion: snapshot tests de salida markdown y pruebas de render en frontend consumidor.

3. Riesgo: XSS en embed/media.
- Mitigacion: sanitizacion backend + allowlist de providers + pruebas negativas QA.

4. Riesgo: ruptura de schemas de prueba existentes.
- Mitigacion: limpiar schemas legacy y recrearlos con `richtexteditor`.

## 6. Criterios de aceptacion de arquitectura

- Sin logica de dominio en controllers.
- Sin breaking change no versionado.
- Sin runtime legacy para `richtext`.
- Validaciones de seguridad implementadas en backend.
- Evidencia de pruebas funcionales y de regresion en QA gate.
