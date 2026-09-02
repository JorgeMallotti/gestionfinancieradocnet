import { Component, computed, inject } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatSelectModule } from '@angular/material/select';
import { MatSidenavModule, MatSidenav } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

import { AuthService } from '../../core/services/auth.service';
import { AppLanguage, LanguageService } from '../../core/services/language.service';
import { ThemeService } from '../../core/services/theme.service';
import { environment } from '../../../environments/environment';
import { NotificationBellComponent } from './notification-bell.component';

interface NavItem {
  route: string;
  labelKey: string;
  icon: string;
  adminOnly?: boolean;
}

/**
 * Protected application shell: toolbar + responsive sidenav + router outlet.
 * Mobile-first: the sidenav overlays on small screens and docks on wide ones
 * (BreakpointObserver from Material CDK).
 */
@Component({
  selector: 'app-layout',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatSidenavModule,
    MatToolbarModule,
    MatListModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    MatMenuModule,
    MatSelectModule,
    MatTooltipModule,
    TranslatePipe,
    NotificationBellComponent,
  ],
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.scss',
})
export class LayoutComponent {
  private readonly breakpoints = inject(BreakpointObserver);
  private readonly themeService = inject(ThemeService);
  private readonly languageService = inject(LanguageService);
  protected readonly auth = inject(AuthService);

  protected readonly navItems: NavItem[] = [
    { route: '/dashboard', labelKey: 'nav.dashboard', icon: 'dashboard' },
    { route: '/movements', labelKey: 'nav.movements', icon: 'receipt_long' },
    { route: '/loans', labelKey: 'nav.loans', icon: 'account_balance' },
    { route: '/claims', labelKey: 'nav.claims', icon: 'report_problem' },
    { route: '/categories', labelKey: 'nav.categories', icon: 'category' },
    { route: '/reports', labelKey: 'nav.reports', icon: 'description' },
    { route: '/admin/clients', labelKey: 'nav.adminClients', icon: 'groups', adminOnly: true },
    { route: '/audit', labelKey: 'nav.audit', icon: 'history', adminOnly: true },
  ];

  /** Docks the sidenav on ≥ tablet; overlays on handset. */
  private readonly isHandset = toSignal(
    this.breakpoints.observe([Breakpoints.Handset]).pipe(map((x) => x.matches)),
    { initialValue: false },
  );

  protected readonly mode = computed(() => (this.isHandset() ? 'over' : 'side'));
  protected readonly isDark = computed(() => this.themeService.current() === 'dark');

  protected readonly languages: { code: AppLanguage; label: string }[] = [
    { code: 'en', label: 'English' },
    { code: 'es', label: 'Español' },
    { code: 'pt', label: 'Português' },
  ];

  protected readonly currentLang = computed(() => this.languageService.current);

  /** Jorge's landing page URL (environment-specific, no secrets). */
  protected readonly landingUrl = environment.landingUrl;

  protected visibleItems = computed(() =>
    this.navItems.filter((item) => !item.adminOnly || this.auth.isAdmin()),
  );

  protected toggleTheme(): void {
    this.themeService.toggle();
  }

  protected setLanguage(lang: AppLanguage): void {
    this.languageService.setLanguage(lang);
  }

  /**
   * Opens Jorge's landing page in a NEW tab. The anchor keeps its href for
   * middle-click / accessibility, but a plain left click is prevented and
   * handled here with window.open — otherwise the native target=_blank
   * navigation AND window.open would open TWO tabs. Closes the overlay
   * sidenav on handset after opening.
   */
  protected openLandingAndClose(event: Event, sidenav: MatSidenav): void {
    event.preventDefault();
    window.open(this.landingUrl, '_blank', 'noopener,noreferrer');
    if (this.mode() === 'over') {
      void sidenav.close();
    }
  }

  protected logout(): void {
    void this.auth.logout();
  }
}
