import { useEffect, useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { authApi } from '../api/auth-api'
import { useAuthStore } from '../store/auth-store'
import { authorizationApi } from '../../authorization/api/authorization-api'
import { parsePermissionsFromAccessToken } from '../../authorization/utils/jwt-permissions'
import { config } from '../../../config/env'
import { buildLoginRedirect } from '../../../shared/auth-redirect'
import { AuthLayout } from '../components/AuthLayout'
import '../components/AuthForm.css'
import type { ApiError } from '../../../shared/api'
import type { SetupStatus } from '../types'

/** Espera `ms` milisegundos. */
const wait = (ms: number) => new Promise<void>((r) => setTimeout(r, ms))

/** Intenta refreshSession con un reintento tras retraso (cubre latencia de persistencia en Authorization). */
async function refreshWithRetry(): Promise<boolean> {
  try {
    await useAuthStore.getState().refreshSession()
    return true
  } catch {
    // Primer intento falló — esperar y reintentar
    await wait(1500)
    try {
      await useAuthStore.getState().refreshSession()
      return true
    } catch {
      return false
    }
  }
}

function getRegisterErrorMessage(err: unknown): string {
  if (err instanceof TypeError && (err.message === 'Failed to fetch' || err.message.includes('Load failed'))) {
    return 'No se puede conectar con el servidor. ¿Está la Identity API en ejecución?'
  }
  const apiErr = err as ApiError
  if (apiErr?.status === 403) return 'El auto-registro está deshabilitado. Contacta a un administrador.'
  if (apiErr?.message) return apiErr.message
  return 'Error al registrar'
}

export function RegisterPage() {
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [setupStatus, setSetupStatus] = useState<SetupStatus | null>(null)
  const [setupStep, setSetupStep] = useState<string | null>(null)
  /** Tras refresh exitoso, si el JWT no incluye permisos se muestra aviso y opción Continuar/Cerrar sesión. */
  const [postSetupJwtNoPermissions, setPostSetupJwtNoPermissions] = useState(false)

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

  /**
   * Configurar SuperAdmin: rol + asignar todos los permisos (GET /permissions) + regla de acceso.
   * No crea permisos (backend/catálogo).
   * Distingue errores 401 (no autenticado), 403 (sin permiso / modo bootstrap no activo)
   * y 409 (recurso ya existe / bootstrap ya hecho).
   */
  const setupSuperAdmin = async (userId: string) => {
    // Si el backend ya asignó reglas al primer usuario (ej. bootstrap-first-user), no hacer setup
    try {
      const existingRules = await authorizationApi.getUserRules(userId)
      if (existingRules.length > 0) return
    } catch (err) {
      // Si falla con 401/403 es posible que el JWT no tenga permisos y no haya modo bootstrap;
      // continuamos para intentar crear desde cero (modo bootstrap permite acceso Admin cuando 0 reglas)
      const status = (err as ApiError)?.status
      console.warn(`[setupSuperAdmin] getUserRules falló (status: ${status ?? 'unknown'}):`, err)
    }

    setSetupStep('Obteniendo permisos del sistema…')
    let allPermIds: string[] = []
    try {
      const perms = await authorizationApi.getPermissions()
      allPermIds = perms.map((p) => p.id)
    } catch (err) {
      const status = (err as ApiError)?.status
      if (status === 403) {
        console.warn('[setupSuperAdmin] getPermissions 403: sin permiso; el modo bootstrap puede permitir el resto')
      } else {
        console.warn(`[setupSuperAdmin] getPermissions falló (status: ${status ?? 'unknown'}):`, err)
      }
    }

    if (allPermIds.length === 0) {
      setSetupStep('⚠ No se encontraron permisos en el sistema. Verifica que la Authorization API esté en ejecución.')
      // Continuar el flujo: el rol se puede crear y los permisos se asignarán cuando los seeders corran
    }

    setSetupStep('Creando rol SuperAdmin…')
    let roleId: string
    try {
      roleId = await authorizationApi.createRole({
        name: 'SuperAdmin',
        description: 'Administrador con acceso total al sistema',
      })
    } catch (err) {
      const status = (err as ApiError)?.status
      if (status === 403) {
        console.warn('[setupSuperAdmin] createRole 403: sin permiso role.manage y modo bootstrap no activo')
      } else if (status === 409) {
        console.info('[setupSuperAdmin] createRole 409: el rol SuperAdmin ya existe')
      } else {
        console.warn(`[setupSuperAdmin] createRole falló (status: ${status ?? 'unknown'}):`, err)
      }
      // El rol puede ya existir, intentar buscarlo
      const roles = await authorizationApi.getRoles()
      const existing = roles.find((r) => r.name === 'SuperAdmin')
      if (!existing) throw new Error('No se pudo crear ni encontrar el rol SuperAdmin')
      roleId = existing.id
    }

    setSetupStep('Asignando permisos al rol…')
    if (allPermIds.length > 0) {
      try {
        await authorizationApi.assignPermissionsToRole(roleId, { permissionIds: allPermIds })
      } catch (err) {
        const status = (err as ApiError)?.status
        console.warn(`[setupSuperAdmin] assignPermissionsToRole falló (status: ${status ?? 'unknown'}):`, err)
      }
    }

    // Crear regla de acceso para el usuario
    setSetupStep('Asignando rol al usuario…')
    try {
      await authorizationApi.createAccessRule({ userId, roleId })
    } catch (err) {
      const status = (err as ApiError)?.status
      if (status === 409) {
        console.info('[setupSuperAdmin] createAccessRule 409: la regla ya existe (bootstrap ya hecho)')
      } else {
        console.warn(`[setupSuperAdmin] createAccessRule falló (status: ${status ?? 'unknown'}):`, err)
        throw err
      }
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    if (isFirstUserSetup && password !== confirmPassword) {
      setError('Las contraseñas no coinciden.')
      return
    }
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
        //    Se reintenta una vez tras 1.5 s para cubrir latencia de persistencia en Authorization
        setSetupStep('Actualizando sesión con permisos…')
        const refreshOk = await refreshWithRetry()

        setSetupStep(null)

        if (!refreshOk) {
          // El usuario queda logueado pero su JWT no tiene permisos → mostrar aviso y marcar para Home
          setError(
            'Tu sesión no tiene permisos aún. Cierra sesión e inicia sesión de nuevo para obtener acceso completo.'
          )
          try {
            sessionStorage.setItem('ioda_first_user_refresh_failed', '1')
          } catch { /* ignore */ }
          navigate('/', { replace: true })
          return
        }

        // Verificación post-setup: si el JWT no incluye permisos, advertir pero permitir continuar (Paso 6)
        const token = useAuthStore.getState().accessToken
        const permissions = parsePermissionsFromAccessToken(token ?? null)
        if (permissions.length === 0) {
          setPostSetupJwtNoPermissions(true)
          return
        }
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

  if (setupStatus === null) {
    return (
      <AuthLayout>
        <p className="auth-form__progress" aria-live="polite">Cargando…</p>
      </AuthLayout>
    )
  }

  if (registrationBlocked) {
    return (
      <AuthLayout>
        <h1 className="auth-form__title">Registro deshabilitado</h1>
        <p className="auth-form__helper">
          El auto-registro está deshabilitado en este sistema.
          Contacta a un administrador para obtener una cuenta.
        </p>
        <Link to="/login" className="auth-form__link">
          Ir a iniciar sesión
        </Link>
      </AuthLayout>
    )
  }

  if (postSetupJwtNoPermissions) {
    return (
      <AuthLayout>
        <h1 className="auth-form__title">Configuración completada</h1>
        <p className="auth-form__subtitle auth-form__subtitle--spaced">
          Tu sesión se creó pero el token no incluye permisos. Esto puede deberse a la configuración del backend (Identity ↔ Authorization).
        </p>
        <p className="auth-form__helper">
          Puedes continuar pero algunas funciones estarán limitadas hasta que inicies sesión de nuevo con el backend correctamente configurado.
        </p>
        <div className="auth-form__actions">
          <button
            type="button"
            className="auth-form__submit"
            onClick={() => navigate('/', { replace: true })}
          >
            Continuar de todos modos
          </button>
          <button
            type="button"
            className="auth-form__submit auth-form__submit--secondary"
            onClick={() => {
              useAuthStore.getState().logout()
              window.location.href = buildLoginRedirect(config.routerType)
            }}
          >
            Cerrar sesión
          </button>
        </div>
      </AuthLayout>
    )
  }

  return (
    <AuthLayout>
      {isFirstUserSetup && (
        <div className="auth-form__badge-wrap">
          <span className="auth-form__badge" role="status">CONFIGURACIÓN INICIAL</span>
        </div>
      )}
      <h1 className="auth-form__title">
        {isFirstUserSetup ? 'Crear administrador' : 'Crear cuenta'}
      </h1>
      <p className="auth-form__subtitle">
        {isFirstUserSetup
          ? 'Este será el primer usuario y se le asignará el rol de SuperAdmin automáticamente.'
          : 'Completa los datos para registrarte en el sistema.'}
      </p>

      {setupStep && (
        <p className="auth-form__progress" role="status" aria-live="polite">
          <span className="auth-form__progress-spinner" aria-hidden>⏳</span>
          {setupStep}
        </p>
      )}

      <form className="auth-form" onSubmit={handleSubmit} noValidate>
        <div className="auth-form__field">
          <label className="auth-form__label" htmlFor="register-email">Correo</label>
          <input
            id="register-email"
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
          <label className="auth-form__label" htmlFor="register-displayName">Nombre</label>
          <input
            id="register-displayName"
            type="text"
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            className="auth-form__input"
            autoComplete="name"
            disabled={loading}
            placeholder={isFirstUserSetup ? 'Nombre del administrador' : 'Opcional'}
          />
        </div>
        <div className="auth-form__field">
          <label className="auth-form__label" htmlFor="register-password">Contraseña</label>
          <input
            id="register-password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="auth-form__input"
            required
            autoComplete="new-password"
            disabled={loading}
            minLength={6}
            aria-invalid={!!error}
          />
        </div>
        {isFirstUserSetup && (
          <div className="auth-form__field">
            <label className="auth-form__label" htmlFor="register-confirmPassword">Confirmar contraseña</label>
            <input
              id="register-confirmPassword"
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              className="auth-form__input"
              required
              autoComplete="new-password"
              disabled={loading}
              minLength={6}
              aria-invalid={!!error}
            />
          </div>
        )}
        {error && (
          <p className="auth-form__error" role="alert">
            {error}
          </p>
        )}
        <button type="submit" className="auth-form__submit" disabled={loading}>
          {loading
            ? (isFirstUserSetup ? 'Configurando sistema…' : 'Registrando…')
            : (isFirstUserSetup ? 'Crear y configurar' : 'Registrarse')}
        </button>
      </form>
      {!isFirstUserSetup && (
        <Link to="/login" className="auth-form__link">
          Ya tengo cuenta
        </Link>
      )}
    </AuthLayout>
  )
}
