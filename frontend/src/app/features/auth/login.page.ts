import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../shared/services/toast.service';
import { authErrorKey, extractError } from '../../shared/utils/errors';

/**
 * Login page. On success the app routes to the dashboard; the refresh token
 * arrives in an httpOnly cookie set by the backend (never stored in JS).
 */
@Component({
  selector: 'app-login-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    TranslatePipe,
  ],
  templateUrl: './login.page.html',
  styleUrl: './auth.pages.scss',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);

  protected readonly form = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
    password: new FormControl('', [Validators.required]),
  });

  protected readonly hidePassword = signal(true);
  protected readonly loading = signal(false);

  async submit(): Promise<void> {
    if (this.form.invalid) return;

    this.loading.set(true);
    try {
      await this.auth.login({
        email: this.form.value.email ?? '',
        password: this.form.value.password ?? '',
      });
      await this.router.navigate(['/dashboard']);
    } catch (error) {
      const message = extractError(error);
      this.toast.error(this.translate.instant(authErrorKey(message)));
    } finally {
      this.loading.set(false);
    }
  }
}
