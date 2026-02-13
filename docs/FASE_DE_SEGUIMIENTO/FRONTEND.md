# Tareas Frontend

Responsable: **Equipo Frontend**.  
Referencia: [DIAGNOSTICO_TECNICO_CMS.md](../DIAGNOSTICO_TECNICO_CMS.md).

---

## Fase 1: Alineación con seguridad y CORS (1–2 sprints)

### 1.1 Orígenes y configuración para CORS

- [ ] Definir y documentar la URL (origen) del frontend en cada entorno (desarrollo, staging, producción).
- [ ] Coordinar con Backend/DevOps la lista de orígenes que se configurarán en CORS; asegurar que `config/env.ts` (o equivalente) exponga la URL base del frontend que coincida con el origen que las APIs acepten.
- [ ] Tras el cambio de Backend a CORS restringido: verificar que todas las pantallas que llaman a Core, Identity, Authorization, Publishing e Indexing funcionen sin errores CORS (mismo origen o orígenes permitidos correctos).

---

### 1.2 Consumo de APIs recién protegidas

- [ ] **Authorization API:** Todas las llamadas (roles, permisos, reglas) deben enviar el token de autenticación (Bearer). Revisar `authorization-api.ts` y módulos que consuman este API; asegurar que el cliente HTTP incluya el token en las peticiones.
- [ ] **Publishing API:** Llamadas a solicitudes de publicación, aprobar/rechazar, etc., deben enviar el token. Revisar el módulo de publishing y adaptar si hoy no envían Authorization.
- [ ] **Indexing API:** Llamadas a indexar o eliminar del índice deben enviar el token. Revisar el módulo de indexing.
- [ ] Gestionar respuestas 401/403: redirigir a login o mostrar mensaje coherente según la política de la aplicación.

---

### 1.3 Variables de entorno y puertos

- [ ] Revisar `config/env.ts` (Vite `import.meta.env`): puertos actuales (5269–5273) vs puertos de docker-compose (5001–5005). Decidir y documentar:
  - Desarrollo local sin Docker: qué base URL usar.
  - Desarrollo con Docker: qué base URL usar (ej. proxy o URLs de los servicios en el compose).
- [ ] Asegurar que en producción las variables de entorno apunten a las URLs correctas de cada API y que no queden referencias a localhost.

---

## Fase 2: Mejoras opcionales (2–3 sprints)

### 2.1 Contrato GetPublicationRequests

- [ ] Cuando Backend sustituya el enum `PublicationRequestStatus` por string o DTO en `GetPublicationRequests`, actualizar el cliente frontend (tipos y parámetros de query) para usar el nuevo contrato (ej. enviar string en lugar de enum).

---

### 2.2 Descarga de media (si se cambia el backend)

- [ ] Si Backend implementa signed URLs o token corto para `MediaController.GetFile`, actualizar el frontend para solicitar/usar la URL firmada o el token en las peticiones de descarga de archivos.

---

## Criterios de aceptación

- Tras restricción de CORS, el frontend funciona en los entornos definidos sin errores CORS.
- Todas las llamadas a Authorization, Publishing e Indexing envían el token y manejan 401/403 de forma coherente.
- Variables de entorno y puertos documentados y correctos para local, Docker y producción.
