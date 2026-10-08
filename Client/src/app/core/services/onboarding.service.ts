import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { Observable, take } from 'rxjs';
import { ConvertToEmployeeValue, OnboardingCase } from '../../shared/models/onboarding';
import { Employee } from '../../shared/models/employee';

@Injectable({
  providedIn: 'root',
})
export class OnboardingService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/onboarding`;

  getByApplication(applicationId: string): Observable<OnboardingCase> {
    return this.http.get<OnboardingCase>(`${this.baseUrl}/by-application/${applicationId}`);
  }

  start(applicationId: string): Observable<OnboardingCase> {
    return this.http.post<OnboardingCase>(this.baseUrl, { applicationId });
  }

  completeTask(caseId: string, taskId: string): Observable<OnboardingCase> {
    return this.http.post<OnboardingCase>(`${this.baseUrl}/${caseId}/tasks/${taskId}/complete`, {});
  }

  reopenTask(caseId: string, taskId: string): Observable<OnboardingCase> {
    return this.http.post<OnboardingCase>(`${this.baseUrl}/${caseId}/tasks/${taskId}/reopen`, {});
  }

  convert(caseId: string, value: ConvertToEmployeeValue): Observable<Employee> {
    return this.http.post<Employee>(`${this.baseUrl}/${caseId}/convert`, value);
  }
}
