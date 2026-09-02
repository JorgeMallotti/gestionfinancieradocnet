import { Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { DecimalPipe } from '@angular/common';

import { LoansService } from '../../core/services/loans.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../shared/services/toast.service';
import { Loan, LoanStatus } from '../../core/models';
import { extractError } from '../../shared/utils/errors';

/**
 * Loans (simple MVP: no interest, no deadlines). A client requests an amount
 * with a reason → the Admin approves/rejects → on approval the treasury funds
 * the client → the client repays anytime (full or partial).
 */
@Component({
  selector: 'app-loans-page',
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    TranslatePipe,
    DecimalPipe,
  ],
  templateUrl: './loans.page.html',
  styleUrl: './loans.page.scss',
})
export class LoansPage {
  private readonly loans = inject(LoansService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);

  protected readonly loading = signal(true);
  protected readonly items = signal<Loan[]>([]);
  protected readonly isAdmin = computed(() => this.auth.isAdmin());
  protected readonly busy = signal<string | null>(null);

  protected readonly requestForm = new FormGroup({
    amount: new FormControl<number | null>(null, [Validators.required, Validators.min(0.01)]),
    reason: new FormControl('', [Validators.required, Validators.maxLength(500)]),
  });

  protected readonly decideNote = new FormControl('', [Validators.maxLength(500)]);

  constructor() {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    try {
      const items = this.isAdmin() ? await this.loans.getAll() : await this.loans.getMine();
      this.items.set(items);
    } catch (error) {
      this.toast.error(extractError(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected statusLabelKey(status: LoanStatus): string {
    return `loans.status.${status}`;
  }

  async request(): Promise<void> {
    if (this.requestForm.invalid) return;

    this.busy.set('request');
    try {
      const created = await this.loans.request({
        amount: this.requestForm.value.amount ?? 0,
        reason: this.requestForm.value.reason?.trim() ?? '',
      });
      this.items.update((list) => [created, ...list]);
      this.requestForm.reset();
      this.toast.success(this.translate.instant('loans.requested'));
    } catch (error) {
      this.toast.error(extractError(error));
    } finally {
      this.busy.set(null);
    }
  }

  /** Admin: approve a pending loan (the treasury funds the client). */
  protected async approve(loan: Loan): Promise<void> {
    await this.decide(loan, true);
  }

  /** Admin: reject a pending loan. */
  protected async reject(loan: Loan): Promise<void> {
    await this.decide(loan, false);
  }

  private async decide(loan: Loan, approve: boolean): Promise<void> {
    this.busy.set(loan.id);
    try {
      const note = this.decideNote.value?.trim() || undefined;
      const updated = await this.loans.decide(loan.id, { approve, note });
      this.items.update((list) => list.map((l) => (l.id === updated.id ? updated : l)));
      this.decideNote.reset();
      this.toast.success(this.translate.instant(approve ? 'loans.approved' : 'loans.rejected'));
    } catch (error) {
      this.toast.error(extractError(error));
    } finally {
      this.busy.set(null);
    }
  }

  /** Client: repay a loan (partial amount — the backend caps at outstanding). */
  protected async repay(loan: Loan, input: HTMLInputElement): Promise<void> {
    const amount = Number(input.value);
    if (!amount || amount <= 0) {
      this.toast.error(this.translate.instant('loans.errorRepayAmount'));
      return;
    }

    this.busy.set(loan.id);
    try {
      const updated = await this.loans.repay(loan.id, { amount });
      this.items.update((list) => list.map((l) => (l.id === updated.id ? updated : l)));
      input.value = '';
      this.toast.success(this.translate.instant('loans.repaid'));
    } catch (error) {
      this.toast.error(extractError(error));
    } finally {
      this.busy.set(null);
    }
  }
}
