import { Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { CategoriesService } from '../../core/services/categories.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../shared/services/toast.service';
import { Category } from '../../core/models';
import { extractError } from '../../shared/utils/errors';
import { ConfirmDialog, ConfirmDialogData } from '../../shared/components/confirm-dialog.component';
import { CategoryDialog } from './category-dialog.component';

/**
 * Categories CRUD. Mutations are shown only to Admin/Finance (backend also
 * enforces this). After each mutation the local list is updated with the
 * returned resource — no refetch (optimistic updates, AGENTS.md §12).
 */
@Component({
  selector: 'app-categories-page',
  imports: [
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressBarModule,
    MatDialogModule,
    TranslatePipe,
  ],
  templateUrl: './categories.page.html',
  styleUrl: './categories.page.scss',
})
export class CategoriesPage {
  private readonly service = inject(CategoriesService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);
  private readonly dialog = inject(MatDialog);

  protected readonly loading = signal(true);
  protected readonly categories = signal<Category[]>([]);
  protected readonly canMutate = computed(() => this.auth.canMutate());

  protected readonly displayedColumns = computed(() =>
    this.canMutate()
      ? ['name', 'description', 'isDefault', 'actions']
      : ['name', 'description', 'isDefault'],
  );

  constructor() {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    try {
      const page = await this.service.getAll({ page: 1, pageSize: 100 });
      this.categories.set(page.items);
    } catch (error) {
      this.toast.error(extractError(error));
    } finally {
      this.loading.set(false);
    }
  }

  openCreate(): void {
    const ref = this.dialog.open(CategoryDialog, {
      width: '440px',
      data: { category: null },
    });

    ref.afterClosed().subscribe(async (dto) => {
      if (!dto) return;
      try {
        const created = await this.service.create(dto);
        this.categories.update((items) =>
          [...items, created].sort((a, b) => a.name.localeCompare(b.name)),
        );
        this.toast.success(this.translate.instant('categories.created'));
      } catch (error) {
        this.toast.error(extractError(error));
      }
    });
  }

  openEdit(category: Category): void {
    const ref = this.dialog.open(CategoryDialog, {
      width: '440px',
      data: { category },
    });

    ref.afterClosed().subscribe(async (dto) => {
      if (!dto) return;
      try {
        const updated = await this.service.update(category.id, dto);
        this.categories.update((items) =>
          items
            .map((item) => (item.id === updated.id ? updated : item))
            .sort((a, b) => a.name.localeCompare(b.name)),
        );
        this.toast.success(this.translate.instant('categories.updated'));
      } catch (error) {
        this.toast.error(extractError(error));
      }
    });
  }

  confirmDelete(category: Category): void {
    const data: ConfirmDialogData = {
      titleKey: 'categories.deleteTitle',
      messageKey: 'categories.deleteMessage',
      confirmKey: 'common.delete',
    };

    const ref = this.dialog.open(ConfirmDialog, { width: '400px', data });

    ref.afterClosed().subscribe(async (confirmed) => {
      if (!confirmed) return;
      try {
        await this.service.delete(category.id);
        this.categories.update((items) => items.filter((item) => item.id !== category.id));
        this.toast.success(this.translate.instant('categories.deleted'));
      } catch (error) {
        this.toast.error(extractError(error));
      }
    });
  }
}
