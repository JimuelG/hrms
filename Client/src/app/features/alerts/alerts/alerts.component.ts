import { CommonModule, DatePipe } from '@angular/common';
import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AlertsService } from '../../../core/services/alerts.service';
import { PermissionService } from '../../../core/services/permission.service';
import { PERMISSIONS } from '../../../shared/constants/permissions';
import { Alert, ALERT_KIND_LABELS, AlertKind } from '../../../shared/models/alerts';
import { firstValueFrom } from 'rxjs';
import { errorMessage } from '../../../shared/utils/api-error.util';

@Component({
  selector: 'app-alerts',
  imports: [
    CommonModule,
    RouterLink,
    DatePipe
  ],
  templateUrl: './alerts.component.html',
  styleUrl: './alerts.component.scss',
})
export class AlertsComponent implements OnInit, OnDestroy {
  private service = inject(AlertsService);
  readonly permissions = inject(PermissionService);
  readonly P = PERMISSIONS;
  readonly Kind = AlertKind;
  readonly kinds = [AlertKind.StaleApproval, AlertKind.DocumentExpiry, AlertKind.OpenAttendance];
  readonly kindLabel = ALERT_KIND_LABELS;

  alerts = signal<Alert[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  scanning = signal(false);
  notice = signal<string | null>(null);
  private timer: ReturnType<typeof setInterval> | null = null;

  total = computed(() => this.alerts().length);

  ngOnInit(): void {
    void this.load();
    this.timer = setInterval(() => void this.load(), 5 * 60_000);
  }

  ngOnDestroy(): void {
    if (this.timer) clearInterval(this.timer);
  }

  async load(): Promise<void> {
    try {
      this.alerts.set(await firstValueFrom(this.service.list()));
      this.error.set(null);
    } catch (err) {
      this.error.set(errorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  of(kind: AlertKind): Alert[] {
    return this.alerts().filter((a) => a.kind === kind);
  }

  async scan(): Promise<void> {
    this.scanning.set(true);
    this.notice.set(null);
    try {
      const r = await firstValueFrom(this.service.scan());
      this.notice.set(r.changed === 0 ? 'Scan finished. Nothing changed.' : `Scan finished. ${r.changed} alert(s) added or cleared.`);
      await this.load();
    } catch (err) {
      this.error.set(errorMessage(err));
    } finally {
      this.scanning.set(false);
    }
  }

  link(a: Alert): string[] | null {
    if (a.kind === AlertKind.StaleApproval) return ['/leave/approvals'];
    return a.subjectId ? ['/employees', a.subjectId] : null;
  }

  linkLabel(a: Alert): string {
    return a.kind === AlertKind.StaleApproval ? 'Open approvals' : 'Open employee';
  }
}
