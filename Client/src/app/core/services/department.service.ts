import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { Observable } from 'rxjs';
import { Department, DepartmentFormValue } from '../../shared/models/organization';

@Injectable({
  providedIn: 'root',
})
export class DepartmentService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/departments`;

  getAll(search?: string): Observable<Department[]> {
    const params = search ? { params: { search } } : {};
    return this.http.get<Department[]>(this.baseUrl, params);
  }

  getById(id: string): Observable<Department> {
    return this.http.get<Department>(`${this.baseUrl}/${id}`);
  }

  create(value: DepartmentFormValue): Observable<Department> {
    return this.http.post<Department>(this.baseUrl, value);
  }

  update(id: string, value: DepartmentFormValue): Observable<Department> {
    return this.http.put<Department>(`${this.baseUrl}/${id}`, value);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
