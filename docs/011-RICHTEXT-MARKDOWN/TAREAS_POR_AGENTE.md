# TAREAS_POR_AGENTE - 011 RichText Markdown

Estado: `Listo para ejecucion controlada`

## 1) Fullstack (`ai/agents/fullstack.agent.md`)

## Objetivo

Implementar cambio end-to-end (backend + frontend) para usar `richtexteditor` como único editor enriquecido respetando contratos y seguridad.

## Backlog obligatorio

1. Backend
- [x] Mantener contrato de schema/content en tipo único `richtexteditor`.
- [x] Validar server-side bloques permitidos y estructura minima.
- [x] Aplicar sanitizacion y allowlist para embeds/media.
- [x] Cancelar migracion de datos legacy (fase desarrollo, datos descartables).
- [x] Eliminar compatibilidad temporal de lectura `richtext`.

2. Frontend
- [x] Integrar editor BlockNote con toolbar de formato requerida.
- [x] Implementar bloques base solicitados.
- [x] Implementar bloques de 2 y 3 columnas.
- [x] Quitar `richtext` de nuevas altas de schema/componentes.
- [x] Eliminar visualizacion legacy para concentrar pruebas en el flujo nuevo.

3. Evidencia minima
- [x] Build backend OK.
- [x] Build frontend OK.
- [ ] Demo funcional de authoring/publicacion.
- [x] Documento de riesgos/trade-offs remanentes.

## Restricciones

- No breaking changes sin versionado.
- No delegar seguridad al frontend.
- No cerrar tarea si solo esta listo backend o solo frontend.

## 2) QA Tester (`ai/agents/qa-tester.agent.md`)

## Objetivo

Validar calidad funcional/tecnica del reemplazo y decidir gate de liberacion.

## Cobertura obligatoria

1. Happy path
- [ ] Crear nota con subtitulo centrado, parrafo multilinea, media, embed, video.
- [ ] Crear contenido con columnas 2 y 3.
- [ ] Publicar y volver a editar sin perdida de formato.

2. Error path
- [ ] Rechazo de embed/provider no permitido.
- [ ] Rechazo de payload corrupto o excedido.
- [ ] Manejo UI de error sin perdida de trabajo del autor.

3. No regresion
- [x] Alcance legacy removido por decisión de desarrollo.
- [ ] Permisos/claims de edicion/publicacion vigentes.
- [x] Validacion de contratos API criticos.

4. Decision
- [ ] Emitir resultado de gate con hallazgos por severidad.
- [ ] Condiciones de liberacion si aplica.

## Evidencia minima

- Matriz de pruebas en `QA_GATE_REPORT.md`.
- Logs/comandos reproducibles.
- Riesgo residual y recomendacion final.
