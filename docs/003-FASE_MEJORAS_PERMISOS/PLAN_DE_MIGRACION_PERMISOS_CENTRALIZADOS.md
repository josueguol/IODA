# Plan de migración a modelo de permisos centralizados

**Objetivo arquitectónico:** Permisos definidos y centralizados en código; policies 1:1 con permisos; roles como agrupadores; primer usuario (SuperAdmin) creado automáticamente con todos los permisos; JWT con claims de permisos efectivos; sin bypass ni creación dinámica de permisos desde el CMS.

---

## Estado actual

### Autorización (Backend)

| Aspecto | Implementación detectada |
|--------|---------------------------|
| **Policies** | Definidas por API: Authorization API `AddPolicy("Admin", RequireRole("Admin"))`; Publishing API `AddPolicy("Editor", RequireRole("Editor", "Admin"))`. Core e Indexing solo `[Authorize]` (usuario autenticado). |
| **RequireRole vs RequireClaim** | Solo `RequireRole`. No existe `RequireClaim` por permiso. Las policies no mapean a permisos del sistema (content.create, role.manage, etc.). |
| **Protección real** | Core/Indexing: cualquier usuario autenticado puede acceder a todos los endpoints. Authorization: CreateRole, CreatePermission, AssignPermissionsToRole, CreateAccessRule, RevokeAccessRule exigen Policy "Admin". Publishing: endpoints exigen Policy "Editor". |

### JWT

| Aspecto | Implementación detectada |
|--------|---------------------------|
| **Generación** | `JwtTokenGenerator.GenerateAccessToken(userId, email, roles?)` en Identity. Solo incluye: `sub`, `email`, `jti`. El parámetro `roles` existe pero **nunca se usa**: `LoginCommandHandler` y `RefreshTokenCommandHandler` llaman `GenerateAccessToken(user.Id, user.Email)` sin roles. |
| **Permisos en JWT** | No hay claims de permisos. El frontend y ningún backend derivan autorización del token; la comprobación de permisos se hace vía API `POST /api/authorization/check`. |

### Roles y permisos en BD (Authorization)

- **Permisos:** Entidad `Permission` (Code, Description). Creación vía `POST /api/authorization/permissions` (CreatePermissionCommand). No hay catálogo fijo en código.
- **Roles:** Entidad `Role` con `RolePermissions` (N:M). Creación vía `POST /api/authorization/roles`. Los roles son solo nombres; no hay restricción a “permisos existentes” más allá de que el permiso exista en BD.
- **Reglas de acceso:** `AccessRule` asocia UserId (Identity) a RoleId con ámbito opcional (ProjectId, EnvironmentId, SchemaId, ContentStatus). CheckAccess resuelve usuario → reglas → roles → permisos del rol.

### Primer usuario / SuperAdmin

- **Identity:** `RegisterCommandHandler` devuelve `RegisterResultDto(userId, isFirstUser)`. No crea ningún rol ni permiso. No hay seeder ni bootstrap en backend.
- **Frontend:** En `RegisterPage`, si `result.isFirstUser`: tras registro hace login y ejecuta `setupSuperAdmin(userId)`: (1) obtiene permisos existentes, (2) **crea los permisos faltantes** según constante `DEFAULT_PERMISSIONS` (19 códigos: content.create, project.create, role.manage, etc.) vía `authorizationApi.createPermission`, (3) crea rol "SuperAdmin", (4) asigna todos los permisos al rol, (5) crea AccessRule usuario–SuperAdmin. Es decir, el “primer usuario” se configura desde el **frontend** creando permisos y rol en Authorization API.
- **Problema de consistencia:** Los endpoints CreateRole y CreatePermission están protegidos con `[Authorize(Policy = "Admin")]`. El JWT no incluye roles; por tanto **ningún usuario tiene claim "Admin"**. El flujo de primer usuario que llama a createPermission/createRole desde el frontend recibiría **403** a menos que exista un bypass no encontrado en el código analizado. *(Si en algún entorno se usa un JWT con rol Admin por otro medio, debe documentarse.)*

### Frontend – Gestión de permisos y roles

- **RolesPermissionsPage:** Pestaña "Permisos": lista GET /permissions, **formulario para crear permiso** (code, description) → POST /permissions. Pestaña "Roles": lista roles, asignar permisos a rol (solo selección de permisos ya existentes), crear reglas de acceso usuario–rol.
- **RegisterPage:** Constante `DEFAULT_PERMISSIONS` (19 códigos) y creación de permisos faltantes vía POST /permissions.
- **Comprobación de permiso:** `usePermission(permissionCode, context)` → llama a `POST /api/authorization/check` con userId del store y contexto. No se usa JWT para decidir permisos; todo va por API.
- **Componentes:** `<Can permission="...">`, `ProtectedRouteByPermission` usan `usePermission` (checkAccess).

### Resumen de violaciones al modelo deseado

1. **Permisos no centralizados en código:** Se crean en BD vía POST /permissions desde UI (RolesPermissionsPage) y desde RegisterPage (DEFAULT_PERMISSIONS).
2. **Policies no 1:1 con permisos:** Policies son "Admin" y "Editor" por rol, no por permiso (ej. content.publish, role.manage).
3. **Roles como únicos agrupadores:** Correcto en dominio; pero las APIs no autorizan por permiso sino por rol en las pocas políticas que hay.
4. **Primer usuario no creado en backend:** SuperAdmin se configura desde frontend (crear permisos + rol + regla). No hay seeder ni “primer usuario con todos los permisos” en backend.
5. **Posible bypass / incoherencia:** No hay bypass explícito para el primer usuario en backend; el flujo actual depende de que el primer usuario pueda llamar endpoints que requieren Policy "Admin" con un JWT sin roles.
6. **JWT sin permisos efectivos:** Solo sub, email, jti. No hay claims de permisos ni de roles utilizados en producción (Identity no inyecta roles).
7. **Frontend puede crear permisos arbitrarios:** RolesPermissionsPage permite crear permiso con code/description libre; RegisterPage crea los de DEFAULT_PERMISSIONS.
8. **Inconsistencia al cambiar permisos:** Si en el futuro el JWT incluyera permisos, habría que definir estrategia de refresco (token corto, refresh que reemita con nuevos permisos, etc.).

### Información no encontrada / a confirmar

- Si existe en algún entorno configuración o middleware que inyecte rol "Admin" en el JWT para el primer usuario o para un usuario concreto.
- Si Authorization API en algún despliegue tiene Policy "Admin" deshabilitada o relajada para permitir el bootstrap del primer usuario.
- Catálogo canónico de permisos: hoy solo existe la constante DEFAULT_PERMISSIONS en frontend; no hay una única fuente de verdad en backend.

---

## Cambios necesarios en Backend

1. **Definir catálogo de permisos en código (Authorization)**  
   - Crear una clase/registro estático o constante en el proyecto Authorization (p. ej. en Domain o Application) que liste todos los permisos del sistema (code + descripción).  
   - Incluir al menos los 19 códigos usados en frontend (content.create, content.edit, …, role.manage) y cualquier otro que se use en policies o en UI.  
   - **Capa:** Authorization.Domain o Authorization.Application.  
   - **Riesgo:** Ninguno si es solo añadir tipo/catálogo.

2. **Seeder de permisos al arranque o migración (Authorization)**  
   - Al iniciar la API o vía migración/script: si la tabla Permissions está vacía (o faltan códigos del catálogo), insertar todos los permisos definidos en el catálogo en código.  
   - Asegurar idempotencia (no duplicar por code).  
   - **Capa:** Authorization.Infrastructure (ejecución del seed) + referencia al catálogo.  
   - **Riesgo:** En entornos con datos existentes, comprobar que los códigos del catálogo coinciden con los ya creados por el frontend para no generar conflictos.

3. **Eliminar endpoint POST /api/authorization/permissions (crear permiso)**  
   - Quitar el endpoint y `CreatePermissionCommand`/handler de la API pública.  
   - Mantener permisos como solo-lectura desde API (GET /permissions).  
   - **Capa:** Authorization.API (controller), Authorization.Application (comando/handler pueden quedar internos o eliminarse).  
   - **Riesgo:** **Breaking change.** El frontend ya no podrá crear permisos; debe consumir solo GET /permissions.

4. **Validar que los permisos asignados a roles existan en el catálogo (Authorization)**  
   - En `AssignPermissionsToRoleCommandHandler`: validar que cada `PermissionId` corresponda a un permiso cuyo Code esté en el catálogo centralizado. Rechazar si no.  
   - **Capa:** Authorization.Application.  
   - **Riesgo:** Bajo; evita asignar permisos “fantasma” o eliminados de BD sin catálogo.

5. **Exponer endpoint oficial “lista de permisos del sistema” (Authorization)**  
   - Mantener o definir GET /api/authorization/permissions devolviendo solo permisos que estén en el catálogo (o todos los de BD si el seed los creó todos). Documentar que es la fuente de verdad para que el frontend muestre solo permisos asignables.  
   - **Capa:** Authorization.API + Application (query existente o filtrada por catálogo).  
   - **Riesgo:** Ninguno si el contrato (DTO) se mantiene.

6. **Definir policies por permiso (todos los APIs que protejan por autorización)**  
   - Sustituir políticas por rol (Admin, Editor) por políticas por permiso, por ejemplo `RequireClaim("permission", "role.manage")` o policy nombrada "RoleManage" que exija ese claim.  
   - Centralizar en un único lugar (p. ej. IODA.Shared.Api o cada API) el mapeo: nombre de policy → permiso requerido (1:1).  
   - Aplicar en Authorization API (role.manage u equivalentes para CRUD roles/permisos/rules), Publishing API (permiso de publicación), y si se añade protección por permiso en Core/Indexing, usar el mismo esquema.  
   - **Capa:** Cada API (Program.cs o extensión de autorización).  
   - **Riesgo:** **Breaking change** hasta que el JWT incluya claims de permisos; mientras tanto, nadie tendría esos claims y las rutas quedarían denegadas salvo que se mantenga temporalmente RequireRole o se inyecten permisos en JWT (ver siguiente punto).

7. **Incluir permisos efectivos en el JWT (Identity + integración con Authorization)**  
   - En Login (y Refresh) Identity debe obtener los “permisos efectivos” del usuario. Opciones: (a) Identity llama a Authorization API (HTTP) con userId y recibe lista de permission codes; (b) evento/cola: al login Identity publica evento y otro servicio devuelve permisos; (c) BD compartida (no recomendado si Identity y Authorization están separados).  
   - Añadir al JWT claims de permiso, por ejemplo tipo "permission" y valor el code (uno por permiso). Alternativa: un único claim con lista serializada (ej. JSON array). Decisión de formato debe ser consistente con cómo se validen las policies (RequireClaim).  
   - Actualizar `IJwtTokenGenerator.GenerateAccessToken` para aceptar `IEnumerable<string> permissions` (y opcionalmente roles si se mantienen) y emitir los claims correspondientes.  
   - **Capa:** Identity.Application (Login/Refresh handlers), Identity.Infrastructure (JwtTokenGenerator); posible cliente HTTP o cliente de Authorization en Identity.Infrastructure.  
   - **Riesgo:** **Breaking change** para clientes que asuman que el JWT no tiene nuevos claims. Latencia extra en login/refresh si se llama a Authorization API. Si el token tiene TTL largo y se revocan permisos, el usuario sigue con permisos hasta que renueve el token (mitigar con TTL corto y/o refresh que reemita con nuevos permisos).

8. **Crear primer usuario (SuperAdmin) automáticamente en backend**  
   - Opción A (recomendada): En Authorization, al arrancar (o en migración), si no existe ningún usuario con el rol SuperAdmin (o si la tabla de reglas está vacía), no crear usuario; Identity sigue siendo quien registra el primer usuario. Tras el primer registro, un proceso (job, endpoint interno, o seeder que lea “primer userId” de Identity) cree en Authorization: rol SuperAdmin, asignación de todos los permisos del catálogo a ese rol, y AccessRule(userId, SuperAdminRoleId). Así el “primer usuario” se asigna en backend sin que el frontend cree permisos ni roles.  
   - Opción B: Identity al detectar primer registro (hasUsers == false después de crear usuario) llama a Authorization API (o publica evento) para que Authorization cree SuperAdmin y asigne ese userId. Authorization debe exponer un endpoint interno o escuchar evento “primer usuario registrado” y crear rol + regla.  
   - En ambos casos: no dar “bypass” por ser el primer usuario; ese usuario debe tener los mismos controles (autorización por permisos en JWT). El primer usuario obtiene permisos porque se le asigna el rol SuperAdmin con todos los permisos, no porque se salte la autorización.  
   - **Capa:** Identity (registro + posible llamada/evento), Authorization (seed de rol SuperAdmin + endpoint o consumer para asignar primer usuario).  
   - **Riesgo:** Coordinación entre servicios; idempotencia al asignar SuperAdmin al primer usuario.

9. **Eliminar dependencia de “Admin”/“Editor” por nombre de rol en políticas**  
   - Una vez JWT lleve permisos y las policies exijan claims de permiso, eliminar políticas que usen `RequireRole("Admin")` o `RequireRole("Editor", "Admin")`.  
   - **Capa:** Authorization.API, Publishing.API (y otros que las usen).  
   - **Riesgo:** Ver punto 6; debe hacerse cuando el flujo de emisión de JWT con permisos esté operativo.

10. **Documentar convención de nombres de policies y permisos**  
    - Documento o comentarios en código: lista policy name → permission code (1:1). Ej. Policy "RoleManage" → permiso "role.manage".  
    - **Capa:** Docs y/o código compartido.  
    - **Riesgo:** Ninguno.

---

## Cambios necesarios en Frontend

1. **Eliminar creación de permisos desde la UI**  
   - En RolesPermissionsPage, quitar el formulario y la lógica que llama a `authorizationApi.createPermission`. Quitar botón/acción “Crear permiso”.  
   - La pestaña Permisos puede seguir mostrando la lista de permisos (GET /permissions) en solo lectura para referencia.  
   - **Riesgo:** **Breaking change** si se elimina POST /permissions antes de desplegar este cambio; coordinar con backend.

2. **Eliminar creación de permisos en el flujo de primer usuario (RegisterPage)**  
   - En `setupSuperAdmin`, no llamar a `authorizationApi.createPermission`. Asumir que los permisos ya existen en el sistema (creados por seeder en backend).  
   - Mantener solo: obtener roles, crear rol SuperAdmin si no existe, obtener permisos (GET), asignar todos los IDs de permisos devueltos al rol SuperAdmin, crear AccessRule usuario–SuperAdmin. Si el backend ya asigna al primer usuario el rol SuperAdmin (ver Backend punto 8), este flujo podría simplificarse o eliminarse y solo hacer login tras registro.  
   - **Riesgo:** Depende del backend: si el primer usuario se asigna en backend, el frontend no debe depender de crear permisos ni rol; si el backend solo crea permisos por seed y el frontend sigue creando rol + regla, asegurar que GET /permissions devuelva los permisos ya sembrados.

3. **Consumir solo GET /api/authorization/permissions para listar permisos**  
   - RolesPermissionsPage y cualquier otra pantalla deben usar únicamente GET /permissions para rellenar listas de permisos asignables a roles. No generar ni asumir códigos de permiso en frontend más que para mostrar/mapear nombres si se desea.  
   - **Riesgo:** Ninguno si el contrato no cambia.

4. **Adaptación a estructura de JWT con permisos (opcional según estrategia)**  
   - Si el frontend pasa a usar permisos desde el JWT para ocultar/mostrar UI (en lugar de o además de checkAccess), leer el claim de permisos del token (tras decodificar o vía endpoint que devuelva permisos del usuario). Mantener o reducir llamadas a checkAccess según diseño (ej. solo para contexto específico).  
   - **Riesgo:** Cambio de comportamiento si se desactiva checkAccess; asegurar que el backend siga validando en cada petición y no confiar solo en UI.

5. **Manejo de sesión cuando cambien permisos**  
   - Si los permisos se emiten en JWT con TTL corto y se refrescan en refresh token, al hacer refresh el nuevo access token puede traer permisos actualizados; invalidar caché de usePermission (invalidatePermissionCache) tras refresh.  
   - Si el usuario pierde un permiso (revocación de rol/regla), considerar: mensaje “sus permisos han cambiado” y forzar re-login o refresh para obtener nuevo JWT.  
   - **Riesgo:** UX; evitar que el usuario vea acciones que luego el backend rechace con 403.

6. **Asignación de permisos a roles: solo permisos existentes**  
   - La UI ya solo permite elegir permisos de la lista GET /permissions. Tras eliminar creación de permisos, no hay cambio funcional; validar que no queden referencias a “crear permiso” ni a constantes de códigos usadas para crear (DEFAULT_PERMISSIONS puede usarse solo para comparar o mostrar si el backend no devuelve descripción suficiente).  
   - **Riesgo:** Bajo.

---

## Impacto en seguridad

### Riesgos actuales

- **Autorización real por rol inexistente:** Las policies "Admin" y "Editor" requieren claims de rol que el JWT no incluye; o bien nadie puede acceder a esos endpoints, o existe un mecanismo no revisado (proxy, otro issuer) que inyecta roles.
- **Core/Indexing sin control por permiso:** Cualquier usuario autenticado puede acceder a todos los endpoints de Core e Indexing; no hay granularidad por permiso.
- **Permisos arbitrarios:** Cualquier usuario que pudiera llamar POST /permissions (si en algún caso no se exige Admin) o que obtenga acceso Admin puede crear códigos de permiso arbitrarios y asignarlos a roles, desalineando el modelo de seguridad del código.
- **Primer usuario:** Si el flujo de bootstrap depende del frontend y a la vez los endpoints exigen Admin, el primer usuario no podría completar el setup sin un bypass o configuración especial.

### Riesgos durante la migración

- **Ventana en la que JWT no lleva permisos pero las policies ya exigen claim de permiso:** Denegación masiva de acceso. Mitigación: desplegar primero la emisión de JWT con permisos (y mantener temporalmente RequireRole si hace falta), luego cambiar policies a RequireClaim(permission).
- **Pérdida de permisos creados “a mano”:** Si el catálogo en código no incluye códigos que ya existían en BD, el seeder no los creará y asignaciones a roles podrían quedar rotas. Mitigación: definir el catálogo a partir de los códigos actualmente usados (DEFAULT_PERMISSIONS + cualquier otro en uso) y documentar la lista.
- **Coordinación Identity–Authorization:** Si Identity llama a Authorization en login/refresh, un fallo de Authorization puede impedir login. Mitigación: timeouts, reintentos, y/o caché de permisos por userId en Authorization para reducir latencia.

### Mitigaciones recomendadas

- Catálogo de permisos como única fuente de verdad; seeder idempotente; sin POST de permisos desde la API pública.
- JWT con permisos efectivos; TTL de access token razonablemente corto; refresh que reemita con permisos actualizados.
- Sin bypass para el primer usuario: mismo flujo de autorización; el primer usuario obtiene acceso por tener asignado el rol SuperAdmin con todos los permisos en backend.
- Backend siempre valida permisos en cada request (policy o atributo por permiso); el frontend solo para UX.

---

## Estrategia de migración por fases

### Fase 1: Cambios internos compatibles (Backend)

- Definir catálogo de permisos en código (Authorization).
- Implementar seeder de permisos (al arranque o migración) idempotente.
- Validar en AssignPermissionsToRole que los permisos asignados pertenezcan al catálogo.
- Documentar convención policy ↔ permiso.
- **No eliminar** aún POST /permissions ni cambiar policies; el frontend puede seguir igual.
- **Entregable:** Permisos sembrados; catálogo en código; validación al asignar.

### Fase 2: JWT con permisos y policies por permiso (Backend)

- Identity: integrar con Authorization para obtener permisos efectivos del usuario en Login y Refresh.
- JwtTokenGenerator: emitir claims de permisos en el JWT (formato acordado).
- En cada API: definir policies 1:1 con permisos (RequireClaim) y aplicarlas a los endpoints que hoy usan Admin/Editor o que se quieran proteger por permiso.
- Crear primer usuario SuperAdmin en backend: al primer registro, asignar rol SuperAdmin con todos los permisos (vía Identity llamando a Authorization o evento).
- Eliminar políticas por rol (Admin, Editor) una vez las nuevas policies estén activas.
- **Entregable:** JWT con permisos; APIs protegidas por permiso; primer usuario asignado en backend.
- **Breaking change:** Clientes que asuman JWT sin claims de permiso; coordinar con frontend.

### Fase 3: Ajuste Frontend

- Eliminar creación de permisos (RolesPermissionsPage y RegisterPage).
- Ajustar flujo de primer usuario: ya no crear permisos; opcionalmente no crear rol/regla si el backend lo hace; si el backend solo asigna, frontend solo hace login tras registro.
- Consumir solo GET /permissions; opcionalmente usar JWT para permisos en UI e invalidar caché en refresh.
- **Entregable:** UI sin crear permisos; dependencia solo de GET /permissions y JWT según diseño.
- **Breaking change:** Eliminación de POST /permissions (backend debe estar ya sin ese endpoint o desactivado).

### Fase 4: Limpieza técnica (Backend + Frontend)

- Eliminar endpoint POST /api/authorization/permissions y comando CreatePermission de la API pública (si no se hizo en Fase 2).
- Eliminar constante DEFAULT_PERMISSIONS del frontend para “crear” permisos; mantener solo si se usa para etiquetado o mapeo de nombres.
- Revisar documentación y tests; actualizar guías de pruebas (ej. PRUEBAS_BE-DTECH_1, etc.) con el nuevo flujo de primer usuario y permisos en JWT.
- **Entregable:** Código y docs alineados con el modelo final.

---

## Resumen de breaking changes

| Cambio | Quién impacta | Cuándo |
|--------|----------------|-------|
| JWT incluye claims de permisos (nuevos claims) | Cualquier cliente que decodifique JWT y espere solo sub/email/jti | Fase 2 |
| Policies pasan de RequireRole a RequireClaim(permission) | Clientes sin permisos en JWT recibirán 403 en esas rutas | Fase 2 |
| Eliminación de POST /api/authorization/permissions | Frontend y cualquier integración que cree permisos por API | Fase 3 (backend puede eliminarlo en Fase 2 o 3) |
| Primer usuario asignado en backend; frontend deja de crear permisos/rol | Flujo de registro del primer usuario | Fase 3 |
