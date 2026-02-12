import type { ApiError, ProblemDetails } from './types';

export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';

export interface RequestConfig {
  baseUrl: string;
  /** Si true, el cliente espera que el caller inyecte Authorization (ej. desde auth store). */
  requireAuth?: boolean;
  /** Timeout en ms. */
  timeout?: number;
}

/**
 * Parsea el cuerpo de error de una respuesta (ProblemDetails o JSON genérico).
 */
async function parseErrorBody(response: Response): Promise<ProblemDetails | Record<string, unknown> | undefined> {
  const contentType = response.headers.get('content-type') ?? '';
  if (!contentType.includes('application/json')) {
    return undefined;
  }
  try {
    return (await response.json()) as ProblemDetails | Record<string, unknown>;
  } catch {
    return undefined;
  }
}

/**
 * Crea un ApiError a partir de una Response.
 */
export async function createApiError(response: Response): Promise<ApiError> {
  const body = await parseErrorBody(response);
  const detail = body && typeof body === 'object' && 'detail' in body ? String((body as ProblemDetails).detail) : response.statusText;
  return {
    status: response.status,
    statusText: response.statusText,
    body,
    message: detail || response.statusText,
  };
}

/**
 * Cliente HTTP base: base URL configurable, manejo de 401/403 y errores en formato ProblemDetails.
 * No inyecta JWT aquí (eso se hará en Fase 1 con el auth store); solo prepara la estructura.
 */
export function createHttpClient(config: RequestConfig) {
  const { baseUrl, timeout = 15_000 } = config;

  async function request<T>(
    path: string,
    options: {
      method?: HttpMethod;
      body?: unknown;
      headers?: Record<string, string>;
      signal?: AbortSignal;
    } = {}
  ): Promise<T> {
    const { method = 'GET', body, headers = {}, signal } = options;
    const url = path.startsWith('http') ? path : `${baseUrl.replace(/\/$/, '')}/${path.replace(/^\//, '')}`;

    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), timeout);
    const effectiveSignal = signal ?? controller.signal;

    const requestHeaders: Record<string, string> = { ...headers };
    if (body != null) {
      requestHeaders['Content-Type'] = 'application/json';
    }

    const response = await fetch(url, {
      method,
      headers: requestHeaders,
      body: body != null ? JSON.stringify(body) : undefined,
      signal: effectiveSignal,
    });

    clearTimeout(timeoutId);

    if (!response.ok) {
      const apiError = await createApiError(response);
      throw apiError;
    }

    const contentType = response.headers.get('content-type') ?? '';
    if (contentType.includes('application/json')) {
      return (await response.json()) as T;
    }
    return undefined as T;
  }

  return {
    get: <T>(path: string, options?: { headers?: Record<string, string>; signal?: AbortSignal }) =>
      request<T>(path, { ...options, method: 'GET' }),
    post: <T>(path: string, body?: unknown, options?: { headers?: Record<string, string>; signal?: AbortSignal }) =>
      request<T>(path, { ...options, method: 'POST', body }),
    put: <T>(path: string, body?: unknown, options?: { headers?: Record<string, string>; signal?: AbortSignal }) =>
      request<T>(path, { ...options, method: 'PUT', body }),
    patch: <T>(path: string, body?: unknown, options?: { headers?: Record<string, string>; signal?: AbortSignal }) =>
      request<T>(path, { ...options, method: 'PATCH', body }),
    delete: <T>(path: string, options?: { headers?: Record<string, string>; signal?: AbortSignal }) =>
      request<T>(path, { ...options, method: 'DELETE' }),
  };
}

export type HttpClient = ReturnType<typeof createHttpClient>;
