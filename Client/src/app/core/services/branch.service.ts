import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { Observable } from 'rxjs';
import { Branch, BranchFormValue } from '../../shared/models/organization';

@Injectable({
  providedIn: 'root',
})
export class BranchService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/branches`;

  getAll(search?: string): Observable<Branch[]> {
    const params = search ? { params: { search } } : {};
    return this.http.get<Branch[]>(this.baseUrl, params);
  }

  getById(id: string): Observable<Branch> {
    return this.http.get<Branch>(`${this.baseUrl}/${id}`);
  }

  create(value: BranchFormValue): Observable<Branch> {
    return this.http.post<Branch>(this.baseUrl, value);
  }

  update(id: string, value: BranchFormValue): Observable<Branch> {
    return this.http.put<Branch>(`${this.baseUrl}/${id}`, value);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
