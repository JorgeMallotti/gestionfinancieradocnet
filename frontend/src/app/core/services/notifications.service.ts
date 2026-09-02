import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AppNotification } from '../models';

/**
 * The caller's own notifications (the bell). Backed by
 * GET /api/notifications, GET /api/notifications/unread-count and the two
 * read mutations. All endpoints are scoped by the JWT — no IDs in the body.
 */
@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private readonly http = inject(HttpClient);

  getMine(unreadOnly = false, limit = 30): Promise<AppNotification[]> {
    const params = new HttpParams()
      .set('unreadOnly', String(unreadOnly))
      .set('limit', String(limit));
    return firstValueFrom(
      this.http.get<AppNotification[]>(`${environment.apiBaseUrl}/notifications`, { params }),
    );
  }

  getUnreadCount(): Promise<number> {
    return firstValueFrom(
      this.http.get<number>(`${environment.apiBaseUrl}/notifications/unread-count`),
    );
  }

  markRead(id: string): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`${environment.apiBaseUrl}/notifications/${id}/read`, null),
    );
  }

  markAllRead(): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`${environment.apiBaseUrl}/notifications/read-all`, null),
    );
  }
}
