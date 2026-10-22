import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ReportService } from '../../../../core/services/report.service';
import { DepartmentService } from '../../../../core/services/department.service';
import { BranchService } from '../../../../core/services/branch.service';
import { shortDate } from '../../../../shared/utils/leave.util';
import { LeaveCalendar, LeaveUsageReport } from '../../../../shared/models/reports';
import { firstValueFrom } from 'rxjs';
import { addDaysIso, downloadBlob } from '../../../../shared/utils/report-ui.util';
import { errorMessage } from '../../../../shared/utils/api-error.util';
import { localDateIso } from '../../../../shared/utils/attendance-ui.utils';

type Tab = 'usage' | 'calendar';

@Component({
  selector: 'app-leave-report',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './leave-report.component.html',
  styleUrl: './leave-report.component.scss',
})
export class LeaveReportComponent implements OnInit{
  private reports = inject(ReportService);
  private departmentService = inject(DepartmentService);
  private branchService = inject(BranchService);
  readonly shortDate = shortDate;

  departments = signal<{ id: string; name: string }[]>([]);
  branches = signal<{ id: string; name: string }[]>([]);

  tab = signal<Tab>('usage');
  year = signal(new Date().getFullYear());
  from = '';
  to = '';
  departmentId = '';
  branchId = '';

  usage = signal<LeaveUsageReport | null>(null);
  calendar = signal<LeaveCalendar | null>(null);
  error = signal<string | null>(null);

  ngOnInit(): void {
    const today = localDateIso();
    this.from = today;
    this.to = addDaysIso(today, 30);
    this.departmentService.getAll().subscribe({ next: (d) => this.departments.set(d), error: () => {} });
    this.branchService.getAll().subscribe({ next: (b) => this.branches.set(b), error: () => {} });
    void this.run();
  }

  async changeYear(delta: number): Promise<void> { this.year.update((y) => y + delta); await this.run(); }

  async run(): Promise<void> {
    this.error.set(null);
    try {
      if (this.tab() === 'usage') {
        this.usage.set(await firstValueFrom(this.reports.leaveUsage(this.year(), this.departmentId, this.branchId)));
      } else {
        if (!this.from || !this.to || this.to < this.from) { this.error.set('Choose a valid date range.'); return; }
        if (this.to > addDaysIso(this.from, 92)) { this.error.set('Choose at most 93 days at a time.'); return; }
        this.calendar.set(await firstValueFrom(this.reports.leaveCalendar(this.from, this.to, this.departmentId, this.branchId)));
      }
    } catch (err) {
      this.error.set(errorMessage(err));
    }
  }

  async setTab(t: Tab): Promise<void> { this.tab.set(t); await this.run(); }

  async download(): Promise<void> {
    try {
      if (this.tab() === 'usage')
        downloadBlob(await firstValueFrom(this.reports.exportLeaveUsage(this.year(), this.departmentId, this.branchId)), `leave-usage-${this.year()}.csv`);
      else
        downloadBlob(await firstValueFrom(this.reports.exportLeaveCalendar(this.from, this.to, this.departmentId, this.branchId)), `leave-canlendar-${this.from}-${this.to}.csv`);
    } catch (err) {
      this.error.set(errorMessage(err));
    }
  }
}
