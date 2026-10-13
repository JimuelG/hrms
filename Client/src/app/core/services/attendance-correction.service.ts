import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { AttendanceCorrection, CreateCorrectionValue } from '../../shared/models/attendance';
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
}
