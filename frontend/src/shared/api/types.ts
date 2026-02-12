/**
 * Tipos para respuestas de error (ProblemDetails, etc.) y cliente HTTP.
 */

/** Formato ProblemDetails (ASP.NET Core). */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  extensions?: Record<string, unknown>;
}

/** Error de API con status y cuerpo parseado. */
export interface ApiError {
  status: number;
  statusText: string;
  body?: ProblemDetails | Record<string, unknown>;
  message: string;
}
