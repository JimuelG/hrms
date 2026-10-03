import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { Observable } from 'rxjs';
import { EmployeeDocument } from '../../shared/models/employee';

@Injectable({
  providedIn: 'root',
})
export class EmployeeDocumentService {
  private http = inject(HttpClient);
  private baseUrl = (employeeId: string) => `${environment.apiUrl}/employees/${employeeId}/documents`;

  getForEmployee(employeeId: string): Observable<EmployeeDocument[]> {
    return this.http.get<EmployeeDocument[]>(this.baseUrl(employeeId));
  }

  upload(employeeId: string, documentType: string, file: File, expirationDate?: string): Observable<EmployeeDocument> {
    const form = new FormData();
    form.append('documentType', documentType);
    form.append('file', file);
    if (expirationDate) form.append('expirationDate', expirationDate);
    return this.http.post<EmployeeDocument>(this.baseUrl(employeeId), form);
  }

  download(employeeId: string, documentId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl(employeeId)}/${documentId}/download`, { responseType: 'blob' });
  }

  verify(employeeId: string, documentId: string, status: number, rejectionReason?: string): Observable<EmployeeDocument> {
    return this.http.post<EmployeeDocument>(`${this.baseUrl(employeeId)}/${documentId}/verify`, { status, rejectionReason });
  }

  delete(employeeId: string, documentId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl(employeeId)}/${documentId}`);
  }
}
