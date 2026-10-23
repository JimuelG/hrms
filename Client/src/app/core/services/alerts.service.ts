import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { Observable } from 'rxjs';
import { Alert } from '../../shared/models/alerts';

@Injectable({
  providedIn: 'root',
})
export class AlertsService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/alerts`;

  list(): Observable<Alert[]> {
    return this.http.get<Alert[]>(this.baseUrl);
  }

  scan(): Observable<{ changed: number}> {
    return this.http.post<{ changed: number}>(`${this.baseUrl}/scan`, {});
  }
}
