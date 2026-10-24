import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { WorkLocation, WorkLocationFormValue } from '../../shared/models/attendance';

@Injectable({
  providedIn: 'root',
})
export class WorkLocationService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/worklocations`;
  private employees = `${environment.apiUrl}/employees`;

  getAll(search?: string): Observable<WorkLocation[]> {
    const params = search ? { params: { search } } : {};
    return this.http.get<WorkLocation[]>(this.baseUrl, params);
  }

  create(value: WorkLocationFormValue): Observable<WorkLocation> {
    return this.http.post<WorkLocation>(this.baseUrl, value);
  }

  update(id: string, value: WorkLocationFormValue): Observable<WorkLocation> {
    return this.http.put<WorkLocation>(`${this.baseUrl}/${id}`, value);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  getForEmployee(employeeId: string): Observable<WorkLocation[]> {
    return this.http.get<WorkLocation[]>(`${this.employees}/${employeeId}/work-locations`);
  }

  setForEmployee(employeeId: string, workLocationIds: string[]): Observable<WorkLocation[]> {
    return this.http.put<WorkLocation[]>(`${this.employees}/${employeeId}/work-locations`, { workLocationIds });
  }
}
