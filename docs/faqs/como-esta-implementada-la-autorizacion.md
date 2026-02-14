# FAQ: ¿Cómo está implementada la autorización?

**Pregunta:** El módulo de autorización está implementada como: ¿Attributes con policy? ¿Middleware? ¿Validación manual dentro de los handlers? ¿Claims-based?

---

## Respuesta

| Enfoque | ¿Se usa? | Dónde |
|--------|----------|--------|
| **Attributes con policy** | **Sí** | Controladores: `[Authorize]`, `[Authorize(Policy = "Admin")]`, `[Authorize(Policy = "Editor")]`. |
| **Middleware** | **No** | No existe middleware propio de autorización; solo el pipeline estándar (`UseAuthentication` → `UseAuthorization`) y middlewares de errores/CORS. |
| **Validación manual en handlers** | **No** | Los handlers no comprueban permisos; solo ejecutan comando/query. No se invoca CheckAccess ni similar desde los handlers de Core/Publishing/Indexing. |
| **Claims-based** | **Sí** | El JWT incluye `sub`, `email`, `jti` y `ClaimTypes.Role`. Las políticas usan `RequireRole("Admin")` / `RequireRole("Editor", "Admin")`, que leen esos claims. |

**Resumen:** La autorización se implementa mediante **attributes con policy** y es **claims-based** (roles en el JWT). No hay middleware de autorización ni validación manual de permisos dentro de los handlers.

---

## Referencias

- Detalle por servicio y políticas: `docs/architecture/autorizacion-apis.md`
- Análisis ampliado: `docs/analysis/autorizacion-y-usuario-actual.md`
