# Orígenes del frontend para CORS

Documento de alineación Frontend / Backend-DevOps para la **Fase 1** (CORS y seguridad).  
Referencia: [FRONTEND.md](./FRONTEND.md).

---

## Origen del frontend por entorno

El **origen** es la URL desde la que se sirve la aplicación (esquema + host + puerto, sin path).  
Las APIs (Core, Identity, Authorization, Publishing, Indexing) deben incluir estos orígenes en su configuración CORS.

| Entorno     | Origen del frontend (ejemplo)     | Notas |
|------------|------------------------------------|-------|
| Desarrollo | `http://localhost:5173`           | Puerto por defecto de Vite. Si cambias el puerto, usa ese (ej. `http://localhost:3000`). |
| Staging    | `https://cms-staging.tudominio.com`| Ajustar al dominio real de staging. |
| Producción | `https://cms.tudominio.com`        | Ajustar al dominio real de producción. |

---

## Dónde se configura en el frontend

- **`frontend/src/config/env.ts`**: expone `config.frontendOrigin`.
  - Si existe la variable de entorno **`VITE_APP_ORIGIN`**, se usa su valor.
  - Si no, en el navegador se usa `window.location.origin`.
  - En entornos sin `window` (build/SSR) el fallback es `http://localhost:5173`.
- **`frontend/.env.example`**: incluye comentarios para `VITE_APP_ORIGIN` (opcional en desarrollo).

Coordinación con Backend/DevOps: la **lista de orígenes permitidos** en CORS debe incluir exactamente las URLs desde las que se sirve el frontend en cada entorno (desarrollo, staging, producción). No debe haber discrepancias (por ejemplo, front en `https://app.ejemplo.com` y CORS permitiendo solo `http://localhost:5173`).

---

## Verificación tras CORS restringido

Una vez el backend tenga CORS restringido a orígenes concretos:

1. Ejecutar el frontend en desarrollo desde el origen indicado (ej. `http://localhost:5173`).
2. Comprobar que no aparecen errores CORS en consola al usar:
   - Core API (proyectos, contenido, schemas, media)
   - Identity API (login, register, refresh)
   - Authorization API (roles, permisos, check access)
   - Publishing API (solicitudes, aprobar, rechazar)
   - Indexing API (búsqueda, reindexar)
3. En staging/producción, asegurar que las variables de entorno (incl. `VITE_APP_ORIGIN` si se usa) apunten a las URLs correctas y que no queden referencias a `localhost`.

---

## Producción: variables de entorno

- Todas las `VITE_*_API_URL` deben apuntar a las URLs reales de cada API (Core, Identity, Authorization, Publishing, Indexing).
- No debe quedar ninguna referencia a `localhost` en las variables usadas en producción.
- Opcional pero recomendable: definir `VITE_APP_ORIGIN` en producción con la URL pública del frontend para consistencia con lo que CORS permite.
