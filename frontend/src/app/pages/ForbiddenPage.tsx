import { Link, useLocation } from 'react-router-dom'
import './ForbiddenPage.css'

export function ForbiddenPage() {
  const location = useLocation()
  const state = location.state as { permission?: string } | undefined
  const permission = state?.permission

  return (
    <div className="forbidden-page">
      <h1 className="forbidden-page__title">Sin permiso</h1>
      <p>
        No tienes permiso para acceder a esta página
        {permission ? ` (se requiere: ${permission})` : ''}.
      </p>
      <p>
        <Link to="/" className="forbidden-page__link">
          Volver al inicio
        </Link>
      </p>
    </div>
  )
}
