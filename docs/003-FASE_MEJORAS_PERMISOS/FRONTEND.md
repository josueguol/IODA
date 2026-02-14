# Tareas Frontend — Migración a permisos centralizados

Responsable: **Equipo Frontend**.  
Referencia: [PLAN_DE_MIGRACION_PERMISOS_CENTRALIZADOS.md](./PLAN_DE_MIGRACION_PERMISOS_CENTRALIZADOS.md).

---

## Fase 1

- No hay tareas de frontend en Fase 1; el backend añade catálogo y seeder sin cambiar contratos de API que consuma el frontend.

---

## Fase 2 (preparación opcional)

- [x] **2.1 (Opcional) Leer permisos desde JWT**  
  Si se acuerda que el frontend usará los permisos del JWT para ocultar/mostrar UI (en lugar de solo checkAccess), implementar lectura del claim de permisos tras decodificar el access token (o usar un endpoint que devuelva permisos del usuario). Mantener usePermission/checkAccess para contexto específico si el backend lo sigue exponiendo. **No es obligatorio en Fase 2;** puede dejarse para cuando el JWT ya incluya permisos y se quiera reducir llamadas a checkAccess.  
  **Hecho:** Util `parsePermissionsFromAccessToken` (claim `permission`, string o string[]), hook `useJwtPermissions()`, e integración en `usePermission`: sin contexto se usa primero el JWT y solo se llama a checkAccess si el permiso no está en el token.

---

## Fase 3: Ajuste tras backend con permisos en JWT y sin creación de permisos

- [x] **3.1 Eliminar creación de permisos en RolesPermissionsPage**  
  En la pestaña Permisos: quitar el formulario y la lógica que llama a `authorizationApi.createPermission`. Quitar botón/acción "Crear permiso". La pestaña debe seguir mostrando la lista de permisos vía GET /api/authorization/permissions (solo lectura).  
  **Hecho:** PermissionsTab solo carga y muestra la lista (GET /permissions); texto "solo lectura". createPermission eliminado del cliente API.

- [x] **3.2 Eliminar creación de permisos en el flujo de primer usuario (RegisterPage)**  
  En `setupSuperAdmin`: no llamar a `authorizationApi.createPermission`. No iterar sobre DEFAULT_PERMISSIONS para crear permisos. Mantener: obtener roles (GET /roles), crear rol "SuperAdmin" si no existe (POST /roles), obtener permisos (GET /permissions), asignar todos los IDs de permisos al rol SuperAdmin (POST /roles/{id}/permissions), crear AccessRule usuario–SuperAdmin (POST /rules).  
  **Hecho:** Constante DEFAULT_PERMISSIONS eliminada. setupSuperAdmin obtiene permisos con GET /permissions (allPermIds = perms.map(p => p.id)). Si GET /users/{userId}/rules devuelve reglas, se omite el setup (backend ya asignó).

- [x] **3.3 Consumir solo GET /permissions para listas de permisos**  
  Asegurar que RolesPermissionsPage y cualquier otra pantalla usen únicamente GET /api/authorization/permissions para rellenar listas de permisos asignables a roles.  
  **Hecho:** RolesPermissionsPage (PermissionsTab y RolesTab) y RegisterPage usan solo getPermissions().

- [x] **3.4 Invalidar caché de permisos en refresh**  
  Tras renovar el access token (refresh), llamar a `invalidatePermissionCache()` para que usePermission vuelva a consultar o use el nuevo token. Integrar en el flujo de refresh del auth store (donde se actualiza el access token).  
  **Hecho:** Caché movida a `shared/permission-cache.ts`; auth-store llama `invalidatePermissionCache()` en `setSession` (login y refresh).

- [x] **3.5 (Opcional) Mensaje cuando los permisos cambien**  
  Si se detecta 403 en una acción que antes estaba permitida (ej. tras asignar/revocar roles), mostrar mensaje tipo "Sus permisos han cambiado" y opcionalmente forzar re-login o refresh para obtener nuevo JWT.  
  **Hecho:** onUnauthorized(reason) en auth-aware-client; ante 403 redirección a login con `?reason=permissions_changed`. LoginPage muestra el mensaje "Sus permisos han cambiado. Inicia sesión de nuevo." cuando existe ese query param.

---

## Fase 4: Limpieza

- [ ] **4.1 Eliminar constante DEFAULT_PERMISSIONS para creación**  
  En RegisterPage (y donde aplique), la constante DEFAULT_PERMISSIONS ya no se usa para crear permisos. Eliminarla o reducirla a uso solo de etiquetado/nombres si hace falta para la UI. Si GET /permissions ya devuelve descripción, no es necesario mantener la lista en frontend.

- [ ] **4.2 Revisar referencias a createPermission**  
  Buscar en el proyecto cualquier llamada restante a `authorizationApi.createPermission` o equivalente y eliminarla. Asegurar que no queden rutas o botones "Crear permiso".

---

## Resumen de breaking changes (Frontend)

| Cambio | Impacto |
|--------|--------|
| Eliminación de POST /permissions (backend) | RolesPermissionsPage y RegisterPage dejan de poder crear permisos; deben estar actualizados (3.1, 3.2) antes o al mismo tiempo. |
| JWT con nuevos claims de permiso | Si el frontend decodifica el JWT, debe ignorar o manejar los nuevos claims; no rompe si solo se usa el token como opaco. |
| Primer usuario asignado en backend | RegisterPage puede simplificar setupSuperAdmin o no ejecutarlo; coordinar con backend. |
