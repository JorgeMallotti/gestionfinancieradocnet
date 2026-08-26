import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'theme';

export type ThemeMode = 'light' | 'dark';

/**
 * Theme mode (light/dark) applied as a class on <html> and persisted in
 * localStorage. The anti-FOUC inline script in index.html restores the class
 * before Angular boots.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly theme = signal<ThemeMode>(this.resolveInitial());

  readonly current = this.theme.asReadonly();

  constructor() {
    this.apply(this.theme());
  }

  toggle(): void {
    const next: ThemeMode = this.theme() === 'light' ? 'dark' : 'light';
    this.theme.set(next);
    this.apply(next);
    localStorage.setItem(STORAGE_KEY, next);
  }

  private resolveInitial(): ThemeMode {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored === 'light' || stored === 'dark') return stored;
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  private apply(mode: ThemeMode): void {
    document.documentElement.classList.toggle('dark', mode === 'dark');
    document.documentElement.style.colorScheme = mode;
  }
}
