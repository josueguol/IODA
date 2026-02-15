import { config } from '../../../config/env'
import { createAuthAwareHttpClient } from '../../../shared/api'
import { buildLoginRedirect } from '../../../shared/auth-redirect'
import { useAuthStore } from '../../auth/store/auth-store'
import type {
  CheckAccessRequest,
  CheckAccessResult,
  RoleDto,
  CreateRoleRequest,
  AssignPermissionsRequest,
  PermissionDto,
  AccessRuleDto,
  CreateAccessRuleRequest,
} from '../types'

const authorizationClient = createAuthAwareHttpClient({
  baseUrl: config.authorizationApiUrl,
  getAccessToken: () => useAuthStore.getState().accessToken,
  refreshSession: () => useAuthStore.getState().refreshSession(),
  onUnauthorized: (reason) => {
    if (reason === '401') {
      useAuthStore.getState().logout()
      window.location.href = buildLoginRedirect(config.routerType, reason)
    }
    // 403: no desloguear ni redirigir; el error se propaga y la UI muestra mensaje (ej. "No tienes permiso…")
  },
})

/** Cliente de la Authorization API (requiere JWT). */
export const authorizationApi = {
  // -----------------------------------------------------------------------
  // Check access
  // -----------------------------------------------------------------------

  /** POST /api/authorization/check */
  checkAccess: (body: CheckAccessRequest) =>
    authorizationClient.post<CheckAccessResult>('api/authorization/check', body),

  // -----------------------------------------------------------------------
  // Roles
  // -----------------------------------------------------------------------

  /** GET /api/authorization/roles */
  getRoles: () =>
    authorizationClient.get<RoleDto[]>('api/authorization/roles'),

  /** POST /api/authorization/roles → devuelve el id del rol creado */
  createRole: (body: CreateRoleRequest) =>
    authorizationClient.post<string>('api/authorization/roles', body),

  /** POST /api/authorization/roles/{roleId}/permissions → 204 */
  assignPermissionsToRole: (roleId: string, body: AssignPermissionsRequest) =>
    authorizationClient.post<void>(`api/authorization/roles/${roleId}/permissions`, body),

  // -----------------------------------------------------------------------
  // Permissions
  // -----------------------------------------------------------------------

  /** GET /api/authorization/permissions (solo lectura; los permisos se gestionan en backend). */
  getPermissions: () =>
    authorizationClient.get<PermissionDto[]>('api/authorization/permissions'),

  // -----------------------------------------------------------------------
  // Access rules
  // -----------------------------------------------------------------------

  /** GET /api/authorization/users/{userId}/rules */
  getUserRules: (userId: string) =>
    authorizationClient.get<AccessRuleDto[]>(`api/authorization/users/${userId}/rules`),

  /** POST /api/authorization/rules → devuelve el id de la regla creada */
  createAccessRule: (body: CreateAccessRuleRequest) =>
    authorizationClient.post<string>('api/authorization/rules', body),

  /** DELETE /api/authorization/rules/{ruleId} → 204 */
  revokeAccessRule: (ruleId: string) =>
    authorizationClient.delete<void>(`api/authorization/rules/${ruleId}`),
}
