import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { MeResponse } from '../../shared/models/auth';
import { AuthService } from '../../core/auth/auth.service';
import { DashboardService } from '../../core/services/dashboard.service';
import { Away, Dashboard, PRESENCE_LABELS } from '../../shared/models/dashboard';
import { shortDate } from '../../shared/utils/leave.util';
import { firstValueFrom } from 'rxjs';
import { errorMessage } from '../../shared/utils/api-error.util';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-dashboard',
  imports: [
    CommonModule,
    RouterLink
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit, OnDestroy {
  private service = inject(DashboardService);
  readonly presence = PRESENCE_LABELS;
  readonly shortDate = shortDate;

  data = signal<Dashboard | null>(null);
  error = signal<string | null>(null);
  private timer: ReturnType<typeof setInterval> | null = null;

  ngOnInit(): void {
    void this.load();
    this.timer = setInterval(() => void this.load(), 60_000);
  }

  ngOnDestroy(): void {
    if (this.timer) clearInterval(this.timer);
  }

  async load(): Promise<void> {
    try {
      this.data.set(await firstValueFrom(this.service.get()));
      this.error.set(null);
    }
    catch (err) {
      this.error.set(errorMessage(err));
    }
  }

  range(a: Away): string {
    return a.from === a.to ? shortDate(a.from) : `${shortDate(a.from)} to ${shortDate(a.to)}`;
  }

  scopeLabel(d: Dashboard): string {
    return d.scope === 'Company' ? 'Company overview' : d.scope === 'Team' ? 'Your team' : 'Your day';
  }
  
}
