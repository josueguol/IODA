import { config } from '../../../config/env'
import { createAuthAwareHttpClient } from '../../../shared/api'
import { useAuthStore } from '../store/auth-store'
import type { UserListItemDto } from '../types'

const identityAdminClient = createAuthAwareHttpClient({
  baseUrl: config.identityApiUrl,
  getAccessToken: () => useAuthStore.getState().accessToken,
  refreshSession: () => useAuthStore.getState().refreshSession(),
  onUnauthorized: () => {
    useAuthStore.getState().logout()
    window.location.href = config.routerType === 'hash' ? '/#/login' : '/login'
  },
})

/** Cliente de la Identity API para administración (requiere JWT). Crear usuarios se hace con authApi.register (POST /api/auth/register). */
export const identityAdminApi = {
  /** GET /api/auth/users */
  getUsers: () => identityAdminClient.get<UserListItemDto[]>('api/auth/users'),
}
