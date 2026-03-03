import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { publishingApi, normalizeStatus } from '../../modules/publishing'
import { useAuthStore } from '../../modules/auth/store/auth-store'
import { useContextStore } from '../../modules/core/store/context-store'
import type { PublicationRequest } from '../../modules/publishing'
import './PublishPage.css'

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
    <div className="publish-page">
      <div className="publish-page__filters">
        <div>
          <label htmlFor="status-filter">Estado </label>
          <br />
          <select
            id="status-filter"
            className="publish-page__select"
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
            className="publish-page__select"
            value={contentIdFilter}
            onChange={(e) => setContentIdFilter(e.target.value)}
            placeholder="UUID del contenido"
          />
        </div>
      </div>

      {message && (
        <p className={`publish-page__message ${message.type === 'error' ? 'publish-page__message--error' : 'publish-page__message--success'}`}>
          {message.text}
        </p>
      )}
      {error && <p className="publish-page__error">{error}</p>}
      {loading && <p className="publish-page__hint">Cargando…</p>}

      {!loading && (
        <table className="publish-page__table">
          <thead>
            <tr>
              <th className="publish-page__th">Content ID</th>
              <th className="publish-page__th">Estado</th>
              <th className="publish-page__th">Solicitado</th>
              <th className="publish-page__th">Resuelto</th>
              <th className="publish-page__th">Acciones</th>
            </tr>
          </thead>
          <tbody>
            {requests.length === 0 ? (
              <tr>
                <td colSpan={5} className="publish-page__td">
                  No hay solicitudes. Desde la edición de un contenido (Draft) puedes &quot;Solicitar publicación&quot;.
                </td>
              </tr>
            ) : (
              requests.map((req) => {
                const status = normalizeStatus(req.status)
                return (
                  <tr key={req.id}>
                    <td className="publish-page__td">
                      <Link
                        to={currentProjectId ? `/content/editor?contentId=${req.contentId}` : '#'}
                        className="publish-page__link"
                      >
                        {req.contentId.slice(0, 8)}…
                      </Link>
                    </td>
                    <td className="publish-page__td">{status}</td>
                    <td className="publish-page__td">
                      {req.requestedAt ? new Date(req.requestedAt).toLocaleString() : '—'}
                    </td>
                    <td className="publish-page__td">
                      {req.resolvedAt ? new Date(req.resolvedAt).toLocaleString() : '—'}
                      {req.rejectionReason && (
                        <div className="publish-page__rejection-hint">Motivo: {req.rejectionReason}</div>
                      )}
                      {req.validationErrors && (
                        <div className="publish-page__validation-errors">{req.validationErrors}</div>
                      )}
                    </td>
                    <td className="publish-page__td">
                      {status === 'Pending' && (
                        <div className="publish-page__actions-cell">
                          <button
                            type="button"
                            className="publish-page__button publish-page__button--approve"
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
                            className="publish-page__reject-input"
                          />
                          <button
                            type="button"
                            className="publish-page__button publish-page__button--reject"
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

      <p className="publish-page__back-wrap">
        <Link to="/content" className="publish-page__link">
          Volver al listado de contenido
        </Link>
      </p>
    </div>
  )
}
