import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { Observable } from 'rxjs';
import { OnboardingTaskTemplate, OnboardingTaskTemplateFormValue } from '../../shared/models/onboarding';

@Injectable({
  providedIn: 'root',
})
export class OnboardingTaskTemplateService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/onboardingtasktemplates`;

  getAll(): Observable<OnboardingTaskTemplate[]> {
    return this.http.get<OnboardingTaskTemplate[]>(this.baseUrl);
  }

  create(value: OnboardingTaskTemplateFormValue): Observable<OnboardingTaskTemplate> {
    return this.http.post<OnboardingTaskTemplate>(this.baseUrl, value);
  }

  update(id: string, value: OnboardingTaskTemplateFormValue): Observable<OnboardingTaskTemplate> {
    return this.http.put<OnboardingTaskTemplate>(`${this.baseUrl}/${id}`, value);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
