import { createApiError } from './http-client'

export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'

export interface AuthAwareClientConfig {
  baseUrl: string
  getAccessToken: () => string | null
  refreshSession: () => Promise<void>
  onUnauthorized: () => void
  timeout?: number
}

/**
 * Cliente HTTP que añade Authorization: Bearer y, ante 401, intenta refresh y reintenta una vez.
 * Si el refresh falla, llama a onUnauthorized (logout + redirigir a login).
 */
export function createAuthAwareHttpClient(config: AuthAwareClientConfig) {
  const { baseUrl, getAccessToken, refreshSession, onUnauthorized, timeout = 15_000 } = config

  async function request<T>(
    path: string,
    options: {
      method?: HttpMethod
      body?: unknown
      headers?: Record<string, string>
      signal?: AbortSignal
      skipAuth?: boolean
    } = {}
  ): Promise<T> {
    const { method = 'GET', body, headers = {}, signal, skipAuth = false } = options
    const url = path.startsWith('http') ? path : `${baseUrl.replace(/\/$/, '')}/${path.replace(/^\//, '')}`

    const controller = new AbortController()
    const timeoutId = setTimeout(() => controller.abort(), timeout)
    const effectiveSignal = signal ?? controller.signal

    const requestHeaders: Record<string, string> = { ...headers }
    const isFormData = body != null && body instanceof FormData
    if (body != null && !isFormData) requestHeaders['Content-Type'] = 'application/json'
    if (!skipAuth) {
      const token = getAccessToken()
      if (token) requestHeaders['Authorization'] = `Bearer ${token}`
    }

    const requestBody = body == null ? undefined : isFormData ? (body as FormData) : JSON.stringify(body)

    let response = await fetch(url, {
      method,
      headers: requestHeaders,
      body: requestBody,
      signal: effectiveSignal,
    })

    clearTimeout(timeoutId)

    if (response.status === 401 && !skipAuth) {
      try {
        await refreshSession()
        const newToken = getAccessToken()
        if (newToken) {
          const retryHeaders: Record<string, string> = { ...headers }
          if (body != null && !(body instanceof FormData)) retryHeaders['Content-Type'] = 'application/json'
          retryHeaders['Authorization'] = `Bearer ${newToken}`
          const retryController = new AbortController()
          const retryTimeoutId = setTimeout(() => retryController.abort(), timeout)
          response = await fetch(url, {
            method,
            headers: retryHeaders,
            body: requestBody,
            signal: retryController.signal,
          })
          clearTimeout(retryTimeoutId)
        }
      } catch {
        onUnauthorized()
        const apiError = await createApiError(response)
        throw apiError
      }
    }

    if (!response.ok) {
      if (response.status === 401 || response.status === 403) onUnauthorized()
      const apiError = await createApiError(response)
      throw apiError
    }

    const contentType = response.headers.get('content-type') ?? ''
    if (contentType.includes('application/json')) {
      return (await response.json()) as T
    }
    return undefined as T
  }

  return {
    get: <T>(path: string, options?: { headers?: Record<string, string>; signal?: AbortSignal }) =>
      request<T>(path, { ...options, method: 'GET' }),
    post: <T>(
      path: string,
      body?: unknown,
      options?: { headers?: Record<string, string>; signal?: AbortSignal }
    ) => request<T>(path, { ...options, method: 'POST', body }),
    put: <T>(
      path: string,
      body?: unknown,
      options?: { headers?: Record<string, string>; signal?: AbortSignal }
    ) => request<T>(path, { ...options, method: 'PUT', body }),
    patch: <T>(
      path: string,
      body?: unknown,
      options?: { headers?: Record<string, string>; signal?: AbortSignal }
    ) => request<T>(path, { ...options, method: 'PATCH', body }),
    delete: <T>(path: string, options?: { headers?: Record<string, string>; signal?: AbortSignal }) =>
      request<T>(path, { ...options, method: 'DELETE' }),
  }
}

export type AuthAwareHttpClient = ReturnType<typeof createAuthAwareHttpClient>
