import { config } from '../../../config/env'
import { useAuthStore } from '../../../modules/auth/store/auth-store'
import { buildLoginRedirect } from '../../../shared/auth-redirect'
import { ErrorBanner } from '../../../shared/components'

export interface HomeProjectsErrorProps {
  message: string
}

export function HomeProjectsError({ message }: HomeProjectsErrorProps) {
  const handleLogout = () => {
    try {
      sessionStorage.removeItem('ioda_first_user_refresh_failed')
    } catch {
      /* ignore */
    }
    useAuthStore.getState().logout()
    window.location.href = buildLoginRedirect(config.routerType)
  }

  return (
    <div>
      <ErrorBanner message={message} />
      {message.includes('No tienes permiso') && (
        <div className="home__error-actions">
          <p>
            {typeof sessionStorage !== 'undefined' &&
            sessionStorage.getItem('ioda_first_user_refresh_failed') === '1'
              ? 'Parece que acabas de registrarte; el refresco de permisos no pudo completarse. Cierra sesión e inicia sesión de nuevo para actualizar tus permisos.'
              : 'Si acabas de registrarte como primer usuario, cierra sesión e inicia sesión de nuevo para actualizar tus permisos.'}{' '}
            <button
              type="button"
              className="home__btn home__btn--secondary home__btn--small"
              onClick={handleLogout}
            >
              Cerrar sesión e iniciar de nuevo
            </button>
          </p>
        </div>
      )}
      {message.includes('autenticación JWT configurada') && (
        <p className="home__error-actions">{message} Recarga la página cuando hayas reiniciado la Core API.</p>
      )}
      {message.includes('Parámetros de búsqueda no válidos') &&
        !message.includes('autenticación JWT') && (
          <div className="home__error-actions">
            <p>
              Error de parámetros al cargar proyectos. Si acabas de registrarte como primer usuario, cierra sesión e inicia sesión de nuevo; si no, recarga la página o contacta soporte.
            </p>
            <button type="button" className="home__btn home__btn--secondary home__btn--small" onClick={handleLogout}>
              Cerrar sesión e iniciar de nuevo
            </button>
          </div>
        )}
    </div>
  )
}
