import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { AttendanceRecord, ClockRequest, MyWorkLocation } from '../../shared/models/attendance';
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
}
