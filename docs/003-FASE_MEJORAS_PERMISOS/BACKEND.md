# Tareas Backend — Migración a permisos centralizados

Responsable: **Equipo Backend**.  
Referencia: [PLAN_DE_MIGRACION_PERMISOS_CENTRALIZADOS.md](./PLAN_DE_MIGRACION_PERMISOS_CENTRALIZADOS.md).

---

## Fase 1: Cambios internos compatibles

- [x] **1.1 Catálogo de permisos en código (Authorization)**  
  Crear en Authorization (Domain o Application) una fuente única de verdad con todos los permisos del sistema: código + descripción. Incluir al menos: content.create, content.edit, content.delete, content.publish, project.create, project.edit, project.delete, environment.create, environment.edit, environment.delete, site.create, site.edit, site.delete, schema.create, schema.edit, schema.delete, user.list, user.create, role.manage. Formato: clase estática, enum con descripción, o lista constante. No exponer aún como API; solo uso interno.

- [x] **1.2 Seeder de permisos (Authorization)**  
  En Authorization.Infrastructure: al arranque de la API (o en una migración EF), si la tabla Permissions está vacía o faltan códigos del catálogo, insertar todos los permisos del catálogo. Idempotente por code (no duplicar). Reutilizar la entidad Permission y el repositorio existente o acceso directo al DbContext según convención del proyecto.

- [x] **1.3 Validar permisos asignados al rol (Authorization)**  
  En AssignPermissionsToRoleCommandHandler: antes de asignar, validar que cada PermissionId corresponda a un permiso cuyo Code esté en el catálogo definido en 1.1. Si algún id no existe o no está en el catálogo, rechazar con 400 (Bad Request) y mensaje claro.

- [canceled] **1.4 Documentar convención policy ↔ permiso**  
  Añadir documento (en docs/003-FASE_MEJORAS_PERMISOS o en código) que liste: nombre de policy → permission code (1:1). Ejemplo: Admin → role.manage; Editor → content.publish (o los que se decidan). Será la referencia para Fase 2.

**Riesgos:** Ninguno breaking si no se quita POST /permissions ni se cambian policies. Verificar que el seeder no entre en conflicto con permisos ya existentes en BD (mismos codes).

---

## Fase 2: JWT con permisos y policies por permiso

- [x] **2.1 Integración Identity → Authorization para permisos**  
  En Identity: en Login y en Refresh, obtener la lista de “permisos efectivos” del usuario (códigos) desde el servicio Authorization. Opción recomendada: cliente HTTP en Identity.Infrastructure que llame a un endpoint de Authorization (ej. GET /api/authorization/users/{userId}/effective-permissions o interno). Authorization debe exponer ese endpoint (o ya existe GetUserAccessRules + cálculo de permisos). Definir contrato (array de strings con códigos de permiso).

- [x] **2.2 Incluir permisos en JWT (Identity)**  
  Extender IJwtTokenGenerator.GenerateAccessToken para aceptar `IEnumerable<string> permissionCodes`. Emitir un claim por permiso (ej. tipo "permission", valor el code) o un único claim con lista serializada; debe ser coherente con cómo se validen las policies (RequireClaim). Actualizar LoginCommandHandler y RefreshTokenCommandHandler para pasar los permisos obtenidos en 2.1 al generador.

- [x] **2.3 Endpoint “effective permissions” (Authorization)**  
  Si no existe: GET /api/authorization/users/{userId}/effective-permissions que devuelva los códigos de permiso efectivos del usuario (resolviendo reglas → roles → permisos de cada rol, sin duplicados). Proteger este endpoint (solo Identity o solo servicio-a-servicio con API key / JWT de sistema, según arquitectura). Documentar.

- [ ] **2.4 Policies por permiso en cada API**  
  En Authorization.API: reemplazar AddPolicy("Admin", RequireRole("Admin")) por policy que exija RequireClaim("permission", "<code>") según la convención (ej. role.manage). En Publishing.API: reemplazar policy "Editor" por permiso correspondiente (ej. content.publish). En Core.API e Indexing.API: si se desea proteger por permiso, añadir policies 1:1 con permisos y aplicarlas a los controladores correspondientes. Usar el mismo tipo de claim que emite Identity.

- [ ] **2.5 Crear SuperAdmin y asignar primer usuario (Authorization + Identity)**  
  En Authorization: seeder o lógica al arranque que cree el rol "SuperAdmin" si no existe y le asigne todos los permisos del catálogo. Exponer endpoint interno o escuchar evento “primer usuario registrado” (UserId). Cuando se registre el primer usuario (Identity lo indica), Authorization crea AccessRule(userId, SuperAdminRoleId). Opción alternativa: Identity tras el primer registro llama a Authorization para “asignar rol SuperAdmin a userId”; Authorization crea la regla. No debe existir bypass de autorización: el primer usuario pasa por el mismo flujo de JWT con permisos una vez asignado el rol.

- [ ] **2.6 Eliminar políticas por rol**  
  Quitar RequireRole("Admin") y RequireRole("Editor", "Admin") una vez las nuevas policies por permiso estén activas y el JWT incluya permisos.

**Riesgos:** Breaking: JWT cambia de estructura; clientes que no esperen claims de permiso. Coordinar con frontend para Fase 3. Si Authorization no está disponible en login, definir fallback (ej. token sin permisos y 403 en rutas protegidas, o reintentos).

---

## Fase 3 / 4: Limpieza

- [ ] **3.1 Eliminar endpoint POST /api/authorization/permissions**  
  Quitar del AuthorizationController el método CreatePermission y la ruta POST /permissions. Opcionalmente mantener CreatePermissionCommand/Handler para uso interno (seed) o eliminarlos si el seed usa solo el catálogo y el DbContext. **Breaking:** Cualquier cliente que cree permisos por API dejará de poder hacerlo; coordinar con frontend (eliminar creación desde UI en la misma ventana).

- [ ] **3.2 Ajustar GET /api/authorization/permissions**  
  Garantizar que devuelva solo permisos que pertenezcan al catálogo (o todos si el seed los creó todos). Si hace falta, filtrar por lista de códigos del catálogo en la query.

---

## Resumen por capa

| Capa | Tareas |
|------|--------|
| Authorization.Domain / Application | 1.1 Catálogo; 1.3 Validación asignación; 2.3 Effective permissions (query); 2.5 Seed SuperAdmin + asignación primer usuario |
| Authorization.Infrastructure | 1.2 Seeder permisos; 2.5 Ejecución seed |
| Authorization.API | 2.3 Endpoint effective-permissions; 2.4 Policies por permiso; 3.1 Eliminar POST permissions; 3.2 GET permissions filtrado |
| Identity.Application | 2.2 Llamar generador con permisos; 2.5 Disparar asignación primer usuario (llamada o evento) |
| Identity.Infrastructure | 2.1 Cliente HTTP a Authorization; 2.2 Generador JWT con claims de permiso |
| Core.API / Publishing.API / Indexing.API | 2.4 Policies por permiso; eliminar RequireRole |
