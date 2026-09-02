import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { AccountsService } from '../../core/services/accounts.service';
import { CategoriesService } from '../../core/services/categories.service';
import { MovementsService } from '../../core/services/movements.service';
import { ToastService } from '../../shared/services/toast.service';
import { AccountRef, Category } from '../../core/models';
import { extractError } from '../../shared/utils/errors';

/**
 * Send-money dialog: the payer is ALWAYS the caller (resolved server-side from
 * the JWT); the body only picks the receiver, amount and optional category.
 * An overdraft is rejected by the backend (no account can go negative).
 */
@Component({
  selector: 'app-transfer-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    TranslatePipe,
  ],
  templateUrl: './transfer-dialog.component.html',
})
export class TransferDialog {
  private readonly accounts = inject(AccountsService);
  private readonly categoriesService = inject(CategoriesService);
  private readonly movements = inject(MovementsService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);
  private readonly dialogRef = inject(MatDialogRef<TransferDialog>);

  protected readonly counterparties = signal<AccountRef[]>([]);
  protected readonly categories = signal<Category[]>([]);
  protected readonly loading = signal(true);
  protected readonly sending = signal(false);

  protected readonly form = new FormGroup({
    toAccountId: new FormControl('', [Validators.required]),
    amount: new FormControl<number | null>(null, [Validators.required, Validators.min(0.01)]),
    categoryId: new FormControl<string | null>(null),
    description: new FormControl('', [Validators.maxLength(200)]),
  });

  constructor() {
    void this.loadOptions();
  }

  private async loadOptions(): Promise<void> {
    try {
      const [counterparties, categories] = await Promise.all([
        this.accounts.getCounterparties(),
        this.categoriesService.getAllUnpaginated().catch(() => [] as Category[]),
      ]);
      this.counterparties.set(counterparties);
      this.categories.set(categories);
    } catch (error) {
      this.toast.error(extractError(error));
    } finally {
      this.loading.set(false);
    }
  }

  async send(): Promise<void> {
    if (this.form.invalid) return;

    this.sending.set(true);
    try {
      const values = this.form.value;
      const created = await this.movements.transfer({
        toAccountId: values.toAccountId ?? '',
        amount: values.amount ?? 0,
        categoryId: values.categoryId ?? undefined,
        description: values.description?.trim() || undefined,
      });
      this.dialogRef.close(created);
    } catch (error) {
      this.toast.error(extractError(error));
      this.sending.set(false);
    }
  }
}
