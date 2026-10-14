import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { ApprovalStatus, AttendanceCorrection, CreateCorrectionValue, PagedResult } from '../../shared/models/attendance';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class AttendanceCorrectionService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/attendancecorrections`;

  create(value: CreateCorrectionValue): Observable<AttendanceCorrection> {
    return this.http.post<AttendanceCorrection>(this.baseUrl, value);
  }

  getMine(): Observable<AttendanceCorrection[]> {
    return this.http.get<AttendanceCorrection[]>(`${this.baseUrl}/me`);
  }

  cancel(id: string): Observable<AttendanceCorrection> {
    return this.http.post<AttendanceCorrection>(`${this.baseUrl}/${id}/cancel`, {});
  }

  query(status: ApprovalStatus | undefined, employeeId: string | undefined, page: number, pageSize: number) : Observable<PagedResult<AttendanceCorrection>> {
    const params: Record<string, string> = { page: String(page), pageSize: String(pageSize) };
    if (status) params['status'] = String(status);
    if (employeeId) params['employeeId'] = employeeId;
    return this.http.get<PagedResult<AttendanceCorrection>>(this.baseUrl, { params });
  }

  approve(id: string, note?: string): Observable<AttendanceCorrection> {
    return this.http.post<AttendanceCorrection>(`${this.baseUrl}/${id}/approve`, { note });
  }

  reject(id: string, note: string): Observable<AttendanceCorrection> {
    return this.http.post<AttendanceCorrection>(`${this.baseUrl}/${id}/reject`, { note });
  }

  createForEmployee(employeeId: string, value: CreateCorrectionValue): Observable<AttendanceCorrection> {
    return this.http.post<AttendanceCorrection>(`${this.baseUrl}/for-emplyoee/${employeeId}`, value);
  }
}
