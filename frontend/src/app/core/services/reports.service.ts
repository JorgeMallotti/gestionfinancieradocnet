import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { EmailReportDto } from '../models';

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly http = inject(HttpClient);

  /**
   * Downloads a generated file as a Blob (browser saves it natively).
   * The server generates the bytes; no client-side generation libraries.
   */
  downloadPdf(from?: string, to?: string): Promise<Blob> {
    return firstValueFrom(
      this.http.get(`${environment.apiBaseUrl}/reports/pdf`, {
        params: this.rangeParams(from, to),
        responseType: 'blob',
      }),
    );
  }

  downloadExcel(from?: string, to?: string): Promise<Blob> {
    return firstValueFrom(
      this.http.get(`${environment.apiBaseUrl}/reports/excel`, {
        params: this.rangeParams(from, to),
        responseType: 'blob',
      }),
    );
  }

  sendByEmail(dto: EmailReportDto): Promise<{ message: string }> {
    return firstValueFrom(
      this.http.post<{ message: string }>(`${environment.apiBaseUrl}/reports/email`, dto),
    );
  }

  private rangeParams(from?: string, to?: string): HttpParams {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return params;
  }
}
