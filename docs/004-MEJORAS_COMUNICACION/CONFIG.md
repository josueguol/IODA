# Tareas Config / DevOps — 004 Mejoras comunicación (SuperAdmin sin permisos)

Responsable: **Configuración de entorno / DevOps**.  
Referencia: [README.md](./README.md).

---

## Configuración Identity ↔ Authorization

### 1. Variables / configuración Identity API

- **1.1** Para que el primer usuario reciba permisos en el JWT y se ejecute el bootstrap del primer usuario, configurar en el entorno donde corre la Identity API:
  - `AuthorizationApi:BaseUrl`: URL base del Authorization API (ej. `http://localhost:5003` o la URL interna en Docker/K8s). No debe terminar en `/api/authorization`; el cliente añade `/api/authorization/` a las peticiones.
  - `AuthorizationApi:ServiceApiKey`: Valor secreto compartido (string). Debe ser el mismo que en Authorization API.
- **1.2** Si `AuthorizationApi:BaseUrl` está vacío, Identity no llamará a bootstrap-first-user ni a effective-permissions; el JWT no incluirá claims `permission` y el primer usuario no tendrá permisos hasta que el frontend haga setup y un refresh (y solo si Identity en ese momento tiene BaseUrl configurado).

### 2. Variables / configuración Authorization API

- **2.1** Configurar `Authorization:ServiceApiKey` con el mismo valor que `AuthorizationApi:ServiceApiKey` en Identity. Las llamadas desde Identity (bootstrap-first-user, effective-permissions) envían el header `X-Service-Api-Key` con ese valor; si no coincide, Authorization devuelve 401 y Identity no puede ni crear la regla ni obtener permisos para el JWT.
- **2.2** Asegurar que la base de datos de Authorization tiene migraciones aplicadas y que al arrancar se ejecutan los seeders (PermissionSeeder, SuperAdminRoleSeeder). Sin el rol SuperAdmin, bootstrap-first-user devuelve conflicto.

### 3. Comprobaciones de entorno

- **3.1** Checklist para entornos donde el primer usuario no recibe permisos:
  - Identity: `AuthorizationApi:BaseUrl` no vacío y alcanzable desde Identity (red/firewall).
  - Identity: `AuthorizationApi:ServiceApiKey` = Authorization: `Authorization:ServiceApiKey`.
  - Authorization: BD con rol "SuperAdmin" y permisos del catálogo (reiniciar Authorization y revisar logs de seeders).
  - Tras registro del primer usuario: en logs de Identity, comprobar si hay "First user ... assigned SuperAdmin role" o "Authorization bootstrap-first-user returned ...".
  - Tras login o refresh: en Identity, comprobar si la llamada a effective-permissions devuelve 200 y una lista no vacía (o revisar logs de warning).
- **3.2** (Opcional) Incluir en documentación de despliegue una tabla con las variables mínimas por servicio (Identity, Authorization, Core) y su relación (qué debe coincidir entre Identity y Authorization).

---

## Resumen

- Sin **Identity** configurado con BaseUrl y ServiceApiKey correctos, el backend no asigna SuperAdmin al primer usuario ni rellena el JWT con permisos.
- Sin **Authorization** con el mismo ServiceApiKey y seeders ejecutados, bootstrap-first-user falla o effective-permissions devuelve vacío.
- Las tareas anteriores son de verificación y documentación; no sustituyen las correcciones de flujo y UX en Backend y Frontend descritas en BACKEND.md y FRONTEND.md.
