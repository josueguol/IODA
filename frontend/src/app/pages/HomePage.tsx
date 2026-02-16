import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { config } from '../../config/env'
import { coreApi } from '../../modules/core/api/core-api'
import { useContextStore } from '../../modules/core/store/context-store'
import { useAuthStore } from '../../modules/auth/store/auth-store'
import { Can } from '../../modules/authorization/components/Can'
import { buildLoginRedirect } from '../../shared/auth-redirect'
import { LoadingSpinner, ErrorBanner } from '../../shared/components'

const styles: Record<string, React.CSSProperties> = {
  container: { maxWidth: 720, margin: '0 auto', color: 'var(--page-text)' },
  title: { marginTop: 0, marginBottom: '0.5rem', fontSize: '1.5rem', color: 'var(--page-text)' },
  step: { marginBottom: '2rem', padding: '1.25rem', background: 'var(--page-bg-elevated)', borderRadius: 8, border: '1px solid var(--page-border)', color: 'var(--page-text)' },
  stepTitle: { margin: '0 0 0.75rem 0', fontSize: '1rem', color: 'var(--page-text)', fontWeight: 600 },
  hint: { color: 'var(--page-text-muted)', fontSize: '0.875rem', marginBottom: '1rem' },
  list: { listStyle: 'none', padding: 0, margin: 0 },
  card: {
    display: 'block',
    width: '100%',
    padding: '0.75rem 1rem',
    marginBottom: '0.5rem',
    background: 'var(--card-bg)',
    borderWidth: 1,
    borderStyle: 'solid',
    borderColor: 'var(--page-border)',
    borderRadius: 6,
    cursor: 'pointer',
    textAlign: 'left',
    fontSize: '0.9375rem',
    color: 'var(--page-text)',
  },
  cardHover: { borderColor: '#0d6efd', color: '#0d6efd' },
  cardSelected: { borderColor: '#0d6efd', background: 'var(--card-selected-bg)', color: '#0d6efd' },
  actions: { marginTop: '1rem', display: 'flex', gap: '0.75rem', flexWrap: 'wrap' },
  button: {
    padding: '0.5rem 1rem',
    background: '#0d6efd',
    color: 'white',
    border: 'none',
    borderRadius: 6,
    cursor: 'pointer',
    fontSize: '0.875rem',
    textDecoration: 'none',
  },
  buttonSecondary: { background: '#6c757d' },
  dashboard: { marginTop: '1.5rem' },
  widgetGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
    gap: '0.75rem',
    marginTop: '1rem',
  },
  widget: {
    display: 'flex',
    flexDirection: 'column' as const,
    padding: '1.25rem',
    background: 'var(--page-bg-elevated)',
    border: '1px solid var(--page-border)',
    borderRadius: 10,
    textDecoration: 'none',
    color: 'var(--page-text)',
    transition: 'border-color 0.15s, box-shadow 0.15s',
  },
  widgetIcon: {
    fontSize: '1.5rem',
    marginBottom: '0.5rem',
  },
  widgetTitle: {
    fontSize: '0.9375rem',
    fontWeight: 600,
    marginBottom: '0.25rem',
    color: 'var(--page-text)',
  },
  widgetDesc: {
    fontSize: '0.8125rem',
    color: 'var(--page-text-muted)',
    margin: 0,
    lineHeight: 1.4,
  },
  contextWidget: {
    padding: '1.25rem',
    background: 'var(--page-bg-elevated)',
    border: '1px solid var(--page-border)',
    borderRadius: 10,
    marginBottom: '1.25rem',
  },
  contextRow: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: '0.5rem 0',
    borderBottom: '1px solid var(--page-border)',
  },
  contextRowLast: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: '0.5rem 0',
  },
  contextRowLabel: {
    fontSize: '0.8125rem',
    color: 'var(--page-text-muted)',
    minWidth: 70,
  },
  contextRowValue: {
    fontSize: '0.9375rem',
    fontWeight: 500,
    color: 'var(--page-text)',
    flex: 1,
    marginLeft: '0.5rem',
  },
  contextChangeBtn: {
    fontSize: '0.75rem',
    padding: '0.2rem 0.5rem',
    background: 'transparent',
    color: '#0d6efd',
    border: '1px solid #0d6efd',
    borderRadius: 4,
    cursor: 'pointer',
    marginLeft: '0.5rem',
    whiteSpace: 'nowrap' as const,
    flexShrink: 0,
  },
  sectionTitle: {
    fontSize: '0.8125rem',
    fontWeight: 600,
    color: 'var(--page-text-muted)',
    textTransform: 'uppercase' as const,
    letterSpacing: '0.05em',
    margin: '1.5rem 0 0.5rem 0',
  },
  formRow: { marginBottom: '0.75rem' },
  input: { width: '100%', maxWidth: 320, padding: '0.5rem', fontSize: '0.875rem', borderRadius: 4, border: '1px solid var(--input-border)', color: 'var(--input-text)', background: 'var(--input-bg)' },
}

export function HomePage() {
  const user = useAuthStore((s) => s.user)
  const {
    currentProjectId,
    currentEnvironmentId,
    currentSiteId,
    projects,
    environments,
    sites,
    projectsLoading,
    environmentsLoading,
    sitesLoading,
    projectsError,
    environmentsError,
    sitesError,
    setProject,
    setEnvironment,
    setSite,
    loadProjects,
    loadEnvironments,
    loadSites,
  } = useContextStore()

  const [showCreateProject, setShowCreateProject] = useState(false)
  const [savingProject, setSavingProject] = useState(false)
  const [newProjectName, setNewProjectName] = useState('')
  const [newProjectDesc, setNewProjectDesc] = useState('')
  const [createProjectError, setCreateProjectError] = useState<string | null>(null)
  const [showCreateEnv, setShowCreateEnv] = useState(false)
  const [savingEnv, setSavingEnv] = useState(false)
  const [newEnvName, setNewEnvName] = useState('')
  const [newEnvDesc, setNewEnvDesc] = useState('')
  const [createEnvError, setCreateEnvError] = useState<string | null>(null)
  const [showCreateSite, setShowCreateSite] = useState(false)
  const [savingSite, setSavingSite] = useState(false)
  const [newSiteName, setNewSiteName] = useState('')
  const [newSiteDomain, setNewSiteDomain] = useState('')
  const [newSiteSubdomain, setNewSiteSubdomain] = useState('')
  const [newSiteSubpath, setNewSiteSubpath] = useState('')
  const [newSiteThemeId, setNewSiteThemeId] = useState('')
  const [createSiteError, setCreateSiteError] = useState<string | null>(null)

  useEffect(() => {
    loadProjects().catch(() => {})
  }, [loadProjects])

  useEffect(() => {
    if (currentProjectId) {
      loadEnvironments(currentProjectId).catch(() => {})
    }
  }, [currentProjectId, loadEnvironments])

  useEffect(() => {
    if (currentProjectId && currentEnvironmentId) {
      loadSites(currentProjectId, currentEnvironmentId).catch(() => {})
    }
  }, [currentProjectId, currentEnvironmentId, loadSites])

  const currentProject = projects.find((p) => p.id === currentProjectId)
  const currentEnvironment = environments.find((e) => e.id === currentEnvironmentId)
  const currentSite = sites.find((s) => s.id === currentSiteId)
  const hasFullContext = Boolean(currentProjectId && currentEnvironmentId && currentSiteId)
  const contextState: 'EMPTY' | 'PROJECT_SELECTED' | 'PROJECT_ENV_SELECTED' | 'FULL_CONTEXT' =
    !currentProjectId
      ? 'EMPTY'
      : !currentEnvironmentId
        ? 'PROJECT_SELECTED'
        : !currentSiteId
          ? 'PROJECT_ENV_SELECTED'
          : 'FULL_CONTEXT'

  const handleToggleCreateProject = () => {
    setCreateProjectError(null)
    setShowCreateProject((x) => !x)
  }

  const handleCreateProject = async () => {
    setCreateProjectError(null)
    if (!user?.userId) {
      setCreateProjectError('No se pudo identificar tu usuario. Cierra sesión y vuelve a entrar.')
      return
    }
    if (!newProjectName.trim()) {
      setCreateProjectError('Escribe un nombre para el proyecto.')
      return
    }
    setSavingProject(true)
    try {
      const id = await coreApi.createProject({
        name: newProjectName.trim(),
        description: newProjectDesc.trim() || null,
        createdBy: user.userId,
      })
      if (id) {
        setNewProjectName('')
        setNewProjectDesc('')
        setShowCreateProject(false)
        await loadProjects()
        setProject(id)
      }
    } catch (e) {
      setCreateProjectError(e instanceof Error ? e.message : 'Error al crear proyecto')
    } finally {
      setSavingProject(false)
    }
  }

  const handleToggleCreateEnv = () => {
    setCreateEnvError(null)
    setShowCreateEnv((x) => !x)
  }

  const handleCreateEnvironment = async () => {
    setCreateEnvError(null)
    if (!currentProjectId) {
      setCreateEnvError('Selecciona un proyecto primero.')
      return
    }
    if (!user?.userId) {
      setCreateEnvError('No se pudo identificar tu usuario. Cierra sesión y vuelve a entrar.')
      return
    }
    if (!newEnvName.trim()) {
      setCreateEnvError('Escribe un nombre para el entorno.')
      return
    }
    setSavingEnv(true)
    try {
      const id = await coreApi.createEnvironment(currentProjectId, {
        name: newEnvName.trim(),
        description: newEnvDesc.trim() || null,
        createdBy: user.userId,
      })
      if (id) {
        setNewEnvName('')
        setNewEnvDesc('')
        setShowCreateEnv(false)
        await loadEnvironments(currentProjectId)
        setEnvironment(id)
      }
    } catch (e) {
      setCreateEnvError(e instanceof Error ? e.message : 'Error al crear entorno')
    } finally {
      setSavingEnv(false)
    }
  }

  const handleToggleCreateSite = () => {
    setCreateSiteError(null)
    setShowCreateSite((x) => !x)
  }

  const handleCreateSite = async () => {
    setCreateSiteError(null)
    if (!currentProjectId) {
      setCreateSiteError('Selecciona un proyecto primero.')
      return
    }
    if (!user?.userId) {
      setCreateSiteError('No se pudo identificar tu usuario. Cierra sesión y vuelve a entrar.')
      return
    }
    if (!newSiteName.trim()) {
      setCreateSiteError('Escribe un nombre para el sitio.')
      return
    }
    if (!newSiteDomain.trim()) {
      setCreateSiteError('Escribe un dominio para el sitio.')
      return
    }
    setSavingSite(true)
    try {
      const id = await coreApi.createSite(currentProjectId, {
        environmentId: currentEnvironmentId || null,
        name: newSiteName.trim(),
        domain: newSiteDomain.trim(),
        subdomain: newSiteSubdomain.trim() || null,
        subpath: newSiteSubpath.trim() || null,
        themeId: newSiteThemeId.trim() || null,
        createdBy: user.userId,
      })
      if (id) {
        setNewSiteName('')
        setNewSiteDomain('')
        setNewSiteSubdomain('')
        setNewSiteSubpath('')
        setNewSiteThemeId('')
        setShowCreateSite(false)
        await loadSites(currentProjectId, currentEnvironmentId ?? undefined)
        setSite(id)
      }
    } catch (e) {
      setCreateSiteError(e instanceof Error ? e.message : 'Error al crear sitio')
    } finally {
      setSavingSite(false)
    }
  }

  return (
    <div style={styles.container}>
      <h1 style={styles.title}>Acceso al CMS</h1>
      <p style={styles.hint}>
        Sigue los pasos a continuación para elegir un <strong>proyecto</strong>, un <strong>entorno</strong> y un <strong>sitio</strong>.
      </p>

      {projectsError && (
        <div>
          <ErrorBanner message={projectsError} />
          {projectsError.includes('No tienes permiso') && (
            <p style={{ fontSize: '0.875rem', color: 'var(--page-text-muted)', marginTop: '0.5rem' }}>
              {typeof sessionStorage !== 'undefined' && sessionStorage.getItem('ioda_first_user_refresh_failed') === '1'
                ? 'Parece que acabas de registrarte; el refresco de permisos no pudo completarse. Cierra sesión e inicia sesión de nuevo para actualizar tus permisos.'
                : 'Si acabas de registrarte como primer usuario, cierra sesión e inicia sesión de nuevo para actualizar tus permisos.'}{' '}
              <button
                type="button"
                style={{ ...styles.button, ...styles.buttonSecondary, padding: '0.25rem 0.75rem', fontSize: '0.8125rem' }}
                onClick={() => {
                  try { sessionStorage.removeItem('ioda_first_user_refresh_failed') } catch { /* ignore */ }
                  useAuthStore.getState().logout()
                  window.location.href = buildLoginRedirect(config.routerType)
                }}
              >
                Cerrar sesión e iniciar de nuevo
              </button>
            </p>
          )}
          {projectsError.includes('autenticación JWT configurada') && (
            <p style={{ fontSize: '0.875rem', color: 'var(--page-text-muted)', marginTop: '0.5rem' }}>
              {projectsError} Recarga la página cuando hayas reiniciado la Core API.
            </p>
          )}
          {projectsError.includes('Parámetros de búsqueda no válidos') && !projectsError.includes('autenticación JWT') && (
            <div style={{ marginTop: '0.5rem' }}>
              <p style={{ fontSize: '0.875rem', color: 'var(--page-text-muted)', marginBottom: '0.5rem' }}>
                Error de parámetros al cargar proyectos. Si acabas de registrarte como primer usuario, cierra sesión e inicia sesión de nuevo; si no, recarga la página o contacta soporte.
              </p>
              <button
                type="button"
                style={{ ...styles.button, ...styles.buttonSecondary, padding: '0.25rem 0.75rem', fontSize: '0.8125rem' }}
                onClick={() => {
                  useAuthStore.getState().logout()
                  window.location.href = buildLoginRedirect(config.routerType)
                }}
              >
                Cerrar sesión e iniciar de nuevo
              </button>
            </div>
          )}
        </div>
      )}
      {environmentsError && <ErrorBanner message={environmentsError} />}

      {/* Paso 1: Proyecto */}
      {contextState === 'EMPTY' && (
        <section style={styles.step}>
          <h2 style={styles.stepTitle}>Paso 1 — Selecciona un proyecto</h2>
          {projectsLoading && !projects.length ? (
            <LoadingSpinner text="Cargando proyectos…" />
          ) : (
            <>
              <ul style={styles.list}>
                {projects.map((p) => (
                  <li key={p.id}>
                    <button
                      type="button"
                      style={{
                        ...styles.card,
                        ...(currentProjectId === p.id ? styles.cardSelected : {}),
                      }}
                      onClick={() => setProject(p.id)}
                    >
                      <strong>{p.name}</strong>
                      {p.description && (
                        <span style={{ display: 'block', fontSize: '0.8125rem', color: 'var(--page-text-muted)', marginTop: '0.25rem' }}>
                          {p.description}
                        </span>
                      )}
                    </button>
                  </li>
                ))}
              </ul>
              <div style={styles.actions}>
                <Can permission="project.create" fallback={null}>
                  <button
                    type="button"
                    style={{ ...styles.button, ...styles.buttonSecondary }}
                    onClick={handleToggleCreateProject}
                    aria-expanded={showCreateProject}
                  >
                    {showCreateProject ? 'Cancelar' : 'Crear proyecto'}
                  </button>
                </Can>
              </div>
              {showCreateProject && (
                <div style={{ marginTop: '1rem', padding: '1rem', background: 'var(--input-bg)', borderRadius: 6, border: '1px solid var(--page-border)', color: 'var(--page-text)' }}>
                  <div style={styles.formRow}>
                    <label>Nombre del proyecto *</label>
                    <br />
                    <input
                      type="text"
                      style={styles.input}
                      value={newProjectName}
                      onChange={(e) => setNewProjectName(e.target.value)}
                      placeholder="Ej. Mi sitio"
                    />
                  </div>
                  <div style={styles.formRow}>
                    <label>Descripción (opcional)</label>
                    <br />
                    <input
                      type="text"
                      style={styles.input}
                      value={newProjectDesc}
                      onChange={(e) => setNewProjectDesc(e.target.value)}
                      placeholder="Breve descripción"
                    />
                  </div>
                  {createProjectError && <ErrorBanner message={createProjectError} />}
                  <button
                    type="button"
                    style={styles.button}
                    disabled={savingProject}
                    onClick={handleCreateProject}
                  >
                    {savingProject ? 'Creando…' : 'Crear'}
                  </button>
                </div>
              )}
            </>
          )}
        </section>
      )}

      {/* Paso 2: Entorno (solo si hay proyecto) */}
      {contextState === 'PROJECT_SELECTED' && (
        <section style={styles.step}>
          <h2 style={styles.stepTitle}>Paso 2 — Selecciona un entorno</h2>
          {environmentsLoading && !environments.length ? (
            <LoadingSpinner text="Cargando entornos…" />
          ) : (
            <>
              <ul style={styles.list}>
                {environments.map((e) => (
                  <li key={e.id}>
                    <button
                      type="button"
                      style={{
                        ...styles.card,
                        ...(currentEnvironmentId === e.id ? styles.cardSelected : {}),
                      }}
                      onClick={() => setEnvironment(e.id)}
                    >
                      <strong>{e.name}</strong>
                      {e.description && (
                        <span style={{ display: 'block', fontSize: '0.8125rem', color: 'var(--page-text-muted)', marginTop: '0.25rem' }}>
                          {e.description}
                        </span>
                      )}
                    </button>
                  </li>
                ))}
              </ul>
              <div style={styles.actions}>
                <Can permission="environment.create" fallback={null}>
                  <button
                    type="button"
                    style={{ ...styles.button, ...styles.buttonSecondary }}
                    onClick={handleToggleCreateEnv}
                    aria-expanded={showCreateEnv}
                  >
                    {showCreateEnv ? 'Cancelar' : 'Crear entorno'}
                  </button>
                </Can>
              </div>
              {showCreateEnv && (
                <div style={{ marginTop: '1rem', padding: '1rem', background: 'var(--input-bg)', borderRadius: 6, border: '1px solid var(--page-border)', color: 'var(--page-text)' }}>
                  <div style={styles.formRow}>
                    <label>Nombre del entorno *</label>
                    <br />
                    <input
                      type="text"
                      style={styles.input}
                      value={newEnvName}
                      onChange={(e) => setNewEnvName(e.target.value)}
                      placeholder="Ej. Development, Staging, Production"
                    />
                  </div>
                  <div style={styles.formRow}>
                    <label>Descripción (opcional)</label>
                    <br />
                    <input
                      type="text"
                      style={styles.input}
                      value={newEnvDesc}
                      onChange={(e) => setNewEnvDesc(e.target.value)}
                      placeholder="Breve descripción"
                    />
                  </div>
                  {createEnvError && <ErrorBanner message={createEnvError} />}
                  <button
                    type="button"
                    style={styles.button}
                    disabled={savingEnv}
                    onClick={handleCreateEnvironment}
                  >
                    {savingEnv ? 'Creando…' : 'Crear'}
                  </button>
                </div>
              )}
            </>
          )}
        </section>
      )}

      {/* Paso 3: Sitio (solo si hay proyecto + entorno) */}
      {contextState === 'PROJECT_ENV_SELECTED' && (
        <section style={styles.step}>
          <h2 style={styles.stepTitle}>Paso 3 — Selecciona o crea un sitio</h2>
          <p style={styles.hint}>
            Un sitio asocia un dominio a tu proyecto y entorno. Selecciona (o crea) uno para continuar.
          </p>
          {sitesLoading && !sites.length ? (
            <LoadingSpinner text="Cargando sitios…" />
          ) : (
            <>
              {sitesError && <ErrorBanner message={sitesError} />}
              <ul style={styles.list}>
                {sites.map((s) => (
                  <li key={s.id}>
                    <button
                      type="button"
                      style={{
                        ...styles.card,
                        ...(currentSiteId === s.id ? styles.cardSelected : {}),
                      }}
                      onClick={() => setSite(s.id)}
                    >
                      <strong>{s.name}</strong>
                      <span style={{ display: 'block', fontSize: '0.8125rem', color: 'var(--page-text-muted)', marginTop: '0.25rem' }}>
                        {s.domain}
                        {s.subdomain ? ` · ${s.subdomain}` : ''}
                        {s.subpath ? ` · ${s.subpath}` : ''}
                        {s.isActive ? '' : ' · Inactivo'}
                      </span>
                    </button>
                  </li>
                ))}
                {sites.length === 0 && (
                  <li>
                    <p style={styles.hint}>No hay sitios en este entorno. Crea uno a continuación.</p>
                  </li>
                )}
              </ul>
              {currentSiteId && (
                <div style={styles.actions}>
                  <button
                    type="button"
                    style={{ ...styles.button, ...styles.buttonSecondary }}
                    onClick={() => setSite(null)}
                  >
                    Deseleccionar sitio
                  </button>
                </div>
              )}
              <div style={styles.actions}>
                <Can permission="site.create" fallback={null}>
                  <button
                    type="button"
                    style={{ ...styles.button, ...styles.buttonSecondary }}
                    onClick={handleToggleCreateSite}
                    aria-expanded={showCreateSite}
                  >
                    {showCreateSite ? 'Cancelar' : 'Crear sitio'}
                  </button>
                </Can>
              </div>
              {showCreateSite && (
                <div style={{ marginTop: '1rem', padding: '1rem', background: 'var(--input-bg)', borderRadius: 6, border: '1px solid var(--page-border)', color: 'var(--page-text)' }}>
                  <div style={styles.formRow}>
                    <label>Nombre del sitio *</label>
                    <br />
                    <input
                      type="text"
                      style={styles.input}
                      value={newSiteName}
                      onChange={(e) => setNewSiteName(e.target.value)}
                      placeholder="Ej. Sitio principal"
                    />
                  </div>
                  <div style={styles.formRow}>
                    <label>Dominio *</label>
                    <br />
                    <input
                      type="text"
                      style={styles.input}
                      value={newSiteDomain}
                      onChange={(e) => setNewSiteDomain(e.target.value)}
                      placeholder="example.com"
                    />
                  </div>
                  <div style={styles.formRow}>
                    <label>Subdominio (opcional)</label>
                    <br />
                    <input
                      type="text"
                      style={styles.input}
                      value={newSiteSubdomain}
                      onChange={(e) => setNewSiteSubdomain(e.target.value)}
                      placeholder="www o blog"
                    />
                  </div>
                  <div style={styles.formRow}>
                    <label>Subruta (opcional)</label>
                    <br />
                    <input
                      type="text"
                      style={styles.input}
                      value={newSiteSubpath}
                      onChange={(e) => setNewSiteSubpath(e.target.value)}
                      placeholder="/blog"
                    />
                  </div>
                  <div style={styles.formRow}>
                    <label>Tema (opcional)</label>
                    <br />
                    <input
                      type="text"
                      style={styles.input}
                      value={newSiteThemeId}
                      onChange={(e) => setNewSiteThemeId(e.target.value)}
                      placeholder="theme-default"
                    />
                  </div>
                  {createSiteError && <ErrorBanner message={createSiteError} />}
                  <button
                    type="button"
                    style={styles.button}
                    disabled={savingSite}
                    onClick={handleCreateSite}
                  >
                    {savingSite ? 'Creando…' : 'Crear'}
                  </button>
                </div>
              )}
            </>
          )}
        </section>
      )}

      {/* Dashboard: solo con proyecto + entorno + sitio */}
      {hasFullContext && (
        <section style={styles.dashboard}>
          <h2 style={{ marginTop: 0, marginBottom: '0.75rem', fontSize: '1.25rem', color: 'var(--page-text)' }}>
            Dashboard
          </h2>

          {/* ── Widget: Contexto actual ── */}
          <div style={styles.contextWidget}>
            <h3 style={{ margin: '0 0 0.5rem 0', fontSize: '0.9375rem', fontWeight: 600, color: 'var(--page-text)' }}>
              Contexto actual
            </h3>
            <div style={styles.contextRow}>
              <span style={styles.contextRowLabel}>Proyecto</span>
              <span style={styles.contextRowValue}>{currentProject?.name ?? '—'}</span>
              <button
                type="button"
                style={styles.contextChangeBtn}
                onClick={() => setProject(null)}
                title="Cambiar proyecto (vuelve al Paso 1)"
              >
                Cambiar
              </button>
            </div>
            <div style={styles.contextRow}>
              <span style={styles.contextRowLabel}>Entorno</span>
              <span style={styles.contextRowValue}>{currentEnvironment?.name ?? '—'}</span>
              <button
                type="button"
                style={styles.contextChangeBtn}
                onClick={() => setEnvironment(null)}
                title="Cambiar entorno (vuelve al Paso 2)"
              >
                Cambiar
              </button>
            </div>
            <div style={styles.contextRowLast}>
              <span style={styles.contextRowLabel}>Sitio</span>
              <span style={styles.contextRowValue}>{currentSite?.name ?? '—'}</span>
              <button
                type="button"
                style={styles.contextChangeBtn}
                onClick={() => setSite(null)}
                title="Cambiar sitio (vuelve al Paso 3)"
              >
                Cambiar
              </button>
            </div>
          </div>

          {/* ── Sección: Contenido ── */}
          <p style={styles.sectionTitle}>Contenido</p>
          <div style={styles.widgetGrid}>
            <Link to="/content" style={styles.widget}>
              <span style={styles.widgetIcon}>&#128196;</span>
              <span style={styles.widgetTitle}>Contenido</span>
              <p style={styles.widgetDesc}>Ver y gestionar las entradas de contenido.</p>
            </Link>
            <Link to="/content/new" style={styles.widget}>
              <span style={styles.widgetIcon}>&#10133;</span>
              <span style={styles.widgetTitle}>Crear contenido</span>
              <p style={styles.widgetDesc}>Crear una nueva entrada de contenido.</p>
            </Link>
            <Can permission="content.publish">
              <Link to="/publish" style={styles.widget}>
                <span style={styles.widgetIcon}>&#128640;</span>
                <span style={styles.widgetTitle}>Publicar</span>
                <p style={styles.widgetDesc}>Publicar contenido aprobado a producción.</p>
              </Link>
            </Can>
            <Link to="/search" style={styles.widget}>
              <span style={styles.widgetIcon}>&#128269;</span>
              <span style={styles.widgetTitle}>Búsqueda</span>
              <p style={styles.widgetDesc}>Buscar contenido publicado en el sitio.</p>
            </Link>
          </div>

          {/* ── Sección: Administración ── */}
          <p style={styles.sectionTitle}>Administración</p>
          <div style={styles.widgetGrid}>
            <Link to="/admin/schemas" style={styles.widget}>
              <span style={styles.widgetIcon}>&#128736;</span>
              <span style={styles.widgetTitle}>Diseñador de schemas</span>
              <p style={styles.widgetDesc}>Crear y editar las estructuras de contenido.</p>
            </Link>
            <Link to="/sites" style={styles.widget}>
              <span style={styles.widgetIcon}>&#127760;</span>
              <span style={styles.widgetTitle}>Gestión de sitios</span>
              <p style={styles.widgetDesc}>Administrar dominios y configuración de sitios.</p>
            </Link>
            <Link to="/admin/roles" style={styles.widget}>
              <span style={styles.widgetIcon}>&#128272;</span>
              <span style={styles.widgetTitle}>Roles y permisos</span>
              <p style={styles.widgetDesc}>Configurar roles y asignar permisos.</p>
            </Link>
            <Can permission="user.list" fallback={null}>
              <Link to="/admin/users" style={styles.widget}>
                <span style={styles.widgetIcon}>&#128101;</span>
                <span style={styles.widgetTitle}>Usuarios</span>
                <p style={styles.widgetDesc}>Listar y gestionar usuarios del sistema.</p>
              </Link>
            </Can>
          </div>
        </section>
      )}
    </div>
  )
}
