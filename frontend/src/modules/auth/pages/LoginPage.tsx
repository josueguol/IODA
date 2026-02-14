import { useState, useEffect } from 'react'
import { useNavigate, Link, useSearchParams } from 'react-router-dom'
import { useAuthStore } from '../store/auth-store'
import { authApi } from '../api/auth-api'
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

const s: Record<string, React.CSSProperties> = {
  container: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: '100vh',
    padding: '1rem',
    background: 'var(--page-bg)',
    color: 'var(--page-text)',
    fontFamily: 'system-ui, sans-serif',
  },
  card: {
    width: '100%',
    maxWidth: 420,
    padding: '2rem',
    background: 'var(--page-bg-elevated, #fff)',
    borderRadius: 12,
    border: '1px solid var(--page-border, #e0e0e0)',
    boxShadow: '0 4px 16px rgba(0,0,0,0.08)',
  },
  title: {
    marginTop: 0,
    marginBottom: '1.5rem',
    fontSize: '1.5rem',
    fontWeight: 700,
    textAlign: 'center',
    color: 'var(--page-text)',
  },
  form: { display: 'flex', flexDirection: 'column', gap: '1rem' },
  label: { display: 'block', marginBottom: 4, fontWeight: 500, fontSize: '0.875rem' },
  input: {
    width: '100%',
    padding: '0.65rem 0.75rem',
    border: '1px solid var(--input-border, #ccc)',
    borderRadius: 6,
    boxSizing: 'border-box' as const,
    fontSize: '0.9375rem',
    color: 'var(--input-text, #222)',
    background: 'var(--input-bg, #fff)',
  },
  button: {
    padding: '0.7rem 1rem',
    background: '#0d6efd',
    color: 'white',
    border: 'none',
    borderRadius: 6,
    cursor: 'pointer',
    fontWeight: 600,
    fontSize: '0.9375rem',
    marginTop: '0.25rem',
  },
  error: {
    color: '#dc3545',
    fontSize: '0.875rem',
    padding: '0.5rem 0.75rem',
    background: '#f8d7da',
    borderRadius: 6,
  },
  link: {
    color: '#0d6efd',
    textDecoration: 'none',
    fontSize: '0.875rem',
    textAlign: 'center',
    display: 'block',
    marginTop: '1rem',
  },
  permissionsChanged: {
    fontSize: '0.875rem',
    padding: '0.5rem 0.75rem',
    background: '#fff3cd',
    color: '#856404',
    borderRadius: 6,
    marginBottom: '1rem',
  },
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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      const result = await authApi.login({ email, password })
      setSession(result)
      navigate('/', { replace: true })
    } catch (err) {
      const message = getLoginErrorMessage(err)
      setError(message)
    } finally {
      setLoading(false)
    }
  }

  if (isRehydrating) {
    return (
      <div style={s.container}>
        <div style={s.card}>
          <p style={{ textAlign: 'center', color: 'var(--page-text-secondary, #666)' }}>Cargando…</p>
        </div>
      </div>
    )
  }

  if (isAuthenticated) return null

  // If no users exist, redirect to register (first-user setup)
  if (setupStatus && !setupStatus.hasUsers) {
    navigate('/register', { replace: true })
    return null
  }

  const showRegisterLink = setupStatus === null || !setupStatus.hasUsers || setupStatus.selfRegistrationEnabled

  return (
    <div style={s.container}>
      <div style={s.card}>
        <h1 style={s.title}>Iniciar sesión</h1>
        {permissionsChanged && (
          <p style={s.permissionsChanged}>
            Sus permisos han cambiado. Inicia sesión de nuevo.
          </p>
        )}
        <form style={s.form} onSubmit={handleSubmit}>
          <div>
            <label style={s.label} htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              style={s.input}
              required
              autoComplete="email"
              disabled={loading}
            />
          </div>
          <div>
            <label style={s.label} htmlFor="password">Contraseña</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              style={s.input}
              required
              autoComplete="current-password"
              disabled={loading}
            />
          </div>
          {error && <p style={s.error}>{error}</p>}
          <button type="submit" style={{ ...s.button, opacity: loading ? 0.7 : 1 }} disabled={loading}>
            {loading ? 'Entrando…' : 'Entrar'}
          </button>
        </form>
        {showRegisterLink && (
          <Link to="/register" style={s.link}>
            ¿No tienes cuenta? Crear cuenta
          </Link>
        )}
      </div>
    </div>
  )
}
