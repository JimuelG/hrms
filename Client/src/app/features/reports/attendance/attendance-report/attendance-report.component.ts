import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ReportService } from '../../../../core/services/report.service';
import { DepartmentService } from '../../../../core/services/department.service';
import { BranchService } from '../../../../core/services/branch.service';
import { formatMinutes, localDateIso } from '../../../../shared/utils/attendance-ui.utils';
import { AttendanceReport } from '../../../../shared/models/reports';
import { addDaysIso, downloadBlob } from '../../../../shared/utils/report-ui.util';
import { first, firstValueFrom } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-attendance-report',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './attendance-report.component.html',
  styleUrl: './attendance-report.component.scss',
})
export class AttendanceReportComponent implements OnInit {
  private reports = inject(ReportService);
  private departmentService = inject(DepartmentService);
  private branchService = inject(BranchService);
  readonly formatMinutes = formatMinutes;

  departments = signal<{ id: string; name: string }[]>([]);
  branches = signal<{ id: string; name: string }[]>([]);

  q = { from: '', to: '', graceMinutes: 0, departmentId: '', branchId: '' };
  report = signal<AttendanceReport | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  ngOnInit(): void {
    const today = localDateIso();
    this.q.to = today;
    this.q.from = `${today.slice(0, 8)}01`;
    this.departmentService.getAll().subscribe({
      next: (d) => {
        this.departments.set(d);
      },
      error: () => {}
    })
    this.branchService.getAll().subscribe({
      next: (b) => this.branches.set(b),
      error: () => {}
    })
    void this.run();
  }

  private validate(): string | null {
    if (!this.q.from || !this.q.to) return 'Choose both dates.';
    if (this.q.to < this.q.from) return 'The end date can\'t be before the start date.';
    if (this.q.to > addDaysIso(this.q.from, 92)) return 'Choose at most 93 days at a time.';
    if (this.q.graceMinutes < 0 || this.q.graceMinutes > 120) return 'Grace must be between 0 and 120 minutes.';
    return null;
  }

  async run(): Promise<void> {
    const problem = this.validate();
    if (problem) { this.error.set(problem); return; }
    this.loading.set(true);
    this.error.set(null);
    try {
      this.report.set(await firstValueFrom(this.reports.attendance({ ...this.q })));
    }
    catch (err) {
      this.report.set(null);
      this.error.set(errorMessage(err));
    }
    finally {
      this.loading.set(false);
    }
  }

  async download(): Promise<void> {
    const problem = this.validate();
    if (problem) { 
      this.error.set(problem);
      return;
    }
    try {
      downloadBlob(await firstValueFrom(this.reports.exportAttendance({ ...this.q})), `attendance-${this.q.from}-${this.q.to}.csv`);
    }
    catch (err) {
      this.error.set(errorMessage(err));
    }
  }
}
