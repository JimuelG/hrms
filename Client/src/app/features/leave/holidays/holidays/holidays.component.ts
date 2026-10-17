import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LeaveAdminService } from '../../../../core/services/leave-admin.service';
import { Holiday, HOLIDAY_TYPE_LABELS, HolidayFormValue, HolidayType } from '../../../../shared/models/leave';
import { longDate } from '../../../../shared/utils/leave-ui.utils';
import { errorMessage } from '../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-holidays',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './holidays.component.html',
  styleUrl: './holidays.component.scss',
})
export class HolidaysComponent implements OnInit {
  private admin = inject(LeaveAdminService);

  readonly typeLabel = HOLIDAY_TYPE_LABELS;
  readonly typeOptions = [HolidayType.Public, HolidayType.Special, HolidayType.Company];
  readonly longDate = longDate;

  year = signal(new Date().getFullYear());
  holidays = signal<Holiday[]>([]);
  loading = signal(true);
  loadError = signal<string | null>(null);
  actionError = signal<string | null>(null);

  formOpen = signal(false);
  editing = signal<Holiday | null>(null);
  form: HolidayFormValue = { date: '', name: '', type: HolidayType.Public };
  formError = signal<string | null>(null);
  saving = signal(false);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.admin.getHolidays(this.year()).subscribe({
      next: (h) => {
        this.holidays.set(h);
        this.loadError.set(null);
        this.loading.set(false);
      },
      error: (err) => {
        this.loadError.set(errorMessage(err));
        this.loading.set(false);
      }
    })
  }

  changeYear(delta: number): void {
    this.year.update((y) => y + delta);
    this.load();
  }

  openCreate(): void {
    this.editing.set(null);
    this.form = { date: `${this.year()}-01-01`, name: '', type: HolidayType.Public };
    this.formError.set(null);
    this.formOpen.set(true);
  }

  openEdit(h: Holiday): void {
    this.editing.set(h);
    this.form = { date: h.date, name: h.name, type: h.type };
    this.formError.set(null);
    this.formOpen.set(true);
  }

  save(): void {
    if (!this.form.date || !this.form.name.trim()) {
      this.formError.set('Pick a date and enter a name.');
      return;
    }

    this.formError.set(null);
    this.saving.set(true);
    const payload = { ...this.form, name: this.form.name.trim() };
    const current = this.editing();
    const request = current
                  ? this.admin.updateHoliday(current.id, payload)
                  : this.admin.createHoliday(payload);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.formOpen.set(false);
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(errorMessage(err));
      }
    });
  }

  remove(h: Holiday): void {
    if (!confirm(`Delete ${h.name}? Leave already filed keeps the days it was calculated with.`)) return;
    this.actionError.set(null);
    this.admin.deleteHoliday(h.id).subscribe({
      next: () => this.load(),
      error: (err) => this.actionError.set(errorMessage(err))
    });
  }
  
}
