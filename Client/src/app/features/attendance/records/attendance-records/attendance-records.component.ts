import { CommonModule, DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AttendanceService } from '../../../../core/services/attendance.service';
import { EmployeeService } from '../../../../core/services/employee.service';
import { AttendanceCorrectionService } from '../../../../core/services/attendance-correction.service';
import { PermissionService } from '../../../../core/services/permission.service';
import { PERMISSIONS } from '../../../../shared/constants/permissions';
import { AttendanceRecord, describeFlags, PagedResult } from '../../../../shared/models/attendance';
import { flagToneClass, formatMinutes, isStaleOpen, localDateIso, mapUrl, toApiTime } from '../../../../shared/utils/attendance-ui.utils';
import { Employee } from '../../../../shared/models/employee';
import { firstValueFrom } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/api-error.util';

interface Filters { 
  from: string;
  to: string;
  employeeId: string;
  needsReview: boolean
}

@Component({
  selector: 'app-attendance-records',
  imports: [
    CommonModule,
    FormsModule,
    DatePipe
  ],
  templateUrl: './attendance-records.component.html',
  styleUrl: './attendance-records.component.scss',
})
export class AttendanceRecordsComponent implements OnInit{
  private attendance = inject(AttendanceService);
  private employeeService = inject(EmployeeService);
  private corrections = inject(AttendanceCorrectionService);
  readonly permissions = inject(PermissionService);
  readonly P = PERMISSIONS;

  readonly describeFlags = describeFlags;
  readonly toneClass = flagToneClass;
  readonly formatMinutes = formatMinutes;
  readonly mapUrl = mapUrl;
  readonly round = Math.round;
  readonly pageSize = 25;

  employees = signal<Employee[]>([]);
  result = signal<PagedResult<AttendanceRecord> | null>(null);
  loading = signal(true);
  loadError = signal<string | null>(null);
  loadedAt = signal(Date.now());
  page = signal(1);

  filters: Filters = {
    from: localDateIso(new Date(Date.now() - 6 * 86_400_000)),
    to: localDateIso(),
    employeeId: '',
    needsReview: true
  };

  totalPages = computed(() => {
    const r = this.result();
    return r ? Math.max(1, Math.ceil(r.total / r.pageSize)) : 1;
  });

  correctOpen = signal(false);
  correctSaving = signal(false);
  correctError = signal<string | null>(null);
  correctForm = {
    employeeId: '',
    date: '',
    clockIn: '',
    clockOut: '',
    reason: ''
  };

  ngOnInit(): void {
    this.employeeService.getAll().subscribe({ next: (e) => this.employees.set(e), error: () => {} });
    void this.load();
  }

  update<K extends keyof Filters>(key: K, value: Filters[K]): void {
    this.filters[key] = value;
    this.page.set(1);
    void this.load();
  }

  goTo(p: number): void {
    this.page.set(p);
    void this.load();
  }

  isStale(r: AttendanceRecord): boolean { return isStaleOpen(r, this.loadedAt()); }

  async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      const res = await firstValueFrom(this.attendance.query({
        from: this.filters.from,
        to: this.filters.to,
        employeeId: this.filters.employeeId || undefined,
        needsReview: this.filters.needsReview,
        page: this.page(),
        pageSize: this.pageSize
      }));

      this.result.set(res);
      this.loadedAt.set(Date.now());
    } catch (err) {
      this.loadError.set(errorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  openCorrect(record?: AttendanceRecord): void {
    this.correctForm = {
      employeeId: record?.employeeId ?? '',
      date: record?.date ?? '',
      clockIn: '',
      clockOut: '',
      reason: ''
    };
    this.correctError.set(null);
    this.correctOpen.set(true);
  }

  submitCorrect(): void {
    const f = this.correctForm;
    if (!f.employeeId) { 
      this.correctError.set('Choose an employee.'); 
      return;
    }

    this.correctError.set(null);
    this.correctSaving.set(true);
    this.corrections.createForEmployee(f.employeeId, {
      date: f.date,
      clockIn: toApiTime(f.clockIn),
      clockOut: toApiTime(f.clockOut),
      reason: f.reason
    }).subscribe({
      next: () => {
        this.correctSaving.set(false);
        this.correctOpen.set(false);
        void this.load();
      },
      error: (err) => {
        this.correctSaving.set(false);
        this.correctError.set(errorMessage(err));
      }
    })
  }

}