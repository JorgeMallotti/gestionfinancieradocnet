import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuditLogEntry, PagedResult } from '../models';

@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly http = inject(HttpClient);

  getAll(page = 1, pageSize = 50): Promise<PagedResult<AuditLogEntry>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return firstValueFrom(
      this.http.get<PagedResult<AuditLogEntry>>(`${environment.apiBaseUrl}/audit`, { params }),
    );
  }
}
