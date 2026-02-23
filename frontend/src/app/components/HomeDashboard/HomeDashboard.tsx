import { Link } from 'react-router-dom'
import { Can } from '../../../modules/authorization/components/Can'
import type { Project, Environment, Site } from '../../../modules/core/types'

export interface HomeDashboardProps {
  currentProject: Project | undefined
  currentEnvironment: Environment | undefined
  currentSite: Site | undefined
  onChangeProject: () => void
  onChangeEnvironment: () => void
  onChangeSite: () => void
}

export function HomeDashboard({
  currentProject,
  currentEnvironment,
  currentSite,
  onChangeProject,
  onChangeEnvironment,
  onChangeSite,
}: HomeDashboardProps) {
  return (
    <section className="home__dashboard">
      <h2 className="home__dashboard-title">Dashboard</h2>

      <div className="home__context">
        <h3 className="home__context-title">Contexto actual</h3>
        <div className="home__context-row">
          <span className="home__context-label">Proyecto</span>
          <span className="home__context-value">{currentProject?.name ?? '—'}</span>
          <button
            type="button"
            className="home__context-change"
            onClick={onChangeProject}
            title="Cambiar proyecto (vuelve al Paso 1)"
          >
            Cambiar
          </button>
        </div>
        <div className="home__context-row">
          <span className="home__context-label">Entorno</span>
          <span className="home__context-value">{currentEnvironment?.name ?? '—'}</span>
          <button
            type="button"
            className="home__context-change"
            onClick={onChangeEnvironment}
            title="Cambiar entorno (vuelve al Paso 2)"
          >
            Cambiar
          </button>
        </div>
        <div className="home__context-row">
          <span className="home__context-label">Sitio</span>
          <span className="home__context-value">{currentSite?.name ?? '—'}</span>
          <button
            type="button"
            className="home__context-change"
            onClick={onChangeSite}
            title="Cambiar sitio (vuelve al Paso 3)"
          >
            Cambiar
          </button>
        </div>
      </div>

      <p className="home__section-title">Contenido</p>
      <div className="home__widget-grid">
        <Link to="/content" className="home__widget">
          <span className="home__widget-icon">&#128196;</span>
          <span className="home__widget-title">Contenido</span>
          <p className="home__widget-desc">Ver y gestionar las entradas de contenido.</p>
        </Link>
        <Link to="/content/new" className="home__widget">
          <span className="home__widget-icon">&#10133;</span>
          <span className="home__widget-title">Crear contenido</span>
          <p className="home__widget-desc">Crear una nueva entrada de contenido.</p>
        </Link>
        <Can permission="content.publish">
          <Link to="/publish" className="home__widget">
            <span className="home__widget-icon">&#128640;</span>
            <span className="home__widget-title">Publicar</span>
            <p className="home__widget-desc">Publicar contenido aprobado a producción.</p>
          </Link>
        </Can>
        <Link to="/search" className="home__widget">
          <span className="home__widget-icon">&#128269;</span>
          <span className="home__widget-title">Búsqueda</span>
          <p className="home__widget-desc">Buscar contenido publicado en el sitio.</p>
        </Link>
      </div>

      <p className="home__section-title">Administración</p>
      <div className="home__widget-grid">
        <Link to="/admin/schemas" className="home__widget">
          <span className="home__widget-icon">&#128736;</span>
          <span className="home__widget-title">Diseñador de schemas</span>
          <p className="home__widget-desc">Crear y editar las estructuras de contenido.</p>
        </Link>
        <Link to="/sites" className="home__widget">
          <span className="home__widget-icon">&#127760;</span>
          <span className="home__widget-title">Gestión de sitios</span>
          <p className="home__widget-desc">Administrar dominios y configuración de sitios.</p>
        </Link>
        <Link to="/admin/roles" className="home__widget">
          <span className="home__widget-icon">&#128272;</span>
          <span className="home__widget-title">Roles y permisos</span>
          <p className="home__widget-desc">Configurar roles y asignar permisos.</p>
        </Link>
        <Can permission="user.list" fallback={null}>
          <Link to="/admin/users" className="home__widget">
            <span className="home__widget-icon">&#128101;</span>
            <span className="home__widget-title">Usuarios</span>
            <p className="home__widget-desc">Listar y gestionar usuarios del sistema.</p>
          </Link>
        </Can>
      </div>
    </section>
  )
}
