import { useEffect, useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { authApi } from '../api/auth-api'
import { useAuthStore } from '../store/auth-store'
import { authorizationApi } from '../../authorization/api/authorization-api'
import type { ApiError } from '../../../shared/api'
import type { SetupStatus } from '../types'

function getRegisterErrorMessage(err: unknown): string {
  if (err instanceof TypeError && (err.message === 'Failed to fetch' || err.message.includes('Load failed'))) {
    return 'No se puede conectar con el servidor. ¿Está la Identity API en ejecución?'
  }
  const apiErr = err as ApiError
  if (apiErr?.status === 403) return 'El auto-registro está deshabilitado. Contacta a un administrador.'
  if (apiErr?.message) return apiErr.message
  return 'Error al registrar'
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
    marginBottom: '0.5rem',
    fontSize: '1.5rem',
    fontWeight: 700,
    textAlign: 'center',
    color: 'var(--page-text)',
  },
  subtitle: {
    marginTop: 0,
    marginBottom: '1.5rem',
    fontSize: '0.875rem',
    textAlign: 'center',
    color: 'var(--page-text-secondary, #666)',
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
  setupProgress: {
    fontSize: '0.875rem',
    color: 'var(--page-text-secondary, #666)',
    textAlign: 'center',
    padding: '1rem 0',
  },
  link: {
    color: '#0d6efd',
    textDecoration: 'none',
    fontSize: '0.875rem',
    textAlign: 'center',
    display: 'block',
    marginTop: '1rem',
  },
  badge: {
    display: 'inline-block',
    padding: '0.25rem 0.75rem',
    background: '#ffc107',
    color: '#333',
    borderRadius: 20,
    fontSize: '0.75rem',
    fontWeight: 600,
    letterSpacing: '0.02em',
    marginBottom: '0.25rem',
  },
  blocked: {
    textAlign: 'center',
    padding: '2rem 0',
    fontSize: '0.95rem',
    color: 'var(--page-text-secondary, #666)',
  },
}

export function RegisterPage() {
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [setupStatus, setSetupStatus] = useState<SetupStatus | null>(null)
  const [setupStep, setSetupStep] = useState<string | null>(null)

  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)

  useEffect(() => {
    if (isAuthenticated) {
      navigate('/', { replace: true })
      return
    }
    let cancelled = false
    authApi.getSetupStatus().then((status) => {
      if (!cancelled) setSetupStatus(status)
    }).catch(() => {
      // Si falla, asumimos que hay usuarios y auto-registro habilitado
      if (!cancelled) setSetupStatus({ hasUsers: true, selfRegistrationEnabled: true })
    })
    return () => { cancelled = true }
  }, [isAuthenticated, navigate])

  const isFirstUserSetup = setupStatus !== null && !setupStatus.hasUsers
  const registrationBlocked = setupStatus !== null && setupStatus.hasUsers && !setupStatus.selfRegistrationEnabled

  /** Configurar SuperAdmin: rol + asignar todos los permisos (GET /permissions) + regla de acceso. No crea permisos (backend/catálogo). */
  const setupSuperAdmin = async (userId: string) => {
    // Si el backend ya asignó reglas al primer usuario (ej. bootstrap-first-user), no hacer setup
    try {
      const existingRules = await authorizationApi.getUserRules(userId)
      if (existingRules.length > 0) return
    } catch { /* seguir con setup si falla la consulta */ }

    setSetupStep('Obteniendo permisos del sistema…')
    let allPermIds: string[] = []
    try {
      const perms = await authorizationApi.getPermissions()
      allPermIds = perms.map((p) => p.id)
    } catch { /* permisos no disponibles */ }

    setSetupStep('Creando rol SuperAdmin…')
    let roleId: string
    try {
      roleId = await authorizationApi.createRole({
        name: 'SuperAdmin',
        description: 'Administrador con acceso total al sistema',
      })
    } catch {
      // Role may already exist, try to find it
      const roles = await authorizationApi.getRoles()
      const existing = roles.find((r) => r.name === 'SuperAdmin')
      if (!existing) throw new Error('No se pudo crear el rol SuperAdmin')
      roleId = existing.id
    }

    setSetupStep('Asignando permisos al rol…')
    if (allPermIds.length > 0) {
      await authorizationApi.assignPermissionsToRole(roleId, { permissionIds: allPermIds })
    }

    // Create access rule for the user
    setSetupStep('Asignando rol al usuario…')
    await authorizationApi.createAccessRule({
      userId,
      roleId,
    })
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setLoading(true)
    setSetupStep(null)
    try {
      // 1. Register
      const result = await authApi.register({
        email,
        password,
        displayName: displayName.trim() || undefined,
      })

      if (result.isFirstUser) {
        // 2. Auto-login to get JWT
        setSetupStep('Iniciando sesión automáticamente…')
        const loginResult = await authApi.login({ email, password })
        useAuthStore.getState().setSession(loginResult)

        // 3. Setup SuperAdmin (rol, permisos al rol, regla de acceso)
        try {
          await setupSuperAdmin(result.userId)
        } catch (err) {
          console.warn('SuperAdmin setup partially failed:', err)
          // Still navigate to home since user is logged in
        }

        // 4. Refresco de token para obtener JWT con permisos (Identity + Authorization ya tienen la regla)
        setSetupStep('Actualizando sesión con permisos…')
        try {
          await useAuthStore.getState().refreshSession()
          // setSession (dentro de refreshSession) ya invalida la caché de permisos
        } catch {
          // Si falla el refresh, el usuario sigue logueado con el token anterior (sin permisos en JWT)
          // La siguiente petición podría recibir 403 hasta que haga login de nuevo
        }

        setSetupStep(null)
        navigate('/', { replace: true })
      } else {
        navigate('/login', { replace: true })
      }
    } catch (err) {
      const message = getRegisterErrorMessage(err)
      setError(message)
      setSetupStep(null)
    } finally {
      setLoading(false)
    }
  }

  // Waiting for setup status
  if (setupStatus === null) {
    return (
      <div style={s.container}>
        <div style={s.card}>
          <p style={{ textAlign: 'center', color: 'var(--page-text-secondary, #666)' }}>Cargando…</p>
        </div>
      </div>
    )
  }

  // Registration blocked
  if (registrationBlocked) {
    return (
      <div style={s.container}>
        <div style={s.card}>
          <h1 style={s.title}>Registro deshabilitado</h1>
          <p style={s.blocked}>
            El auto-registro está deshabilitado en este sistema.<br />
            Contacta a un administrador para obtener una cuenta.
          </p>
          <Link to="/login" style={s.link}>
            Ir a iniciar sesión
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div style={s.container}>
      <div style={s.card}>
        {isFirstUserSetup && (
          <div style={{ textAlign: 'center', marginBottom: '0.5rem' }}>
            <span style={s.badge}>CONFIGURACIÓN INICIAL</span>
          </div>
        )}
        <h1 style={s.title}>
          {isFirstUserSetup ? 'Crear administrador' : 'Crear cuenta'}
        </h1>
        <p style={s.subtitle}>
          {isFirstUserSetup
            ? 'Este será el primer usuario y se le asignará el rol de SuperAdmin automáticamente.'
            : 'Completa los datos para registrarte en el sistema.'}
        </p>

        {setupStep && (
          <div style={s.setupProgress}>
            <span style={{ display: 'inline-block', marginRight: 6, animation: 'spin 1s linear infinite' }}>⏳</span>
            {setupStep}
          </div>
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
            <label style={s.label} htmlFor="displayName">Nombre</label>
            <input
              id="displayName"
              type="text"
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              style={s.input}
              autoComplete="name"
              disabled={loading}
              placeholder={isFirstUserSetup ? 'Nombre del administrador' : 'Opcional'}
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
              autoComplete="new-password"
              disabled={loading}
              minLength={6}
            />
          </div>
          {error && <p style={s.error}>{error}</p>}
          <button type="submit" style={{ ...s.button, opacity: loading ? 0.7 : 1 }} disabled={loading}>
            {loading
              ? (isFirstUserSetup ? 'Configurando sistema…' : 'Registrando…')
              : (isFirstUserSetup ? 'Crear administrador y configurar' : 'Registrarse')}
          </button>
        </form>
        {!isFirstUserSetup && (
          <Link to="/login" style={s.link}>
            Ya tengo cuenta
          </Link>
        )}
      </div>
    </div>
  )
}
