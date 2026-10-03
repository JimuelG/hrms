import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { Observable } from 'rxjs';
import { Employee, EmployeeFormValue } from '../../shared/models/employee';

@Injectable({
  providedIn: 'root',
})
export class EmployeeService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/employees`;

  getAll(search?: string): Observable<Employee[]> {
    const params = search ? { params: { search } } : {};
    return this.http.get<Employee[]>(this.baseUrl, params);
  }

  getById(id: string): Observable<Employee> {
    return this.http.get<Employee>(`${this.baseUrl}/${id}`);
  }

  create(value: EmployeeFormValue): Observable<Employee> {
    return this.http.post<Employee>(this.baseUrl, value);
  }

  update(id: string, value: EmployeeFormValue): Observable<Employee> {
    return this.http.put<Employee>(`${this.baseUrl}/${id}`, value);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
