import { Routes } from '@angular/router';

import { adminGuard, authGuard } from './core/guards/auth.guard';

/**
 * Application routes. Every feature is lazy-loaded (loadComponent) and the
 * protected shell (LayoutComponent) is behind the auth guard.
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: '/dashboard' },

  // ── Public auth pages ─────────────────────────────────────────────────
  {
    path: 'auth/login',
    loadComponent: () => import('./features/auth/login.page').then((m) => m.LoginPage),
  },
  {
    path: 'auth/register',
    loadComponent: () => import('./features/auth/register.page').then((m) => m.RegisterPage),
  },

  // ── Protected shell (sidenav + toolbar) ───────────────────────────────
  {
    path: '',
    loadComponent: () =>
      import('./features/layout/layout.component').then((m) => m.LayoutComponent),
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.page').then((m) => m.DashboardPage),
      },
      {
        path: 'transactions',
        loadComponent: () =>
          import('./features/transactions/transactions.page').then((m) => m.TransactionsPage),
      },
      {
        path: 'categories',
        loadComponent: () =>
          import('./features/categories/categories.page').then((m) => m.CategoriesPage),
      },
      {
        path: 'reports',
        loadComponent: () => import('./features/reports/reports.page').then((m) => m.ReportsPage),
      },
      {
        path: 'audit',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/audit/audit.page').then((m) => m.AuditPage),
      },
    ],
  },

  { path: '**', redirectTo: '/dashboard' },
];
