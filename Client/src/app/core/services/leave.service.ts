import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { CreateLeaveRequestValve, DayPortion, LeaveAttachment, LeaveBalance, LeaveBalances, LeaveFilingResult, LeavePreview, LeaveRequest, LeaveType, LedgerEntry } from '../../shared/models/leave';
import { ApprovalStatus, PagedResult } from '../../shared/models/attendance';

@Injectable({
  providedIn: 'root',
})
export class LeaveService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/leave`;

  getTypes(): Observable<LeaveType[]> {
    return this.http.get<LeaveType[]>(`${environment.apiUrl}/leavetypes`, { params: { activeOnly: 'true'} });
  }

  getMyBalances(year?: number): Observable<LeaveBalances> {
    return this.http.get<LeaveBalances>(`${this.baseUrl}/balances/me`, { params: year ? { year: String(year)} : {} });
  }

  getMyLedger(year: number, leaveTypeId?: string): Observable<LedgerEntry[]> {
    const params: Record<string, string> = { year: String(year) };
    if (leaveTypeId) params['leaveTypeId'] = leaveTypeId;
    return this.http.get<LedgerEntry[]>(`${this.baseUrl}/balances/me/ledger`, { params });
  }

  preview(typeId: string, start: string, end: string, sp: DayPortion, ep: DayPortion): Observable<LeavePreview> {
    return this.http.get<LeavePreview>(`${this.baseUrl}/preview`, {
      params: { leaveTypeId: typeId, startDate: start, endDate: end, startPortion: String(sp), endPortion: String(ep) }
    })
  }

  file(value: CreateLeaveRequestValve): Observable<LeaveFilingResult> {
    return this.http.post<LeaveFilingResult>(`${this.baseUrl}/requests`, value);
  }

  getMyRequests(page = 1, pageSize = 2): Observable<PagedResult<LeaveRequest>> {
    return this.http.get<PagedResult<LeaveRequest>>(`${this.baseUrl}/requests/me`, {
      params: { page: String(page), pageSize: String(pageSize) }
    });
  }

  cancel(id: string, reason?: string): Observable<LeaveRequest> {
    return this.http.post<LeaveRequest>(`${this.baseUrl}/requests/${id}/cancel`, { reason });
  }

  getDocuments(id: string): Observable<LeaveAttachment[]> {
    return this.http.get<LeaveAttachment[]>(`${this.baseUrl}/requests/${id}/documents`);
  }

  uploadDocument(id: string, file: File): Observable<LeaveAttachment> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<LeaveAttachment>(`${this.baseUrl}/requests/${id}/documents`, form);
  }

  downloadDocument(id: string, attachmentId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/requests/${id}/documents/${attachmentId}`, { responseType: 'blob'});
  }

  deleteDocument(id: string, attachmentId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/requests/${id}/documents/${attachmentId}`);
  }

  getRequest(id: string): Observable<LeaveRequest> {
    return this.http.get<LeaveRequest>(`${this.baseUrl}/requests/${id}`);
  }

  queryRequests(q: { status?: ApprovalStatus; employeeId?: string; from?: string; to?: string; page: number; pageSize: number }): Observable<PagedResult<LeaveRequest>> {
    const params: Record<string, string> = { page: String(q.page), pageSize: String(q.pageSize) };
    if (q.status) params['status'] = String(q.status);
    if (q.employeeId) params['employeeId'] = q.employeeId;
    if (q.from) params['from'] = q.from;
    if (q.to) params['to'] = q.to;

    return this.http.get<PagedResult<LeaveRequest>>(`${this.baseUrl}/requests`, { params });
  }
}
