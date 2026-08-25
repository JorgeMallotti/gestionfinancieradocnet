import { HttpErrorResponse } from '@angular/common/http';

/**
 * Extracts a readable error message from an HttpErrorResponse.
 * The backend returns ProblemDetails { detail } for expected failures
 * (RFC 7807) — we prefer that, then the generic fallback.
 */
export function extractError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as { detail?: string; title?: string } | undefined;
    if (body?.detail) return body.detail;
    if (body?.title) return body.title;
    return `HTTP ${error.status}`;
  }
  return 'Unexpected error';
}

/**
 * Maps a backend auth error detail to an i18n key (or returns the raw
 * detail when there is no match). Pure function — safe to use in any
 * component without touching the DI graph.
 */
export function authErrorKey(detail: string | undefined): string {
  if (!detail) return 'auth.errorGeneric';

  const lower = detail.toLowerCase();
  if (lower.includes('email')) return 'auth.errorEmailExists';
  if (lower.includes('password') || lower.includes('locked')) return 'auth.errorInvalidCredentials';
  if (lower.includes('smtp')) return 'reports.smtpNotConfigured';
  return detail;
}
