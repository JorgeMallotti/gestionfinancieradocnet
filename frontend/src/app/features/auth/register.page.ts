import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../shared/services/toast.service';
import { authErrorKey, extractError } from '../../shared/utils/errors';
import { ClientKind } from '../../core/models';

/**
 * Register page: opens a CLIENT account (person or company) under the bank.
 * The account starts as Pending — the bank Admin must approve it before the
 * client can operate. The UX explains this ("wait for Admin approval").
 */
@Component({
  selector: 'app-register-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    TranslatePipe,
  ],
  templateUrl: './register.page.html',
  styleUrl: './auth.pages.scss',
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);

  protected readonly kinds: { value: ClientKind; labelKey: string }[] = [
    { value: 'Person', labelKey: 'auth.kindPerson' },
    { value: 'Company', labelKey: 'auth.kindCompany' },
  ];

  protected readonly form = new FormGroup(
    {
      kind: new FormControl<ClientKind>('Person', [Validators.required]),
      displayName: new FormControl('', [Validators.required, Validators.maxLength(120)]),
      email: new FormControl('', [Validators.required, Validators.email]),
      password: new FormControl('', [Validators.required, Validators.minLength(8)]),
      confirmPassword: new FormControl('', [Validators.required]),
    },
    {
      validators: (group) =>
        group.value.password === group.value.confirmPassword ? null : { mismatch: true },
    },
  );

  protected readonly hidePassword = signal(true);
  protected readonly loading = signal(false);

  async submit(): Promise<void> {
    if (this.form.invalid) return;

    this.loading.set(true);
    try {
      await this.auth.register({
        displayName: this.form.value.displayName ?? '',
        kind: this.form.value.kind ?? 'Person',
        email: this.form.value.email ?? '',
        password: this.form.value.password ?? '',
      });
      // The account is Pending — show the "wait for approval" screen.
      await this.router.navigate(['/auth/pending']);
    } catch (error) {
      const message = extractError(error);
      this.toast.error(this.translate.instant(authErrorKey(message)));
    } finally {
      this.loading.set(false);
    }
  }
}
