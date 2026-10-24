import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';

export interface LoginAccount {
  hasLogin: boolean;
  email: string | null;
  isActive: boolean;
  mustChangePassword: boolean;
  alsoInOtherCompanies: boolean;
}

export interface IssuedLogin {
  email: string;
  temporaryPassword: string | null;
  expiresAtUtc: string | null;
  existingAccount: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class LoginAccountService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/loginaccounts/employee`;

  get(employeeId: string): Observable<LoginAccount> {
    return this.http.get<LoginAccount>(`${this.baseUrl}/${employeeId}`);
  }

  create(employeeId: string, email: string): Observable<IssuedLogin> {
    return this.http.post<IssuedLogin>(`${this.baseUrl}/${employeeId}`, { email });
  }

  reset(employeeId: string): Observable<IssuedLogin> {
    return this.http.post<IssuedLogin>(`${this.baseUrl}/${employeeId}/reset-password`, {});
  }
}
