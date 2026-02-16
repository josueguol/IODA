import { useEffect, useState } from 'react'
import { authApi } from '../../modules/auth/api/auth-api'
import { identityAdminApi } from '../../modules/auth/api/identity-admin-api'
import { authorizationApi } from '../../modules/authorization/api/authorization-api'
import { SUPERADMIN_ROLE_NAME } from '../../modules/authorization/constants'
import { invalidatePermissionCache } from '../../modules/authorization/hooks/usePermission'
import { useContextStore } from '../../modules/core/store/context-store'
import { Can } from '../../modules/authorization/components/Can'
import { LoadingSpinner, ErrorBanner } from '../../shared/components'
import type { UserListItemDto } from '../../modules/auth/types'
import type { RoleDto, AccessRuleDto } from '../../modules/authorization/types'

const styles: Record<string, React.CSSProperties> = {
  container: { maxWidth: 960, color: 'var(--page-text)' },
  title: { marginTop: 0, marginBottom: '1rem', color: 'var(--page-text)', fontSize: '1.5rem' },
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
    textDecoration: 'none',
  },
  buttonPrimary: { background: '#0d6efd', color: 'white', border: 'none' },
  buttonDanger: { background: '#dc3545', color: 'white', border: 'none' },
  buttonSmall: { padding: '0.35rem 0.6rem', fontSize: '0.8125rem' },
  form: {
    maxWidth: 480,
    marginBottom: '1.5rem',
    padding: '1rem',
    background: 'var(--page-bg-elevated)',
    borderRadius: 8,
    border: '1px solid var(--page-border)',
    color: 'var(--page-text)',
  },
  formRow: { marginBottom: '0.75rem' },
  label: { display: 'block', marginBottom: '0.25rem', fontSize: '0.875rem', fontWeight: 600, color: 'var(--page-text)' },
  input: {
    width: '100%',
    maxWidth: 360,
    padding: '0.5rem',
    fontSize: '0.875rem',
    borderRadius: 4,
    border: '1px solid var(--input-border)',
    color: 'var(--input-text)',
    background: 'var(--input-bg)',
  },
  select: {
    padding: '0.5rem',
    fontSize: '0.875rem',
    minWidth: 200,
    borderRadius: 4,
    border: '1px solid var(--input-border)',
    color: 'var(--input-text)',
    background: 'var(--input-bg)',
  },
  hint: { color: 'var(--page-text-muted)', fontSize: '0.875rem', marginTop: '0.25rem' },
  badge: {
    display: 'inline-block',
    padding: '0.15rem 0.4rem',
    borderRadius: 4,
    fontSize: '0.75rem',
    fontWeight: 500,
  },
  badgeActive: { background: '#d1e7dd', color: '#0f5132' },
  badgeInactive: { background: '#f8d7da', color: '#842029' },
  rolePanel: {
    padding: '1rem',
    background: 'var(--page-bg-elevated)',
    borderRadius: 8,
    border: '1px solid var(--page-border)',
    marginTop: '0.5rem',
    marginBottom: '0.5rem',
  },
  roleTag: {
    display: 'inline-flex',
    alignItems: 'center',
    gap: '0.35rem',
    padding: '0.25rem 0.5rem',
    borderRadius: 4,
    fontSize: '0.8125rem',
    background: 'var(--badge-bg)',
    color: 'var(--badge-text)',
    marginRight: '0.35rem',
    marginBottom: '0.35rem',
  },
  revokeBtn: {
    background: 'none',
    border: 'none',
    color: '#dc3545',
    cursor: 'pointer',
    fontSize: '0.875rem',
    padding: '0 0.15rem',
    lineHeight: 1,
  },
}

function formatDate(iso: string) {
  try {
    return new Date(iso).toLocaleString()
  } catch {
    return iso
  }
}

/* ─── Panel de roles para un usuario ─── */

function UserRolesPanel({
  user,
  roles,
  onClose,
}: {
  user: UserListItemDto
  roles: RoleDto[]
  onClose: () => void
}) {
  const { projects, environments } = useContextStore()
  const [rules, setRules] = useState<AccessRuleDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Formulario para asignar rol
  const [showAssign, setShowAssign] = useState(false)
  const [selectedRoleId, setSelectedRoleId] = useState('')
  const [selectedProjectId, setSelectedProjectId] = useState('')
  const [selectedEnvId, setSelectedEnvId] = useState('')
  const [saving, setSaving] = useState(false)
  const [assignError, setAssignError] = useState<string | null>(null)

  const loadRules = async () => {
    setLoading(true)
    setError(null)
    try {
      const list = await authorizationApi.getUserRules(user.id)
      setRules(list ?? [])
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al cargar reglas')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadRules()
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user.id])

  const handleAssign = async () => {
    setAssignError(null)
    if (!selectedRoleId) {
      setAssignError('Selecciona un rol.')
      return
    }
    setSaving(true)
    try {
      await authorizationApi.createAccessRule({
        userId: user.id,
        roleId: selectedRoleId,
        projectId: selectedProjectId || null,
        environmentId: selectedEnvId || null,
        schemaId: null,
        contentStatus: null,
      })
      setSelectedRoleId('')
      setSelectedProjectId('')
      setSelectedEnvId('')
      setShowAssign(false)
      invalidatePermissionCache()
      await loadRules()
    } catch (e) {
      setAssignError(e instanceof Error ? e.message : 'Error al asignar rol')
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
      setError(e instanceof Error ? e.message : 'Error al revocar regla')
    }
  }

  const roleName = (roleId: string) => roles.find((r) => r.id === roleId)?.name ?? roleId
  const projectName = (id: string | null | undefined) =>
    id ? (projects.find((p) => p.id === id)?.name ?? id) : 'Global'
  const envName = (id: string | null | undefined) =>
    id ? (environments.find((e) => e.id === id)?.name ?? id) : 'Todos'

  return (
    <div style={styles.rolePanel}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.75rem' }}>
        <h3 style={{ margin: 0, fontSize: '0.9375rem', color: 'var(--page-text)' }}>
          Roles de <strong>{user.displayName ?? user.email}</strong>
        </h3>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button
            type="button"
            style={{ ...styles.button, ...styles.buttonSmall, ...styles.buttonPrimary }}
            onClick={() => { setShowAssign((x) => !x); setAssignError(null) }}
          >
            {showAssign ? 'Cancelar' : 'Asignar rol'}
          </button>
          <button type="button" style={{ ...styles.button, ...styles.buttonSmall }} onClick={onClose}>
            Cerrar
          </button>
        </div>
      </div>

      {/* Formulario de asignación */}
      {showAssign && (
        <div style={{ ...styles.form, maxWidth: '100%', marginBottom: '0.75rem' }}>
          <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap', alignItems: 'flex-end' }}>
            <div>
              <label style={styles.label}>Rol *</label>
              <select style={styles.select} value={selectedRoleId} onChange={(e) => setSelectedRoleId(e.target.value)}>
                <option value="">— Seleccionar rol —</option>
                {roles.filter((r) => r.name !== SUPERADMIN_ROLE_NAME).map((r) => (
                  <option key={r.id} value={r.id}>{r.name}</option>
                ))}
              </select>
            </div>
            <div>
              <label style={styles.label}>Proyecto (opcional)</label>
              <select style={styles.select} value={selectedProjectId} onChange={(e) => setSelectedProjectId(e.target.value)}>
                <option value="">— Global (todos) —</option>
                {projects.map((p) => (
                  <option key={p.id} value={p.id}>{p.name}</option>
                ))}
              </select>
            </div>
            <div>
              <label style={styles.label}>Entorno (opcional)</label>
              <select style={styles.select} value={selectedEnvId} onChange={(e) => setSelectedEnvId(e.target.value)}>
                <option value="">— Todos —</option>
                {environments.map((e) => (
                  <option key={e.id} value={e.id}>{e.name}</option>
                ))}
              </select>
            </div>
            <button
              type="button"
              style={{ ...styles.button, ...styles.buttonPrimary }}
              disabled={saving}
              onClick={handleAssign}
            >
              {saving ? 'Asignando…' : 'Asignar'}
            </button>
          </div>
          {assignError && <div style={{ marginTop: '0.5rem' }}><ErrorBanner message={assignError} /></div>}
        </div>
      )}

      {/* Lista de reglas actuales */}
      {error && <ErrorBanner message={error} />}
      {loading ? (
        <LoadingSpinner text="Cargando roles…" />
      ) : rules.length === 0 ? (
        <p style={styles.hint}>Este usuario no tiene roles asignados.</p>
      ) : (
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.25rem' }}>
          {rules.map((rule) => {
            const isSuperAdminRule = roleName(rule.roleId) === SUPERADMIN_ROLE_NAME
            return (
              <span key={rule.id} style={styles.roleTag} title={isSuperAdminRule ? 'No se puede revocar el rol SuperAdmin' : `Proyecto: ${projectName(rule.projectId)} · Entorno: ${envName(rule.environmentId)}`}>
                <strong>{roleName(rule.roleId)}</strong>
                {rule.projectId && (
                  <span style={{ fontSize: '0.75rem', opacity: 0.7 }}>
                    ({projectName(rule.projectId)}{rule.environmentId ? ` / ${envName(rule.environmentId)}` : ''})
                  </span>
                )}
                {isSuperAdminRule ? (
                  <span style={{ fontSize: '0.75rem', color: 'var(--page-text-muted)', marginLeft: '0.25rem' }} title="No se puede revocar el rol SuperAdmin">
                    (rol del sistema)
                  </span>
                ) : (
                  <button
                    type="button"
                    style={styles.revokeBtn}
                    onClick={() => handleRevoke(rule.id)}
                    title="Revocar esta regla"
                  >
                    &#10005;
                  </button>
                )}
              </span>
            )
          })}
        </div>
      )}
    </div>
  )
}

/* ─── Página principal de Usuarios ─── */

export function UsersPage() {
  const [users, setUsers] = useState<UserListItemDto[]>([])
  const [roles, setRoles] = useState<RoleDto[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [showForm, setShowForm] = useState(false)
  const [saving, setSaving] = useState(false)
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [formError, setFormError] = useState<string | null>(null)
  const [managingUserId, setManagingUserId] = useState<string | null>(null)

  const load = async () => {
    setLoading(true)
    setError(null)
    try {
      const [userList, roleList] = await Promise.all([
        identityAdminApi.getUsers(),
        authorizationApi.getRoles(),
      ])
      setUsers(Array.isArray(userList) ? userList : [])
      setRoles(roleList ?? [])
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al cargar datos')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
  }, [])

  const handleCreate = async () => {
    setFormError(null)
    if (!email.trim()) {
      setFormError('El email es obligatorio.')
      return
    }
    if (!password.trim()) {
      setFormError('La contraseña es obligatoria.')
      return
    }
    if (password.length < 8) {
      setFormError('La contraseña debe tener al menos 8 caracteres.')
      return
    }
    setSaving(true)
    try {
      await authApi.register({
        email: email.trim(),
        password,
        displayName: displayName.trim() || undefined,
      })
      setEmail('')
      setPassword('')
      setDisplayName('')
      setShowForm(false)
      await load()
    } catch (e) {
      setFormError(e instanceof Error ? e.message : 'Error al crear usuario')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div style={styles.container}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem', flexWrap: 'wrap', gap: '0.5rem' }}>
        <h1 style={styles.title}>Usuarios</h1>
        <Can permission="user.create" fallback={null}>
          <button
            type="button"
            style={{ ...styles.button, ...styles.buttonPrimary }}
            onClick={() => {
              setShowForm((x) => !x)
              setFormError(null)
            }}
          >
            {showForm ? 'Cancelar' : 'Crear usuario'}
          </button>
        </Can>
      </div>

      {showForm && (
        <div style={styles.form}>
          <h2 style={{ margin: '0 0 0.75rem 0', fontSize: '1rem', color: 'var(--page-text)' }}>Nuevo usuario</h2>
          <div style={styles.formRow}>
            <label style={styles.label}>Email *</label>
            <input
              type="email"
              style={styles.input}
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="usuario@ejemplo.com"
            />
          </div>
          <div style={styles.formRow}>
            <label style={styles.label}>Contraseña *</label>
            <input
              type="password"
              style={styles.input}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Mínimo 8 caracteres"
            />
          </div>
          <div style={styles.formRow}>
            <label style={styles.label}>Nombre para mostrar (opcional)</label>
            <input
              type="text"
              style={styles.input}
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              placeholder="Ej. María García"
            />
          </div>
          {formError && <ErrorBanner message={formError} />}
          <button type="button" style={{ ...styles.button, ...styles.buttonPrimary }} disabled={saving} onClick={handleCreate}>
            {saving ? 'Creando…' : 'Crear'}
          </button>
        </div>
      )}

      {error && <ErrorBanner message={error} />}
      {loading ? (
        <LoadingSpinner text="Cargando usuarios…" />
      ) : (
        <table style={styles.table}>
          <thead>
            <tr>
              <th style={styles.th}>Email</th>
              <th style={styles.th}>Nombre</th>
              <th style={styles.th}>Estado</th>
              <th style={styles.th}>Creado</th>
              <th style={styles.th}>Acciones</th>
            </tr>
          </thead>
          <tbody>
            {users.length === 0 ? (
              <tr>
                <td colSpan={5} style={styles.td}>
                  No hay usuarios.
                </td>
              </tr>
            ) : (
              users.map((u) => (
                <>
                  <tr key={u.id}>
                    <td style={styles.td}>
                      <strong>{u.email}</strong>
                    </td>
                    <td style={styles.td}>{u.displayName ?? '—'}</td>
                    <td style={styles.td}>
                      <span style={{ ...styles.badge, ...(u.isActive ? styles.badgeActive : styles.badgeInactive) }}>
                        {u.isActive ? 'Activo' : 'Inactivo'}
                      </span>
                    </td>
                    <td style={styles.td}>{formatDate(u.createdAt)}</td>
                    <td style={styles.td}>
                      <button
                        type="button"
                        style={{
                          ...styles.button,
                          ...styles.buttonSmall,
                          ...(managingUserId === u.id ? styles.buttonPrimary : {}),
                        }}
                        onClick={() => setManagingUserId(managingUserId === u.id ? null : u.id)}
                      >
                        {managingUserId === u.id ? 'Cerrar roles' : 'Gestionar roles'}
                      </button>
                    </td>
                  </tr>
                  {managingUserId === u.id && (
                    <tr key={`${u.id}-roles`}>
                      <td colSpan={5} style={{ padding: '0.25rem 0.5rem', borderBottom: '1px solid var(--page-border)' }}>
                        <UserRolesPanel
                          user={u}
                          roles={roles}
                          onClose={() => setManagingUserId(null)}
                        />
                      </td>
                    </tr>
                  )}
                </>
              ))
            )}
          </tbody>
        </table>
      )}
    </div>
  )
}
