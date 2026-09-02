import { Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { DecimalPipe, DatePipe } from '@angular/common';

import { AccountsService } from '../../core/services/accounts.service';
import { ToastService } from '../../shared/services/toast.service';
import { Account, AccountStatus } from '../../core/models';
import { extractError } from '../../shared/utils/errors';
import { ConfirmDialog, ConfirmDialogData } from '../../shared/components/confirm-dialog.component';

/**
 * Bank Admin panel: manage client accounts. Pending registrations are approved
 * here; active clients can be suspended (no operations until re-approved).
 * The bank treasury is never listed as a client.
 */
@Component({
  selector: 'app-admin-clients-page',
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatFormFieldModule,
    MatSelectModule,
    MatProgressBarModule,
    MatDialogModule,
    TranslatePipe,
    DecimalPipe,
    DatePipe,
  ],
  templateUrl: './admin-clients.page.html',
  styleUrl: './admin-clients.page.scss',
})
export class AdminClientsPage {
  private readonly accounts = inject(AccountsService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);
  private readonly dialog = inject(MatDialog);

  protected readonly loading = signal(true);
  protected readonly items = signal<Account[]>([]);
  protected readonly statusFilter = signal<AccountStatus | null>(null);
  protected readonly busy = signal<string | null>(null);

  protected readonly pendingCount = computed(
    () => this.items().filter((c) => c.status === 'Pending').length,
  );

  constructor() {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    try {
      const items = await this.accounts.listClients(this.statusFilter() ?? undefined);
      this.items.set(items);
    } catch (error) {
      this.toast.error(extractError(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected async onFilter(status: AccountStatus | null): Promise<void> {
    this.statusFilter.set(status);
    await this.load();
  }

  protected statusLabelKey(status: AccountStatus): string {
    return `admin.clients.status.${status}`;
  }

  protected kindLabelKey(kind: string): string {
    return kind === 'Company' ? 'auth.kindCompany' : 'auth.kindPerson';
  }

  protected async approve(client: Account): Promise<void> {
    this.busy.set(client.id);
    try {
      const updated = await this.accounts.approveClient(client.id);
      this.items.update((list) => list.map((c) => (c.id === updated.id ? updated : c)));
      this.toast.success(this.translate.instant('admin.clients.approved'));
    } catch (error) {
      this.toast.error(extractError(error));
    } finally {
      this.busy.set(null);
    }
  }

  /** Suspending a client is a serious action — require an explicit double check. */
  confirmSuspend(client: Account): void {
    const data: ConfirmDialogData = {
      titleKey: 'admin.clients.suspendTitle',
      messageKey: 'admin.clients.suspendMessage',
      confirmKey: 'admin.clients.suspend',
    };

    const ref = this.dialog.open(ConfirmDialog, { width: '420px', data });

    ref.afterClosed().subscribe(async (confirmed: boolean) => {
      if (!confirmed) return;
      await this.suspend(client);
    });
  }

  private async suspend(client: Account): Promise<void> {
    this.busy.set(client.id);
    try {
      const updated = await this.accounts.suspendClient(client.id);
      this.items.update((list) => list.map((c) => (c.id === updated.id ? updated : c)));
      this.toast.success(this.translate.instant('admin.clients.suspended'));
    } catch (error) {
      this.toast.error(extractError(error));
    } finally {
      this.busy.set(null);
    }
  }
}
