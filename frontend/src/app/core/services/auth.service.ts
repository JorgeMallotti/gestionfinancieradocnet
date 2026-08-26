import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthResponse, LoginDto, RegisterDto, UserRole } from '../models';

/**
 * Authentication state (Signals) + API calls.
 *
 * Security model (AGENTS.md §13):
 * - The access token lives ONLY in memory (never localStorage — XSS-safe).
 * - The refresh token lives in an httpOnly cookie set by the backend.
 * - On app start we call /api/auth/refresh to restore the session from the
 *   cookie (the browser sends it automatically).
 *
 * NOTE: this service deliberately does NOT depend on TranslateService —
 * that would create a circular dependency (TranslateService loads via HTTP
 * → the auth interceptor → AuthService). Error keys are mapped in the UI.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private accessToken: string | null = null;
  private readonly refreshPromise = new Map<string, Promise<AuthResponse>>();

  readonly user = signal<AuthResponse | null>(null);
  readonly isAuthenticated = computed(() => this.user() !== null);
  readonly isAdmin = computed(() => this.user()?.role === 'Admin');
  readonly canMutate = computed(() => {
    const role = this.user()?.role as UserRole | undefined;
    return role === 'Admin' || role === 'Finance';
  });

  /** Returns the in-memory access token for the HTTP interceptor. */
  getAccessToken(): string | null {
    return this.accessToken;
  }

  async login(credentials: LoginDto): Promise<AuthResponse> {
    const auth = await firstValueFrom(
      this.http.post<AuthResponse>(`${environment.apiBaseUrl}/auth/login`, credentials),
    );
    this.setSession(auth);
    return auth;
  }

  async register(payload: RegisterDto): Promise<AuthResponse> {
    const auth = await firstValueFrom(
      this.http.post<AuthResponse>(`${environment.apiBaseUrl}/auth/register`, payload),
    );
    this.setSession(auth);
    return auth;
  }

  async logout(): Promise<void> {
    try {
      await firstValueFrom(this.http.post<void>(`${environment.apiBaseUrl}/auth/logout`, null));
    } finally {
      this.clearSession();
      this.router.navigate(['/auth/login']);
    }
  }

  /**
   * Restores the session from the httpOnly cookie (POST /auth/refresh).
   * Safe to call on app start — if there is no cookie it fails fast.
   */
  async restoreSession(): Promise<boolean> {
    if (this.accessToken) return true;
    try {
      const auth = await this.refresh();
      this.setSession(auth);
      return true;
    } catch {
      return false;
    }
  }

  /**
   * Refreshes the access token. Deduplicates concurrent calls (a burst of
   * 401s triggers one refresh, all callers await the same promise).
   */
  async refresh(): Promise<AuthResponse> {
    const key = 'refresh';
    const pending = this.refreshPromise.get(key);
    if (pending) return pending;

    const attempt = firstValueFrom(
      this.http.post<AuthResponse>(`${environment.apiBaseUrl}/auth/refresh`, null),
    );

    this.refreshPromise.set(key, attempt);

    try {
      const auth = await attempt;
      this.setSession(auth);
      return auth;
    } finally {
      this.refreshPromise.delete(key);
    }
  }

  private setSession(auth: AuthResponse): void {
    this.accessToken = auth.accessToken;
    this.user.set(auth);
  }

  private clearSession(): void {
    this.accessToken = null;
    this.user.set(null);
  }
}
