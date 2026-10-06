import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { Observable } from 'rxjs';
import { CreateJobOfferValue, JobOffer } from '../../shared/models/recruitment';

@Injectable({
  providedIn: 'root',
})
export class JobOfferService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/joboffers`;

  getByApplication(applicationId: string): Observable<JobOffer[]> {
    return this.http.get<JobOffer[]>(`${this.baseUrl}/by-application/${applicationId}`);
  }

  create(value: CreateJobOfferValue): Observable<JobOffer> {
    return this.http.post<JobOffer>(this.baseUrl, value);
  }

  send(id: string): Observable<JobOffer> {
    return this.http.post<JobOffer>(`${this.baseUrl}/${id}/send`, {});
  }

  accept(id: string): Observable<JobOffer> {
    return this.http.post<JobOffer>(`${this.baseUrl}/${id}/accept`, {});
  }

  decline(id: string, declineReason?: string): Observable<JobOffer> {
    return this.http.post<JobOffer>(`${this.baseUrl}/${id}/decline`, { declineReason });
  }

  withdraw(id: string): Observable<JobOffer> {
    return this.http.post<JobOffer>(`${this.baseUrl}/${id}/withdraw`, {});
  }
}
