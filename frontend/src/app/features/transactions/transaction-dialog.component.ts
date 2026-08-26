import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { TranslatePipe } from '@ngx-translate/core';

import { Category, CreateTransactionDto, Transaction, TransactionType } from '../../core/models';

export interface TransactionDialogData {
  transaction: Transaction | null;
  categories: Category[];
}

/**
 * Create/edit transaction form. Amount > 0, category required, ISO currency
 * (mirrors the backend FluentValidation contract).
 */
@Component({
  selector: 'app-transaction-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatButtonModule,
    TranslatePipe,
  ],
  templateUrl: './transaction-dialog.component.html',
  styleUrl: './transaction-dialog.component.scss',
})
export class TransactionDialog {
  private readonly dialogRef = inject(MatDialogRef<TransactionDialog>);
  private readonly data = inject<TransactionDialogData>(MAT_DIALOG_DATA);

  protected readonly isEdit = signal(this.data.transaction !== null);
  protected readonly categories = this.data.categories;

  protected readonly form = new FormGroup({
    categoryId: new FormControl(this.data.transaction?.categoryId ?? '', [Validators.required]),
    type: new FormControl<TransactionType>(this.data.transaction?.type ?? 'Expense', [
      Validators.required,
    ]),
    amount: new FormControl<number | null>(this.data.transaction?.amount ?? null, [
      Validators.required,
      Validators.min(0.01),
    ]),
    currency: new FormControl(this.data.transaction?.currency ?? 'EUR', [
      Validators.required,
      Validators.pattern('^[A-Z]{3}$'),
    ]),
    date: new FormControl<Date>(
      this.data.transaction ? new Date(this.data.transaction.date) : new Date(),
      [Validators.required],
    ),
    description: new FormControl(this.data.transaction?.description ?? '', [
      Validators.maxLength(500),
    ]),
  });

  submit(): void {
    if (this.form.invalid) return;

    const dto: CreateTransactionDto = {
      categoryId: this.form.value.categoryId ?? '',
      type: this.form.value.type ?? 'Expense',
      amount: Number(this.form.value.amount),
      currency: (this.form.value.currency ?? 'EUR').toUpperCase(),
      date: (this.form.value.date ?? new Date()).toISOString(),
      description: this.form.value.description?.trim() || undefined,
    };

    this.dialogRef.close(dto);
  }
}
