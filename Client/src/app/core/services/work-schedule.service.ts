import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { WorkSchedule, WorkScheduleFormValue } from '../../shared/models/attendance';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root',
})
export class WorkScheduleService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/workschedules`;

  getAll(search?: string): Observable<WorkSchedule[]> {
    const params = search ? { params: { search } } : {};
    return this.http.get<WorkSchedule[]>(this.baseUrl, params);
  }

  create(value: WorkScheduleFormValue): Observable<WorkSchedule> {
    return this.http.post<WorkSchedule>(this.baseUrl, value);
  }

  update(id: string, value: WorkScheduleFormValue): Observable<WorkSchedule> {
    return this.http.put<WorkSchedule>(`${this.baseUrl}/${id}`, value);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
