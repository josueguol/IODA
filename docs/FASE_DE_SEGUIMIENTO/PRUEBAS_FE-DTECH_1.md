# Pruebas Fase 1 — Alineación seguridad y CORS (FE-DTECH-1)

Checklist para validar la **Fase 1** del frontend tras los cambios de configuración CORS y manejo 401/403.

---

## 1. Origen del frontend y config

- [ ] En desarrollo, con `npm run dev`, la app se sirve desde un origen (ej. `http://localhost:5173`). Comprobar que `config.frontendOrigin` en runtime coincide (en consola: `import { config } from './config/env'; console.log(config.frontendOrigin)` o inspeccionar llamadas en Network).
- [ ] Si defines `VITE_APP_ORIGIN` en `.env`, comprobar que `config.frontendOrigin` toma ese valor.

---

## 2. CORS (tras restricción en Backend)

Con el backend configurado para aceptar solo orígenes concretos (incluido el del frontend):

- [ ] **Core API:** Navegar a proyectos, contenido, schemas, media. No debe aparecer error CORS en consola.
- [ ] **Identity API:** Login y registro. Sin errores CORS.
- [ ] **Authorization API:** Pantallas que usen roles/permisos (ej. RolesPermissionsPage, UsersPage). Sin errores CORS.
- [ ] **Publishing API:** Solicitudes de publicación, aprobar/rechazar (PublishPage, EditContentPage). Sin errores CORS.
- [ ] **Indexing API:** Búsqueda (SearchPage) y reindexar desde EditContentPage. Sin errores CORS.

Si algún origen no está en la whitelist del backend, aparecerá un error CORS en la consola del navegador; coordinar con Backend/DevOps la lista en [CORS_ORIGINS.md](./CORS_ORIGINS.md).

---

## 3. Token en peticiones protegidas

- [ ] Con usuario logueado, en DevTools → pestaña Network, revisar peticiones a:
  - Authorization, Publishing, Indexing, Core.
- [ ] En Request Headers debe aparecer `Authorization: Bearer <token>` en las llamadas que requieren autenticación (todas excepto login/register/refresh y, si aplica, setup-status).

---

## 4. 401 / 403

- [ ] **401:** Simular token inválido o expirado (ej. borrar token en localStorage o esperar expiración). La siguiente petición protegida debería: (1) intentar refresh y, si falla, (2) hacer logout y redirigir a login.
- [ ] **403:** Si el backend devuelve 403 (sin permiso), la app debe llamar a `onUnauthorized` (logout + redirección a login). Comprobar que no quede el usuario “colgado” sin redirección.

---

## 5. Variables de entorno en producción (backlog)

- [ ] En despliegue de producción, todas las `VITE_*_API_URL` apuntan a las URLs reales de las APIs.
- [ ] No hay referencias a `localhost` en las variables usadas en producción.
- [ ] Opcional: `VITE_APP_ORIGIN` definido con la URL pública del frontend.
