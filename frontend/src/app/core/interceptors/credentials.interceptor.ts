import { HttpInterceptorFn } from '@angular/common/http';

import { environment } from '../../../environments/environment';

/**
 * Sends the httpOnly refresh-token cookie along with every API call.
 *
 * Why this is needed: after login the backend sets the refresh token as an httpOnly +
 * Secure + SameSite=Strict cookie. Browsers never attach cookies to cross-origin XHR
 * unless `withCredentials` is enabled — so without this interceptor `POST /api/auth/refresh`
 * arrives with no cookie and the session cannot be restored (401).
 *
 * Why it was invisible during development: `ng serve` proxies `/api` to the API
 * (see `proxy.conf.json`), so requests looked same-origin and cookies travelled anyway.
 * Production talks to a different origin (api.mallottidigital.com) and the failure appears.
 *
 * It must be the OUTERMOST interceptor: `authInterceptor` retries a 401 by calling
 * `refresh()` from inside its own `catchError`, so this interceptor has to wrap that
 * inner request too (interceptors run outermost-first, and the retry happens downstream).
 *
 * Note: sending credentials requires the server to answer with an explicit
 * `Access-Control-Allow-Origin` (never `*`) plus `Access-Control-Allow-Credentials: true`,
 * which the API already configures (`Program.cs` → CORS policy `.AllowCredentials()`).
 */
export const credentialsInterceptor: HttpInterceptorFn = (req, next) => {
  // Only API calls need the cookie; static assets (i18n JSON, fonts) never do.
  if (!req.url.startsWith(environment.apiBaseUrl)) {
    return next(req);
  }

  return next(req.clone({ withCredentials: true }));
};
