# MIGRACION_LEGACY_CHECKLIST

Estado: `Cancelada en esta fase de desarrollo`

## Alcance

La migracion legacy no se ejecuta en esta fase. El objetivo de este documento es dejar constancia del descarte y del reemplazo operativo en DEV/QA.

## 1. Decisión vigente

- [x] `richtext` fue retirado del runtime.
- [x] `richtexteditor` queda como tipo unico soportado.
- [x] No habrá conversion automatica de contenido legacy en esta fase.

## 2. Accion operativa en DEV/QA

- [ ] Identificar schemas de prueba que aun tengan campos `richtext`.
- [ ] Eliminar o recrear esos schemas con `richtexteditor`.
- [ ] Eliminar o recrear contenidos de prueba dependientes de esos schemas.
- [ ] Reejecutar happy path completo con contenido nuevo.

## 3. Cierre

- [ ] Confirmar que no quedan schemas activos con `fieldType=richtext`.
- [ ] Confirmar que QA manual usa solo `richtexteditor`.
- [ ] Registrar evidencia final en `QA_GATE_REPORT.md`.
