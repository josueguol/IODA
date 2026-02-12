import { config } from '../../../config/env'
import { createHttpClient } from '../../../shared/api'
import type { LoginRequest, LoginResult, RegisterRequest, RegisterResult, RefreshTokenRequest, SetupStatus } from '../types'

const identityClient = createHttpClient({
  baseUrl: config.identityApiUrl,
  timeout: 15_000,
})

/** Cliente de la Identity API (sin JWT; login/register/refresh son públicos). */
export const authApi = {
  login: (body: LoginRequest) => identityClient.post<LoginResult>('api/auth/login', body),

  register: (body: RegisterRequest) =>
    identityClient.post<RegisterResult>('api/auth/register', body),

  refresh: (body: RefreshTokenRequest) =>
    identityClient.post<LoginResult>('api/auth/refresh', body),

  /** Estado de configuración del sistema (público). */
  getSetupStatus: () => identityClient.get<SetupStatus>('api/auth/setup-status'),
}
