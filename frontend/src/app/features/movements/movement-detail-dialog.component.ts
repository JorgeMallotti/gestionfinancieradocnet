import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '@ngx-translate/core';
import { DatePipe, DecimalPipe } from '@angular/common';

import { Movement } from '../../core/models';

/**
 * Read-only movement details. Movements are immutable — there is nothing to
 * edit or delete; if something is wrong, the client opens a claim on it.
 */
@Component({
  selector: 'app-movement-detail-dialog',
  imports: [MatDialogModule, MatIconModule, TranslatePipe, DatePipe, DecimalPipe],
  templateUrl: './movement-detail-dialog.component.html',
  styles: [
    `
      .detail-row {
        display: flex;
        justify-content: space-between;
        gap: 16px;
        padding: 8px 0;
        border-bottom: 1px solid var(--mat-sys-outline-variant);
      }
      .detail-row:last-of-type {
        border-bottom: none;
      }
      .detail-label {
        opacity: 0.7;
        flex-shrink: 0;
      }
      .detail-value {
        text-align: right;
        font-weight: 500;
        word-break: break-word;
      }
      .amount-line {
        display: flex;
        align-items: center;
        justify-content: flex-end;
        gap: 8px;
        font-size: 1.4rem;
        font-weight: 700;
      }
    `,
  ],
})
export class MovementDetailDialog {
  protected readonly movement = inject<Movement>(MAT_DIALOG_DATA);

  protected typeLabelKey(type: string): string {
    return `movements.types.${type}`;
  }
}
