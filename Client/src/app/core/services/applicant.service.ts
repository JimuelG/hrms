import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { Observable } from 'rxjs';
import { Applicant, ApplicantFormValue } from '../../shared/models/recruitment';
import { TimelineEvent } from '../../shared/models/employee';

@Injectable({
  providedIn: 'root',
})
export class ApplicantService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/applicants`;

  getAll(search?: string): Observable<Applicant[]> {
    const params = search ? { params: { search } } : {};

    return this.http.get<Applicant[]>(this.baseUrl, params);
  }

  getById(id: string): Observable<Applicant> {
    return this.http.get<Applicant>(`${this.baseUrl}/${id}`);
  }

  create(value: ApplicantFormValue): Observable<Applicant> {
    return this.http.post<Applicant>(this.baseUrl, value)
  }

  update(id: string, value: ApplicantFormValue): Observable<Applicant> {
    return this.http.put<Applicant>(`${this.baseUrl}/${id}`, value);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  uploadResume(id: string, file: File): Observable<Applicant> {
    const form = new FormData();
    form.append('file', file);
    
    return this.http.post<Applicant>(`${this.baseUrl}/${id}/resume`, form);
  }

  downloadResume(id: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/${id}/resume`, { responseType: 'blob' })
  }

  getTimeline(id: string): Observable<TimelineEvent[]> {
    return this.http.get<TimelineEvent[]>(`${this.baseUrl}/${id}/timeline`);
  }

  addTimelineNote(id: string, title: string, description?: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/timeline/notes`, { title, description });
  }
}
