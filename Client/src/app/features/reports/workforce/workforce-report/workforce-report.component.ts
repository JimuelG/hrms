import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { ReportService } from '../../../../core/services/report.service';
import { shortDate } from '../../../../shared/utils/leave.util';
import { Breakdown, CountRow, WorkforceReport } from '../../../../shared/models/reports';
import { firstValueFrom } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-workforce-report',
  imports: [
    CommonModule
  ],
  templateUrl: './workforce-report.component.html',
  styleUrl: './workforce-report.component.scss',
})
export class WorkforceReportComponent implements OnInit {
  private service = inject(ReportService);
  readonly shortDate = shortDate;

  report = signal<WorkforceReport | null>(null);
  error = signal<string | null>(null);
  exportError = signal<string | null>(null);

  ngOnInit(): void {
    void this.load();
  }

  private async load(): Promise<void> {
    try {
      this.report.set(await firstValueFrom(this.service.workforce()));
      this.error.set(null);
    } catch (err) {
      this.error.set(errorMessage(err));
    }
  }

  sections(r: WorkforceReport): { title: string; key: Breakdown; rows: CountRow[] }[] {
    return [
      { title: 'By department', key: 'department', rows: r.byDepartment },
      { title: 'By branch', key: 'branch', rows: r.byBranch },
      { title: 'By position', key: 'position', rows: r.byPosition },
      { title: 'By employment type', key: 'type', rows: r.byEmploymentType },
      { title: 'By status', key: 'status', rows: r.byStatus },
      { title: 'By length of service', key: 'tenure', rows: r.tenure }
    ];
  }

  width(count: number, total: number): number { return total > 0 ? Math.round((count / total) * 100) : 0; }

  async download(key: Breakdown): Promise<void> {
    this.exportError.set(null);
    try {
      const blob = await firstValueFrom(this.service.exportWorkforce(key));
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `workforce-${key}.csv`;
      a.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      this.exportError.set(errorMessage(err));
    }
  }

  monthDay(c: { month: number; day: number }): string {
    return new Date(2000, c.month - 1, c.day).toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
  }
}
