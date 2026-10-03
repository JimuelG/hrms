import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { Observable } from 'rxjs';
import { TenantSettings, UpdateTenantSettings } from '../../shared/models/tenant-settings';

@Injectable({
  providedIn: 'root',
})
export class TenantSettingsService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/tenantsettings`;

  get(): Observable<TenantSettings> {
    return this.http.get<TenantSettings>(this.baseUrl);
  }

  update(value: UpdateTenantSettings): Observable<TenantSettings> {
    return this.http.put<TenantSettings>(this.baseUrl, value);
  }
}
