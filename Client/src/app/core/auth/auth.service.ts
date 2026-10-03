import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { LoginRequest, LoginResponse, MeResponse } from '../../shared/models/auth';
import { catchError, map, Observable, of, tap } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  private accessToken: string | null = null;
  private accessTokenExpiresAtUtc: string | null = null;

  readonly isAuthenticated = signal(false);
  readonly tenantId = signal<string | null>(null);
  readonly isPlatformAdmin = signal(false);

  getAccessToken(): string | null {
    return this.accessToken;
  }

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.baseUrl}/auth/login`, request, { withCredentials: true })
      .pipe(
        tap((res) => {
          if (res.accessToken) {
            this.setSession(res.accessToken, res.accessTokenExpiresAtUtc);
          }
        })
      );
  }

  refresh(): Observable<LoginResponse | null> {
    return this.http.post<LoginResponse>(`${this.baseUrl}/auth/refresh`, {}, { withCredentials: true})
      .pipe(
        tap((res) => {
          if (res.accessToken) this.setSession(res.accessToken, res.accessTokenExpiresAtUtc);
        }),
        catchError(() => {
          this.clearSession();
          return of(null);
        })
      )
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/auth/logout`, {}, { withCredentials: true})
      .pipe(
        tap(() => this.clearSession()),
        catchError(() => {
          this.clearSession();
          return of(void 0);
        })
      );
  }

  me(): Observable<MeResponse> {
    return this.http.get<MeResponse>(`${this.baseUrl}/auth/me`);
  }

  trySilentRefresh(): Observable<boolean> {
    return this.refresh().pipe(map((res) => res !== null && !!res.accessToken));
  }

  private setSession(token: string, expiresAtUtc: string | null): void {
    this.accessToken= token;
    this.accessTokenExpiresAtUtc = expiresAtUtc;
    this.isAuthenticated.set(true);

    const claims = this.decodeClaims(token);
    this.tenantId.set(claims.tenantId);
    this.isPlatformAdmin.set(claims.isPlatformAdmin);
  }

  private clearSession(): void {
    this.accessToken = null;
    this.accessTokenExpiresAtUtc = null;
    this.isAuthenticated.set(false);
    this.tenantId.set(null);
    this.isPlatformAdmin.set(false);
  }

  private decodeClaims(token: string): { tenantId: string | null; isPlatformAdmin: boolean} {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return {
        tenantId: payload['tenant_id'] ?? null,
        isPlatformAdmin: payload['platform_admin'] === 'true'
      };
    } catch {
      return { tenantId: null, isPlatformAdmin: false };
    }
  }
}
