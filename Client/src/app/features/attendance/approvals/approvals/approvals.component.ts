import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { APPROVAL_STATUS_LABELS, ApprovalStatus, AttendanceCorrection, OvertimeRequest, PagedResult } from '../../../../shared/models/attendance';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AttendanceCorrectionService } from '../../../../core/services/attendance-correction.service';
import { OvertimeService } from '../../../../core/services/overtime.service';
import { approvalStatusClass, formatMinutes, formatTimeOnly } from '../../../../shared/utils/attendance-ui.utils';
import { first, firstValueFrom } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/api-error.util';

type Kind = 'corrections' | 'overtime';
type ReviewTarget =
  | { kind: 'correction'; action: 'approve' | 'reject'; item: AttendanceCorrection }
  | { kind: 'overtime'; action: 'approve' | 'reject'; item: OvertimeRequest };

@Component({
  selector: 'app-approvals',
  imports: [
    CommonModule,
    FormsModule,
    DatePipe
  ],
  templateUrl: './approvals.component.html',
  styleUrl: './approvals.component.scss',
})
export class ApprovalsComponent implements OnInit{
  private corrections = inject(AttendanceCorrectionService);
  private overtimeService = inject(OvertimeService);

  readonly APPROVAL = ApprovalStatus;
  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = approvalStatusClass;
  readonly formatMinutes = formatMinutes;
  readonly formatTimeOnly = formatTimeOnly;
  readonly pageSize = 20;
  readonly statusFilters: { value: ApprovalStatus | null; label: string }[] = [
    { value: ApprovalStatus.Pending, label: 'Pending' },
    { value: ApprovalStatus.Approved, label: 'Approved' },
    { value: ApprovalStatus.Rejected, label: 'Rejected' },
    { value: ApprovalStatus.Cancelled, label: 'Cancelled' },
    { value: null, label: 'All' }
  ]

  kind = signal<Kind>('corrections');
  statusFilter = signal<ApprovalStatus | null>(ApprovalStatus.Pending);
  page = signal(1);

  correctionPage = signal<PagedResult<AttendanceCorrection> | null>(null);
  overtimePage = signal<PagedResult<OvertimeRequest> | null>(null);
  pendingCorrections = signal(0);
  pendingOvertime = signal(0);
  loading = signal(true);
  loadError = signal<string | null>(null);

  total = computed(() => (this.kind() === 'corrections' ? this.correctionPage() : this.overtimePage())?.total ?? 0);
  totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));

  review = signal<ReviewTarget | null>(null);
  reviewNote = '';
  reviewMinutes = 0;
  reviewSaving = signal(false);
  reviewError = signal<string | null>(null);

  ngOnInit(): void {
    
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    const status = this.statusFilter() ?? undefined;

    try {
      if (this.kind() === 'corrections') {
        this.correctionPage.set(await firstValueFrom(this.corrections.query(status, undefined, this.page(), this.pageSize)));
      } else {
        this.overtimePage.set(await firstValueFrom(this.overtimeService.query(status, undefined, this.page(), this.pageSize)));
      }
    } catch (err) {
      this.loadError.set(errorMessage(err));
    } finally {
      this.loading.set(false);
    }

    void this.refreshCounts();
  }

  private async refreshCounts(): Promise<void> {
    try {
      const [c, o] = await Promise.all([
        firstValueFrom(this.corrections.query(ApprovalStatus.Pending, undefined, 1, 1)),
        firstValueFrom(this.overtimeService.query(ApprovalStatus.Pending, undefined, 1, 1))
      ]);
      this.pendingCorrections.set(c.total);
      this.pendingOvertime.set(o.total);
    } catch {

    }
  }

  setKind(k: Kind): void { this.kind.set(k); this.page.set(1); void this.load(); }
  setStatus(s: ApprovalStatus | null): void { this.statusFilter.set(s); this.page.set(1); void this.load(); }
  goTo(p: number): void { this.page.set(p); void this.load(); }

  openReview(target: ReviewTarget): void {
    this.reviewNote = '';
    this.reviewMinutes = target.kind === 'overtime' ? target.item.requestedMinutes : 0;
    this.reviewError.set(null);
    this.review.set(target);
  }

  async submitReview(): Promise<void> {
    const r = this.review();
    if (!r) return;

    const note = this.reviewNote.trim() || undefined;
    if (r.action === 'reject' && !note) {
      this.reviewError.set('A reason is required when rejecting a request.');
      return;
    }

    this.reviewSaving.set(true);
    this.reviewError.set(null);
    try {
      if (r.kind === 'correction') {
        await firstValueFrom(r.action === 'approve'
          ? this.corrections.approve(r.item.id, note)
          : this.corrections.reject(r.item.id, note!));
      } else if (r.action === 'approve') {
        await firstValueFrom(this.overtimeService.approve(r.item.id, this.reviewMinutes, note));
      } else {
        await firstValueFrom(this.overtimeService.reject(r.item.id, note!));
      }
      this.review.set(null);
      await this.load();
    } catch (err) {
      this.reviewError.set(errorMessage(err));
    } finally {
      this.reviewSaving.set(false);
    }
  }
}
