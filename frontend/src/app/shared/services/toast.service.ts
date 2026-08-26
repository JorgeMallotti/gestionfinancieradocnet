import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

/**
 * Central toast service (Material SnackBar). Translates error details when
 * available and always shows a readable message.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.snackBar.open(message, undefined, { duration: 3000, panelClass: ['snack-success'] });
  }

  error(message: string): void {
    this.snackBar.open(message, undefined, { duration: 5000, panelClass: ['snack-error'] });
  }

  info(message: string): void {
    this.snackBar.open(message, undefined, { duration: 3000 });
  }
}
