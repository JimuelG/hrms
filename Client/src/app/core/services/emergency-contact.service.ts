import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { Observable } from 'rxjs';
import { EmergencyContact, EmergencyContactFormValue } from '../../shared/models/employee';

@Injectable({
  providedIn: 'root',
})
export class ContactService {
  private http = inject(HttpClient);
  private baseUrl = (employeeId: string) => `${environment.apiUrl}/employees/${employeeId}/emergency-contacts`;

  getForEmployee(employeeId: string): Observable<EmergencyContact[]> {
    return this.http.get<EmergencyContact[]>(this.baseUrl(employeeId));
  }

  create(employeeId: string, value: EmergencyContactFormValue): Observable<EmergencyContact> {
    return this.http.post<EmergencyContact>(this.baseUrl(employeeId), value);
  }

  update(employeeId: string, contactId: string, value: EmergencyContactFormValue): Observable<EmergencyContact> {
    return this.http.put<EmergencyContact>(`${this.baseUrl(employeeId)}/${contactId}`, value);
  }

  delete(employeeId: string, contactId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl(employeeId)}/${contactId}`);
  }
}
