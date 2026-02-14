# Recomendación: Usuario actual desde JWT en comandos de auditoría

**Contexto:** En Core (y flujos que pasan “quién actuó”), los campos `CreatedBy`, `UpdatedBy`, `PublishedBy`, `UnpublishedBy` se envían en el body y se aceptan sin validar contra el usuario autenticado.

**Problema:** Cualquier usuario autenticado puede enviar el GUID de otro usuario y el sistema lo registrará como autor de la acción. La auditoría deja de ser fiable.

---

## Recomendación

1. **Obligar que el “actor” sea siempre el usuario autenticado.** No aceptar `CreatedBy` / `UpdatedBy` / `PublishedBy` / `UnpublishedBy` en el body (o ignorarlos si se envían).
2. **Obtener el userId en la capa de presentación (API)** desde el JWT (`User` / `ClaimsPrincipal`), p. ej. claim `sub` (estándar en el token actual).
3. **Pasar ese userId al comando** como único valor de “quién ejecuta la acción”. Los handlers no deben recibir el “actor” desde el cliente.

Consecuencias:

- La auditoría refleja al usuario que realmente hizo la petición.
- No se puede suplantar a otro usuario en los metadatos.
- Los contratos de la API dejan de exponer campos de auditoría editables por el cliente (o se documentan como ignorados).

---

## Alternativas y trade-offs

| Enfoque | Ventaja | Desventaja |
|--------|---------|------------|
| **Usuario actual solo desde JWT (recomendado)** | Una sola fuente de verdad; auditoría fiable. | El frontend no puede “actuar en nombre de” otro usuario (para eso haría falta un flujo explícito de impersonación con privilegios Admin). |
| **Mantener body pero validar que coincida con JWT** | El contrato del API no cambia. | Duplicación de lógica en cada endpoint; riesgo de olvidar la validación en uno. |
| **Delegar en un middleware que inyecte “current user” en el contexto** | Centraliza la lectura del JWT. | Sigue siendo necesario no aceptar el actor desde el body; el middleware solo ayuda a no repetir código de lectura de claims. |

Recomendación: **usuario actual solo desde JWT** y, opcionalmente, un servicio o extensión que lea `sub` del `ClaimsPrincipal` y lo inyecte en los comandos para no duplicar lógica en cada controller.
