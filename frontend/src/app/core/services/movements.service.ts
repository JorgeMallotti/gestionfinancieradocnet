import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Movement, MovementQuery, PagedResult, TransferDto } from '../models';

/**
 * The immutable ledger: the caller's own movements (GET only — movements are
 * never edited or deleted) and P2P transfers (POST /transfers appends one).
 */
@Injectable({ providedIn: 'root' })
export class MovementsService {
  private readonly http = inject(HttpClient);

  getMine(query: MovementQuery = {}): Promise<PagedResult<Movement>> {
    let params = new HttpParams();
    if (query.page) params = params.set('page', query.page);
    if (query.pageSize) params = params.set('pageSize', query.pageSize);
    if (query.type) params = params.set('type', query.type);
    if (query.from) params = params.set('from', query.from);
    if (query.to) params = params.set('to', query.to);

    return firstValueFrom(
      this.http.get<PagedResult<Movement>>(`${environment.apiBaseUrl}/movements`, { params }),
    );
  }

  getById(id: string): Promise<Movement> {
    return firstValueFrom(this.http.get<Movement>(`${environment.apiBaseUrl}/movements/${id}`));
  }

  /** Sends money from the caller's account to another account (append-only). */
  transfer(dto: TransferDto): Promise<Movement> {
    return firstValueFrom(this.http.post<Movement>(`${environment.apiBaseUrl}/transfers`, dto));
  }
}
