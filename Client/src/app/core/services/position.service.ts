import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { Observable } from 'rxjs';
import { Position, PositionFormValue } from '../../shared/models/organization';

@Injectable({
  providedIn: 'root',
})
export class PositionService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/positions`;

  getAll(search?: string): Observable<Position[]> {
    const params = search ? { params: { search } } : {};
    return this.http.get<Position[]>(this.baseUrl, params);
  }

  getById(id: string): Observable<Position> {
    return this.http.get<Position>(`${this.baseUrl}/${id}`);
  }

  create(value: PositionFormValue): Observable<Position> {
    return this.http.post<Position>(this.baseUrl, value);
  }

  update(id: string, value: PositionFormValue): Observable<Position> {
    return this.http.put<Position>(`${this.baseUrl}/${id}`, value);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
