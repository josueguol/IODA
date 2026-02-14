/**
 * URL de redirección a login (para uso en onUnauthorized de clientes API).
 * Con reason '403' se añade ?reason=permissions_changed para mostrar mensaje en la página de login.
 */
export function buildLoginRedirect(
  routerType: 'hash' | 'browser',
  reason?: '401' | '403'
): string {
  const base = routerType === 'hash' ? '/#/login' : '/login'
  return reason === '403' ? `${base}?reason=permissions_changed` : base
}
