import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { Holiday, HolidayFormValue, LeaveBalances, LeaveType, LeaveTypeFormValue, LedgerEntry, PostLedgerValue } from '../../shared/models/leave';

@Injectable({
  providedIn: 'root',
})
export class LeaveAdminService {
  private http = inject(HttpClient);
  private types = `${environment.apiUrl}/leavetypes`;
  private holidays = `${environment.apiUrl}/holidays`;
  private leave = `${environment.apiUrl}/leave`;

  getTypes(): Observable<LeaveType[]> {
    return this.http.get<LeaveType[]>(this.types);
  }

  createType(v: LeaveTypeFormValue): Observable<LeaveType> {
    return this.http.post<LeaveType>(this.types, v);
  }

  updateType(id: string, v: LeaveTypeFormValue): Observable<LeaveType> {
    return this.http.put<LeaveType>(`${this.types}/${id}`, v);
  }

  deleteType(id: string): Observable<void> {
    return this.http.delete<void>(`${this.types}/${id}`);
  }

  getHolidays(year: number): Observable<Holiday[]> {
    return this.http.get<Holiday[]>(this.holidays, { params: { year: String(year) } });
  }

  createHoliday(v: HolidayFormValue): Observable<Holiday> {
    return this.http.post<Holiday>(this.holidays, v);
  }

  updateHoliday(id: string, v: HolidayFormValue): Observable<Holiday> {
    return this.http.put<Holiday>(`${this.holidays}/${id}`, v);
  }

  deleteHoliday(id: string): Observable<void> {
    return this.http.delete<void>(`${this.holidays}/${id}`);
  }

  getEmployeeBalances(employeeId: string, year: number): Observable<LeaveBalances> {
    return this.http.get<LeaveBalances>(`${this.leave}/balances/employee/${employeeId}`, { params: { year: String(year) } });
  }

  getEmployeeLedger(employeeId: string, year: number, leaveTypeId: string): Observable<LedgerEntry[]> {
    return this.http.get<LedgerEntry[]>(`${this.leave}/balances/employee/${employeeId}/ledger`, { params: { year: String(year), leaveTypeId } });
  }

  grant(v: PostLedgerValue): Observable<unknown> {
    return this.http.post(`${this.leave}/balances/grant`, v);
  }

  adjust(v: PostLedgerValue): Observable<unknown> {
    return this.http.post(`${this.leave}/balances/adjust`, v);
  }
}
