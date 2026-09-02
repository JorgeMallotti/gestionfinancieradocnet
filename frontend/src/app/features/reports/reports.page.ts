import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { ReportsService } from '../../core/services/reports.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../shared/services/toast.service';
import { extractError } from '../../shared/utils/errors';

/**
 * Reports page: download the financial report as PDF or Excel, or send it by
 * email (Admin/Finance). Downloads use responseType blob — the file is saved
 * by the browser; no client-side generation libraries.
 */
@Component({
  selector: 'app-reports-page',
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatProgressBarModule,
    TranslatePipe,
  ],
  templateUrl: './reports.page.html',
  styleUrl: './reports.page.scss',
})
export class ReportsPage {
  private readonly reports = inject(ReportsService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);

  protected readonly downloading = signal<string | null>(null);
  protected readonly canSendEmail = this.auth.isAdmin;

  protected readonly emailForm = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
    from: new FormControl<Date | null>(null),
    to: new FormControl<Date | null>(null),
  });

  async downloadPdf(): Promise<void> {
    await this.download('pdf', () => this.reports.downloadPdf());
  }

  async downloadExcel(): Promise<void> {
    await this.download('excel', () => this.reports.downloadExcel());
  }

  async sendEmail(): Promise<void> {
    if (this.emailForm.invalid) return;

    this.downloading.set('email');
    try {
      const values = this.emailForm.value;
      await this.reports.sendByEmail({
        email: values.email ?? '',
        from: values.from ? values.from.toISOString() : undefined,
        to: values.to ? values.to.toISOString() : undefined,
      });
      this.translate.instant('reports.emailSent');
      this.toast.success('reports.emailSent');
      this.emailForm.reset();
    } catch (error) {
      this.toast.error(extractError(error));
    } finally {
      this.downloading.set(null);
    }
  }

  private async download(kind: 'pdf' | 'excel', action: () => Promise<Blob>): Promise<void> {
    if (this.downloading()) return;

    this.downloading.set(kind);
    try {
      const blob = await action();
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = kind === 'pdf' ? 'financial-report.pdf' : 'transactions.xlsx';
      link.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      this.toast.error(extractError(error));
    } finally {
      this.downloading.set(null);
    }
  }
}
