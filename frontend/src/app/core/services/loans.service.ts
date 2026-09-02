import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { DecideLoanDto, Loan, LoanStatus, RepayLoanDto, RequestLoanDto } from '../models';

/**
 * Loans (simple MVP: no interest, no deadlines). Clients request and repay;
 * the Admin decides (approve → treasury funds the client via the ledger).
 */
@Injectable({ providedIn: 'root' })
export class LoansService {
  private readonly http = inject(HttpClient);

  getMine(): Promise<Loan[]> {
    return firstValueFrom(this.http.get<Loan[]>(`${environment.apiBaseUrl}/loans/mine`));
  }

  getAll(status?: LoanStatus): Promise<Loan[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return firstValueFrom(this.http.get<Loan[]>(`${environment.apiBaseUrl}/loans`, { params }));
  }

  request(dto: RequestLoanDto): Promise<Loan> {
    return firstValueFrom(this.http.post<Loan>(`${environment.apiBaseUrl}/loans`, dto));
  }

  decide(id: string, dto: DecideLoanDto): Promise<Loan> {
    return firstValueFrom(
      this.http.post<Loan>(`${environment.apiBaseUrl}/loans/${id}/decide`, dto),
    );
  }

  repay(id: string, dto: RepayLoanDto): Promise<Loan> {
    return firstValueFrom(this.http.post<Loan>(`${environment.apiBaseUrl}/loans/${id}/repay`, dto));
  }
}
