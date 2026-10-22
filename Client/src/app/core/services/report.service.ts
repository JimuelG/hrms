import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { Observable } from 'rxjs';
import { AttendanceQuery, AttendanceReport, Breakdown, LeaveCalendar, LeaveUsageReport, WorkforceReport } from '../../shared/models/reports';

@Injectable({
  providedIn: 'root',
})
export class ReportService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/reports`;

  workforce(): Observable<WorkforceReport> {
    return this.http.get<WorkforceReport>(`${this.baseUrl}/workforce`);
  }

  exportWorkforce(breakdown: Breakdown): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/workforce/export`, { params: { breakdown}, responseType: 'blob' });
  }

  private params(values: Record<string, string | number | null | undefined>): Record<string, string> {
    const p: Record<string, string> = {};
    for (const [k, v] of Object.entries(values)) if (v !== null && v !== undefined && v !== '') p[k] = String(v);
    return p;
  }

  attendance(q: AttendanceQuery): Observable<AttendanceReport> {
    return this.http.get<AttendanceReport>(`${this.baseUrl}/attendance`, { params: this.params({ ...q }) });
  }

  exportAttendance(q: AttendanceQuery): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/attendance/export`, { params: this.params({ ...q }), responseType: 'blob' });
  }

  leaveUsage(year: number, departmentId: string, branchId: string): Observable<LeaveUsageReport> {
    return this.http.get<LeaveUsageReport>(`${this.baseUrl}/leave-usage`, { params: this.params({ year, departmentId, branchId }) });
  }

  exportLeaveUsage(year: number, departmentId: string, branchId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/leave-usage/export`, { params: this.params({ year, departmentId, branchId }), responseType: 'blob'});
  }

  leaveCalendar(from: string, to: string, departmentId: string, branchId: string): Observable<LeaveCalendar> {
    return this.http.get<LeaveCalendar>(`${this.baseUrl}/leave-calendar`, { params: this.params({ from, to, departmentId, branchId }) });
  }

  exportLeaveCalendar(from: string, to: string, departmentId: string, branchId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/leave-calendar/export`, { params: this.params({ from, to, departmentId, branchId }), responseType: 'blob' });
  }
}
