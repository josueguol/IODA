/**
 * Tipos alineados a la Identity API (login, register, refresh).
 * La API devuelve camelCase por defecto (ASP.NET Core).
 */

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  email: string
  password: string
  displayName?: string | null
}

export interface RefreshTokenRequest {
  refreshToken: string
}

/** Respuesta de login y refresh (LoginResultDto). */
export interface LoginResult {
  accessToken: string
  refreshToken: string
  expiresInSeconds: number
  userId: string
  email: string
  displayName: string | null
}

export interface AuthUser {
  userId: string
  email: string
  displayName: string | null
}

/** Respuesta de registro. */
export interface RegisterResult {
  userId: string
  isFirstUser: boolean
}

/** Estado de configuración del sistema. */
export interface SetupStatus {
  hasUsers: boolean
  selfRegistrationEnabled: boolean
}

/** Usuario en lista de administración (Identity API GET /api/auth/users). */
export interface UserListItemDto {
  id: string
  email: string
  displayName: string | null
  isActive: boolean
  createdAt: string
}
