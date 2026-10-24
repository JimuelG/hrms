import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { ApprovalStatus, CreateOvertimeValue, OvertimeRequest, PagedResult } from '../../shared/models/attendance';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class OvertimeService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/overtimerequests`;

  create(value: CreateOvertimeValue): Observable<OvertimeRequest> {
    return this.http.post<OvertimeRequest>(this.baseUrl, value);
  }

  getMine(): Observable<OvertimeRequest[]> {
    return this.http.get<OvertimeRequest[]>(`${this.baseUrl}/me`);
  }

  cancel(id: string): Observable<OvertimeRequest> {
    return this.http.post<OvertimeRequest>(`${this.baseUrl}/${id}/cancel`, {});
  }

  query(status: ApprovalStatus | undefined, employeeId: string | undefined, page: number, pageSize: number) : Observable<PagedResult<OvertimeRequest>> {
    const params: Record<string, string> = { page: String(page), pageSize: String(pageSize) };

    if (status) params['status'] = String(status);
    if (employeeId) params['employeeId'] = employeeId;

    return this.http.get<PagedResult<OvertimeRequest>>(this.baseUrl, { params });
  }

  approve(id: string, approvedMinutes?: number, note?: string): Observable<OvertimeRequest> {
    return this.http.post<OvertimeRequest>(`${this.baseUrl}/${id}/approve`, { approvedMinutes, note });
  }

  reject(id: string, note: string): Observable<OvertimeRequest> {
    return this.http.post<OvertimeRequest>(`${this.baseUrl}/${id}/reject`, { note });
  }
}
