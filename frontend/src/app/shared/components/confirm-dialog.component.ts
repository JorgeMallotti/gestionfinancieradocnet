import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';

export interface ConfirmDialogData {
  titleKey: string;
  messageKey: string;
  confirmKey?: string;
}

/**
 * Reusable confirmation dialog (Material). Used for deletes and other
 * destructive actions. All text comes from @ngx-translate.
 */
@Component({
  selector: 'app-confirm-dialog',
  imports: [MatDialogModule, MatButtonModule, FormsModule, TranslatePipe],
  template: `
    <h2 mat-dialog-title>{{ data().titleKey | translate }}</h2>
    <mat-dialog-content>
      <p>{{ data().messageKey | translate }}</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>{{ 'common.cancel' | translate }}</button>
      <button mat-raised-button color="warn" (click)="confirm()">
        {{ data().confirmKey ?? 'common.confirm' | translate }}
      </button>
    </mat-dialog-actions>
  `,
})
export class ConfirmDialog {
  private readonly dialogRef = inject(MatDialogRef<ConfirmDialog>);

  /**
   * The payload passed to MatDialog.open(..., { data }) is injected through
   * the MAT_DIALOG_DATA token — it is NOT available on the dialog ref while
   * this component is still being constructed.
   */
  private readonly injected = inject<ConfirmDialogData | null>(MAT_DIALOG_DATA);

  protected readonly data = signal<ConfirmDialogData>(
    this.injected ?? {
      titleKey: 'common.confirm',
      messageKey: 'common.error',
    },
  );

  confirm(): void {
    this.dialogRef.close(true);
  }
}
