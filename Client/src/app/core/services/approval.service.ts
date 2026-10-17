import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { Observable } from 'rxjs';
import { PagedResult } from '../../shared/models/attendance';
import { ApprovalDetail, ApprovalSummary } from '../../shared/models/approval';

@Injectable({
  providedIn: 'root',
})
export class ApprovalService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/approvals`;

  inbox(page: number, pageSize: number): Observable<PagedResult<ApprovalSummary>> {
    return this.http.get<PagedResult<ApprovalSummary>>(`${this.baseUrl}/inbox`, { params: { page: String(page), pageSize: String(pageSize) } });
  }

  get(id: string): Observable<ApprovalDetail> {
    return this.http.get<ApprovalDetail>(`${this.baseUrl}/${id}`);
  }

  approve(id: string, note?: string): Observable<ApprovalDetail> {
    return this.http.post<ApprovalDetail>(`${this.baseUrl}/${id}/approve`, { note });
  }

  reject(id: string, note: string): Observable<ApprovalDetail> {
    return this.http.post<ApprovalDetail>(`${this.baseUrl}/${id}/reject`, { note });
  }

  comment(id: string, note: string): Observable<ApprovalDetail> {
    return this.http.post<ApprovalDetail>(`${this.baseUrl}/${id}/comments`, { note }); 
  }
}
