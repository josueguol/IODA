# Fase 3 — Mejoras de permisos (modelo centralizado)

Esta carpeta contiene el análisis y el plan de migración para alinear el sistema con un modelo de permisos centralizados en código, JWT con claims de permisos y sin creación dinámica de permisos desde el CMS.

## Documentos

| Documento | Contenido |
|-----------|------------|
| [PLAN_DE_MIGRACION_PERMISOS_CENTRALIZADOS.md](./PLAN_DE_MIGRACION_PERMISOS_CENTRALIZADOS.md) | Estado actual, cambios Backend/Frontend, impacto en seguridad, estrategia por fases. |
| [BACKEND.md](./BACKEND.md) | Tareas técnicas Backend por fase (catálogo, seeder, JWT, policies, eliminación POST permissions). |
| [FRONTEND.md](./FRONTEND.md) | Tareas técnicas Frontend por fase (eliminar creación de permisos, consumir GET /permissions, caché en refresh). |

## Objetivos arquitectónicos (resumen)

1. Permisos definidos y centralizados en código (no creados desde UI).
2. Policies 1:1 con permisos reales del sistema.
3. Roles únicamente como agrupadores de permisos existentes.
4. Primer usuario (SuperAdmin) creado automáticamente en backend con todos los permisos.
5. No bypass de autorización para el primer usuario.
6. JWT con permisos efectivos (claims de permisos).
7. Frontend solo asigna permisos existentes a roles; no crea permisos.
8. No permitir crear permisos arbitrarios desde el CMS.
9. Manejo correcto de JWT cuando cambien permisos (refresh, invalidación de caché).

## Orden de ejecución recomendado

1. **Fase 1 (Backend):** Catálogo, seeder, validación al asignar permisos a roles.
2. **Fase 2 (Backend):** JWT con permisos, policies por permiso, primer usuario SuperAdmin en backend; después eliminar POST /permissions.
3. **Fase 3 (Frontend):** Eliminar creación de permisos en UI y en flujo de primer usuario; consumir solo GET /permissions; invalidar caché en refresh.
4. **Fase 4:** Limpieza y documentación.

Los breaking changes están indicados en el plan y en cada documento de tareas.
