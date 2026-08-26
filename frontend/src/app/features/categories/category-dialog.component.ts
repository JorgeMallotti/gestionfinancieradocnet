import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslatePipe } from '@ngx-translate/core';

import { Category, CreateCategoryDto } from '../../core/models';

export interface CategoryDialogData {
  category: Category | null;
}

/**
 * Create/edit category form (Material dialog). Reused for both operations;
 * the result is a CreateCategoryDto that the page applies optimistically.
 */
@Component({
  selector: 'app-category-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    TranslatePipe,
  ],
  templateUrl: './category-dialog.component.html',
  styleUrl: './category-dialog.component.scss',
})
export class CategoryDialog {
  private readonly dialogRef = inject(MatDialogRef<CategoryDialog>);
  private readonly data = inject<CategoryDialogData>(MAT_DIALOG_DATA);

  protected readonly isEdit = signal(this.data.category !== null);

  protected readonly form = new FormGroup({
    name: new FormControl(this.data.category?.name ?? '', [
      Validators.required,
      Validators.maxLength(100),
    ]),
    description: new FormControl(this.data.category?.description ?? '', [
      Validators.maxLength(500),
    ]),
  });

  submit(): void {
    if (this.form.invalid) return;

    const dto: CreateCategoryDto = {
      name: (this.form.value.name ?? '').trim(),
      description: this.form.value.description?.trim() || undefined,
    };

    this.dialogRef.close(dto);
  }
}
