/**
 * Tipos alineados con la Publishing API (PublicationRequestDto, etc.).
 */

export type PublicationRequestStatus = 'Pending' | 'Approved' | 'Rejected'

export interface PublicationRequest {
  id: string
  contentId: string
  projectId: string
  environmentId: string
  requestedBy: string
  /** Backend puede enviar 0=Pending, 1=Approved, 2=Rejected. */
  status: PublicationRequestStatus | number
  requestedAt: string
  resolvedAt: string | null
  resolvedBy: string | null
  rejectionReason: string | null
  validationErrors: string | null
}

const STATUS_MAP: Record<number, PublicationRequestStatus> = {
  0: 'Pending',
  1: 'Approved',
  2: 'Rejected',
}

export function normalizeStatus(s: PublicationRequestStatus | number): PublicationRequestStatus {
  return typeof s === 'number' ? (STATUS_MAP[s] ?? 'Pending') : s
}

export interface RequestPublicationResponse {
  id: string
}
