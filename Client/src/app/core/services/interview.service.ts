import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { Observable } from 'rxjs';
import { Interview, ScheduleInterviewValue, SubmitEvaluationValue } from '../../shared/models/recruitment';

@Injectable({
  providedIn: 'root',
})
export class InterviewService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/interviews`;

  getByApplication(applicationId: string): Observable<Interview[]> {
    return this.http.get<Interview[]>(`${this.baseUrl}/by-application/${applicationId}`);
  }

  schedule(value: ScheduleInterviewValue): Observable<Interview> {
    return this.http.post<Interview>(this.baseUrl, value);
  }

  reschedule(id: string, scheduledAtUtc: string, durationMinutes: number, location?: string): Observable<Interview> {
    return this.http.put<Interview>(`${this.baseUrl}/${id}/reschedule`, { scheduledAtUtc, durationMinutes, location });
  }

  cancel(id: string, cancellationReason: string): Observable<Interview> {
    return this.http.post<Interview>(`${this.baseUrl}/${id}/cancel`, { cancellationReason });
  }

  complete(id: string): Observable<Interview> {
    return this.http.post<Interview>(`${this.baseUrl}/${id}/complete`, {});
  }

  submitEvaluation(id: string, value: SubmitEvaluationValue): Observable<Interview> {
    return this.http.post<Interview>(`${this.baseUrl}/${id}/evaluation`, value);
  }
}
