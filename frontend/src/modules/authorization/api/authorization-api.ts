import { config } from '../../../config/env'
import { createAuthAwareHttpClient } from '../../../shared/api'
import { useAuthStore } from '../../auth/store/auth-store'
import type {
  CheckAccessRequest,
  CheckAccessResult,
  RoleDto,
  CreateRoleRequest,
  AssignPermissionsRequest,
  PermissionDto,
  CreatePermissionRequest,
  AccessRuleDto,
  CreateAccessRuleRequest,
} from '../types'

const authorizationClient = createAuthAwareHttpClient({
  baseUrl: config.authorizationApiUrl,
  getAccessToken: () => useAuthStore.getState().accessToken,
  refreshSession: () => useAuthStore.getState().refreshSession(),
  onUnauthorized: () => {
    useAuthStore.getState().logout()
    window.location.href = config.routerType === 'hash' ? '/#/login' : '/login'
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

  /** GET /api/authorization/permissions */
  getPermissions: () =>
    authorizationClient.get<PermissionDto[]>('api/authorization/permissions'),

  /** POST /api/authorization/permissions → devuelve el id del permiso creado */
  createPermission: (body: CreatePermissionRequest) =>
    authorizationClient.post<string>('api/authorization/permissions', body),

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
