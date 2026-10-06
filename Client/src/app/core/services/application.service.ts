import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { ApplicationItem, ApplicationStatus } from '../../shared/models/recruitment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ApplicationService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/applications`;

  getByPosting(jobPostingId: string, status?: ApplicationStatus): Observable<ApplicationItem[]> {
    const params = status ? { params: { status: String(status) } } : {};

    return this.http.get<ApplicationItem[]>(`${this.baseUrl}/by-posting/${jobPostingId}`, params);
  }

  getByApplicant(applicantId: string): Observable<ApplicationItem[]> {
    return this.http.get<ApplicationItem[]>(`${this.baseUrl}/by-applicant/${applicantId}`);
  }

  getById(id: string): Observable<ApplicationItem> {
    return this.http.get<ApplicationItem>(`${this.baseUrl}/${id}`);
  }

  create(applicantId: string, jobPostingId: string, notes?: string): Observable<ApplicationItem> {
    return this.http.post<ApplicationItem>(this.baseUrl, { applicantId, jobPostingId, notes });
  }

  updateStatus(id: string, status: ApplicationStatus, notes?: string): Observable<ApplicationItem> {
    return this.http.put<ApplicationItem>(`${this.baseUrl}/${id}/status`, { status, notes });
  }
}
