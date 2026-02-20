import { useEffect, useState } from 'react'
import { coreApi } from '../../modules/core/api/core-api'
import { useContextStore } from '../../modules/core/store/context-store'
import { useAuthStore } from '../../modules/auth/store/auth-store'
import { Can } from '../../modules/authorization/components/Can'
import { LoadingSpinner, ErrorBanner } from '../../shared/components'
import type { Site } from '../../modules/core/types'

const styles: Record<string, React.CSSProperties> = {
  container: { maxWidth: 900, color: 'var(--page-text)' },
  title: { marginTop: 0, marginBottom: '1rem', color: 'var(--page-text)', fontSize: '1.5rem' },
  actions: { display: 'flex', gap: '0.75rem', flexWrap: 'wrap', marginBottom: '1.5rem', alignItems: 'center' },
  button: {
    padding: '0.5rem 1rem',
    fontSize: '0.875rem',
    cursor: 'pointer',
    borderRadius: 6,
    border: '1px solid var(--input-border)',
    background: 'var(--input-bg)',
    textDecoration: 'none',
    color: 'var(--page-text)',
  },
  buttonPrimary: { background: '#0d6efd', color: 'white', border: 'none' },
  buttonDanger: { background: '#dc3545', color: 'white', border: 'none' },
  buttonSmall: { padding: '0.35rem 0.6rem', fontSize: '0.8125rem' },
  table: { width: '100%', borderCollapse: 'collapse', fontSize: '0.875rem', color: 'var(--page-text)' },
  th: { textAlign: 'left', padding: '0.5rem', borderBottom: '2px solid var(--page-border)', color: 'var(--page-text)' },
  td: { padding: '0.5rem', borderBottom: '1px solid var(--page-border)', color: 'var(--page-text)' },
  hint: { color: 'var(--page-text-muted)', fontSize: '0.875rem' },
  form: { maxWidth: 480, marginBottom: '1.5rem', padding: '1rem', background: 'var(--page-bg-elevated)', borderRadius: 8, border: '1px solid var(--page-border)', color: 'var(--page-text)' },
  formRow: { marginBottom: '0.75rem' },
  label: { display: 'block', marginBottom: '0.25rem', fontSize: '0.875rem', fontWeight: 600, color: 'var(--page-text)' },
  input: { width: '100%', maxWidth: 360, padding: '0.5rem', fontSize: '0.875rem', borderRadius: 4, border: '1px solid var(--input-border)', color: 'var(--input-text)', background: 'var(--input-bg)' },
  select: { padding: '0.5rem', fontSize: '0.875rem', minWidth: 200, borderRadius: 4, border: '1px solid var(--input-border)', color: 'var(--input-text)', background: 'var(--input-bg)' },
  badge: { display: 'inline-block', padding: '0.2rem 0.5rem', borderRadius: 4, fontSize: '0.75rem', fontWeight: 500 },
  badgeActive: { background: '#d1e7dd', color: '#0f5132' },
  badgeInactive: { background: '#f8d7da', color: '#842029' },
}

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
    if (!currentProjectId || !newName.trim() || !newDomain.trim() || !user?.userId) return
    setCreateError(null)
    setActionLoading('create')
    try {
      await coreApi.createSite(currentProjectId, {
        environmentId: newEnvironmentId || null,
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
      <div style={styles.container}>
        <h1 style={styles.title}>Sitios</h1>
        <p style={styles.hint}>Selecciona un proyecto en la barra superior.</p>
      </div>
    )
  }

  return (
    <div style={styles.container}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem', flexWrap: 'wrap', gap: '0.5rem' }}>
        <h1 style={styles.title}>Gestión de sitios</h1>
        <Can permission="site.create" fallback={null}>
          <button
            type="button"
            style={{ ...styles.button, ...styles.buttonPrimary }}
            onClick={() => { setShowCreate((x) => !x); setCreateError(null); }}
          >
            {showCreate ? 'Cancelar' : 'Crear sitio'}
          </button>
        </Can>
      </div>

      {showCreate && (
        <form style={styles.form} onSubmit={handleCreate}>
          <h3 style={{ marginTop: 0, marginBottom: '1rem', fontSize: '1rem' }}>Nuevo sitio</h3>
          <div style={styles.formRow}>
            <label style={styles.label}>Nombre *</label>
            <input
              type="text"
              style={styles.input}
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              placeholder="Ej. Sitio principal"
              required
            />
          </div>
          <div style={styles.formRow}>
            <label style={styles.label}>Dominio *</label>
            <input
              type="text"
              style={styles.input}
              value={newDomain}
              onChange={(e) => setNewDomain(e.target.value)}
              placeholder="example.com"
              required
            />
          </div>
          <div style={styles.formRow}>
            <label style={styles.label}>Subdominio (opcional)</label>
            <input
              type="text"
              style={styles.input}
              value={newSubdomain}
              onChange={(e) => setNewSubdomain(e.target.value)}
              placeholder="www o blog"
            />
          </div>
          <div style={styles.formRow}>
            <label style={styles.label}>Subruta (opcional)</label>
            <input
              type="text"
              style={styles.input}
              value={newSubpath}
              onChange={(e) => setNewSubpath(e.target.value)}
              placeholder="/blog"
            />
          </div>
          <div style={styles.formRow}>
            <label style={styles.label}>Tema (opcional)</label>
            <input
              type="text"
              style={styles.input}
              value={newThemeId}
              onChange={(e) => setNewThemeId(e.target.value)}
              placeholder="theme-default"
            />
          </div>
          <div style={styles.formRow}>
            <label style={styles.label}>Plantilla URL (opcional)</label>
            <input
              type="text"
              style={styles.input}
              value={newUrlTemplate}
              onChange={(e) => setNewUrlTemplate(e.target.value)}
              placeholder="/{slug}"
            />
            <p style={{ ...styles.hint, marginTop: '0.25rem' }}>Ej: /{'{slug}'} para rutas por slug. Usado por el endpoint de entrega por path.</p>
          </div>
          <div style={styles.formRow}>
            <label style={styles.label}>Entorno (opcional)</label>
            <select
              style={styles.select}
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
          <button type="submit" style={{ ...styles.button, ...styles.buttonPrimary }} disabled={!!actionLoading}>
            {actionLoading === 'create' ? 'Creando…' : 'Crear'}
          </button>
        </form>
      )}

      {error && <ErrorBanner message={error} />}
      {loading && <LoadingSpinner text="Cargando sitios…" />}

      {!loading && (
        <>
          <table style={styles.table}>
            <thead>
              <tr>
                <th style={styles.th}>Nombre</th>
                <th style={styles.th}>Dominio</th>
                <th style={styles.th}>Subdominio</th>
                <th style={styles.th}>Subruta</th>
                <th style={styles.th}>Tema</th>
                <th style={styles.th}>Plantilla URL</th>
                <th style={styles.th}>Estado</th>
                <th style={styles.th}>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {items.length === 0 ? (
                <tr>
                  <td colSpan={8} style={styles.td}>
                    No hay sitios. Crea uno con el botón «Crear sitio».
                  </td>
                </tr>
              ) : (
                items.map((site) => (
                  <tr key={site.id}>
                    {editingId === site.id ? (
                      <>
                        <td colSpan={8} style={styles.td}>
                          <form onSubmit={handleUpdate} style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem', alignItems: 'center' }}>
                            <input
                              type="text"
                              style={{ ...styles.input, maxWidth: 140 }}
                              value={editName}
                              onChange={(e) => setEditName(e.target.value)}
                              placeholder="Nombre"
                              required
                            />
                            <input
                              type="text"
                              style={{ ...styles.input, maxWidth: 160 }}
                              value={editDomain}
                              onChange={(e) => setEditDomain(e.target.value)}
                              placeholder="Dominio"
                              required
                            />
                            <input
                              type="text"
                              style={{ ...styles.input, maxWidth: 100 }}
                              value={editSubdomain}
                              onChange={(e) => setEditSubdomain(e.target.value)}
                              placeholder="Subdominio"
                            />
                            <input
                              type="text"
                              style={{ ...styles.input, maxWidth: 100 }}
                              value={editSubpath}
                              onChange={(e) => setEditSubpath(e.target.value)}
                              placeholder="Subruta"
                            />
                            <input
                              type="text"
                              style={{ ...styles.input, maxWidth: 120 }}
                              value={editThemeId}
                              onChange={(e) => setEditThemeId(e.target.value)}
                              placeholder="Tema"
                            />
                            <input
                              type="text"
                              style={{ ...styles.input, maxWidth: 140 }}
                              value={editUrlTemplate}
                              onChange={(e) => setEditUrlTemplate(e.target.value)}
                              placeholder="/{slug}"
                            />
                            {updateError && <span style={{ color: '#dc3545', fontSize: '0.875rem' }}>{updateError}</span>}
                            <button type="submit" style={{ ...styles.button, ...styles.buttonPrimary, ...styles.buttonSmall }} disabled={!!actionLoading}>
                              Guardar
                            </button>
                            <button type="button" style={{ ...styles.button, ...styles.buttonSmall }} onClick={() => setEditingId(null)}>
                              Cancelar
                            </button>
                          </form>
                        </td>
                      </>
                    ) : (
                      <>
                        <td style={styles.td}>{site.name}</td>
                        <td style={styles.td}>{site.domain}</td>
                        <td style={styles.td}>{site.subdomain ?? '—'}</td>
                        <td style={styles.td}>{site.subpath ?? '—'}</td>
                        <td style={styles.td}>{site.themeId ?? '—'}</td>
                        <td style={styles.td}>{site.urlTemplate ?? '—'}</td>
                        <td style={styles.td}>
                          <span style={{ ...styles.badge, ...(site.isActive ? styles.badgeActive : styles.badgeInactive) }}>
                            {site.isActive ? 'Activo' : 'Inactivo'}
                          </span>
                        </td>
                        <td style={styles.td}>
                          <button
                            type="button"
                            style={{ ...styles.button, ...styles.buttonSmall, marginRight: '0.35rem' }}
                            onClick={() => startEdit(site)}
                            disabled={!!actionLoading}
                          >
                            Editar
                          </button>
                          {site.isActive ? (
                            <button
                              type="button"
                              style={{ ...styles.button, ...styles.buttonSmall, marginRight: '0.35rem' }}
                              onClick={() => handleDeactivate(site.id)}
                              disabled={!!actionLoading}
                            >
                              Desactivar
                            </button>
                          ) : (
                            <button
                              type="button"
                              style={{ ...styles.button, ...styles.buttonSmall, marginRight: '0.35rem' }}
                              onClick={() => handleActivate(site.id)}
                              disabled={!!actionLoading}
                            >
                              Activar
                            </button>
                          )}
                          <button
                            type="button"
                            style={{ ...styles.button, ...styles.buttonSmall, ...styles.buttonDanger }}
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
