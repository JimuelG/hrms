import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { Observable } from 'rxjs';
import { CreateTenantValue, PlatformTenant } from '../../shared/models/platforrm-tenant';

@Injectable({
  providedIn: 'root',
})
export class PlatformTenantService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/platform/tenants`;

  getAll(): Observable<PlatformTenant[]> {
    return this.http.get<PlatformTenant[]>(this.baseUrl);
  }

  create(value: CreateTenantValue): Observable<PlatformTenant> {
    return this.http.post<PlatformTenant>(this.baseUrl, value);
  }

  suspend(id: string): Observable<PlatformTenant> {
    return this.http.post<PlatformTenant>(`${this.baseUrl}/${id}/suspend`, {});
  }

  reactivate(id: string): Observable<PlatformTenant> {
    return this.http.post<PlatformTenant>(`${this.baseUrl}/${id}/reactivate`, {});
  }
}
