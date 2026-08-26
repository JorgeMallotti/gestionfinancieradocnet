import { Injectable, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export type AppLanguage = 'en' | 'es' | 'pt';

const STORAGE_KEY = 'lang';
const DEFAULT_LANG: AppLanguage = 'en';

/**
 * Language management with persistence. Default: stored preference →
 * browser language (en/es/pt) → en. All user-facing text goes through
 * @ngx-translate (AGENTS.md §11).
 */
@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);

  init(): void {
    // fallbackLang is configured in the provider (app.config.ts).
    this.translate.use(this.resolveInitial());
  }

  get current(): AppLanguage {
    const lang = this.translate.currentLang();
    return lang === 'en' || lang === 'es' || lang === 'pt' ? lang : DEFAULT_LANG;
  }

  setLanguage(lang: AppLanguage): void {
    localStorage.setItem(STORAGE_KEY, lang);
    this.translate.use(lang);
  }

  private resolveInitial(): AppLanguage {
    const stored = localStorage.getItem(STORAGE_KEY) as AppLanguage | null;
    if (stored && ['en', 'es', 'pt'].includes(stored)) return stored;

    const browser = this.translate.getBrowserLang() as AppLanguage | undefined;
    if (browser && ['en', 'es', 'pt'].includes(browser)) return browser;

    return DEFAULT_LANG;
  }
}
