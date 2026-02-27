import { useEffect, useState } from 'react'
import { coreApi } from '../../modules/core/api/core-api'
import { useContextStore } from '../../modules/core/store/context-store'
import { useAuthStore } from '../../modules/auth/store/auth-store'
import { Can } from '../../modules/authorization/components/Can'
import { LoadingSpinner, ErrorBanner } from '../../shared/components'
import type { Site } from '../../modules/core/types'
import './SitesPage.css'

export function SitesPage() {
  const { currentProjectId, environments, loadSites } = useContextStore()
  const user = useAuthStore((s) => s.user)
  const [items, setItems] = useState<Site[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [showCreate, setShowCreate] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [createError, setCreateError] = useState<string | null>(null)
  const [updateError, setUpdateError] = useState<string | null>(null)
  const [actionLoading, setActionLoading] = useState<string | null>(null)

  // Create form
  const [newName, setNewName] = useState('')
  const [newDomain, setNewDomain] = useState('')
  const [newSubdomain, setNewSubdomain] = useState('')
  const [newSubpath, setNewSubpath] = useState('')
  const [newThemeId, setNewThemeId] = useState('')
  const [newUrlTemplate, setNewUrlTemplate] = useState('')
  const [newEnvironmentId, setNewEnvironmentId] = useState<string>('')

  // Edit form (when editingId is set)
  const [editName, setEditName] = useState('')
  const [editDomain, setEditDomain] = useState('')
  const [editSubdomain, setEditSubdomain] = useState('')
  const [editSubpath, setEditSubpath] = useState('')
  const [editThemeId, setEditThemeId] = useState('')
  const [editUrlTemplate, setEditUrlTemplate] = useState('')

  useEffect(() => {
    if (currentProjectId) {
      loadSites(currentProjectId).catch(() => {})
    }
  }, [currentProjectId, loadSites])

  useEffect(() => {
    if (!currentProjectId) return
    setLoading(true)
    setError(null)
    coreApi
      .getSites(currentProjectId)
      .then((list) => setItems(list ?? []))
      .catch((e) => setError(e instanceof Error ? e.message : 'Error al cargar sitios'))
      .finally(() => setLoading(false))
  }, [currentProjectId])

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!currentProjectId || !newName.trim() || !newDomain.trim() || !newEnvironmentId || !user?.userId) return
    setCreateError(null)
    setActionLoading('create')
    try {
      await coreApi.createSite(currentProjectId, {
        environmentId: newEnvironmentId,
        name: newName.trim(),
        domain: newDomain.trim(),
        subdomain: newSubdomain.trim() || null,
        subpath: newSubpath.trim() || null,
        themeId: newThemeId.trim() || null,
        urlTemplate: newUrlTemplate.trim() || null,
        createdBy: user.userId,
      })
      setNewName('')
      setNewDomain('')
      setNewSubdomain('')
      setNewSubpath('')
      setNewThemeId('')
      setNewUrlTemplate('')
      setNewEnvironmentId('')
      setShowCreate(false)
      const list = await coreApi.getSites(currentProjectId)
      setItems(list ?? [])
      loadSites(currentProjectId).catch(() => {})
    } catch (e) {
      setCreateError(e instanceof Error ? e.message : 'Error al crear sitio')
    } finally {
      setActionLoading(null)
    }
  }

  const startEdit = (site: Site) => {
    setEditingId(site.id)
    setEditName(site.name)
    setEditDomain(site.domain)
    setEditSubdomain(site.subdomain ?? '')
    setEditSubpath(site.subpath ?? '')
    setEditThemeId(site.themeId ?? '')
    setEditUrlTemplate(site.urlTemplate ?? '')
    setUpdateError(null)
  }

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!currentProjectId || !editingId) return
    setUpdateError(null)
    setActionLoading(editingId)
    try {
      await coreApi.updateSite(currentProjectId, editingId, {
        name: editName.trim(),
        domain: editDomain.trim(),
        subdomain: editSubdomain.trim() || null,
        subpath: editSubpath.trim() || null,
        themeId: editThemeId.trim() || null,
        urlTemplate: editUrlTemplate.trim() || null,
      })
      setEditingId(null)
      const list = await coreApi.getSites(currentProjectId)
      setItems(list ?? [])
      loadSites(currentProjectId).catch(() => {})
    } catch (e) {
      setUpdateError(e instanceof Error ? e.message : 'Error al actualizar')
    } finally {
      setActionLoading(null)
    }
  }

  const handleActivate = async (siteId: string) => {
    if (!currentProjectId) return
    setActionLoading(siteId)
    try {
      await coreApi.activateSite(currentProjectId, siteId)
      const list = await coreApi.getSites(currentProjectId)
      setItems(list ?? [])
      loadSites(currentProjectId).catch(() => {})
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al activar')
    } finally {
      setActionLoading(null)
    }
  }

  const handleDeactivate = async (siteId: string) => {
    if (!currentProjectId) return
    setActionLoading(siteId)
    try {
      await coreApi.deactivateSite(currentProjectId, siteId)
      const list = await coreApi.getSites(currentProjectId)
      setItems(list ?? [])
      loadSites(currentProjectId).catch(() => {})
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al desactivar')
    } finally {
      setActionLoading(null)
    }
  }

  const handleDelete = async (siteId: string) => {
    if (!currentProjectId || !window.confirm('¿Eliminar este sitio? Esta acción no se puede deshacer.')) return
    setActionLoading(siteId)
    try {
      await coreApi.deleteSite(currentProjectId, siteId)
      setItems((prev) => prev.filter((s) => s.id !== siteId))
      loadSites(currentProjectId).catch(() => {})
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al eliminar')
    } finally {
      setActionLoading(null)
    }
  }

  if (!currentProjectId) {
    return (
      <div className="sites-page">
        <h1 className="sites-page__title">Sitios</h1>
        <p className="sites-page__hint">Selecciona un proyecto en la barra superior.</p>
      </div>
    )
  }

  return (
    <div className="sites-page">
      <div className="sites-page__header">
        <h1 className="sites-page__title">Gestión de sitios</h1>
        <Can permission="site.create" fallback={null}>
          <button
            type="button"
            className="sites-page__button sites-page__button--primary"
            onClick={() => { setShowCreate((x) => !x); setCreateError(null); }}
          >
            {showCreate ? 'Cancelar' : 'Crear sitio'}
          </button>
        </Can>
      </div>

      {showCreate && (
        <form className="sites-page__form" onSubmit={handleCreate}>
          <h3 className="sites-page__form-title">Nuevo sitio</h3>
          <div className="sites-page__form-row">
            <label className="sites-page__label">Nombre *</label>
            <input
              type="text"
              className="sites-page__input"
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              placeholder="Ej. Sitio principal"
              required
            />
          </div>
          <div className="sites-page__form-row">
            <label className="sites-page__label">Dominio *</label>
            <input
              type="text"
              className="sites-page__input"
              value={newDomain}
              onChange={(e) => setNewDomain(e.target.value)}
              placeholder="example.com"
              required
            />
          </div>
          <div className="sites-page__form-row">
            <label className="sites-page__label">Subdominio (opcional)</label>
            <input
              type="text"
              className="sites-page__input"
              value={newSubdomain}
              onChange={(e) => setNewSubdomain(e.target.value)}
              placeholder="www o blog"
            />
          </div>
          <div className="sites-page__form-row">
            <label className="sites-page__label">Subruta (opcional)</label>
            <input
              type="text"
              className="sites-page__input"
              value={newSubpath}
              onChange={(e) => setNewSubpath(e.target.value)}
              placeholder="/blog"
            />
          </div>
          <div className="sites-page__form-row">
            <label className="sites-page__label">Tema (opcional)</label>
            <input
              type="text"
              className="sites-page__input"
              value={newThemeId}
              onChange={(e) => setNewThemeId(e.target.value)}
              placeholder="theme-default"
            />
          </div>
          <div className="sites-page__form-row">
            <label className="sites-page__label">Plantilla URL (opcional)</label>
            <input
              type="text"
              className="sites-page__input"
              value={newUrlTemplate}
              onChange={(e) => setNewUrlTemplate(e.target.value)}
              placeholder="/{slug}"
            />
            <p className="sites-page__hint sites-page__hint--mt">Ej: /{'{slug}'} para rutas por slug. Usado por el endpoint de entrega por path.</p>
          </div>
          <div className="sites-page__form-row">
            <label className="sites-page__label">Entorno (opcional)</label>
            <select
              className="sites-page__select"
              value={newEnvironmentId}
              onChange={(e) => setNewEnvironmentId(e.target.value)}
            >
              <option value="">— Sin entorno —</option>
              {environments.map((e) => (
                <option key={e.id} value={e.id}>{e.name}</option>
              ))}
            </select>
          </div>
          {createError && <ErrorBanner message={createError} />}
          <button type="submit" className="sites-page__button sites-page__button--primary" disabled={!!actionLoading}>
            {actionLoading === 'create' ? 'Creando…' : 'Crear'}
          </button>
        </form>
      )}

      {error && <ErrorBanner message={error} />}
      {loading && <LoadingSpinner text="Cargando sitios…" />}

      {!loading && (
        <>
          <table className="sites-page__table">
            <thead>
              <tr>
                <th className="sites-page__th">Nombre</th>
                <th className="sites-page__th">Dominio</th>
                <th className="sites-page__th">Subdominio</th>
                <th className="sites-page__th">Subruta</th>
                <th className="sites-page__th">Tema</th>
                <th className="sites-page__th">Plantilla URL</th>
                <th className="sites-page__th">Estado</th>
                <th className="sites-page__th">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {items.length === 0 ? (
                <tr>
                  <td colSpan={8} className="sites-page__td">
                    No hay sitios. Crea uno con el botón «Crear sitio».
                  </td>
                </tr>
              ) : (
                items.map((site) => (
                  <tr key={site.id}>
                    {editingId === site.id ? (
                      <>
                        <td colSpan={8} className="sites-page__td">
                          <form onSubmit={handleUpdate} className="sites-page__edit-form">
                            <input
                              type="text"
                              className="sites-page__input sites-page__input--sm"
                              value={editName}
                              onChange={(e) => setEditName(e.target.value)}
                              placeholder="Nombre"
                              required
                            />
                            <input
                              type="text"
                              className="sites-page__input sites-page__input--md"
                              value={editDomain}
                              onChange={(e) => setEditDomain(e.target.value)}
                              placeholder="Dominio"
                              required
                            />
                            <input
                              type="text"
                              className="sites-page__input sites-page__input--xs"
                              value={editSubdomain}
                              onChange={(e) => setEditSubdomain(e.target.value)}
                              placeholder="Subdominio"
                            />
                            <input
                              type="text"
                              className="sites-page__input sites-page__input--xs"
                              value={editSubpath}
                              onChange={(e) => setEditSubpath(e.target.value)}
                              placeholder="Subruta"
                            />
                            <input
                              type="text"
                              className="sites-page__input sites-page__input--theme"
                              value={editThemeId}
                              onChange={(e) => setEditThemeId(e.target.value)}
                              placeholder="Tema"
                            />
                            <input
                              type="text"
                              className="sites-page__input sites-page__input--url"
                              value={editUrlTemplate}
                              onChange={(e) => setEditUrlTemplate(e.target.value)}
                              placeholder="/{slug}"
                            />
                            {updateError && <span className="sites-page__update-error">{updateError}</span>}
                            <button type="submit" className="sites-page__button sites-page__button--primary sites-page__button--small" disabled={!!actionLoading}>
                              Guardar
                            </button>
                            <button type="button" className="sites-page__button sites-page__button--small" onClick={() => setEditingId(null)}>
                              Cancelar
                            </button>
                          </form>
                        </td>
                      </>
                    ) : (
                      <>
                        <td className="sites-page__td">{site.name}</td>
                        <td className="sites-page__td">{site.domain}</td>
                        <td className="sites-page__td">{site.subdomain ?? '—'}</td>
                        <td className="sites-page__td">{site.subpath ?? '—'}</td>
                        <td className="sites-page__td">{site.themeId ?? '—'}</td>
                        <td className="sites-page__td">{site.urlTemplate ?? '—'}</td>
                        <td className="sites-page__td">
                          <span className={`sites-page__badge ${site.isActive ? 'sites-page__badge--active' : 'sites-page__badge--inactive'}`}>
                            {site.isActive ? 'Activo' : 'Inactivo'}
                          </span>
                        </td>
                        <td className="sites-page__td">
                          <button
                            type="button"
                            className="sites-page__button sites-page__button--small"
                            style={{ marginRight: '0.35rem' }}
                            onClick={() => startEdit(site)}
                            disabled={!!actionLoading}
                          >
                            Editar
                          </button>
                          {site.isActive ? (
                            <button
                              type="button"
                              className="sites-page__button sites-page__button--small"
                              style={{ marginRight: '0.35rem' }}
                              onClick={() => handleDeactivate(site.id)}
                              disabled={!!actionLoading}
                            >
                              Desactivar
                            </button>
                          ) : (
                            <button
                              type="button"
                              className="sites-page__button sites-page__button--small"
                              style={{ marginRight: '0.35rem' }}
                              onClick={() => handleActivate(site.id)}
                              disabled={!!actionLoading}
                            >
                              Activar
                            </button>
                          )}
                          <button
                            type="button"
                            className="sites-page__button sites-page__button--small sites-page__button--danger"
                            onClick={() => handleDelete(site.id)}
                            disabled={!!actionLoading}
                          >
                            Eliminar
                          </button>
                        </td>
                      </>
                    )}
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </>
      )}
    </div>
  )
}
