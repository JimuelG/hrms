import { Component, inject, OnInit, signal } from '@angular/core';
import { WorkScheduleService } from '../../../../core/services/work-schedule.service';
import { PermissionService } from '../../../../core/services/permission.service';
import { PERMISSIONS } from '../../../../shared/constants/permissions';
import { WORKING_DAY_OPTIONS } from '../../../../shared/models/tenant-settings';
import { formatTimeOnly, minutesBetween, toApiTime } from '../../../../shared/utils/attendance-ui.utils';
import { WorkSchedule, WorkScheduleFormValue } from '../../../../shared/models/attendance';
import { errorMessage } from '../../../../shared/utils/api-error.util';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-work-schedules',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './work-schedules.component.html',
  styleUrl: './work-schedules.component.scss',
})
export class WorkSchedulesComponent implements OnInit{
  private service = inject(WorkScheduleService);
  readonly permissions = inject(PermissionService);
  readonly P = PERMISSIONS;
  readonly dayOptions = WORKING_DAY_OPTIONS;
  readonly formatTimeOnly = formatTimeOnly;

  schedules = signal<WorkSchedule[]>([]);
  loading = signal(true);
  loadError = signal<string | null>(null);
  actionError = signal<string | null>(null);

  drawerOpen = signal(false);
  editing = signal<WorkSchedule | null>(null);
  formError = signal<string | null>(null);
  saving = signal(false);
  form: WorkScheduleFormValue = this.emptyForm();


  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service.getAll().subscribe({
      next: (data) => {
        this.schedules.set(data);
        this.loadError.set(null);
        this.loading.set(false);
      },
      error: (err) => {
        this.loadError.set(errorMessage(err));
        this.loading.set(false);
      }
    })
  }

  isDay(mask: number, day: number): boolean { return (mask & day) === day; }

  dayChipClass(active: boolean): string {
    return active
      ? 'bg-indigo-50 text-indigo-700 dark:bg-indigo-950 dark:text-indigo-400'
      : 'text-slate-300 dark:text-slate-600';
  }

  dayButtonClass(active: boolean): string {
    return active
      ? 'bg-indigo-600 text-white border-indigo-600'
      : 'bg-white dark:bg-slate-800 text-slate-500 border-slate-200 dark:border-slate-700';
  }

  toggleDay(day: number): void {
    this.form.workingDays = this.isDay(this.form.workingDays, day)
      ? this.form.workingDays & ~day
      : this.form.workingDays | day;
  }

  openCreate(): void {
    this.editing.set(null);
    this.form = this.emptyForm();
    this.formError.set(null);
    this.drawerOpen.set(true);
  }

  openEdit(s: WorkSchedule): void {
    this.editing.set(s);
    this.form = {
      name: s.name,
      startTime: s.startTime.slice(0, 5),
      endTime: s.endTime.slice(0, 5),
      workingDays: s.workingDays,
      isActive: s.isActive
    };
    this.formError.set(null);
    this.drawerOpen.set(true);
  }

  closeDrawer(): void { this.drawerOpen.set(false) }

  save(): void {
    const f = this.form;
    if (!f.name.trim()) { this.formError.set('Give the schedule a name.'); return; }
    if (!f.startTime || !f.endTime) { this.formError.set('Set a start and end time.'); return; }
    if (minutesBetween(f.startTime, f.endTime) <= 0) { this.formError.set('The end time must be after the start time.'); return; }
    if (f.workingDays === 0) { this.formError.set('Select at least one working day.'); return; }

    this.formError.set(null);
    this.saving.set(true);

    const payload: WorkScheduleFormValue = {
      ...f,
      name: f.name.trim(),
      startTime: toApiTime(f.startTime)!,
      endTime: toApiTime(f.endTime)!,
    };

    const current = this.editing();
    const request = current ? this.service.update(current.id, payload) : this.service.create(payload);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.closeDrawer();
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(errorMessage(err));
      }
    });
  }

  remove(s: WorkSchedule): void {
    if (!confirm(`Delete "${s.name}"? Employees using it will show no schedule.`)) return;
    this.actionError.set(null);
    this.service.delete(s.id).subscribe({
      next: () => this.load,
      error: (err) => this.actionError.set(errorMessage(err))
    })
  }

  private emptyForm(): WorkScheduleFormValue {
    return {
      name: '',
      startTime: '09:00',
      endTime: '18:00',
      workingDays: 31,
      isActive: true
    };
  }
}
