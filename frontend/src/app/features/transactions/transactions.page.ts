import { Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { DatePipe, DecimalPipe } from '@angular/common';

import { AuthService } from '../../core/services/auth.service';
import { CategoriesService } from '../../core/services/categories.service';
import { TransactionsService } from '../../core/services/transactions.service';
import { ToastService } from '../../shared/services/toast.service';
import { Category, Transaction, TransactionQuery, TransactionType } from '../../core/models';
import { extractError } from '../../shared/utils/errors';
import { ConfirmDialog, ConfirmDialogData } from '../../shared/components/confirm-dialog.component';
import { TransactionDialog } from './transaction-dialog.component';

/**
 * Transactions list: filters (type, category, date range) + pagination.
 * Mutations update the list with the returned resource (no refetch) and
 * roll back on error (AGENTS.md §12).
 */
@Component({
  selector: 'app-transactions-page',
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatChipsModule,
    MatProgressBarModule,
    MatDialogModule,
    TranslatePipe,
    DatePipe,
    DecimalPipe,
  ],
  templateUrl: './transactions.page.html',
  styleUrl: './transactions.page.scss',
})
export class TransactionsPage {
  private readonly service = inject(TransactionsService);
  private readonly categoriesService = inject(CategoriesService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);
  private readonly dialog = inject(MatDialog);

  protected readonly loading = signal(true);
  protected readonly transactions = signal<Transaction[]>([]);
  protected readonly categories = signal<Category[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(10);
  protected readonly canMutate = computed(() => this.auth.canMutate());

  protected readonly filterForm = new FormGroup({
    type: new FormControl<TransactionType | null>(null),
    categoryId: new FormControl<string | null>(null),
    from: new FormControl<Date | null>(null),
    to: new FormControl<Date | null>(null),
  });

  protected readonly displayedColumns = computed(() =>
    this.canMutate()
      ? ['date', 'category', 'type', 'amount', 'description', 'actions']
      : ['date', 'category', 'type', 'amount', 'description'],
  );

  constructor() {
    void this.loadCategories();
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    try {
      const query = this.buildQuery();
      const page = await this.service.getAll(query);
      this.transactions.set(page.items);
      this.total.set(page.totalCount);
    } catch (error) {
      this.toast.error(extractError(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected async applyFilters(): Promise<void> {
    this.page.set(1);
    await this.load();
  }

  protected async clearFilters(): Promise<void> {
    this.filterForm.reset();
    this.page.set(1);
    await this.load();
  }

  protected async onPage(event: PageEvent): Promise<void> {
    this.page.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    await this.load();
  }

  protected typeLabel(type: TransactionType): string {
    return type === 'Income' ? 'transactions.income' : 'transactions.expense';
  }

  openCreate(): void {
    const ref = this.dialog.open(TransactionDialog, {
      width: '520px',
      data: { transaction: null, categories: this.categories() },
    });

    ref.afterClosed().subscribe(async (dto) => {
      if (!dto) return;
      try {
        await this.service.create(dto);
        await this.load();
        this.toast.success(this.translate.instant('transactions.created'));
      } catch (error) {
        this.toast.error(extractError(error));
      }
    });
  }

  openEdit(transaction: Transaction): void {
    const ref = this.dialog.open(TransactionDialog, {
      width: '520px',
      data: { transaction, categories: this.categories() },
    });

    ref.afterClosed().subscribe(async (dto) => {
      if (!dto) return;
      try {
        const updated = await this.service.update(transaction.id, dto);
        this.transactions.update((items) =>
          items.map((item) => (item.id === updated.id ? updated : item)),
        );
        this.toast.success(this.translate.instant('transactions.updated'));
      } catch (error) {
        this.toast.error(extractError(error));
      }
    });
  }

  confirmDelete(transaction: Transaction): void {
    const data: ConfirmDialogData = {
      titleKey: 'transactions.deleteTitle',
      messageKey: 'transactions.deleteMessage',
      confirmKey: 'common.delete',
    };

    const ref = this.dialog.open(ConfirmDialog, { width: '400px', data });

    ref.afterClosed().subscribe(async (confirmed) => {
      if (!confirmed) return;
      try {
        await this.service.delete(transaction.id);
        this.transactions.update((items) => items.filter((item) => item.id !== transaction.id));
        this.total.update((value) => Math.max(0, value - 1));
        this.toast.success(this.translate.instant('transactions.deleted'));
      } catch (error) {
        this.toast.error(extractError(error));
      }
    });
  }

  private buildQuery(): TransactionQuery {
    const values = this.filterForm.value;
    return {
      page: this.page(),
      pageSize: this.pageSize(),
      type: values.type ?? undefined,
      categoryId: values.categoryId ?? undefined,
      from: values.from ? values.from.toISOString() : undefined,
      to: values.to ? values.to.toISOString() : undefined,
    };
  }

  private async loadCategories(): Promise<void> {
    try {
      this.categories.set(await this.categoriesService.getAllUnpaginated());
    } catch {
      // Non-critical for the list; filters will simply be empty.
    }
  }
}
