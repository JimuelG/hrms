import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ReportService } from '../../../../core/services/report.service';
import { DepartmentService } from '../../../../core/services/department.service';
import { RecruitmentReport } from '../../../../shared/models/reports';
import { addDaysIso, downloadBlob } from '../../../../shared/utils/report-ui.util';
import { firstValueFrom } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/api-error.util';
import { localDateIso } from '../../../../shared/utils/attendance-ui.utils';

@Component({
  selector: 'app-recruitment-report',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './recruitment-report.component.html',
  styleUrl: './recruitment-report.component.scss',
})
export class RecruitmentReportComponent implements OnInit {
  private reports = inject(ReportService);
  private departmentService = inject(DepartmentService);

  departments = signal<{ id: string; name: string }[]>([]);
  q = { from: '', to: '', jobPostingId: '', departmentId: '' };
  report = signal<RecruitmentReport | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  ngOnInit(): void {
    const today = localDateIso();
    this.q.to = today;
    this.q.from = addDaysIso(today, -89);
    this.departmentService.getAll().subscribe({
      next: (d) => {
        this.departments.set(d);
      },
      error: () => {}
    })
    void this.run();
  }

  private validate(): string | null {
    if (!this.q.from || !this.q.to) return 'Choose both dates.';
    if (this.q.to < this.q.to) return 'The end date can\'t be before the start date.';
    if (this.q.to > addDaysIso(this.q.from, 365)) return 'Choose at most 366 days at a time.';
    return null;
  }

  async run(): Promise<void> {
    const problem = this.validate();
    if (problem) { this.error.set(problem); return; }
    this.loading.set(true);
    this.error.set(null);
    try {
      this.report.set(await firstValueFrom(this.reports.recruitment({ ...this.q })));
    } catch (err) {
      this.report.set(null);
      this.error.set(errorMessage(err));
    } finally {
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
      downloadBlob(await firstValueFrom(this.reports.exportRecruitment({ ...this.q })), `recruitment-${this.q.from}-${this.q.to}.csv`);
    } catch (err) {
      this.error.set(errorMessage(err));
    }
  }

  width(count: number, max: number): number {
    return max > 0 ? Math.round((count / max) * 100) : 0;
  }

  max(rows: { count: number }[]): number {
    return rows.reduce((m, r) => Math.max(m, r.count), 0);
  }
}
