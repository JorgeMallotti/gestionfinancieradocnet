import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(),
    // i18n (@ngx-translate v18): fetches JSON files from assets/i18n/{lang}.json
    provideTranslateService({
      lang: 'en',
      fallbackLang: 'en',
      loader: provideTranslateHttpLoader({
        prefix: './assets/i18n/',
        suffix: '.json',
      }),
    }),
  ],
};
