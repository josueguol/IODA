import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { publishingApi, normalizeStatus } from '../../modules/publishing'
import { useAuthStore } from '../../modules/auth/store/auth-store'
import { useContextStore } from '../../modules/core/store/context-store'
import type { PublicationRequest } from '../../modules/publishing'

const styles: Record<string, React.CSSProperties> = {
  container: { maxWidth: 900, color: 'var(--page-text)' },
  title: { marginTop: 0, marginBottom: '1rem', color: 'var(--page-text)' },
  filters: { display: 'flex', gap: '1rem', flexWrap: 'wrap', marginBottom: '1.5rem', alignItems: 'flex-end' },
  select: { padding: '0.5rem', fontSize: '0.875rem', minWidth: 160, borderRadius: 4, border: '1px solid var(--input-border)', background: 'var(--input-bg)', color: 'var(--input-text)' },
  table: { width: '100%', borderCollapse: 'collapse', fontSize: '0.875rem', color: 'var(--page-text)' },
  th: { textAlign: 'left', padding: '0.5rem', borderBottom: '2px solid var(--page-border)', color: 'var(--page-text)' },
  td: { padding: '0.5rem', borderBottom: '1px solid var(--page-border)', color: 'var(--page-text)' },
  link: { color: '#0d6efd', textDecoration: 'none' },
  button: { padding: '0.35rem 0.65rem', fontSize: '0.8rem', cursor: 'pointer', borderRadius: 4, border: 'none', marginRight: '0.35rem' },
  approveBtn: { background: '#198754', color: 'white' },
  rejectBtn: { background: '#dc3545', color: 'white' },
  error: { color: '#dc3545', fontSize: '0.875rem', marginBottom: '0.5rem' },
  hint: { color: 'var(--page-text-muted)', fontSize: '0.875rem' },
  validationErrors: { fontSize: '0.8rem', color: '#856404', background: '#fff3cd', padding: '0.35rem', borderRadius: 4, marginTop: '0.25rem' },
}

export function PublishPage() {
  const user = useAuthStore((s) => s.user)
  const { currentProjectId } = useContextStore()
  const [requests, setRequests] = useState<PublicationRequest[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [statusFilter, setStatusFilter] = useState<string>('')
  const [contentIdFilter, setContentIdFilter] = useState('')
  const [actionLoading, setActionLoading] = useState<string | null>(null)
  const [rejectReason, setRejectReason] = useState<Record<string, string>>({})
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null)

  useEffect(() => {
    setLoading(true)
    setError(null)
    publishingApi
      .getPublicationRequests({
        contentId: contentIdFilter.trim() || undefined,
        status: statusFilter ? (statusFilter as 'Pending' | 'Approved' | 'Rejected') : undefined,
      })
      .then((list) => setRequests(list ?? []))
      .catch((e) => setError(e instanceof Error ? e.message : 'Error al cargar solicitudes'))
      .finally(() => setLoading(false))
  }, [statusFilter, contentIdFilter])

  const refetchRequests = () => {
    setLoading(true)
    publishingApi
      .getPublicationRequests({
        contentId: contentIdFilter.trim() || undefined,
        status: statusFilter ? (statusFilter as 'Pending' | 'Approved' | 'Rejected') : undefined,
      })
      .then((list) => setRequests(list ?? []))
      .catch(() => {})
      .finally(() => setLoading(false))
  }

  const handleApprove = async (requestId: string) => {
    const approvedBy = user?.userId
    if (!approvedBy) return
    setActionLoading(requestId)
    setMessage(null)
    try {
      await publishingApi.approvePublication(requestId, { approvedBy })
      setMessage({ type: 'success', text: 'Solicitud aprobada. El contenido ha pasado a Published en Core.' })
      refetchRequests()
    } catch (e) {
      setMessage({ type: 'error', text: e instanceof Error ? e.message : 'Error al aprobar' })
    } finally {
      setActionLoading(null)
    }
  }

  const handleReject = async (requestId: string) => {
    const rejectedBy = user?.userId
    if (!rejectedBy) return
    setActionLoading(requestId)
    setMessage(null)
    try {
      await publishingApi.rejectPublication(requestId, {
        rejectedBy,
        reason: rejectReason[requestId]?.trim() || undefined,
      })
      setMessage({ type: 'success', text: 'Solicitud rechazada.' })
      setRejectReason((prev) => ({ ...prev, [requestId]: '' }))
      refetchRequests()
    } catch (e) {
      setMessage({ type: 'error', text: e instanceof Error ? e.message : 'Error al rechazar' })
    } finally {
      setActionLoading(null)
    }
  }

  return (
    <div style={styles.container}>
      <h1 style={styles.title}>Solicitudes de publicación</h1>
      <p style={styles.hint}>
        Lista de solicitudes de publicación. Puedes aprobar o rechazar las que estén en estado Pending (requiere permiso content.publish).
      </p>

      <div style={styles.filters}>
        <div>
          <label htmlFor="status-filter">Estado </label>
          <br />
          <select
            id="status-filter"
            style={styles.select}
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
          >
            <option value="">Todos</option>
            <option value="Pending">Pending</option>
            <option value="Approved">Approved</option>
            <option value="Rejected">Rejected</option>
          </select>
        </div>
        <div>
          <label htmlFor="content-id-filter">Content ID (opcional) </label>
          <br />
          <input
            id="content-id-filter"
            type="text"
            style={styles.select}
            value={contentIdFilter}
            onChange={(e) => setContentIdFilter(e.target.value)}
            placeholder="UUID del contenido"
          />
        </div>
      </div>

      {message && (
        <p style={{ color: message.type === 'error' ? '#dc3545' : '#198754', fontSize: '0.875rem', marginBottom: '0.5rem' }}>
          {message.text}
        </p>
      )}
      {error && <p style={styles.error}>{error}</p>}
      {loading && <p style={styles.hint}>Cargando…</p>}

      {!loading && (
        <table style={styles.table}>
          <thead>
            <tr>
              <th style={styles.th}>Content ID</th>
              <th style={styles.th}>Estado</th>
              <th style={styles.th}>Solicitado</th>
              <th style={styles.th}>Resuelto</th>
              <th style={styles.th}>Acciones</th>
            </tr>
          </thead>
          <tbody>
            {requests.length === 0 ? (
              <tr>
                <td colSpan={5} style={styles.td}>
                  No hay solicitudes. Desde la edición de un contenido (Draft) puedes &quot;Solicitar publicación&quot;.
                </td>
              </tr>
            ) : (
              requests.map((req) => {
                const status = normalizeStatus(req.status)
                return (
                  <tr key={req.id}>
                    <td style={styles.td}>
                      <Link
                        to={currentProjectId ? `/content/${req.contentId}/edit` : '#'}
                        style={styles.link}
                      >
                        {req.contentId.slice(0, 8)}…
                      </Link>
                    </td>
                    <td style={styles.td}>{status}</td>
                    <td style={styles.td}>
                      {req.requestedAt ? new Date(req.requestedAt).toLocaleString() : '—'}
                    </td>
                    <td style={styles.td}>
                      {req.resolvedAt ? new Date(req.resolvedAt).toLocaleString() : '—'}
                      {req.rejectionReason && (
                        <div style={{ fontSize: '0.8rem', color: '#666' }}>Motivo: {req.rejectionReason}</div>
                      )}
                      {req.validationErrors && (
                        <div style={styles.validationErrors}>{req.validationErrors}</div>
                      )}
                    </td>
                    <td style={styles.td}>
                      {status === 'Pending' && (
                        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.25rem', alignItems: 'center' }}>
                          <button
                            type="button"
                            style={{ ...styles.button, ...styles.approveBtn }}
                            onClick={() => handleApprove(req.id)}
                            disabled={!!actionLoading}
                          >
                            {actionLoading === req.id ? '…' : 'Aprobar'}
                          </button>
                          <input
                            type="text"
                            placeholder="Motivo rechazo (opcional)"
                            value={rejectReason[req.id] ?? ''}
                            onChange={(e) => setRejectReason((prev) => ({ ...prev, [req.id]: e.target.value }))}
                            style={{ width: 140, padding: '0.25rem', fontSize: '0.8rem' }}
                          />
                          <button
                            type="button"
                            style={{ ...styles.button, ...styles.rejectBtn }}
                            onClick={() => handleReject(req.id)}
                            disabled={!!actionLoading}
                          >
                            {actionLoading === req.id ? '…' : 'Rechazar'}
                          </button>
                        </div>
                      )}
                    </td>
                  </tr>
                )
              })
            )}
          </tbody>
        </table>
      )}

      <p style={{ marginTop: '1rem' }}>
        <Link to="/content" style={styles.link}>
          Volver al listado de contenido
        </Link>
      </p>
    </div>
  )
}
