import { create } from 'zustand'
import type { AuthUser, LoginResult } from '../types'
import { authApi } from '../api/auth-api'

const REFRESH_TOKEN_KEY = 'ioda_refresh_token'

export interface AuthState {
  user: AuthUser | null
  accessToken: string | null
  refreshToken: string | null
  expiresAt: number | null
  isAuthenticated: boolean
  /** true mientras se intenta rehidratar sesión al cargar la app. */
  isRehydrating: boolean
  setSession: (result: LoginResult) => void
  logout: () => void
  /** Obtiene nuevo access token con el refresh token; lanza si falla. */
  refreshSession: () => Promise<void>
  /** Restaura sesión desde localStorage (refresh token) al iniciar la app. */
  rehydrate: () => Promise<void>
}

function parseExpiresAt(expiresInSeconds: number): number {
  return Date.now() + expiresInSeconds * 1000
}

function resultToUser(result: LoginResult): AuthUser {
  return {
    userId: result.userId,
    email: result.email,
    displayName: result.displayName ?? null,
  }
}

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  accessToken: null,
  refreshToken: null,
  expiresAt: null,
  isAuthenticated: false,
  isRehydrating: true,

  setSession: (result: LoginResult) => {
    const refreshToken = result.refreshToken
    try {
      localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken)
    } catch {
      // localStorage no disponible (SSR, privado)
    }
    set({
      user: resultToUser(result),
      accessToken: result.accessToken,
      refreshToken,
      expiresAt: parseExpiresAt(result.expiresInSeconds),
      isAuthenticated: true,
    })
  },

  logout: () => {
    try {
      localStorage.removeItem(REFRESH_TOKEN_KEY)
    } catch {
      // ignore
    }
    set({
      user: null,
      accessToken: null,
      refreshToken: null,
      expiresAt: null,
      isAuthenticated: false,
    })
  },

  refreshSession: async () => {
    const { refreshToken } = get()
    const stored = (() => {
      try {
        return localStorage.getItem(REFRESH_TOKEN_KEY)
      } catch {
        return null
      }
    })()
    const token = refreshToken ?? stored
    if (!token) {
      throw new Error('No refresh token')
    }
    const result = await authApi.refresh({ refreshToken: token })
    get().setSession(result)
  },

  rehydrate: async () => {
    set({ isRehydrating: true })
    try {
      const stored = (() => {
        try {
          return localStorage.getItem(REFRESH_TOKEN_KEY)
        } catch {
          return null
        }
      })()
      if (!stored) {
        set({ isRehydrating: false })
        return
      }
      const result = await authApi.refresh({ refreshToken: stored })
      get().setSession(result)
    } catch {
      try {
        localStorage.removeItem(REFRESH_TOKEN_KEY)
      } catch {
        // ignore
      }
      set({
        user: null,
        accessToken: null,
        refreshToken: null,
        expiresAt: null,
        isAuthenticated: false,
      })
    } finally {
      set({ isRehydrating: false })
    }
  },
}))
