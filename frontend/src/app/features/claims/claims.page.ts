import { Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CdkTextareaAutosize } from '@angular/cdk/text-field';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { DatePipe, DecimalPipe } from '@angular/common';

import { ClaimsService } from '../../core/services/claims.service';
import { MovementsService } from '../../core/services/movements.service';
import { AccountsService } from '../../core/services/accounts.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../shared/services/toast.service';
import { AccountRef, Claim, ClaimStatus, Movement } from '../../core/models';
import { extractError } from '../../shared/utils/errors';

/**
 * Claims (reclamaciones): if a movement is wrong, the involved client opens a
 * claim → the Admin (mediator) proposes a corrective transfer → both parties
 * consent → the corrective movement is executed and STACKED on the ledger.
 */
@Component({
  selector: 'app-claims-page',
  imports: [
    ReactiveFormsModule,
    CdkTextareaAutosize,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressBarModule,
    TranslatePipe,
    DatePipe,
    DecimalPipe,
  ],
  templateUrl: './claims.page.html',
  styleUrl: './claims.page.scss',
})
export class ClaimsPage {
  private readonly claims = inject(ClaimsService);
  private readonly movements = inject(MovementsService);
  private readonly accounts = inject(AccountsService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);

  protected readonly loading = signal(true);
  protected readonly items = signal<Claim[]>([]);
  protected readonly isAdmin = computed(() => this.auth.isAdmin());
  protected readonly busy = signal<string | null>(null);

  protected readonly myAccountId = signal<string | null>(null);
  protected readonly myMovements = signal<Movement[]>([]);
  protected readonly counterparties = signal<AccountRef[]>([]);

  protected readonly openForm = new FormGroup({
    movementId: new FormControl('', [Validators.required]),
    reason: new FormControl('', [Validators.required, Validators.maxLength(500)]),
  });

  /** Admin proposal form (fields per claim are filled from a selected claim). */
  protected readonly proposeForm = new FormGroup({
    amount: new FormControl<number | null>(null, [Validators.required, Validators.min(0.01)]),
    fromAccountId: new FormControl('', [Validators.required]),
    toAccountId: new FormControl('', [Validators.required]),
    note: new FormControl('', [Validators.maxLength(500)]),
  });
  protected readonly proposingClaimId = signal<string | null>(null);

  constructor() {
    void this.load();
    if (!this.auth.isAdmin()) void this.loadClientOptions();
    else void this.loadAdminOptions();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    try {
      const items = this.isAdmin() ? await this.claims.getAll() : await this.claims.getMine();
      this.items.set(items);
    } catch (error) {
      this.toast.error(extractError(error));
    } finally {
      this.loading.set(false);
    }
  }

  /** Client: my account + recent movements to dispute + counterparties. */
  private async loadClientOptions(): Promise<void> {
    try {
      const [account, page] = await Promise.all([
        this.accounts.getMyAccount(),
        this.movements.getMine({ page: 1, pageSize: 50 }),
      ]);
      this.myAccountId.set(account.id);
      this.myMovements.set(page.items);
    } catch (error) {
      this.toast.error(extractError(error));
    }
  }

  /** Admin: the parties available for a corrective transfer proposal. */
  private async loadAdminOptions(): Promise<void> {
    try {
      const counterparties = await this.accounts.getCounterparties();
      // The admin's counterparties = clients; proposal needs both parties of
      // the movement, which the service validates anyway.
      this.counterparties.set(counterparties);
    } catch {
      /* non-fatal */
    }
  }

  protected statusLabelKey(status: ClaimStatus): string {
    return `claims.status.${status}`;
  }

  /** True if the claim is open and the caller is one of the parties. */
  protected isParty(claim: Claim): boolean {
    const mine = this.myAccountId();
    if (!mine) return false;
    return (
      claim.claimantAccountId === mine ||
      claim.correctiveFromAccountId === mine ||
      claim.correctiveToAccountId === mine
    );
  }

  /** Whether the caller has already consented to this claim's correction. */
  protected hasConsented(claim: Claim): boolean {
    const mine = this.myAccountId();
    if (!mine) return false;
    if (claim.correctiveFromAccountId === mine) return claim.payerConsented;
    if (claim.correctiveToAccountId === mine) return claim.payeeConsented;
    return false;
  }

  /** Client: open a claim on one of my movements. */
  async openClaim(): Promise<void> {
    if (this.openForm.invalid) return;

    this.busy.set('open');
    try {
      const created = await this.claims.open({
        movementId: this.openForm.value.movementId ?? '',
        reason: this.openForm.value.reason?.trim() ?? '',
      });
      this.items.update((list) => [created, ...list]);
      this.openForm.reset();
      this.toast.success(this.translate.instant('claims.opened'));
    } catch (error) {
      this.toast.error(extractError(error));
    } finally {
      this.busy.set(null);
    }
  }

  /** Client: consent or refuse a proposed correction. */
  protected async consent(claim: Claim, approve: boolean): Promise<void> {
    this.busy.set(claim.id);
    try {
      const updated = await this.claims.consent(claim.id, approve);
      this.items.update((list) => list.map((c) => (c.id === updated.id ? updated : c)));
      this.toast.success(this.translate.instant(approve ? 'claims.consented' : 'claims.refused'));
    } catch (error) {
      this.toast.error(extractError(error));
    } finally {
      this.busy.set(null);
    }
  }

  /** Admin: fill the proposal form for a claim. */
  protected beginProposal(claim: Claim): void {
    this.proposingClaimId.set(claim.id);
    this.proposeForm.reset();
  }

  /** Admin: propose a corrective transfer between the two parties. */
  protected async submitProposal(claim: Claim): Promise<void> {
    if (this.proposeForm.invalid) return;

    this.busy.set(claim.id);
    try {
      const values = this.proposeForm.value;
      const updated = await this.claims.propose(claim.id, {
        amount: values.amount ?? 0,
        fromAccountId: values.fromAccountId ?? '',
        toAccountId: values.toAccountId ?? '',
        note: values.note?.trim() || undefined,
      });
      this.items.update((list) => list.map((c) => (c.id === updated.id ? updated : c)));
      this.proposingClaimId.set(null);
      this.toast.success(this.translate.instant('claims.proposed'));
    } catch (error) {
      this.toast.error(extractError(error));
    } finally {
      this.busy.set(null);
    }
  }

  protected movementSummary(movement: Movement): string {
    return `${movement.occurredAt.slice(0, 10)} · ${movement.fromDisplayName} → ${movement.toDisplayName} · ${movement.amount}`;
  }

  protected openFormMovementSummary(id: string): string {
    const found = this.myMovements().find((m) => m.id === id);
    return found ? this.movementSummary(found) : id;
  }
}
