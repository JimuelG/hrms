import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { Observable } from 'rxjs';
import { AddTimelimeNoteValue, TimelineEvent } from '../../shared/models/employee';

@Injectable({
  providedIn: 'root',
})
export class TimelineService {
  private http = inject(HttpClient);
  private baseUrl = (employeeId: string) => `${environment.apiUrl}/employees/${employeeId}/timeline`;

  getForEmployee(employeeId: string): Observable<TimelineEvent[]> {
    return this.http.get<TimelineEvent[]>(this.baseUrl(employeeId));
  }

  addNote(employeeId: string, value: AddTimelimeNoteValue): Observable<void> {
    return this.http.post<void>(`${this.baseUrl(employeeId)}/notes`, value);
  }
}
