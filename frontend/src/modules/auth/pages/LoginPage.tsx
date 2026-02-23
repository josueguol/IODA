import { useState, useEffect } from 'react'
import { useNavigate, Link, useSearchParams } from 'react-router-dom'
import { useAuthStore } from '../store/auth-store'
import { authApi } from '../api/auth-api'
import { AuthLayout } from '../components/AuthLayout'
import '../components/AuthForm.css'
import type { ApiError } from '../../../shared/api'
import type { SetupStatus } from '../types'

function getLoginErrorMessage(err: unknown): string {
  if (err instanceof TypeError && (err.message === 'Failed to fetch' || err.message.includes('Load failed'))) {
    return 'No se puede conectar con el servidor. ¿Está la Identity API en ejecución? (ej. http://localhost:5270)'
  }
  const apiErr = err as ApiError
  if (apiErr?.message) return apiErr.message
  return 'Error al iniciar sesión'
}

export function LoginPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const setSession = useAuthStore((s) => s.setSession)
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const isRehydrating = useAuthStore((s) => s.isRehydrating)
  const [setupStatus, setSetupStatus] = useState<SetupStatus | null>(null)
  const permissionsChanged = searchParams.get('reason') === 'permissions_changed'

  useEffect(() => {
    if (!isRehydrating && isAuthenticated) {
      navigate('/', { replace: true })
      return
    }
    let cancelled = false
    authApi.getSetupStatus().then((status) => {
      if (!cancelled) setSetupStatus(status)
    }).catch(() => {
      if (!cancelled) setSetupStatus({ hasUsers: true, selfRegistrationEnabled: true })
    })
    return () => { cancelled = true }
  }, [isRehydrating, isAuthenticated, navigate])

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      const result = await authApi.login({ email, password })
      setSession(result)
      navigate('/', { replace: true })
    } catch (err) {
      setError(getLoginErrorMessage(err))
    } finally {
      setLoading(false)
    }
  }

  if (isRehydrating) {
    return (
      <AuthLayout>
        <p className="auth-form__progress" aria-live="polite">Cargando…</p>
      </AuthLayout>
    )
  }

  if (isAuthenticated) return null

  if (setupStatus && !setupStatus.hasUsers) {
    navigate('/register', { replace: true })
    return null
  }

  const showRegisterLink = setupStatus === null || !setupStatus.hasUsers || setupStatus.selfRegistrationEnabled

  return (
    <AuthLayout>
      <h1 className="auth-form__title">Iniciar sesión</h1>
      {permissionsChanged && (
        <p className="auth-form__notice" role="alert">
          Sus permisos han cambiado. Inicia sesión de nuevo.
        </p>
      )}
      <form className="auth-form" onSubmit={handleSubmit} noValidate>
        <div className="auth-form__field">
          <label className="auth-form__label" htmlFor="login-email">
            Correo
          </label>
          <input
            id="login-email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="auth-form__input"
            required
            autoComplete="email"
            disabled={loading}
            aria-invalid={!!error}
          />
        </div>
        <div className="auth-form__field">
          <label className="auth-form__label" htmlFor="login-password">
            Contraseña
          </label>
          <input
            id="login-password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="auth-form__input"
            required
            autoComplete="current-password"
            disabled={loading}
            aria-invalid={!!error}
          />
        </div>
        {error && (
          <p className="auth-form__error" role="alert">
            {error}
          </p>
        )}
        <button type="submit" className="auth-form__submit" disabled={loading}>
          {loading ? 'Entrando…' : 'Entrar'}
        </button>
      </form>
      {showRegisterLink && (
        <Link to="/register" className="auth-form__link">
          Crear una cuenta
        </Link>
      )}
    </AuthLayout>
  )
}
