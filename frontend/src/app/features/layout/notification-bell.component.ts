import { DatePipe, formatNumber } from '@angular/common';
import { Component, computed, ElementRef, HostListener, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { MatBadgeModule } from '@angular/material/badge';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { filter, interval } from 'rxjs';

import { AppNotification, NotificationType } from '../../core/models';
import { NotificationsService } from '../../core/services/notifications.service';

/** How often the bell re-checks the unread count (badge). */
const POLL_INTERVAL_MS = 30_000;

/** The demo bank runs on a single currency; amounts carry no per-row currency. */
const BANK_CURRENCY = 'EUR';

/**
 * Notification bell in the toolbar: unread badge (+1/+9 → 99+), a dropdown
 * panel sized in relative units, click-to-navigate by type and click-outside
 * (or Escape) to close. The list is refreshed when opened, the badge every
 * 30 s and after every navigation.
 */
@Component({
  selector: 'app-notification-bell',
  imports: [
    MatBadgeModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    DatePipe,
    TranslatePipe,
  ],
  templateUrl: './notification-bell.component.html',
  styleUrl: './notification-bell.component.scss',
})
export class NotificationBellComponent {
  private readonly api = inject(NotificationsService);
  private readonly translate = inject(TranslateService);
  private readonly router = inject(Router);
  private readonly host = inject(ElementRef<HTMLElement>).nativeElement;

  /** All notifications shown in the dropdown (newest first). */
  protected readonly items = signal<AppNotification[]>([]);
  /** Unread count for the badge (server-truth, refreshed periodically). */
  protected readonly unread = signal(0);
  /** Dropdown visibility (click-outside / Escape closes it). */
  protected readonly open = signal(false);

  /** Bumped whenever the language changes so translated rows rebuild. */
  private readonly langTick = signal(0);

  protected readonly rows = computed(() => {
    // Rebuilds when the items or the language change.
    this.langTick();
    return this.items().map((n) => this.toRow(n));
  });

  /** Badge content: "1".."99", "99+" above that, empty (hidden) when none. */
  protected readonly badgeLabel = computed(() => {
    const count = this.unread();
    if (count === 0) return '';
    return count > 99 ? '99+' : String(count);
  });

  protected readonly hasUnread = computed(() => this.unread() > 0);

  constructor() {
    // Keep the badge fresh: after any navigation and on a light polling loop.
    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => void this.refreshUnread());

    interval(POLL_INTERVAL_MS)
      .pipe(takeUntilDestroyed())
      .subscribe(() => void this.refreshUnread());

    // Rebuild row texts when the user switches language.
    this.translate.onLangChange
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.langTick.update((t) => t + 1));

    void this.refreshUnread();
  }

  @HostListener('document:pointerdown', ['$event'])
  onDocumentPointerDown(event: PointerEvent): void {
    if (this.open() && !this.host.contains(event.target as Node)) {
      this.open.set(false);
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.open.set(false);
  }

  protected async togglePanel(): Promise<void> {
    if (this.open()) {
      this.open.set(false);
      return;
    }
    // Refresh the list + badge right before showing the panel.
    try {
      const [items, count] = await Promise.all([this.api.getMine(), this.api.getUnreadCount()]);
      this.items.set(items);
      this.unread.set(count);
    } catch {
      // Transient network/401 — keep whatever we already have.
    }
    this.open.set(true);
  }

  /** Marks the row read, closes and navigates to its page. */
  protected async select(row: AppNotification): Promise<void> {
    if (!row.isRead) {
      // Optimistic update first, then let the server confirm BEFORE we
      // navigate — the navigation-triggered badge refresh must see the
      // final count (otherwise the badge flickers back for ~30 s).
      this.items.update((list) => list.map((n) => (n.id === row.id ? { ...n, isRead: true } : n)));
      this.unread.update((c) => Math.max(0, c - 1));
      try {
        await this.api.markRead(row.id);
      } catch {
        // Roll back on failure so the next poll fixes the truth.
        this.items.update((list) =>
          list.map((n) => (n.id === row.id ? { ...n, isRead: false } : n)),
        );
        this.unread.update((c) => c + 1);
      }
    }
    this.open.set(false);
    await this.router.navigateByUrl(this.routeFor(row.type));
  }

  /** Marks every notification as read (optimistic), no server refetch. */
  protected async markAllRead(): Promise<void> {
    if (!this.hasUnread()) return;
    this.items.update((list) => list.map((n) => ({ ...n, isRead: true })));
    this.unread.set(0);
    try {
      await this.api.markAllRead();
    } catch {
      // Roll back so the next poll fixes the truth.
      this.unread.set(this.items().filter((n) => !n.isRead).length);
    }
  }

  /** Maps a notification type to the page that shows its subject. */
  protected routeFor(type: NotificationType): string {
    switch (type) {
      case 'LoanApproved':
      case 'LoanRejected':
      case 'LoanRepaid':
      case 'LoanRequested':
        return '/loans';
      case 'ClaimProposed':
      case 'ClaimCounterpartyConsented':
      case 'ClaimResolved':
        return '/claims';
      case 'ClientApproved':
      case 'ClientSuspended':
      case 'TransferReceived':
      default:
        return '/movements';
    }
  }

  private async refreshUnread(): Promise<void> {
    try {
      this.unread.set(await this.api.getUnreadCount());
    } catch {
      // Ignore transient failures — the next poll retries.
    }
  }

  private toRow(n: AppNotification): AppNotification & { message: string; icon: string } {
    const params = {
      actor: n.actorName ?? '',
      amount: n.amount == null ? '' : this.amountText(n.amount),
    };
    return {
      ...n,
      message: this.translate.instant(`notifications.types.${n.type}`, params),
      icon: this.iconFor(n.type),
    };
  }

  /** "1,234.56 EUR" — same shape as `| number:'1.2-2'` + currency elsewhere. */
  private amountText(amount: number): string {
    return `${formatNumber(amount, 'en-US', '1.2-2')} ${BANK_CURRENCY}`;
  }

  private iconFor(type: NotificationType): string {
    switch (type) {
      case 'LoanApproved':
      case 'LoanRejected':
      case 'LoanRepaid':
      case 'LoanRequested':
        return 'account_balance';
      case 'ClaimProposed':
      case 'ClaimCounterpartyConsented':
      case 'ClaimResolved':
        return 'report';
      case 'ClientApproved':
        return 'how_to_reg';
      case 'ClientSuspended':
        return 'block';
      case 'TransferReceived':
      default:
        return 'payments';
    }
  }
}
