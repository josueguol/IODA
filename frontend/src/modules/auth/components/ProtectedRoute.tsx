import { Navigate, useLocation } from 'react-router-dom'
import { useAuthStore } from '../store/auth-store'

interface ProtectedRouteProps {
  children: React.ReactNode
}

/** Redirige a /login si no hay sesión. Muestra nada (o un loader) mientras rehidrata. */
export function ProtectedRoute({ children }: ProtectedRouteProps) {
  const location = useLocation()
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const isRehydrating = useAuthStore((s) => s.isRehydrating)

  if (isRehydrating) {
    return (
      <div style={{ padding: '2rem', fontFamily: 'system-ui', textAlign: 'center' }}>
        Cargando…
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  return <>{children}</>
}
