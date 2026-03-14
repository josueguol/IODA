# DECISIONES_APROBADAS - 014 MediaManager

Fecha: `2026-03-13`
Responsable: `Gobernanza Tecnica / Orquestador`

## D-014-001: `Multimedia` como modulo de primer nivel

- Decision: agregar item de menu `Multimedia` despues de `Contenido`.
- Motivo: separar gestion multimedia como capacidad de producto, no como control secundario por campo.

## D-014-002: Campo `media` mantiene semantica 1:1

- Decision: `media` sigue siendo un solo archivo por campo.
- Motivo: evitar comportamiento de galeria en tipo que no lo define.
- Nota: un tipo futuro `mediaCollection` queda fuera de alcance.

## D-014-003: Implementacion por fases (no big bang)

- Decision: entregar MVP operativo antes de procesamiento avanzado.
- Motivo: reducir riesgo y acelerar feedback de UX.

## D-014-004: Storage provider strategy obligatoria

- Decision: mantener abstraccion `IMediaStorage` con proveedores `local` y `do_spaces`.
- Motivo: portabilidad entre entornos y desacople de infraestructura.

## D-014-005: DigitalOcean Spaces explicitamente soportado

- Decision: proveedor S3 del alcance es DigitalOcean Spaces Object Storage.
- Motivo: evitar ambiguedad de implementacion.

## D-014-006: Roadmap de microservicio dedicado

- Decision: `MediaManager` se diseña como bounded context propio; implementacion puede iniciar en modulo aislado y migrar a servicio independiente.
- Motivo: balancear time-to-value con cumplimiento de ADR de microservicios reales.

## D-014-007: Contratos API versionados y transicion controlada

- Decision: nuevos endpoints de media se publican versionados y con coexistencia temporal de contratos legacy.
- Motivo: evitar breaking changes abruptos.

## D-014-008: Etapa 2 inicia con derivados logicos

- Decision: la primera entrega de Etapa 2 registra variantes por tipo en metadata (`variants`) y estados asinc sin transcodificacion fisica real.
- Motivo: habilitar pipeline/observabilidad primero y desacoplarlo de complejidad de procesamiento pesado.
- Consecuencia: se requiere subetapa posterior para derivados fisicos reales (resize, poster real, etc.).

## D-014-009: Publicacion/indexacion con fields enriquecidos de media

- Decision: `ContentPublishedEventV1` incluye `Fields` como propiedad aditiva para transportar media proyectada con URL estable y variantes.
- Motivo: permitir indexacion y consumidores downstream con datos listos para consumo sin resolver IDs de media en caliente.
- Consecuencia: mantiene backward compatibility (campo opcional) y habilita integracion progresiva en servicios consumidores.
