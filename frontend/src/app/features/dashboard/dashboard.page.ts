import { Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { TranslatePipe } from '@ngx-translate/core';
import { ChartConfiguration } from 'chart.js';
import { toSignal } from '@angular/core/rxjs-interop';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { map } from 'rxjs';
import { DecimalPipe } from '@angular/common';

import { ChartComponent } from '../../shared/components/chart.component';
import { DashboardService } from '../../core/services/dashboard.service';
import {
  CategoryBreakdown,
  DashboardSummary,
  MonthlyPoint,
  TransactionType,
} from '../../core/models';
import { ToastService } from '../../shared/services/toast.service';

/**
 * Dashboard: KPI cards (income/expenses/balance/count) + two charts:
 * monthly income-vs-expenses bars and per-category doughnut (type toggle).
 * All data comes from the aggregation endpoints (no client-side math).
 */
@Component({
  selector: 'app-dashboard-page',
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatButtonToggleModule,
    ChartComponent,
    TranslatePipe,
    DecimalPipe,
  ],
  templateUrl: './dashboard.page.html',
  styleUrl: './dashboard.page.scss',
})
export class DashboardPage {
  private readonly dashboard = inject(DashboardService);
  private readonly toast = inject(ToastService);
  private readonly breakpoints = inject(BreakpointObserver);

  protected readonly loading = signal(true);
  protected readonly summary = signal<DashboardSummary | null>(null);
  protected readonly breakdown = signal<CategoryBreakdown[]>([]);
  protected readonly monthly = signal<MonthlyPoint[]>([]);
  protected readonly breakdownType = signal<TransactionType>('Expense');

  private readonly isHandset = toSignal(
    this.breakpoints.observe([Breakpoints.Handset]).pipe(map((x) => x.matches)),
    { initialValue: false },
  );

  protected readonly isHandsetLayout = computed(() => this.isHandset());

  constructor() {
    void this.load();
  }

  protected readonly balanceColor = computed(() => {
    const balance = this.summary()?.balance ?? 0;
    return balance >= 0 ? 'var(--mat-sys-primary)' : 'var(--mat-sys-error)';
  });

  protected readonly monthlyChart = computed<ChartConfiguration | null>(() => {
    const points = this.monthly();
    if (points.length === 0) return null;

    return {
      type: 'bar',
      data: {
        labels: points.map((p) => `${p.year}-${String(p.month).padStart(2, '0')}`),
        datasets: [
          {
            label: 'Income',
            data: points.map((p) => p.income),
            backgroundColor: '#4caf50',
            borderRadius: 4,
          },
          {
            label: 'Expenses',
            data: points.map((p) => p.expenses),
            backgroundColor: '#f44336',
            borderRadius: 4,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { position: 'bottom' } },
      },
    };
  });

  protected readonly breakdownChart = computed<ChartConfiguration | null>(() => {
    const rows = this.breakdown();
    if (rows.length === 0) return null;

    return {
      type: 'doughnut',
      data: {
        labels: rows.map((r) => r.categoryName),
        datasets: [
          {
            data: rows.map((r) => r.amount),
            backgroundColor: palette(rows.length),
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { position: 'right' } },
      },
    };
  });

  protected async switchBreakdownType(type: TransactionType): Promise<void> {
    // mat-button-toggle-group can emit an invalid/null value on init — guard it.
    if (type !== 'Income' && type !== 'Expense') return;
    if (type === this.breakdownType()) return;

    this.breakdownType.set(type);
    await this.loadBreakdown();
  }

  protected async reload(): Promise<void> {
    await this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    try {
      await Promise.all([this.loadSummary(), this.loadBreakdown(), this.loadMonthly()]);
    } catch (error) {
      this.toast.error(error instanceof Error ? error.message : 'Error');
    } finally {
      this.loading.set(false);
    }
  }

  private async loadSummary(): Promise<void> {
    this.summary.set(await this.dashboard.getSummary());
  }

  private async loadBreakdown(): Promise<void> {
    this.breakdown.set(await this.dashboard.getBreakdown(this.breakdownType()));
  }

  private async loadMonthly(): Promise<void> {
    this.monthly.set(await this.dashboard.getMonthly());
  }
}

/** Generates a stable categorical palette for the doughnut chart. */
function palette(count: number): string[] {
  const colors = [
    '#1e88e5',
    '#43a047',
    '#fb8c00',
    '#e53935',
    '#8e24aa',
    '#00acc1',
    '#d81b60',
    '#3949ab',
    '#fdd835',
    '#6d4c41',
  ];
  return Array.from({ length: count }, (_, i) => colors[i % colors.length]);
}
