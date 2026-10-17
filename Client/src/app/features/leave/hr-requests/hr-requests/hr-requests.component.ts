import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LeaveService } from '../../../../core/services/leave.service';
import { EmployeeService } from '../../../../core/services/employee.service';
import { APPROVAL_STATUS_LABELS, ApprovalStatus, PagedResult } from '../../../../shared/models/attendance';
import { approvalStatusClass } from '../../../../shared/utils/attendance-ui.utils';
import { formatDays, shortDate } from '../../../../shared/utils/leave.util';
import { Employee } from '../../../../shared/models/employee';
import { LeaveRequest } from '../../../../shared/models/leave';
import { firstValueFrom } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-hr-requests',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './hr-requests.component.html',
  styleUrl: './hr-requests.component.scss',
})
export class HrRequestsComponent implements OnInit{
  private leave = inject(LeaveService);
  private employeeService = inject(EmployeeService);

  readonly APPROVAL = ApprovalStatus;
  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = approvalStatusClass;
  readonly formatDays = formatDays;
  readonly shortDate = shortDate;
  readonly pageSize = 20;
  readonly statuses = [
    { value: null, label: 'All statuses' },
    { value: ApprovalStatus.Pending, label: 'Pending'},
    { value: ApprovalStatus.Approved, label: 'Approved'},
    { value: ApprovalStatus.Rejected, label: 'Rejected'},
    { value: ApprovalStatus.Cancelled, label: 'Cancelled'},
  ]

  employees = signal<Employee[]>([]);
  result = signal<PagedResult<LeaveRequest> | null>(null);
  page = signal(1);
  loading = signal(true);
  loadError = signal<string | null>(null);
  actionError = signal<string | null>(null);
  notice = signal<string | null>(null);
  totalPages = computed(() => { const r = this.result(); return r ? Math.max(1, Math.ceil(r.total / r.pageSize)) : 1; });

  filters: { 
    status: ApprovalStatus | null;
    employeeId: string;
    from: string;
    to: string 
  } = { status: null, employeeId: '', from: '', to: ''};

  ngOnInit(): void {
    this.employeeService.getAll().subscribe({ next: (e) => this.employees.set(e), error: () => {} });
    void this.load();
  }

  changed(): void {
    this.page.set(1);
    void this.load();
  }

  goTo(p: number): void {
    this.page.set(p);
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      this.result.set(await firstValueFrom(this.leave.queryRequests({
        status: this.filters.status ?? undefined,
        employeeId: this.filters.employeeId || undefined,
        from: this.filters.from || undefined,
        to: this.filters.to || undefined,
        page: this.page(),
        pageSize: this.pageSize
      })));
    } catch (err) {
      this.loadError.set(errorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  async cancel(r: LeaveRequest): Promise<void> {
    const reason = prompt(`Cancel ${r.employeeName}'s approved ${r.leaveTypeName}? The days go back to their balance. Reason:`);
    if (!reason?.trim()) return;

    this.actionError.set(null);
    try {
      await firstValueFrom(this.leave.cancel(r.id, reason.trim()));
      this.notice.set('Leave cancelled and the days were returned.');
      await this.load();
    } catch (err) {
      this.actionError.set(errorMessage(err));
    }
  }
}
