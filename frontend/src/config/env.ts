/**
 * Variables de entorno (VITE_*) expuestas por Vite.
 * Usar solo estas variables para URLs y config pública.
 */
const env = import.meta.env;

/** Origen del frontend (esquema + host + puerto). Debe coincidir con los orígenes permitidos en CORS del backend. */
function getFrontendOrigin(): string {
  const fromEnv = env.VITE_APP_ORIGIN as string | undefined
  if (fromEnv) return fromEnv
  if (typeof window !== 'undefined') return window.location.origin
  return 'http://localhost:5173'
}

export const config = {
  /** Origen del frontend para CORS. Backend/DevOps deben whitelist este origen. */
  frontendOrigin: getFrontendOrigin(),
  /** Base URL de la Core API (proyectos, contenido, esquemas, publish). */
  coreApiUrl: (env.VITE_CORE_API_URL as string) ?? 'http://localhost:5269',
  /** Base URL de la Identity API (login, register, refresh). */
  identityApiUrl: (env.VITE_IDENTITY_API_URL as string) ?? 'http://localhost:5270',
  /** Base URL de la Authorization API (check access, roles, rules). */
  authorizationApiUrl: (env.VITE_AUTHORIZATION_API_URL as string) ?? 'http://localhost:5271',
  /** Base URL de la Publishing API (solicitudes, aprobar, rechazar). */
  publishingApiUrl: (env.VITE_PUBLISHING_API_URL as string) ?? 'http://localhost:5272',
  /** Base URL de la Indexing API (búsqueda). */
  indexingApiUrl: (env.VITE_INDEXING_API_URL as string) ?? 'http://localhost:5273',
  /**
   * Tipo de routing: 'hash' para hosting estático (CDN, S3, GitHub Pages),
   * 'browser' para server-side routing (nginx, Express, etc.).
   * Por defecto: 'browser'.
   */
  routerType: ((env.VITE_ROUTER_TYPE as string) ?? 'browser') as 'browser' | 'hash',
  /** Entorno: development | production. */
  mode: env.MODE as string,
  /** Si estamos en desarrollo. */
  isDev: env.DEV === true,
  /** Si estamos en producción. */
  isProd: env.PROD === true,
} as const;

export type Config = typeof config;
