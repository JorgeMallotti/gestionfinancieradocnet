import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '@ngx-translate/core';

/**
 * Shown right after a successful registration: the client account is Pending
 * and the bank Admin must approve it before it can operate (bank-like onboarding).
 */
@Component({
  selector: 'app-pending-page',
  imports: [
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatDividerModule,
    MatIconModule,
    TranslatePipe,
  ],
  templateUrl: 'pending.page.html',
  styleUrl: './auth.pages.scss',
})
export class PendingPage {}
