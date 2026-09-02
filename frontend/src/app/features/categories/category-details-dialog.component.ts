import { Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '@ngx-translate/core';

import { Category } from '../../core/models';

export interface CategoryDetailsData {
  category: Category;
  isAdmin: boolean;
}

export type CategoryDetailsResult = { action: 'edit' } | { action: 'delete' } | null;

/**
 * Category details popup (opened by clicking the name). Shows the details and,
 * for the Admin, the actions Edit / Delete. Delete requires a DOUBLE CHECK:
 * the Admin must tick "I understand the consequences" to enable the button.
 */
@Component({
  selector: 'app-category-details-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatCheckboxModule,
    MatIconModule,
    TranslatePipe,
  ],
  templateUrl: './category-details-dialog.component.html',
})
export class CategoryDetailsDialog {
  private readonly dialogRef = inject(MatDialogRef<CategoryDetailsDialog>);
  private readonly data = inject<CategoryDetailsData>(MAT_DIALOG_DATA);

  protected readonly category = this.data.category;
  protected readonly isAdmin = this.data.isAdmin;

  protected readonly understood = new FormControl<boolean>(false, [Validators.requiredTrue]);

  protected close(): void {
    this.dialogRef.close(null);
  }

  protected edit(): void {
    this.dialogRef.close({ action: 'edit' } satisfies CategoryDetailsResult);
  }

  protected confirmDelete(): void {
    if (!this.understood.value) return;
    this.dialogRef.close({ action: 'delete' } satisfies CategoryDetailsResult);
  }
}
