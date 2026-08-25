import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, from, switchMap, throwError } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';

/**
 * Attaches the Bearer token to every API call and transparently handles
 * access-token expiry: on 401 it refreshes once and retries the request.
 * If the refresh fails (expired refresh cookie) it redirects to login.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  // Never intercept auth endpoints (they manage their own tokens/cookie).
  if (req.url.startsWith(`${environment.apiBaseUrl}/auth/`)) {
    return next(req);
  }

  const token = auth.getAccessToken();
  const authorized = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authorized).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || req.url.includes('/auth/')) {
        return throwError(() => error);
      }

      // Refresh once and retry the original request.
      return from(auth.refresh()).pipe(
        switchMap(() => {
          const retried = req.clone({
            setHeaders: { Authorization: `Bearer ${auth.getAccessToken() ?? ''}` },
          });
          return next(retried);
        }),
        catchError((refreshError) => {
          void auth.logout().finally(() => router.navigate(['/auth/login']));
          return throwError(() => refreshError);
        }),
      );
    }),
    // Avoid duplicate refresh storms when several requests 401 at once.
    // (The AuthService deduplicates refreshes internally.)
  );
};
