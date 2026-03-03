import { useEffect, useRef, useState } from 'react'
import { useNavigate, Link, useLocation } from 'react-router-dom'
import {
  House,
  Settings,
  SquareUserRound,
  ChevronDown,
  Search,
} from 'lucide-react'
import { useAuthStore } from '../../modules/auth/store/auth-store'
import { useContextStore } from '../../modules/core/store/context-store'
import { Can } from '../../modules/authorization/components/Can'
import './AppLayout.css'

const ICON_SIZE = 20

function Dropdown({
  label,
  icon,
  triggerClass,
  children,
}: {
  label: string
  icon?: React.ReactNode
  triggerClass?: string
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
    <div ref={ref} className="app-layout__dropdown-wrapper">
      <button
        type="button"
        className={`app-layout__dropdown-trigger ${triggerClass ?? ''}`.trim()}
        onClick={() => setOpen((o) => !o)}
        aria-expanded={open}
        aria-haspopup="true"
      >
        {icon && <span className="app-layout__dropdown-icon" aria-hidden>{icon}</span>}
        {label}
        <ChevronDown className="app-layout__dropdown-chevron" size={16} aria-hidden />
      </button>
      {open && (
        <div className="app-layout__dropdown-menu" role="menu" onClick={() => setOpen(false)}>
          {children}
        </div>
      )}
    </div>
  )
}

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

  const isActive = (path: string, exact: boolean) =>
    exact ? location.pathname === path : location.pathname.startsWith(path)
  const isAdminMode =
    location.pathname === '/sites' ||
    location.pathname.startsWith('/admin/schemas') ||
    location.pathname === '/admin/hierarchies' ||
    location.pathname === '/admin/tags'

  return (
    <div className="app-layout">
      {/* Barra superior (.pen): logo ioda + Settings/User derecha */}
      <header className="app-layout__top-bar" role="banner">
        <Link to="/" className="app-layout__logo" aria-label="Inicio">
          ioda
        </Link>
        {hasFullContext && (
          <form onSubmit={handleSearch} className="app-layout__search-form">
            <input
              type="text"
              className="app-layout__search-input"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder={hasFullContext ? 'Buscar contenido…' : 'Selecciona un contexto para buscar'}
              disabled={!hasFullContext}
              title={hasFullContext ? 'Buscar contenido publicado' : 'Necesitas proyecto, entorno y sitio para buscar'}
              aria-label="Buscar"
            />
            <button
              type="submit"
              className="app-layout__search-btn"
              disabled={!hasFullContext}
              title="Buscar"
              aria-label="Buscar"
            >
              <Search size={ICON_SIZE} strokeWidth={2} />
            </button>
          </form>
        )}
        <div className="app-layout__top-right">
          <Dropdown
            label="Settings"
            icon={<Settings size={ICON_SIZE} strokeWidth={2} />}
            triggerClass="app-layout__dropdown-trigger--top"
          >
            <Link to="/" className="app-layout__dropdown-item" role="menuitem">
              Dashboard
            </Link>
            <div className="app-layout__dropdown-sep" />
            <Link to="/admin/schemas" className="app-layout__dropdown-item" role="menuitem">
              Schemas
            </Link>
            <Link to="/sites" className="app-layout__dropdown-item" role="menuitem">
              Sitios
            </Link>
            <Link to="/admin/hierarchies" className="app-layout__dropdown-item" role="menuitem">
              Jerarquías
            </Link>
            <Link to="/admin/tags" className="app-layout__dropdown-item" role="menuitem">
              Etiquetas
            </Link>
            <div className="app-layout__dropdown-sep" />
            <Link to="/admin/roles" className="app-layout__dropdown-item" role="menuitem">
              Roles y permisos
            </Link>
            <Can permission="user.list" fallback={null}>
              <Link to="/admin/users" className="app-layout__dropdown-item" role="menuitem">
                Usuarios
              </Link>
            </Can>
          </Dropdown>
          <Dropdown
            label={displayName}
            icon={<SquareUserRound size={ICON_SIZE} strokeWidth={2} />}
            triggerClass="app-layout__dropdown-trigger--top"
          >
            <div className="app-layout__dropdown-item app-layout__dropdown-item--info">
              {user?.email ?? ''}
            </div>
            <button
              type="button"
              className="app-layout__dropdown-item app-layout__dropdown-item--danger"
              onClick={handleLogout}
              role="menuitem"
            >
              Cerrar sesión
            </button>
          </Dropdown>
        </div>
      </header>

      {/* Barra nav + breadcrumb: 48px #35a0e8 — Home [› Proyecto › Entorno › Sitio] si hay contexto */}
      <div className="app-layout__nav-bar" role="navigation" aria-label="Contexto">
        <Link to="/" className="app-layout__nav-home" aria-label="Inicio" title="Inicio">
          <House size={24} strokeWidth={2} />
        </Link>
        {currentProjectId && (
          <>
            <span className="app-layout__breadcrumb-sep" aria-hidden>›</span>
            <button
              type="button"
              className="app-layout__nav-breadcrumb-value"
              onClick={() => { setProject(null); navigate('/') }}
              title="Cambiar proyecto"
            >
              {projectName ?? '…'}
            </button>
            {currentEnvironmentId && (
              <>
                <span className="app-layout__breadcrumb-sep" aria-hidden>›</span>
                <button
                  type="button"
                  className="app-layout__nav-breadcrumb-value"
                  onClick={() => { setEnvironment(null); navigate('/') }}
                  title="Cambiar entorno"
                >
                  {envName ?? '…'}
                </button>
              </>
            )}
            {currentSiteId && (
              <>
                <span className="app-layout__breadcrumb-sep" aria-hidden>›</span>
                <button
                  type="button"
                  className="app-layout__nav-breadcrumb-value"
                  onClick={() => { setSite(null); navigate('/') }}
                  title="Cambiar sitio"
                >
                  {siteName ?? '…'}
                </button>
              </>
            )}
          </>
        )}
      </div>

      <div className="app-layout__tabs">
        {hasFullContext && (
          <nav className="app-layout__tablist" role="tablist" aria-label="Pestañas principales">
            {isAdminMode ? (
              <>
                <Link
                  to="/"
                  role="tab"
                  aria-selected={isActive('/', true)}
                  aria-current={isActive('/', true) ? 'page' : undefined}
                  className={`app-layout__tab ${isActive('/', true) ? 'app-layout__tab--active' : ''}`}
                >
                  Dashboard
                </Link>
                <Link
                  to="/sites"
                  role="tab"
                  aria-selected={isActive('/sites', true)}
                  aria-current={isActive('/sites', true) ? 'page' : undefined}
                  className={`app-layout__tab ${isActive('/sites', true) ? 'app-layout__tab--active' : ''}`}
                >
                  Configuración de sitio
                </Link>
                <Link
                  to="/admin/schemas"
                  role="tab"
                  aria-selected={isActive('/admin/schemas', false)}
                  aria-current={isActive('/admin/schemas', false) ? 'page' : undefined}
                  className={`app-layout__tab ${isActive('/admin/schemas', false) ? 'app-layout__tab--active' : ''}`}
                >
                  Schemas
                </Link>
                <Link
                  to="/admin/hierarchies"
                  role="tab"
                  aria-selected={isActive('/admin/hierarchies', true)}
                  aria-current={isActive('/admin/hierarchies', true) ? 'page' : undefined}
                  className={`app-layout__tab ${isActive('/admin/hierarchies', true) ? 'app-layout__tab--active' : ''}`}
                >
                  Jerarquías
                </Link>
                <Link
                  to="/admin/tags"
                  role="tab"
                  aria-selected={isActive('/admin/tags', true)}
                  aria-current={isActive('/admin/tags', true) ? 'page' : undefined}
                  className={`app-layout__tab ${isActive('/admin/tags', true) ? 'app-layout__tab--active' : ''}`}
                >
                  Etiquetas
                </Link>
              </>
            ) : (
              <>
                <Link
                  to="/"
                  role="tab"
                  aria-selected={isActive('/', true)}
                  aria-current={isActive('/', true) ? 'page' : undefined}
                  className={`app-layout__tab ${isActive('/', true) ? 'app-layout__tab--active' : ''}`}
                >
                  Dashboard
                </Link>
                <Link
                  to="/content"
                  role="tab"
                  aria-selected={isActive('/content', false)}
                  aria-current={isActive('/content', false) ? 'page' : undefined}
                  className={`app-layout__tab ${isActive('/content', false) ? 'app-layout__tab--active' : ''}`}
                >
                  Contenido
                </Link>
                <Can permission="content.publish">
                  <Link
                    to="/publish"
                    role="tab"
                    aria-selected={isActive('/publish', true)}
                    aria-current={isActive('/publish', true) ? 'page' : undefined}
                    className={`app-layout__tab ${isActive('/publish', true) ? 'app-layout__tab--active' : ''}`}
                  >
                    Publicar
                  </Link>
                </Can>
              </>
            )}
          </nav>
        )}
      </div>

      {/* Área de contenido (.pen: espacio de escritorio) */}
      <main className="app-layout__main">{children}</main>
    </div>
  )
}
