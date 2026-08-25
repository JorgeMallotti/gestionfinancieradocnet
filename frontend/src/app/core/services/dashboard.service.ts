import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CategoryBreakdown, DashboardSummary, MonthlyPoint, TransactionType } from '../models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  getSummary(from?: string, to?: string): Promise<DashboardSummary> {
    return firstValueFrom(
      this.http.get<DashboardSummary>(`${environment.apiBaseUrl}/dashboard/summary`, {
        params: this.rangeParams(from, to),
      }),
    );
  }

  getBreakdown(type: TransactionType, from?: string, to?: string): Promise<CategoryBreakdown[]> {
    const params = this.rangeParams(from, to).set('type', type);
    return firstValueFrom(
      this.http.get<CategoryBreakdown[]>(`${environment.apiBaseUrl}/dashboard/breakdown`, {
        params,
      }),
    );
  }

  getMonthly(from?: string, to?: string): Promise<MonthlyPoint[]> {
    return firstValueFrom(
      this.http.get<MonthlyPoint[]>(`${environment.apiBaseUrl}/dashboard/monthly`, {
        params: this.rangeParams(from, to),
      }),
    );
  }

  private rangeParams(from?: string, to?: string): HttpParams {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return params;
  }
}
