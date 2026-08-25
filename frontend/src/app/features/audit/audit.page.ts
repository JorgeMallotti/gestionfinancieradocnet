import { Component, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslatePipe } from '@ngx-translate/core';
import { DatePipe, SlicePipe } from '@angular/common';

import { AuditService } from '../../core/services/audit.service';
import { AuditLogEntry } from '../../core/models';
import { ToastService } from '../../shared/services/toast.service';
import { extractError } from '../../shared/utils/errors';

/**
 * Audit trail viewer (Admin only — enforced by the route guard and the
 * backend [Authorize(Roles = "Admin")]). Read-only, paginated.
 */
@Component({
  selector: 'app-audit-page',
  imports: [
    MatCardModule,
    MatTableModule,
    MatPaginatorModule,
    MatIconModule,
    MatProgressBarModule,
    MatTooltipModule,
    TranslatePipe,
    DatePipe,
    SlicePipe,
  ],
  templateUrl: './audit.page.html',
  styleUrl: './audit.page.scss',
})
export class AuditPage {
  private readonly service = inject(AuditService);
  private readonly toast = inject(ToastService);

  protected readonly loading = signal(true);
  protected readonly entries = signal<AuditLogEntry[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(20);

  protected readonly displayedColumns = ['createdAt', 'entity', 'action', 'userId', 'ipAddress'];

  constructor() {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    try {
      const result = await this.service.getAll(this.page(), this.pageSize());
      this.entries.set(result.items);
      this.total.set(result.totalCount);
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

  protected actionClass(action: string): string {
    switch (action) {
      case 'Create':
        return 'action-create';
      case 'Update':
        return 'action-update';
      case 'Delete':
        return 'action-delete';
      default:
        return '';
    }
  }
}
