import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { AuditFacets, AuditFilters, AuditPage } from '../../shared/models/audit';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class AuditService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/audit`;

  search(f: AuditFilters, page: number, pageSize: number): Observable<AuditPage> {
    const params: Record<string, string> = { page: String(page), pageSize: String(pageSize) };
    for (const [k, v] of Object.entries(f)) if (v) params[k] = v;
    return this.http.get<AuditPage>(this.baseUrl, { params });
  }

  facets(): Observable<AuditFacets> {
    return this.http.get<AuditFacets>(`${this.baseUrl}/facets`);
  }
}
