import { Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { map } from 'rxjs';

import { CategoriesService } from '../../core/services/categories.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../shared/services/toast.service';
import { Category } from '../../core/models';
import { extractError } from '../../shared/utils/errors';
import { CategoryDialog } from './category-dialog.component';
import { CategoryDetailsDialog, CategoryDetailsResult } from './category-details-dialog.component';

/**
 * Categories are the bank Admin's catalog (visible to every client as an
 * optional movement tag). Clicking a category name opens a popup with the
 * details + Edit/Delete for the Admin; deleting requires a double check.
 */
@Component({
  selector: 'app-categories-page',
  imports: [
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
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
  private readonly breakpoints = inject(BreakpointObserver);

  protected readonly loading = signal(true);
  protected readonly categories = signal<Category[]>([]);
  protected readonly isAdmin = computed(() => this.auth.isAdmin());

  private readonly isHandset = toSignal(
    this.breakpoints.observe([Breakpoints.Handset]).pipe(map((x) => x.matches)),
    { initialValue: false },
  );

  protected readonly isHandsetLayout = computed(() => this.isHandset());

  protected readonly displayedColumns = computed(() =>
    this.isAdmin() ? ['name', 'description', 'actions'] : ['name', 'description'],
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

  /** Opens the create dialog (Admin). */
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

  /** Opens the edit dialog (Admin). */
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

  /** Click on a category name → popup with details (+ Edit/Delete for Admin). */
  openDetails(category: Category): void {
    const ref = this.dialog.open(CategoryDetailsDialog, {
      width: '440px',
      data: { category, isAdmin: this.isAdmin() },
    });

    ref.afterClosed().subscribe(async (result: CategoryDetailsResult) => {
      if (!result) return;
      if (result.action === 'edit') this.openEdit(category);
      if (result.action === 'delete') await this.deleteCategory(category);
    });
  }

  private async deleteCategory(category: Category): Promise<void> {
    try {
      await this.service.delete(category.id);
      this.categories.update((items) => items.filter((item) => item.id !== category.id));
      this.toast.success(this.translate.instant('categories.deleted'));
    } catch (error) {
      this.toast.error(extractError(error));
    }
  }
}
