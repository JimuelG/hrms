import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { Observable } from 'rxjs';
import { JobPosting, JobPostingFormValue, JobPostingStatus } from '../../shared/models/recruitment';

@Injectable({
  providedIn: 'root',
})
export class JobPostingService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/jobpostings`;

  getAll(search?: string, status?: JobPostingStatus): Observable<JobPosting[]> {
    const params: Record<string, string> = {};

    if (search) params['search'] = search;
    if (status) params['status'] = String(status);

    return this.http.get<JobPosting[]>(this.baseUrl, { params });
  }

  getById(id: string): Observable<JobPosting> {
    return this.http.get<JobPosting>(`${this.baseUrl}/${id}`);
  }

  create(value: JobPostingFormValue): Observable<JobPosting> {
    return this.http.post<JobPosting>(this.baseUrl, value);
  }

  update(id: string, value: JobPostingFormValue): Observable<JobPosting> {
    return this.http.put<JobPosting>(`${this.baseUrl}/${id}`, value);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`)
  }
}
