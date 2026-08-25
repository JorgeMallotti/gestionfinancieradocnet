import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  CreateTransactionDto,
  PagedResult,
  Transaction,
  TransactionQuery,
  UpdateTransactionDto,
} from '../models';

@Injectable({ providedIn: 'root' })
export class TransactionsService {
  private readonly http = inject(HttpClient);

  getAll(query: TransactionQuery = {}): Promise<PagedResult<Transaction>> {
    let params = new HttpParams();
    if (query.page) params = params.set('page', query.page);
    if (query.pageSize) params = params.set('pageSize', query.pageSize);
    if (query.type) params = params.set('type', query.type);
    if (query.categoryId) params = params.set('categoryId', query.categoryId);
    if (query.from) params = params.set('from', query.from);
    if (query.to) params = params.set('to', query.to);

    return firstValueFrom(
      this.http.get<PagedResult<Transaction>>(`${environment.apiBaseUrl}/transactions`, { params }),
    );
  }

  getById(id: string): Promise<Transaction> {
    return firstValueFrom(
      this.http.get<Transaction>(`${environment.apiBaseUrl}/transactions/${id}`),
    );
  }

  create(dto: CreateTransactionDto): Promise<Transaction> {
    return firstValueFrom(
      this.http.post<Transaction>(`${environment.apiBaseUrl}/transactions`, dto),
    );
  }

  update(id: string, dto: UpdateTransactionDto): Promise<Transaction> {
    return firstValueFrom(
      this.http.put<Transaction>(`${environment.apiBaseUrl}/transactions/${id}`, dto),
    );
  }

  delete(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${environment.apiBaseUrl}/transactions/${id}`));
  }
}
