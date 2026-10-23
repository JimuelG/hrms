import { CommonModule, DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuditService } from '../../../core/services/audit.service';
import { AuditEntry, AuditFacets, AuditFilters, AuditPage } from '../../../shared/models/audit';
import { firstValueFrom } from 'rxjs';
import { errorMessage } from '../../../shared/utils/api-error.util';

@Component({
  selector: 'app-audit-log',
  imports: [
    CommonModule,
    FormsModule,
    DatePipe
  ],
  templateUrl: './audit-log.component.html',
  styleUrl: './audit-log.component.scss',
})
export class AuditLogComponent implements OnInit {
  private service = inject(AuditService);
  readonly pageSize = 50;

  facets = signal<AuditFacets>({ entities: [], actions: [], users: [] });
  result = signal<AuditPage | null>(null);
  page = signal(1);
  loading = signal(false);
  error = signal<string | null>(null);
  open = signal<ReadonlySet<string>>(new Set());

  f: AuditFilters = { 
    entity: '',
    entityId: '',
    action: '',
    userId: '',
    from: '',
    to: ''
  };

  totalPages = computed(() => { 
    const r = this.result();
    return r ? Math.max(1, Math.ceil(r.total / r.pageSize)) : 1;
  })

  ngOnInit(): void {
    this.service.facets().subscribe({
      next: (x) => {
        this.facets.set(x)
      },
      error: () => {}
    })
  }

  apply(): void {
    this.page.set(1);
    void this.load();
  }

  clear(): void {
    this.f = {
      entity: '',
      entityId: '',
      action: '',
      userId: '',
      from: '',
      to: ''
    };
    this.apply();
  }

  goTo(p: number): void {
    this.page.set(p);
    void this.load();
  }

  history(e: AuditEntry): void {
    this.f = {
      ...this.f,
      entity: e.entityName,
      entityId: e.entityId,
    };

    this.apply();
  }

  toggle(id: string): void {
    this.open.update((s) => {
      const n = new Set(s);
      if (!n.delete(id)) n.add(id);
      return n;
    })
  }

  async load(): Promise<void> {
    if (this.f.from && this.f.to && this.f.to < this.f.from) {
      this.error.set('The end date can\'t be before the start date.');
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    try {
      this.result.set(await firstValueFrom(this.service.search(this.f, this.page(), this.pageSize)));
    } catch (err) {
      this.error.set(errorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }
}
