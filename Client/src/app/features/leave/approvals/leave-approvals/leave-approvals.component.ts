import { CommonModule, DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApprovalService } from '../../../../core/services/approval.service';
import { LeaveService } from '../../../../core/services/leave.service';
import { APPROVAL_STATUS_LABELS, ApprovalStatus, PagedResult } from '../../../../shared/models/attendance';
import { ApprovalDetail, ApprovalStepStatus, ApprovalSubjectType, ApprovalSummary, EVENT_TYPE_LABELS, STEP_STATUS_LABELS, stepStatusClass } from '../../../../shared/models/approval';
import { approvalStatusClass } from '../../../../shared/utils/attendance-ui.utils';
import { formatBytes, formatDays, portionLabel } from '../../../../shared/utils/leave.util';
import { longDate } from '../../../../shared/utils/leave-ui.utils';
import { LeaveAttachment, LeaveRequest } from '../../../../shared/models/leave';
import { first, firstValueFrom } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/api-error.util';

type Action = 'approve' | 'reject' | 'comment';

@Component({
  selector: 'app-leave-approvals',
  imports: [
    CommonModule,
    FormsModule,
    DatePipe
  ],
  templateUrl: './leave-approvals.component.html',
  styleUrl: './leave-approvals.component.scss',
})
export class LeaveApprovalsComponent implements OnInit{
  private approvals = inject(ApprovalService);
  private leave = inject(LeaveService);

  readonly APPROVAL = ApprovalStatus;
  readonly Step = ApprovalStepStatus;
  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = approvalStatusClass;
  readonly stepLabel = STEP_STATUS_LABELS;
  readonly stepDot = stepStatusClass;
  readonly eventLabel = EVENT_TYPE_LABELS;
  readonly formatDays = formatDays;
  readonly formatBytes = formatBytes;
  readonly longDate = longDate;
  readonly portionLabel = portionLabel;
  readonly pageSize = 20;

  inbox = signal<PagedResult<ApprovalSummary> | null>(null);
  page = signal(1);
  loading = signal(true);
  loadError = signal<string | null>(null);
  totalPages = computed(() => { const r = this.inbox(); return r ? Math.max(1, Math.ceil(r.total / r.pageSize)) : 1; });

  detail = signal<ApprovalDetail | null>(null);
  leaveRequest = signal<LeaveRequest | null>(null);
  docs = signal<LeaveAttachment[]>([]);
  detailLoading = signal(false);
  detailError = signal<string | null>(null);

  action = signal<Action | null>(null);
  note = '';
  busy = signal(false);
  notice = signal<string | null>(null);

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    try {
      this.inbox.set(await firstValueFrom(this.approvals.inbox(this.page(), this.pageSize)));
      this.loadError.set(null);
    } catch (err) {
      this.loadError.set(errorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  goTo(p: number): void { 
    this.page.set(p); 
    void this.load(); 
  }

  async open(s: ApprovalSummary): Promise<void> {
    this.detail.set(null);
    this.leaveRequest.set(null);
    this.docs.set([]);
    this.detailError.set(null);
    this.action.set(null);
    this.note = '';
    this.detailLoading.set(true);

    try {
      const d = await firstValueFrom(this.approvals.get(s.id));
      this.detail.set(d);

      if (d.request.subjectType === ApprovalSubjectType.LeaveRequest) {
        const [req, docs] = await Promise.allSettled([
          firstValueFrom(this.leave.getRequest(d.request.subjectId)),
          firstValueFrom(this.leave.getDocuments(d.request.subjectId))
        ]);
        if (req.status === 'fulfilled') this.leaveRequest.set(req.value);
        if (docs.status === 'fulfilled') this.docs.set(docs.value);
      }
    } catch (err) {
      this.detailError.set(errorMessage(err));
    } finally {
      this.detailLoading.set(false);
    }
  }

  close(): void {
    this.detail.set(null);
    this.detailError.set(null);
  }

  choose(a: Action): void {
    this.action.set(a);
    this.detailError.set(null);
  }

  async submit(): Promise<void> {
    const d = this.detail();
    const a = this.action();
    if (!d || !a) return;

    const text = this.note.trim();
    if (a === 'reject' && !text) {
      this.detailError.set('A reason is required when rejecting a request.');
      return;
    }
    if (a === 'comment' && !text) {
      this.detailError.set('Write a comment first.');
      return;
    }

    this.busy.set(true);
    this.detailError.set(null);
    try {
      const id = d.request.id;
      const updated = await firstValueFrom(
        a === 'approve' ? this.approvals.approve(id, text || undefined)
        : a === 'reject' ? this.approvals.reject(id, text)
        : this.approvals.comment(id, text));

      this.detail.set(updated);
      this.action.set(null);
      this.note = '';
      if (a !== 'comment') {
        this.notice.set(updated.request.status === ApprovalStatus.Approved ? 'Approved. That was the final step.'
          : updated.request.status === ApprovalStatus.Rejected ? 'Request rejected.'
          : 'Approved. It has moved to the next step.');
        await this.load();
      }
    } catch (err) {
      this.detailError.set(errorMessage(err));
    } finally {
      this.busy.set(false);
    }
  }

  async download(d: LeaveAttachment): Promise<void> {
    const r = this.leaveRequest();
    if (!r) return;
    try {
      const blob = await firstValueFrom(this.leave.downloadDocument(r.id, d.id));
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = d.fileName;
      link.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      this.detailError.set(errorMessage(err));
    }
  }
}
