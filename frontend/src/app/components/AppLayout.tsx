import { useEffect, useRef, useState } from 'react'
import { useNavigate, Link, useLocation } from 'react-router-dom'
import { useAuthStore } from '../../modules/auth/store/auth-store'
import { useContextStore } from '../../modules/core/store/context-store'
import { Can } from '../../modules/authorization/components/Can'

/* ─── Estilos ─── */

const styles: Record<string, React.CSSProperties> = {
  layout: {
    minHeight: '100vh',
    display: 'flex',
    flexDirection: 'column',
    fontFamily: 'system-ui, sans-serif',
  },
  /* ── Barra superior ── */
  header: {
    display: 'flex',
    alignItems: 'center',
    padding: '0 1rem',
    height: 48,
    borderBottom: '1px solid var(--page-border)',
    color: 'var(--page-text)',
    gap: '0.5rem',
    flexShrink: 0,
    width: '100%',
    boxSizing: 'border-box' as const,
  },
  logo: {
    margin: 0,
    fontSize: '1rem',
    fontWeight: 700,
    color: 'var(--page-text)',
    textDecoration: 'none',
    whiteSpace: 'nowrap',
    marginRight: '0.25rem',
  },
  navLinks: {
    display: 'flex',
    alignItems: 'center',
    gap: '0.125rem',
    flexShrink: 0,
  },
  navLink: {
    padding: '0.3rem 0.6rem',
    fontSize: '0.8125rem',
    color: 'var(--page-text)',
    textDecoration: 'none',
    borderRadius: 5,
    whiteSpace: 'nowrap',
    transition: 'background 0.15s',
  },
  navLinkActive: {
    background: 'var(--page-bg-elevated)',
    fontWeight: 600,
  },
  /* ── Buscador (ocupa espacio restante) ── */
  searchForm: {
    display: 'flex',
    flex: 1,
    minWidth: 0,
    margin: '0 0.75rem',
  },
  searchInput: {
    flex: 1,
    minWidth: 0,
    padding: '0.3rem 0.65rem',
    fontSize: '0.8125rem',
    borderRadius: '5px 0 0 5px',
    border: '1px solid var(--input-border)',
    borderRight: 'none',
    background: 'var(--input-bg)',
    color: 'var(--input-text)',
  },
  searchBtn: {
    padding: '0.3rem 0.6rem',
    fontSize: '0.8125rem',
    background: 'var(--page-bg-elevated)',
    color: 'var(--page-text)',
    border: '1px solid var(--input-border)',
    borderRadius: '0 5px 5px 0',
    cursor: 'pointer',
    flexShrink: 0,
  },
  /* ── Zona derecha (Admin + Usuario) ── */
  rightGroup: {
    display: 'flex',
    alignItems: 'center',
    gap: '0.25rem',
    flexShrink: 0,
  },
  /* ── Dropdown ── */
  dropdownWrapper: {
    position: 'relative' as const,
  },
  dropdownTrigger: {
    display: 'flex',
    alignItems: 'center',
    gap: '0.3rem',
    padding: '0.3rem 0.55rem',
    fontSize: '0.8125rem',
    background: 'transparent',
    color: 'var(--page-text)',
    border: 'none',
    borderRadius: 5,
    cursor: 'pointer',
    whiteSpace: 'nowrap' as const,
  },
  dropdownMenu: {
    position: 'absolute' as const,
    right: 0,
    top: '100%',
    marginTop: 4,
    minWidth: 180,
    background: 'var(--page-bg-elevated)',
    border: '1px solid var(--page-border)',
    borderRadius: 8,
    boxShadow: '0 4px 16px rgba(0,0,0,0.15)',
    zIndex: 1000,
    padding: '0.35rem 0',
    display: 'flex',
    flexDirection: 'column' as const,
  },
  dropdownItem: {
    padding: '0.5rem 0.85rem',
    fontSize: '0.8125rem',
    color: 'var(--page-text)',
    textDecoration: 'none',
    background: 'transparent',
    border: 'none',
    textAlign: 'left' as const,
    cursor: 'pointer',
    display: 'block',
    width: '100%',
  },
  dropdownSep: {
    height: 1,
    background: 'var(--page-border)',
    margin: '0.25rem 0',
  },
  /* ── Breadcrumb ── */
  breadcrumb: {
    display: 'flex',
    alignItems: 'center',
    gap: '0.4rem',
    padding: '0.4rem 1rem',
    fontSize: '0.8125rem',
    color: 'var(--page-text-muted)',
    borderBottom: '1px solid var(--page-border)',
    background: 'var(--page-bg-elevated)',
    flexShrink: 0,
  },
  breadcrumbSep: {
    opacity: 0.45,
  },
  breadcrumbValue: {
    color: 'var(--page-text)',
    fontWeight: 500,
    background: 'none',
    border: 'none',
    padding: 0,
    font: 'inherit',
    cursor: 'pointer',
    textDecoration: 'none',
    borderBottom: '1px dashed transparent',
    transition: 'border-color 0.15s',
  },
  /* ── Main ── */
  main: {
    flex: 1,
    padding: '1.5rem 2rem',
    color: 'var(--page-text)',
  },
}

/* ─── Dropdown genérico ─── */

function Dropdown({
  label,
  icon,
  children,
}: {
  label: string
  icon?: string
  children: React.ReactNode
}) {
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [])

  return (
    <div ref={ref} style={styles.dropdownWrapper}>
      <button
        type="button"
        style={styles.dropdownTrigger}
        onClick={() => setOpen((o) => !o)}
        aria-expanded={open}
        aria-haspopup="true"
      >
        {icon && <span>{icon}</span>}
        {label}
        <span style={{ fontSize: '0.6rem', opacity: 0.7 }}>&#9662;</span>
      </button>
      {open && (
        <div style={styles.dropdownMenu} role="menu" onClick={() => setOpen(false)}>
          {children}
        </div>
      )}
    </div>
  )
}

/* ─── AppLayout ─── */

export function AppLayout({ children }: { children: React.ReactNode }) {
  const navigate = useNavigate()
  const location = useLocation()
  const user = useAuthStore((s) => s.user)
  const logout = useAuthStore((s) => s.logout)
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)

  const {
    currentProjectId,
    currentEnvironmentId,
    currentSiteId,
    projects,
    environments,
    sites,
    setProject,
    setEnvironment,
    setSite,
    loadProjects,
    rehydrate,
  } = useContextStore()

  useEffect(() => {
    rehydrate()
  }, [rehydrate])

  useEffect(() => {
    if (isAuthenticated) {
      loadProjects().catch(() => {})
    }
  }, [isAuthenticated, loadProjects])

  const [searchQuery, setSearchQuery] = useState('')

  const handleLogout = () => {
    logout()
    useContextStore.getState().clearLists()
    navigate('/login', { replace: true })
  }

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault()
    if (searchQuery.trim()) {
      navigate(`/search?q=${encodeURIComponent(searchQuery.trim())}`)
      setSearchQuery('')
    }
  }

  const hasFullContext = Boolean(currentProjectId && currentEnvironmentId && currentSiteId)

  const projectName = projects.find((p) => p.id === currentProjectId)?.name
  const envName = environments.find((e) => e.id === currentEnvironmentId)?.name
  const siteName = sites.find((s) => s.id === currentSiteId)?.name

  const displayName = user?.displayName ?? user?.email ?? user?.userId ?? 'Usuario'

  const navLinkStyle = (path: string, exact = false): React.CSSProperties => ({
    ...styles.navLink,
    ...((exact ? location.pathname === path : location.pathname.startsWith(path))
      ? styles.navLinkActive
      : {}),
  })

  return (
    <div style={styles.layout}>
      {/* ══════ Barra superior ══════ */}
      <header style={styles.header}>
        {/* Logo */}
        <Link to="/" style={styles.logo}>
          IODA CMS
        </Link>

        {/* Navegación */}
        <nav style={styles.navLinks}>
          <Link to="/" style={navLinkStyle('/', true)}>
            Dashboard
          </Link>
          {hasFullContext && (
            <>
              <Link to="/content" style={navLinkStyle('/content')}>
                Contenido
              </Link>
              <Can permission="content.publish">
                <Link to="/publish" style={navLinkStyle('/publish')}>
                  Publicar
                </Link>
              </Can>
            </>
          )}
        </nav>

        {/* Buscador (ocupa el espacio restante) */}
        <form onSubmit={handleSearch} style={styles.searchForm}>
          <input
            type="text"
            style={styles.searchInput}
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder={hasFullContext ? 'Buscar contenido…' : 'Selecciona un contexto para buscar'}
            disabled={!hasFullContext}
            title={hasFullContext ? 'Buscar contenido publicado' : 'Necesitas proyecto, entorno y sitio para buscar'}
          />
          <button
            type="submit"
            style={{
              ...styles.searchBtn,
              opacity: hasFullContext ? 1 : 0.5,
              cursor: hasFullContext ? 'pointer' : 'not-allowed',
            }}
            disabled={!hasFullContext}
            title="Buscar"
          >
            &#128269;
          </button>
        </form>

        {/* Admin + Usuario (derecha) */}
        <div style={styles.rightGroup}>
          <Dropdown label="Admin" icon="&#9881;">
            <Link to="/admin/schemas" style={styles.dropdownItem} role="menuitem">
              Schemas
            </Link>
            <Link to="/sites" style={styles.dropdownItem} role="menuitem">
              Sitios
            </Link>
            <div style={styles.dropdownSep} />
            <Link to="/admin/roles" style={styles.dropdownItem} role="menuitem">
              Roles y permisos
            </Link>
            <Can permission="user.list" fallback={null}>
              <Link to="/admin/users" style={styles.dropdownItem} role="menuitem">
                Usuarios
              </Link>
            </Can>
          </Dropdown>

          <Dropdown label={displayName} icon="&#128100;">
            <div
              style={{
                padding: '0.5rem 0.85rem',
                fontSize: '0.75rem',
                color: 'var(--page-text-muted)',
                borderBottom: '1px solid var(--page-border)',
                marginBottom: '0.25rem',
              }}
            >
              {user?.email ?? ''}
            </div>
            <button
              type="button"
              style={{ ...styles.dropdownItem, color: '#dc3545' }}
              onClick={handleLogout}
              role="menuitem"
            >
              Cerrar sesión
            </button>
          </Dropdown>
        </div>
      </header>

      {/* ══════ Breadcrumb ══════ */}
      <div style={styles.breadcrumb}>
        {currentProjectId ? (
          <>
            <button
              type="button"
              style={styles.breadcrumbValue}
              onClick={() => { setProject(null); navigate('/') }}
              title="Cambiar proyecto"
            >
              {projectName ?? '…'}
            </button>
            {currentEnvironmentId && (
              <>
                <span style={styles.breadcrumbSep}>/</span>
                <button
                  type="button"
                  style={styles.breadcrumbValue}
                  onClick={() => { setEnvironment(null); navigate('/') }}
                  title="Cambiar entorno"
                >
                  {envName ?? '…'}
                </button>
              </>
            )}
            {currentSiteId && (
              <>
                <span style={styles.breadcrumbSep}>/</span>
                <button
                  type="button"
                  style={styles.breadcrumbValue}
                  onClick={() => { setSite(null); navigate('/') }}
                  title="Cambiar sitio"
                >
                  {siteName ?? '…'}
                </button>
              </>
            )}
          </>
        ) : (
          <span>
            Sin contexto —{' '}
            <button
              type="button"
              style={{ ...styles.breadcrumbValue, color: '#0d6efd' }}
              onClick={() => navigate('/')}
            >
              configurar
            </button>
          </span>
        )}
      </div>

      {/* ══════ Contenido ══════ */}
      <main style={styles.main}>{children}</main>
    </div>
  )
}
