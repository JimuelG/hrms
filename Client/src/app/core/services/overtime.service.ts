import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { CreateOvertimeValue, OvertimeRequest } from '../../shared/models/attendance';
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
}
