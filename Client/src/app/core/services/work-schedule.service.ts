import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { Observable } from 'rxjs';
import { WorkSchedule } from '../../shared/models/attendance';
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
}
