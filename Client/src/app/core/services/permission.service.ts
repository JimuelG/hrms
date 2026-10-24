import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class PermissionService {
  private http = inject(HttpClient);
  private granted = signal<ReadonlySet<string>>(new Set());
  private inflight: Promise<void> | null = null;

  readonly loaded = signal(false);

  can(code: string): boolean {
    return this.granted().has(code);
  }

  load(): Promise<void> {
    this.inflight ??= firstValueFrom(this.http.get<string[]>(`${environment.apiUrl}/auth/permissions`))
      .then((codes) => this.granted.set(new Set(codes)))
      .catch(() => this.granted.set(new Set()))
      .finally(() => { this.loaded.set(true); this.inflight = null; })
    return this.inflight;
  }

  ensureLoaded(): Promise<void> {
    return this.loaded() ? Promise.resolve() : this.load();
  }

  clear(): void {
    this.granted.set(new Set());
    this.loaded.set(false);
  }
}
