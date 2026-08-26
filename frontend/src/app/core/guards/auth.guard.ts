import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../services/auth.service';

/**
 * Route guard: requires an authenticated session. Restores the session from
 * the httpOnly refresh cookie when the page is loaded fresh (token in memory
 * only survives until reload).
 */
export const authGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const ok = await auth.restoreSession();
  if (ok) return true;

  return router.createUrlTree(['/auth/login']);
};

/** Route guard: Admin role only (audit log, user management). */
export const adminGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const ok = await auth.restoreSession();
  if (!ok) return router.createUrlTree(['/auth/login']);
  if (!auth.isAdmin()) return router.createUrlTree(['/dashboard']);

  return true;
};
