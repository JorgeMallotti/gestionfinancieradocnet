import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Account, AccountRef, AccountStatus } from '../models';

/**
 * The caller's own account (clients see theirs; the Admin owns the treasury)
 * + active transfer counterparties + the Admin's client management calls.
 */
@Injectable({ providedIn: 'root' })
export class AccountsService {
  private readonly http = inject(HttpClient);

  getMyAccount(): Promise<Account> {
    return firstValueFrom(this.http.get<Account>(`${environment.apiBaseUrl}/accounts/me`));
  }

  getCounterparties(): Promise<AccountRef[]> {
    return firstValueFrom(
      this.http.get<AccountRef[]>(`${environment.apiBaseUrl}/accounts/counterparties`),
    );
  }

  // ── Admin: client management ──────────────────────────────────────────

  listClients(status?: AccountStatus): Promise<Account[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return firstValueFrom(
      this.http.get<Account[]>(`${environment.apiBaseUrl}/admin/clients`, { params }),
    );
  }

  approveClient(accountId: string): Promise<Account> {
    return firstValueFrom(
      this.http.post<Account>(`${environment.apiBaseUrl}/admin/clients/${accountId}/approve`, null),
    );
  }

  suspendClient(accountId: string): Promise<Account> {
    return firstValueFrom(
      this.http.post<Account>(`${environment.apiBaseUrl}/admin/clients/${accountId}/suspend`, null),
    );
  }
}
