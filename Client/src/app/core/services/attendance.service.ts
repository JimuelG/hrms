import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { AttendanceQuery, AttendanceRecord, ClockRequest, MyWorkLocation, PagedResult } from '../../shared/models/attendance';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class AttendanceService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/attendance`;

  clockIn(body: ClockRequest): Observable<AttendanceRecord> {
    return this.http.post<AttendanceRecord>(`${this.baseUrl}/clock-in`, body);
  }

  clockOut(body: ClockRequest): Observable<AttendanceRecord> {
    return this.http.post<AttendanceRecord>(`${this.baseUrl}/clock-out`, body);
  }

  getMine(from?: string, to?: string): Observable<AttendanceRecord[]> {
    const params: Record<string, string> = {};
    if (from) params['from'] = from;
    if (to) params['to'] = to;
    return this.http.get<AttendanceRecord[]>(`${this.baseUrl}/me`, { params });
  }

  getMyLocations(): Observable<MyWorkLocation[]> {
    return this.http.get<MyWorkLocation[]>(`${this.baseUrl}/my-locations`);
  }

  query(q: AttendanceQuery): Observable<PagedResult<AttendanceRecord>> {
    const params: Record<string, string> = {
      from: q.from,
      to: q.to,
      page: String(q.page ?? 1),
      pageSize: String(q.pageSize ?? 25),
      needsReview: String(!!q.needsReview)
    };

    if (q.employeeId) params['employeeId'] = q.employeeId;
    return this.http.get<PagedResult<AttendanceRecord>>(this.baseUrl, { params });
  }
}
