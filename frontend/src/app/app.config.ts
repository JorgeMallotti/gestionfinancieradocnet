import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideNativeDateAdapter } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { credentialsInterceptor } from './core/interceptors/credentials.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    // HTTP interceptors — ORDER MATTERS: they run top-down, so `credentialsInterceptor`
    // sits outermost and still sees the retry/refresh request that `authInterceptor`
    // issues internally after a 401 (that inner call is what needs the refresh cookie).
    provideHttpClient(withInterceptors([credentialsInterceptor, authInterceptor])),
    provideAnimationsAsync(),
    // Native DateAdapter for the Material date pickers.
    provideNativeDateAdapter(),
    // i18n (@ngx-translate v18): fetches JSON files from public/i18n/{lang}.json
    provideTranslateService({
      lang: 'en',
      fallbackLang: 'en',
      loader: provideTranslateHttpLoader({
        prefix: '/i18n/',
        suffix: '.json',
      }),
    }),
  ],
};
