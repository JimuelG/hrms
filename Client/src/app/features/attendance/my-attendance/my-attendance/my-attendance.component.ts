import { CommonModule, DatePipe } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AttendanceService } from '../../../../core/services/attendance.service';
import { AttendanceCorrectionService } from '../../../../core/services/attendance-correction.service';
import { OvertimeService } from '../../../../core/services/overtime.service';
import { GeoFix, GeolocationError, GeolocationService } from '../../../../core/services/geolocation.service';
import { APPROVAL_STATUS_LABELS, ApprovalStatus, AttendanceCorrection, AttendanceRecord, ClockRequest, describeFlags, MyWorkLocation, OvertimeRequest, WorkLocationType } from '../../../../shared/models/attendance';
import { approvalStatusClass, flagToneClass, formatMinutes, formatTimeOnly, localDateIso, minutesBetween, toApiTime } from '../../../../shared/utils/attendance-ui.utils';
import { firstValueFrom } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { extractApiError } from '../../../../shared/utils/api-error.util';

type Tab = 'records' | 'corrections' | 'overtime';
const STATLE_SHIFT_MS = 16 * 60 * 60 * 1000;

@Component({
  selector: 'app-my-attendance',
  imports: [
    CommonModule,
    FormsModule,
    DatePipe
  ],
  templateUrl: './my-attendance.component.html',
  styleUrl: './my-attendance.component.scss',
})
export class MyAttendanceComponent implements OnInit {
  private attendance = inject(AttendanceService);
  private correctionService = inject(AttendanceCorrectionService);
  private overtimeService = inject(OvertimeService);
  private geo = inject(GeolocationService);

  readonly APPROVAL = ApprovalStatus;
  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly describeFlags = describeFlags;
  readonly formatMinutes = formatMinutes;
  readonly formatTimeOnly = formatTimeOnly;
  readonly statusClass = approvalStatusClass;
  readonly toneClass = flagToneClass;
  readonly round = Math.round;
  readonly tabs: { key: Tab; label: string }[] = [
    { key: 'records', label: 'My records' },
    { key: 'corrections', label: 'Corrections' },
    { key: 'overtime', label: 'Overtime' }
  ];

  loading = signal(true);
  loadError = signal<string | null>(null);
  records = signal<AttendanceRecord[]>([]);
  locations = signal<MyWorkLocation[]>([]);
  corrections = signal<AttendanceCorrection[]>([]);
  overtime = signal<OvertimeRequest[]>([]);
  tab = signal<Tab>('records');
  listError = signal<string | null>(null);

  now = signal(Date.now());
  busy = signal(false);
  actionMessage = signal<string | null>(null);
  actionError = signal<string | null>(null);
  actionNotice = signal<string | null>(null);
  lastFix = signal<GeoFix | null>(null);

  todayIso = computed(() => localDateIso(new Date(this.now())));
  openRecord = computed(() => this.records().find((r) => !r.clockOutUtc) ?? null);
  latest = computed(() => this.records()[0] ?? null);
  remoteLocations = computed(() => this.locations().filter((l) => l.type === WorkLocationType.Remote));
  hasOnsite = computed(() => this.locations().some((l) => l.type == WorkLocationType.Remote));

  status = computed<'open' | 'stale' | 'done' | 'idle'>(() => {
    const open = this.openRecord();
    if (open) return this.now() - Date.parse(open.clockInUtc) > STATLE_SHIFT_MS ? 'stale' : 'open';
    const last = this.latest();
    return last && last.date === this.todayIso() ? 'done' : 'idle';
  });

  elapsedMinutes = computed(() => {
    const open = this.openRecord();
    return open ? Math.max(0, Math.floor((this.now() - Date.parse(open.clockInUtc)) / 60000)) : 0;
  })

  correctionOpen = signal(false);
  correctionSaving = signal(false);
  correctionError = signal<string | null>(null);
  correctionForm = { date: '', clockIn: '', clockOut: '', reason: ''};

  overtimeOpen = signal(false);
  overtimeSaving = signal(false);
  overtimeError = signal<string | null>(null);
  overtimeForm = { date: '', startTime: '', endTime: '', reason: ''};

  constructor() {
    const timer = setInterval(() => this.now.set(Date.now()), 1000);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  ngOnInit(): void {
    void this.loadAll();
  }

  private async loadAll(): Promise<void> {
    this.loading.set(true);
    try {
      const [records, locations] = await Promise.all([
        firstValueFrom(this.attendance.getMine()),
        firstValueFrom(this.attendance.getMyLocations())
      ]);
      this.records.set(records);
      this.locations.set(locations);
      this.loadError.set(null);
      this.loadCorrections();
      this.loadOvertime();
    } catch (err) {
      this.loadError.set(this.describe(err));
    } finally {
      this.loading.set(false);
    }
  }

  private async reloadRecords(): Promise<void> {
    this.records.set(await firstValueFrom(this.attendance.getMine()));
  }

  async clockIn(remote?: MyWorkLocation): Promise<void> {
    this.begin();
    try {
      const body: ClockRequest = remote ? { workLocationId: remote.id } : await this.readPosition();
      this.actionMessage.set('Clocking in...');
      await firstValueFrom(this.attendance.clockIn(body));
      await this.reloadRecords();
    } catch (err) {
      this.actionError.set(this.describe(err));
    } finally {
      this.busy.set(false);
      this.actionMessage.set(null);
    }
  }

  async clockOut(): Promise<void> {
    const open = this.openRecord();
    if (!open) return;

    this.begin();
    let notice: string | null = null;
    try {
      const remote = this.remoteSessionLocation(open);
      let body: ClockRequest;

      if (remote) {
        body = { workLocationId: remote.id };
      } else {
        try {
          body = await this.readPosition();
        } catch (e) {
          body = {};
          notice = `${this.describe(e)} Your clock-out was recorded and flagged for review.`;
        }
      }

      this.actionMessage.set('Clocking out...');
      await firstValueFrom(this.attendance.clockOut(body));
      await this.reloadRecords();
    } catch (err) {
      this.actionError.set(this.describe(err));
    } finally {
      this.busy.set(false);
      this.actionMessage.set(null);
    }
  }

  private remoteSessionLocation(open: AttendanceRecord): MyWorkLocation | null {
    return this.remoteLocations().find((l) => l.name === open.clockInLocationName) ?? null;
  }

  private async readPosition(): Promise<ClockRequest> {
    this.actionMessage.set('Getting your location...');
    const fix = await this.geo.getPosition();
    this.lastFix.set(fix);
    return { latitude: fix.latitude, longitude: fix.longitude, accuracyMeters: fix.accuracyMeters }
  }

  private begin(): void {
    this.busy.set(true);
    this.actionError.set(null);
    this.actionNotice.set(null);
    this.actionMessage.set(null);
  }

  private describe(err: unknown): string {
    if (err instanceof GeolocationError) return err.message;
    if (err instanceof HttpErrorResponse) return extractApiError(err);
    return 'Something went wrong. Please try again.';
  }

  loadCorrections(): void {
    this.correctionService.getMine().subscribe({ next: (c) => this.corrections.set(c), error: () => {} });
  }

  openCorrection(date?: string): void {
    this.correctionForm = { 
      date: date ?? '',
      clockIn: '',
      clockOut: '',
      reason: ''
    };
    this.correctionError.set(null);
    this.correctionOpen.set(true);
  }

  submitCorrection(): void {
    const f = this.correctionForm;
    this.correctionError.set(null);
    this.correctionSaving.set(true);

    this.correctionService.create({
      date: f.date,
      clockIn: toApiTime(f.clockIn),
      clockOut: toApiTime(f.clockOut),
      reason: f.reason
    }).subscribe({
      next: () => {
        this.correctionSaving.set(false);
        this.correctionOpen.set(false);
        this.loadCorrections();
        this.tab.set('corrections');
      },
      error: (err) => { 
        this.correctionSaving.set(false);
        this.correctionError.set(extractApiError(err));
      }
    });
  }

  cancelCorrection(c: AttendanceCorrection): void {
    if (!confirm('Cancel this correction request?')) return;
    this.listError.set(null);
    this.correctionService.cancel(c.id).subscribe({
      next: () => this.loadCorrections(),
      error: (err) => this.listError.set(extractApiError(err))
    });
  }

  loadOvertime(): void {
    this.overtimeService.getMine().subscribe({
      next: (o) =>
        this.overtime.set(o),
      error: () => {} });
  }

  openOvertime(): void {
    this.overtimeForm = { 
      date: this.todayIso(),
      startTime: '',
      endTime: '',
      reason: ''
    };
    this.overtimeError.set(null);
    this.overtimeOpen.set(true);
  }

  overtimePreview(): string | null {
    const { startTime, endTime } = this.overtimeForm;
    if (!startTime || !endTime) return null;
    const mins = minutesBetween(startTime, endTime);
    return mins > 0 ? formatMinutes(mins) : null;
  }

  submitOvertime(): void {
    const f = this.overtimeForm;
    this.overtimeError.set(null);
    this.overtimeSaving.set(true);

    this.overtimeService.create({
      date: f.date, 
      startTime: toApiTime(f.startTime) ?? '',
      endTime: toApiTime(f.endTime) ?? '',
      reason: f.reason
    }).subscribe({
      next: () => {
        this.overtimeSaving.set(false);
        this.overtimeOpen.set(false);
        this.loadOvertime();
        this.tab.set('overtime');
      },
      error: (err) => {
        this.overtimeSaving.set(false);
        this.overtimeError.set(extractApiError(err));
      }
    });
  }

  cancelOvertime(o: OvertimeRequest): void {
    if (!confirm('Cancel this overtime request?')) return;
    this.listError.set(null);
    this.overtimeService.cancel(o.id).subscribe({
      next: () => this.loadOvertime(),
      error: (err) => this.listError.set(extractApiError(err))
    });
  }
}
