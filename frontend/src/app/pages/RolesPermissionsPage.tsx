import { useEffect, useState } from 'react'
import { authorizationApi } from '../../modules/authorization/api/authorization-api'
import { useAuthStore } from '../../modules/auth/store/auth-store'
import { useContextStore } from '../../modules/core/store/context-store'
import { invalidatePermissionCache } from '../../shared/permission-cache'
import { LoadingSpinner, ErrorBanner } from '../../shared/components'
import type { ApiError } from '../../shared/api'
import type {
  RoleDto,
  PermissionDto,
  AccessRuleDto,
} from '../../modules/authorization/types'

type Tab = 'permissions' | 'roles' | 'rules'

/** Mensaje amigable cuando Authorization API devuelve 403 (no se desloguea al usuario). */
const AUTHORIZATION_403_MESSAGE =
  'No tienes permiso para realizar esta acción. Si acabas de configurar tu usuario, cierra sesión y vuelve a entrar para actualizar tus permisos.'

function getAuthorizationErrorMessage(err: unknown, defaultMessage: string): string {
  if (err && typeof err === 'object' && 'status' in err && (err as ApiError).status === 403) {
    return AUTHORIZATION_403_MESSAGE
  }
  return err instanceof Error ? err.message : defaultMessage
}

const styles: Record<string, React.CSSProperties> = {
  container: { maxWidth: 960, color: 'var(--page-text)' },
  title: { marginTop: 0, marginBottom: '1rem', color: 'var(--page-text)', fontSize: '1.5rem' },
  tabs: { display: 'flex', gap: 0, marginBottom: '1.5rem', borderBottom: '2px solid var(--page-border)' },
  tab: {
    padding: '0.6rem 1.25rem',
    background: 'transparent',
    border: 'none',
    borderBottom: '2px solid transparent',
    marginBottom: -2,
    cursor: 'pointer',
    fontSize: '0.9375rem',
    fontWeight: 500,
    color: 'var(--tab-inactive)',
  },
  tabActive: { borderBottomColor: '#0d6efd', color: '#0d6efd' },
  section: { marginBottom: '2rem' },
  table: { width: '100%', borderCollapse: 'collapse', fontSize: '0.875rem', color: 'var(--page-text)' },
  th: { textAlign: 'left', padding: '0.5rem', borderBottom: '2px solid var(--page-border)', color: 'var(--page-text)' },
  td: { padding: '0.5rem', borderBottom: '1px solid var(--page-border)', color: 'var(--page-text)' },
  button: {
    padding: '0.5rem 1rem',
    fontSize: '0.875rem',
    cursor: 'pointer',
    borderRadius: 6,
    border: '1px solid var(--input-border)',
    background: 'var(--input-bg)',
    color: 'var(--page-text)',
  },
  buttonPrimary: { background: '#0d6efd', color: 'white', border: 'none' },
  buttonDanger: { background: '#dc3545', color: 'white', border: 'none' },
  buttonSmall: { padding: '0.35rem 0.6rem', fontSize: '0.8125rem' },
  form: { maxWidth: 480, marginBottom: '1.5rem', padding: '1rem', background: 'var(--page-bg-elevated)', borderRadius: 8, border: '1px solid var(--page-border)', color: 'var(--page-text)' },
  formRow: { marginBottom: '0.75rem' },
  label: { display: 'block', marginBottom: '0.25rem', fontSize: '0.875rem', fontWeight: 600, color: 'var(--page-text)' },
  input: { width: '100%', maxWidth: 360, padding: '0.5rem', fontSize: '0.875rem', borderRadius: 4, border: '1px solid var(--input-border)', color: 'var(--input-text)', background: 'var(--input-bg)' },
  select: { padding: '0.5rem', fontSize: '0.875rem', minWidth: 200, borderRadius: 4, border: '1px solid var(--input-border)', color: 'var(--input-text)', background: 'var(--input-bg)' },
  hint: { color: 'var(--page-text-muted)', fontSize: '0.875rem' },
  badge: { display: 'inline-block', padding: '0.15rem 0.4rem', borderRadius: 4, fontSize: '0.75rem', background: 'var(--badge-bg)', color: 'var(--badge-text)', marginRight: '0.25rem', marginBottom: '0.25rem' },
}

// ---------------------------------------------------------------------------
// Permissions Tab
// ---------------------------------------------------------------------------

/** Pestaña Permisos: solo lectura vía GET /api/authorization/permissions (los permisos se gestionan en backend). */
function PermissionsTab() {
  const [items, setItems] = useState<PermissionDto[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = async () => {
    setLoading(true)
    setError(null)
    try {
      const list = await authorizationApi.getPermissions()
      setItems(list ?? [])
    } catch (e) {
      setError(getAuthorizationErrorMessage(e, 'Error al cargar permisos'))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  return (
    <div>
      <h2 style={{ margin: '0 0 1rem 0', fontSize: '1.1rem' }}>Permisos</h2>
      <p style={styles.hint}>Lista de permisos del sistema (solo lectura). Se gestionan en el backend.</p>
      {error && <ErrorBanner message={error} />}
      {loading ? <LoadingSpinner text="Cargando permisos…" /> : (
        <table style={styles.table}>
          <thead><tr><th style={styles.th}>Código</th><th style={styles.th}>Descripción</th><th style={styles.th}>ID</th></tr></thead>
          <tbody>
            {items.length === 0 ? (
              <tr><td colSpan={3} style={styles.td}>No hay permisos.</td></tr>
            ) : items.map((p) => (
              <tr key={p.id}>
                <td style={styles.td}><code>{p.code}</code></td>
                <td style={styles.td}>{p.description || '—'}</td>
                <td style={{ ...styles.td, fontSize: '0.75rem', color: 'var(--page-text-muted)' }}>{p.id}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}

// ---------------------------------------------------------------------------
// Roles Tab
// ---------------------------------------------------------------------------

function RolesTab() {
  const [roles, setRoles] = useState<RoleDto[]>([])
  const [permissions, setPermissions] = useState<PermissionDto[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [showForm, setShowForm] = useState(false)
  const [saving, setSaving] = useState(false)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [formError, setFormError] = useState<string | null>(null)

  // Assign permissions
  const [assigningRoleId, setAssigningRoleId] = useState<string | null>(null)
  const [selectedPermIds, setSelectedPermIds] = useState<string[]>([])
  const [assignError, setAssignError] = useState<string | null>(null)
  const [assignSaving, setAssignSaving] = useState(false)

  const load = async () => {
    setLoading(true)
    setError(null)
    try {
      const [r, p] = await Promise.all([authorizationApi.getRoles(), authorizationApi.getPermissions()])
      setRoles(r ?? [])
      setPermissions(p ?? [])
    } catch (e) {
      setError(getAuthorizationErrorMessage(e, 'Error al cargar roles'))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  const handleCreate = async () => {
    setFormError(null)
    if (!name.trim()) { setFormError('Escribe un nombre para el rol.'); return }
    setSaving(true)
    try {
      await authorizationApi.createRole({ name: name.trim(), description: description.trim() || undefined })
      setName('')
      setDescription('')
      setShowForm(false)
      await load()
    } catch (e) {
      setFormError(getAuthorizationErrorMessage(e, 'Error al crear rol'))
    } finally {
      setSaving(false)
    }
  }

  const startAssign = (roleId: string) => {
    setAssigningRoleId(roleId)
    setSelectedPermIds([])
    setAssignError(null)
  }

  const togglePerm = (id: string) => {
    setSelectedPermIds((prev) => prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id])
  }

  const handleAssign = async () => {
    if (!assigningRoleId || selectedPermIds.length === 0) {
      setAssignError('Selecciona al menos un permiso.')
      return
    }
    setAssignSaving(true)
    setAssignError(null)
    try {
      await authorizationApi.assignPermissionsToRole(assigningRoleId, { permissionIds: selectedPermIds })
      setAssigningRoleId(null)
      setSelectedPermIds([])
      invalidatePermissionCache()
    } catch (e) {
      setAssignError(getAuthorizationErrorMessage(e, 'Error al asignar permisos'))
    } finally {
      setAssignSaving(false)
    }
  }

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
        <h2 style={{ margin: 0, fontSize: '1.1rem' }}>Roles</h2>
        <button type="button" style={{ ...styles.button, ...styles.buttonPrimary }} onClick={() => { setShowForm((x) => !x); setFormError(null) }}>
          {showForm ? 'Cancelar' : 'Crear rol'}
        </button>
      </div>

      {showForm && (
        <div style={styles.form}>
          <div style={styles.formRow}>
            <label style={styles.label}>Nombre *</label>
            <input type="text" style={styles.input} value={name} onChange={(e) => setName(e.target.value)} placeholder="Ej. Editor" />
          </div>
          <div style={styles.formRow}>
            <label style={styles.label}>Descripción (opcional)</label>
            <input type="text" style={styles.input} value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Descripción del rol" />
          </div>
          {formError && <ErrorBanner message={formError} />}
          <button type="button" style={{ ...styles.button, ...styles.buttonPrimary }} disabled={saving} onClick={handleCreate}>
            {saving ? 'Creando…' : 'Crear'}
          </button>
        </div>
      )}

      {error && <ErrorBanner message={error} />}
      {loading ? <LoadingSpinner text="Cargando roles…" /> : (
        <table style={styles.table}>
          <thead><tr><th style={styles.th}>Nombre</th><th style={styles.th}>Descripción</th><th style={styles.th}>Acciones</th></tr></thead>
          <tbody>
            {roles.length === 0 ? (
              <tr><td colSpan={3} style={styles.td}>No hay roles.</td></tr>
            ) : roles.map((r) => (
              <tr key={r.id}>
                <td style={styles.td}><strong>{r.name}</strong></td>
                <td style={styles.td}>{r.description || '—'}</td>
                <td style={styles.td}>
                  <button type="button" style={{ ...styles.button, ...styles.buttonSmall }} onClick={() => startAssign(r.id)}>
                    Asignar permisos
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {assigningRoleId && (
        <div style={{ ...styles.form, marginTop: '1rem' }}>
          <h3 style={{ margin: '0 0 0.75rem 0', fontSize: '1rem' }}>
            Asignar permisos al rol: <strong>{roles.find((r) => r.id === assigningRoleId)?.name}</strong>
          </h3>
          <p style={styles.hint}>Selecciona los permisos y pulsa «Asignar». Esto <strong>reemplaza</strong> los permisos del rol.</p>
          <div style={{ marginBottom: '0.75rem', maxHeight: 200, overflowY: 'auto', border: '1px solid var(--page-border)', borderRadius: 4, padding: '0.5rem', color: 'var(--page-text)' }}>
            {permissions.length === 0 ? (
              <p style={styles.hint}>No hay permisos en el sistema.</p>
            ) : permissions.map((p) => (
              <label key={p.id} style={{ display: 'block', padding: '0.25rem 0', cursor: 'pointer', fontSize: '0.875rem' }}>
                <input
                  type="checkbox"
                  checked={selectedPermIds.includes(p.id)}
                  onChange={() => togglePerm(p.id)}
                  style={{ marginRight: '0.5rem' }}
                />
                <code>{p.code}</code>
                {p.description && <span style={{ color: 'var(--page-text-muted)' }}> — {p.description}</span>}
              </label>
            ))}
          </div>
          {assignError && <ErrorBanner message={assignError} />}
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            <button type="button" style={{ ...styles.button, ...styles.buttonPrimary }} disabled={assignSaving} onClick={handleAssign}>
              {assignSaving ? 'Asignando…' : `Asignar (${selectedPermIds.length})`}
            </button>
            <button type="button" style={styles.button} onClick={() => setAssigningRoleId(null)}>
              Cancelar
            </button>
          </div>
        </div>
      )}
    </div>
  )
}

// ---------------------------------------------------------------------------
// Access Rules Tab
// ---------------------------------------------------------------------------

function RulesTab() {
  const user = useAuthStore((s) => s.user)
  const { projects, environments } = useContextStore()
  const [rules, setRules] = useState<AccessRuleDto[]>([])
  const [roles, setRoles] = useState<RoleDto[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Create rule form
  const [showForm, setShowForm] = useState(false)
  const [saving, setSaving] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [ruleUserId, setRuleUserId] = useState('')
  const [ruleRoleId, setRuleRoleId] = useState('')
  const [ruleProjectId, setRuleProjectId] = useState('')
  const [ruleEnvironmentId, setRuleEnvironmentId] = useState('')
  const [ruleSchemaId, setRuleSchemaId] = useState('')
  const [ruleContentStatus, setRuleContentStatus] = useState('')

  // Lookup user rules
  const [lookupUserId, setLookupUserId] = useState('')

  const loadRoles = async () => {
    try {
      const r = await authorizationApi.getRoles()
      setRoles(r ?? [])
    } catch { /* ignore */ }
  }

  const loadRules = async (userId: string) => {
    if (!userId.trim()) return
    setLoading(true)
    setError(null)
    try {
      const r = await authorizationApi.getUserRules(userId.trim())
      setRules(r ?? [])
    } catch (e) {
      setError(getAuthorizationErrorMessage(e, 'Error al cargar reglas'))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadRoles()
    if (user?.userId) {
      setLookupUserId(user.userId)
      loadRules(user.userId)
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const handleLookup = () => {
    if (lookupUserId.trim()) loadRules(lookupUserId.trim())
  }

  const handleCreate = async () => {
    setFormError(null)
    if (!ruleUserId.trim()) { setFormError('Escribe el ID del usuario.'); return }
    if (!ruleRoleId) { setFormError('Selecciona un rol.'); return }
    setSaving(true)
    try {
      await authorizationApi.createAccessRule({
        userId: ruleUserId.trim(),
        roleId: ruleRoleId,
        projectId: ruleProjectId || null,
        environmentId: ruleEnvironmentId || null,
        schemaId: ruleSchemaId.trim() || null,
        contentStatus: ruleContentStatus.trim() || null,
      })
      setShowForm(false)
      setRuleUserId('')
      setRuleRoleId('')
      setRuleProjectId('')
      setRuleEnvironmentId('')
      setRuleSchemaId('')
      setRuleContentStatus('')
      invalidatePermissionCache()
      if (lookupUserId.trim()) await loadRules(lookupUserId.trim())
    } catch (e) {
      setFormError(getAuthorizationErrorMessage(e, 'Error al crear regla'))
    } finally {
      setSaving(false)
    }
  }

  const handleRevoke = async (ruleId: string) => {
    if (!window.confirm('¿Revocar esta regla de acceso?')) return
    try {
      await authorizationApi.revokeAccessRule(ruleId)
      setRules((prev) => prev.filter((r) => r.id !== ruleId))
      invalidatePermissionCache()
    } catch (e) {
      setError(getAuthorizationErrorMessage(e, 'Error al revocar regla'))
    }
  }

  const roleName = (roleId: string) => roles.find((r) => r.id === roleId)?.name ?? roleId
  const projectName = (id: string | null | undefined) => {
    if (!id) return '—'
    return projects.find((p) => p.id === id)?.name ?? id
  }
  const envName = (id: string | null | undefined) => {
    if (!id) return '—'
    return environments.find((e) => e.id === id)?.name ?? id
  }

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem', flexWrap: 'wrap', gap: '0.5rem' }}>
        <h2 style={{ margin: 0, fontSize: '1.1rem' }}>Reglas de acceso</h2>
        <button type="button" style={{ ...styles.button, ...styles.buttonPrimary }} onClick={() => { setShowForm((x) => !x); setFormError(null) }}>
          {showForm ? 'Cancelar' : 'Asignar rol a usuario'}
        </button>
      </div>

      {/* Lookup */}
      <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '1rem', alignItems: 'center', flexWrap: 'wrap' }}>
        <label style={{ fontSize: '0.875rem', fontWeight: 500 }}>Usuario ID:</label>
        <input
          type="text"
          style={{ ...styles.input, maxWidth: 300 }}
          value={lookupUserId}
          onChange={(e) => setLookupUserId(e.target.value)}
          placeholder="GUID del usuario"
        />
        <button type="button" style={{ ...styles.button, ...styles.buttonSmall }} onClick={handleLookup}>
          Buscar reglas
        </button>
      </div>

      {showForm && (
        <div style={styles.form}>
          <h3 style={{ margin: '0 0 0.75rem 0', fontSize: '1rem' }}>Nueva regla de acceso</h3>
          <p style={styles.hint}>Asigna un rol a un usuario, opcionalmente limitado a un proyecto, entorno, schema o estado de contenido.</p>
          <div style={styles.formRow}>
            <label style={styles.label}>ID del usuario *</label>
            <input type="text" style={styles.input} value={ruleUserId} onChange={(e) => setRuleUserId(e.target.value)} placeholder="GUID del usuario" />
          </div>
          <div style={styles.formRow}>
            <label style={styles.label}>Rol *</label>
            <select style={styles.select} value={ruleRoleId} onChange={(e) => setRuleRoleId(e.target.value)}>
              <option value="">— Seleccionar rol —</option>
              {roles.map((r) => (
                <option key={r.id} value={r.id}>{r.name}</option>
              ))}
            </select>
          </div>
          <div style={styles.formRow}>
            <label style={styles.label}>Proyecto (opcional)</label>
            <select style={styles.select} value={ruleProjectId} onChange={(e) => setRuleProjectId(e.target.value)}>
              <option value="">— Global (todos) —</option>
              {projects.map((p) => (
                <option key={p.id} value={p.id}>{p.name}</option>
              ))}
            </select>
          </div>
          <div style={styles.formRow}>
            <label style={styles.label}>Entorno (opcional)</label>
            <select style={styles.select} value={ruleEnvironmentId} onChange={(e) => setRuleEnvironmentId(e.target.value)}>
              <option value="">— Todos —</option>
              {environments.map((e) => (
                <option key={e.id} value={e.id}>{e.name}</option>
              ))}
            </select>
          </div>
          <div style={styles.formRow}>
            <label style={styles.label}>Schema ID (opcional)</label>
            <input type="text" style={styles.input} value={ruleSchemaId} onChange={(e) => setRuleSchemaId(e.target.value)} placeholder="GUID del schema" />
          </div>
          <div style={styles.formRow}>
            <label style={styles.label}>Estado de contenido (opcional)</label>
            <select style={styles.select} value={ruleContentStatus} onChange={(e) => setRuleContentStatus(e.target.value)}>
              <option value="">— Todos —</option>
              <option value="Draft">Draft</option>
              <option value="InReview">InReview</option>
              <option value="Published">Published</option>
              <option value="Archived">Archived</option>
            </select>
          </div>
          {formError && <ErrorBanner message={formError} />}
          <button type="button" style={{ ...styles.button, ...styles.buttonPrimary }} disabled={saving} onClick={handleCreate}>
            {saving ? 'Guardando…' : 'Crear regla'}
          </button>
        </div>
      )}

      {error && <ErrorBanner message={error} />}
      {loading ? <LoadingSpinner text="Cargando reglas…" /> : (
        <table style={styles.table}>
          <thead>
            <tr>
              <th style={styles.th}>Rol</th>
              <th style={styles.th}>Proyecto</th>
              <th style={styles.th}>Entorno</th>
              <th style={styles.th}>Schema</th>
              <th style={styles.th}>Estado</th>
              <th style={styles.th}>Acciones</th>
            </tr>
          </thead>
          <tbody>
            {rules.length === 0 ? (
              <tr><td colSpan={6} style={styles.td}>No hay reglas para este usuario.</td></tr>
            ) : rules.map((rule) => (
              <tr key={rule.id}>
                <td style={styles.td}><strong>{roleName(rule.roleId)}</strong></td>
                <td style={styles.td}>{projectName(rule.projectId)}</td>
                <td style={styles.td}>{envName(rule.environmentId)}</td>
                <td style={styles.td}>{rule.schemaId ?? '—'}</td>
                <td style={styles.td}>{rule.contentStatus ?? '—'}</td>
                <td style={styles.td}>
                  <button type="button" style={{ ...styles.button, ...styles.buttonSmall, ...styles.buttonDanger }} onClick={() => handleRevoke(rule.id)}>
                    Revocar
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}

// ---------------------------------------------------------------------------
// Main page
// ---------------------------------------------------------------------------

export function RolesPermissionsPage() {
  const [tab, setTab] = useState<Tab>('permissions')

  return (
    <div style={styles.container}>
      <h1 style={styles.title}>Roles y permisos</h1>

      <div style={styles.tabs}>
        <button type="button" style={{ ...styles.tab, ...(tab === 'permissions' ? styles.tabActive : {}) }} onClick={() => setTab('permissions')}>
          Permisos
        </button>
        <button type="button" style={{ ...styles.tab, ...(tab === 'roles' ? styles.tabActive : {}) }} onClick={() => setTab('roles')}>
          Roles
        </button>
        <button type="button" style={{ ...styles.tab, ...(tab === 'rules' ? styles.tabActive : {}) }} onClick={() => setTab('rules')}>
          Reglas de acceso
        </button>
      </div>

      {tab === 'permissions' && <PermissionsTab />}
      {tab === 'roles' && <RolesTab />}
      {tab === 'rules' && <RulesTab />}
    </div>
  )
}
