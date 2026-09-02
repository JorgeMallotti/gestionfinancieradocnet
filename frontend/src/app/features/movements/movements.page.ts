import { Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { map } from 'rxjs';

import { MovementsService } from '../../core/services/movements.service';
import { AccountsService } from '../../core/services/accounts.service';
import { ReportsService } from '../../core/services/reports.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../shared/services/toast.service';
import { Movement, MovementType } from '../../core/models';
import { extractError } from '../../shared/utils/errors';
import { MovementDetailDialog } from './movement-detail-dialog.component';
import { TransferDialog } from './transfer-dialog.component';
import { saveBlob } from '../../shared/utils/download';

/**
 * The caller's own immutable ledger. List shows TWO columns (description +
 * amount); clicking a row opens read-only details. Movements can never be
 * edited or deleted — corrections are new movements stacked on top.
 * Quick access: send money (P2P) and export the statement as PDF/Excel.
 */
@Component({
  selector: 'app-movements-page',
  imports: [
    MatCardModule,
    MatTableModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    TranslatePipe,
    DatePipe,
    DecimalPipe,
  ],
  templateUrl: './movements.page.html',
  styleUrl: './movements.page.scss',
})
export class MovementsPage {
  private readonly movements = inject(MovementsService);
  private readonly accounts = inject(AccountsService);
  private readonly reports = inject(ReportsService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);
  private readonly dialog = inject(MatDialog);
  private readonly breakpoints = inject(BreakpointObserver);

  protected readonly loading = signal(true);
  protected readonly items = signal<Movement[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(10);
  protected readonly exporting = signal<string | null>(null);

  /** My account id — decides whether a movement is money in or money out. */
  protected readonly myAccountId = signal<string | null>(null);

  private readonly isHandset = toSignal(
    this.breakpoints.observe([Breakpoints.Handset]).pipe(map((x) => x.matches)),
    { initialValue: false },
  );

  protected readonly isHandsetLayout = computed(() => this.isHandset());

  constructor() {
    void this.loadAccount();
    void this.load();
  }

  /** Resolves my account id (needed to render +/− amounts correctly). */
  private async loadAccount(): Promise<void> {
    try {
      const account = await this.accounts.getMyAccount();
      this.myAccountId.set(account.id);
    } catch (error) {
      this.toast.error(extractError(error));
    }
  }

  /** True when the movement CREDITED my account (money in). */
  protected isIncoming(movement: Movement): boolean {
    return movement.toAccountId === this.myAccountId();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    try {
      const page = await this.movements.getMine({
        page: this.page(),
        pageSize: this.pageSize(),
      });
      this.items.set(page.items);
      this.total.set(page.totalCount);
    } catch (error) {
      this.toast.error(extractError(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected async onPage(event: PageEvent): Promise<void> {
    this.page.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    await this.load();
  }

  /** Description to show in the list (the "visible name" of the row). */
  protected displayName(movement: Movement): string {
    if (movement.description) return movement.description;
    return this.isIncoming(movement) ? movement.fromDisplayName : movement.toDisplayName;
  }

  protected typeLabelKey(type: MovementType): string {
    return `movements.types.${type}`;
  }

  /** Opens the read-only movement details. */
  protected openDetails(movement: Movement): void {
    this.dialog.open(MovementDetailDialog, {
      width: '520px',
      data: movement,
    });
  }

  /** Sends money to another account (opens the P2P dialog). */
  protected openTransfer(): void {
    const ref = this.dialog.open(TransferDialog, { width: '520px' });
    ref.afterClosed().subscribe((created: Movement | null) => {
      if (!created) return;
      this.toast.success(this.translate.instant('transfers.sent'));
      void this.load(); // the balance/ledger changed — reload the first page
    });
  }

  /** Quick access: download my statement as PDF/Excel (any role). */
  protected async exportPdf(): Promise<void> {
    await this.export('pdf', () => this.reports.downloadPdf());
  }

  protected async exportExcel(): Promise<void> {
    await this.export('excel', () => this.reports.downloadExcel());
  }

  private async export(kind: 'pdf' | 'excel', call: () => Promise<Blob>): Promise<void> {
    this.exporting.set(kind);
    try {
      const blob = await call();
      saveBlob(blob, kind === 'pdf' ? 'account-statement.pdf' : 'movements.xlsx');
    } catch (error) {
      this.toast.error(extractError(error));
    } finally {
      this.exporting.set(null);
    }
  }
}
