import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Claim, ClaimStatus, OpenClaimDto, ProposeCorrectionDto } from '../models';

/**
 * Claims (reclamaciones): a client disputes a movement it is part of → the
 * Admin (mediator) proposes a corrective transfer → both parties consent →
 * the corrective movement is executed and stacked on the ledger.
 */
@Injectable({ providedIn: 'root' })
export class ClaimsService {
  private readonly http = inject(HttpClient);

  getMine(): Promise<Claim[]> {
    return firstValueFrom(this.http.get<Claim[]>(`${environment.apiBaseUrl}/claims/mine`));
  }

  getAll(status?: ClaimStatus): Promise<Claim[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return firstValueFrom(this.http.get<Claim[]>(`${environment.apiBaseUrl}/claims`, { params }));
  }

  open(dto: OpenClaimDto): Promise<Claim> {
    return firstValueFrom(this.http.post<Claim>(`${environment.apiBaseUrl}/claims`, dto));
  }

  propose(id: string, dto: ProposeCorrectionDto): Promise<Claim> {
    return firstValueFrom(
      this.http.post<Claim>(`${environment.apiBaseUrl}/claims/${id}/propose`, dto),
    );
  }

  consent(id: string, approve: boolean): Promise<Claim> {
    return firstValueFrom(
      this.http.post<Claim>(`${environment.apiBaseUrl}/claims/${id}/consent`, null, {
        params: new HttpParams().set('approve', String(approve)),
      }),
    );
  }
}
