import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { IdentifierAccess, IdentifierField, Identifiers, IdentifiersUpdate } from '../../shared/models/identifiers';

@Injectable({
  providedIn: 'root',
})
export class IdentifierService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/identifiers`;

  get(employeeId: string): Observable<Identifiers> {
    return this.http.get<Identifiers>(`${this.baseUrl}/employee/${employeeId}`);
  }

  save(employeeId: string, v: IdentifiersUpdate): Observable<Identifiers> {
    return this.http.put<Identifiers>(`${this.baseUrl}/employee/${employeeId}`, v);
  }

  reveal(employeeId: string, field: IdentifierField): Observable<{ field: number; value: string}> {
    return this.http.post<{ field: number; value: string}>(`${this.baseUrl}/employee/${employeeId}/reveal`, { field });
  }

  accessLog(employeeId: string): Observable<IdentifierAccess[]> {
    return this.http.get<IdentifierAccess[]>(`${this.baseUrl}/employee/${employeeId}/access-log`);
  }
}
