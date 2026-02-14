# Ajustes Frontend para el PR fix/BE-DTECH-1/add-auth-on-apis

Este documento resume la revisión del frontend respecto a la **Fase 1 Backend** (BACKEND.md) y la guía de pruebas (PRUEBAS_BE-DTECH_1.md). Indica qué está ya cubierto y qué ajustes, si hay, debe realizar el equipo frontend.

---

## Resumen

**El frontend ya cumple con lo necesario para que el PR de backend funcione.** No hay cambios obligatorios; solo verificaciones y una mejora opcional (403).

---

## Lo que ya cumple el frontend

### 1. Envío de JWT a todos los APIs protegidos

- **Core API:** `core-api.ts` usa `createAuthAwareHttpClient` con `getAccessToken` y `refreshSession`. Todas las llamadas llevan `Authorization: Bearer <token>`.
- **Authorization API:** `authorization-api.ts` — mismo cliente con token.
- **Publishing API:** `publishing-api.ts` — mismo cliente con token.
- **Indexing API:** `indexing-api.ts` — mismo cliente con token.

No hace falta cambiar ningún cliente para enviar el token.

### 2. Manejo de 401 (no autenticado)

- En `auth-aware-client.ts`, ante **401** el cliente:
  1. Intenta `refreshSession()` y reintenta la petición una vez con el nuevo token.
  2. Si sigue 401 (o el refresh falla), llama a `onUnauthorized()`: logout y redirección a login (`/#/login` o `/login` según `routerType`).

Esto coincide con lo que pide la Fase 1 (gestionar 401 y redirigir a login).

### 3. CORS y orígenes

- En **Development**, el backend usa por defecto: `http://localhost:3000`, `http://localhost:5173`, `https://localhost:3000`, `https://localhost:5173`.
- El frontend con Vite se sirve por defecto en **http://localhost:5173**, que está en esa lista.
- Si el frontend se ejecuta en **http://localhost:5173** (o 3000), no habrá errores CORS por origen.

No hace falta cambiar código; solo asegurarse de que en desarrollo el frontend corra en uno de esos orígenes (p. ej. 5173).

### 4. URLs de los APIs (puertos)

- `config/env.ts` usa por defecto `localhost:5269` (Core), `5270` (Identity), `5271` (Authorization), `5272` (Publishing), `5273` (Indexing).
- Los `launchSettings.json` del backend (Core, Authorization, Publishing, Indexing) usan esos mismos puertos.
- Con backend y frontend en local, las URLs por defecto coinciden.

Si en algún entorno los APIs usan otros puertos (p. ej. Docker 5001–5005), hay que configurar las variables `VITE_*_API_URL` en `.env`; el `.env.example` ya indica que en Docker suelen ser 5001–5005.

---

## Ajustes recomendados (opcional)

### 1. Manejo explícito de 403 (Forbidden)

- **Situación:** El backend puede devolver **403** cuando el usuario no tiene el rol necesario (p. ej. Publishing con política "Editor": sin rol Editor/Admin → 403). El `auth-aware-client` hoy no tiene un callback específico para 403; ante cualquier `!response.ok` lanza el error y solo llama `onUnauthorized()` cuando el status es 401.
- **Recomendación (opcional):** Tratar 403 de forma distinta a 401, por ejemplo:
  - Mostrar un mensaje tipo "No tienes permiso para esta acción" en la pantalla donde ocurra, o
  - Redirigir a una ruta tipo `/forbidden` o a la home con un toast/banner.

No es obligatorio para que el PR funcione: el usuario verá el error de la API; solo mejora la UX.

**Dónde:** En `frontend/src/shared/api/auth-aware-client.ts`, tras comprobar `response.status === 401`, se podría añadir algo como `if (response.status === 403) { ... }` (callback opcional `onForbidden` en la config y/o manejo en la UI que consume el API).

---

## Verificaciones sugeridas antes de dar por cerrado el PR

1. **Ejecutar frontend en desarrollo** en `http://localhost:5173` (o 3000) y backend en los puertos por defecto (5269–5273); hacer login y usar:
   - Listado/creación de contenido (Core).
   - Roles/permisos/reglas (Authorization).
   - Solicitudes de publicación, aprobar/rechazar (Publishing).
   - Búsqueda (Indexing).  
   No debe haber errores CORS ni 401 en llamadas hechas con sesión iniciada.

2. **Probar 401:** Con sesión expirada o sin token, una acción que llame a un API protegido debe acabar en logout y redirección a login.

3. **Si usan Docker para los APIs:** Configurar en el frontend `.env` las URLs correctas (p. ej. `http://localhost:5001` … `5005`) y, si el frontend corre en otro host/puerto, que ese origen esté en `Cors:AllowedOrigins` del backend (o usar el valor por defecto en Development si aplica).

---

## Conclusión

- **Cambios obligatorios en frontend para el PR BE-DTECH-1:** ninguno.
- **Recomendado:** Verificar CORS/origen y flujo con login en los cuatro APIs; opcionalmente mejorar el manejo de 403 en cliente y/o UI.

Cuando el backend esté en la rama `fix/BE-DTECH-1/add-auth-on-apis`, el frontend actual puede integrarse y probarse siguiendo la guía PRUEBAS_BE-DTECH_1.md (por ejemplo, obteniendo un JWT vía login desde la app y usando la aplicación con sesión iniciada).
