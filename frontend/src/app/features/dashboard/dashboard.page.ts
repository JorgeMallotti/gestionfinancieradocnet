import { Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ChartConfiguration } from 'chart.js';
import { toSignal } from '@angular/core/rxjs-interop';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { map } from 'rxjs';
import { DecimalPipe } from '@angular/common';

import { ChartComponent } from '../../shared/components/chart.component';
import { DashboardService } from '../../core/services/dashboard.service';
import { AccountsService } from '../../core/services/accounts.service';
import { ThemeService } from '../../core/services/theme.service';
import { DashboardSummary, MonthlyPoint } from '../../core/models';
import { ToastService } from '../../shared/services/toast.service';
import { extractError } from '../../shared/utils/errors';

/**
 * Dashboard for the CALLER'S OWN account (clients see theirs; the Admin sees
 * the bank treasury): balance + incoming/outgoing totals + movement count and
 * a monthly incoming-vs-outgoing chart. All data comes from the API.
 */
@Component({
  selector: 'app-dashboard-page',
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressBarModule,
    ChartComponent,
    TranslatePipe,
    DecimalPipe,
  ],
  templateUrl: './dashboard.page.html',
  styleUrl: './dashboard.page.scss',
})
export class DashboardPage {
  private readonly dashboard = inject(DashboardService);
  private readonly accounts = inject(AccountsService);
  private readonly toast = inject(ToastService);
  private readonly breakpoints = inject(BreakpointObserver);
  private readonly themeService = inject(ThemeService);
  private readonly translate = inject(TranslateService);

  protected readonly loading = signal(true);
  protected readonly summary = signal<DashboardSummary | null>(null);
  protected readonly monthly = signal<MonthlyPoint[]>([]);
  protected readonly accountName = signal('');
  protected readonly isTreasury = signal(false);

  private readonly isHandset = toSignal(
    this.breakpoints.observe([Breakpoints.Handset]).pipe(map((x) => x.matches)),
    { initialValue: false },
  );

  protected readonly isHandsetLayout = computed(() => this.isHandset());
  protected readonly isDark = computed(() => this.themeService.current() === 'dark');

  constructor() {
    void this.load();
  }

  protected readonly balanceColor = computed(() => {
    const balance = this.summary()?.balance ?? 0;
    return balance >= 0 ? 'var(--mat-sys-primary)' : 'var(--mat-sys-error)';
  });

  /** Readable Chart.js text/grid colors for the current theme (WCAG). */
  private readonly chartPalette = computed(() =>
    this.isDark()
      ? { text: '#e0e0e0', grid: 'rgba(255, 255, 255, 0.08)' }
      : { text: '#424242', grid: 'rgba(0, 0, 0, 0.08)' },
  );

  protected readonly monthlyChart = computed<ChartConfiguration | null>(() => {
    const points = this.monthly();
    if (points.length === 0) return null;

    const colors = this.chartPalette();

    return {
      type: 'bar',
      data: {
        labels: points.map((p) => `${p.year}-${String(p.month).padStart(2, '0')}`),
        datasets: [
          {
            label: this.translate.instant('dashboard.incoming'),
            data: points.map((p) => p.incoming),
            backgroundColor: '#4caf50',
            borderRadius: 4,
          },
          {
            label: this.translate.instant('dashboard.outgoing'),
            data: points.map((p) => p.outgoing),
            backgroundColor: '#f44336',
            borderRadius: 4,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { position: 'bottom', labels: { color: colors.text } },
        },
        scales: {
          x: { ticks: { color: colors.text }, grid: { color: colors.grid } },
          y: { ticks: { color: colors.text }, grid: { color: colors.grid } },
        },
      },
    };
  });

  protected async reload(): Promise<void> {
    await this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      const [account, summary, monthly] = await Promise.all([
        this.accounts.getMyAccount(),
        this.dashboard.getSummary(),
        this.dashboard.getMonthly(),
      ]);
      this.accountName.set(account.displayName);
      this.isTreasury.set(account.isTreasury);
      this.summary.set(summary);
      this.monthly.set(monthly);
    } catch (error) {
      this.toast.error(extractError(error));
    } finally {
      this.loading.set(false);
    }
  }
}
