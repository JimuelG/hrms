import { CommonModule, DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LeaveService } from '../../../../core/services/leave.service';
import { APPROVAL_STATUS_LABELS, ApprovalStatus } from '../../../../shared/models/attendance';
import { DayPortion, ENTRY_TYPE_LABELS, LeaveAttachment, LeaveBalance, LeaveBalances, LeaveDayStatus, LeaveEntitlementMode, LeavePreview, LeaveRequest, LeaveType, LedgerEntry } from '../../../../shared/models/leave';
import { approvalStatusClass, localDateIso } from '../../../../shared/utils/attendance-ui.utils';
import { formatBytes, formatDays, portionLabel, shortDate } from '../../../../shared/utils/leave.util';
import { endWith, first, firstValueFrom } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/api-error.util';

type Tab = 'balances' | 'requests';

@Component({
  selector: 'app-my-leave',
  imports: [
    CommonModule,
    FormsModule,
    DatePipe
  ],
  templateUrl: './my-leave.component.html',
  styleUrl: './my-leave.component.scss',
})
export class MyLeaveComponent implements OnInit{
  private leave = inject(LeaveService);

  readonly APPROVAL = ApprovalStatus;
  readonly DayStatus = LeaveDayStatus;
  readonly Portion = DayPortion;
  readonly Mode = LeaveEntitlementMode;
  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = approvalStatusClass;
  readonly entryLabel = ENTRY_TYPE_LABELS;
  readonly formatDays = formatDays;
  readonly formatBytes = formatBytes;
  readonly portionLabel = portionLabel;
  readonly shortDate = shortDate;

  loading = signal(true);
  loadError = signal<string | null>(null);
  tab = signal<Tab>('balances');
  actionError = signal<string | null>(null);
  actionNotice = signal<string | null>(null);

  year = signal(new Date().getFullYear());
  balances = signal<LeaveBalances | null>(null);
  types = signal<LeaveType[]>([]);
  requests = signal<LeaveRequest[]>([]);

  ledgerFor = signal<LeaveBalance | null>(null);
  ledger = signal<LedgerEntry[]>([]);
  ledgerLoading = signal(false);

  formOpen = signal(false);
  form = { 
    leaveTypeId: '', 
    startDate: '', 
    endDate: '',
    startPortion: DayPortion.Full,
    endPortion: DayPortion.Full,
    reason: ''
  };
  formError = signal<string | null>(null);
  saving = signal(false);
  preview = signal<LeavePreview | null>(null);
  previewError = signal<string | null>(null);
  previewLoading = signal(false);
  private previewTimer: ReturnType<typeof setTimeout> | null = null;
  private previewSeq = 0;
  pendingFile: File | null = null;

  docsFor = signal<LeaveRequest | null>(null);
  docs = signal<LeaveAttachment[]>([]);
  docsError = signal<string | null>(null);
  docsBusy = signal(false);

  selectedType = computed(() => this.types().find((t) => t.id === this.form.leaveTypeId) ?? null);
  today = localDateIso();

  ngOnInit(): void {
    void this.loadAll();
  }

  private async loadAll(): Promise<void> {
    this.loading.set(true);
    try {
      const [balances, types, requests] = await Promise.all([
        firstValueFrom(this.leave.getMyBalances(this.year())),
        firstValueFrom(this.leave.getTypes()),
        firstValueFrom(this.leave.getMyRequests())
      ]);

      this.balances.set(balances);
      this.types.set(types);
      this.requests.set(requests.items);
      this.loadError.set(null);
    } catch (err) {
      this.loadError.set(errorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  async changeYear(delta: number): Promise<void> {
    this.year.update((y) => y + delta);
    try { this.balances.set(await firstValueFrom(this.leave.getMyBalances(this.year()))); }
    catch (err) { this.actionError.set(errorMessage(err)); }
  }

  async openLedger(b: LeaveBalance): Promise<void> {
    this.ledgerFor.set(b);
    this.ledger.set([]);
    this.ledgerLoading.set(true);
    try { this.ledger.set(await firstValueFrom(this.leave.getMyLedger(b.year, b.leaveTypeId))); }
    catch (err) { this.actionError.set(errorMessage(err)); }
    finally { this.ledgerLoading.set(false) }
  }

  openForm(): void {
    this.form = {
      leaveTypeId: '',
      startDate: '',
      endDate: '',
      startPortion: DayPortion.Full,
      endPortion: DayPortion.Full,
      reason: ''
    };
    this.formError.set(null);
    this.preview.set(null);
    this.previewError.set(null);
    this.pendingFile = null;
    this.formOpen.set(true);
  }

  closeForm(): void { this.formOpen.set(false); }

  onFormChange(): void {
    if (this.previewTimer) clearTimeout(this.previewTimer);
    this.preview.set(null);
    this.previewError.set(null);

    const f = this.form;
    if (!f.leaveTypeId || !f.startDate || !f.endDate) return;

    this.previewTimer = setTimeout(() => void this.runPreview(), 350);
  }

  onStartChange(): void {
    if (!this.form.endDate || this.form.endDate < this.form.startDate) this.form.endDate = this.form.startDate;
    this.normalizePortions();
    this.onFormChange();
  }

  onEndChange(): void { this.normalizePortions(); this.onFormChange(); }

  private normalizePortions(): void {
    const f = this.form;
    if (!f.startDate || !f.endDate) return;
    if (f.startDate === f.endDate) {
      f.endPortion = f.startPortion
    } else {
      if (f.startPortion === DayPortion.Morning) f.startPortion = DayPortion.Full;
      if (f.endPortion === DayPortion.Afternoon) f.endPortion = DayPortion.Full;
    }
  }

  private async runPreview(): Promise<void> {
    const f = this.form;
    const seq = ++this.previewSeq;
    this.previewLoading.set(true);
    try {
      const result = await firstValueFrom(
        this.leave.preview(
          f.leaveTypeId, 
          f.startDate, 
          f.endDate, 
          f.startPortion, 
          f.endPortion));
      if (seq === this.previewSeq) { 
        this.preview.set(result); 
        this.previewError.set(null);
      } 
    }
    catch (err) {
      if (seq === this.previewSeq) {
        this.preview.set(null);
        this.previewError.set(errorMessage(err));
      }
    } finally {
      if (seq === this.previewSeq) {
        this.previewLoading.set(false);
      }
    }
  }

  onFileSelected(event: Event): void {
    this.pendingFile = (event.target as HTMLInputElement).files?.[0] ?? null;
  }

  async submit(): Promise<void> {
    const f = this.form;
    if (!f.leaveTypeId || !f.startDate || !f.endDate) {
      this.formError.set('Choosea leave type and dates.');
      return;
    }
    if (!f.reason.trim()) {
      this.formError.set('Give a reason for the request.');
      return;
    }

    this.formError.set(null);
    this.saving.set(true);
    try {
      const result = await firstValueFrom(this.leave.file({ ...f, reason: f.reason.trim() }));
      const notes = [...result.notes];

      if (this.pendingFile) {
        try {
          await firstValueFrom(this.leave.uploadDocument(result.request.id, this.pendingFile));
        } catch (err) {
          notes.push(`Your request was sent, but the document wasn't attached: ${errorMessage(err)}`);
        }
      }

      this.formOpen.set(false);
      this.actionNotice.set(notes.length ? notes.join(' ') : 'Your request was sent for approval.');
      await this.loadAll();
      this.tab.set('requests');
    } catch (err) {
      this.formError.set(errorMessage(err));
    } finally {
      this.saving.set(false);
    }
  }

  canCancel(r: LeaveRequest): boolean {
    if (r.status === ApprovalStatus.Pending) return true;

    return r.status === ApprovalStatus.Approved && r.startDate > this.today;
  }

  async cancel(r: LeaveRequest): Promise<void> {
    const approved = r.status === ApprovalStatus.Approved;
    let reason: string | undefined;

    if (approved) {
      const answer = prompt('Why are you cancelling this approved leave?');
      if (!answer?.trim()) return;
      reason = answer.trim();
    } else if (!confirm('Withdraw this request?')) {
      return;
    }

    this.actionError.set(null);
    try {
      await firstValueFrom(this.leave.cancel(r.id, reason));
      this.actionNotice.set(approved ? 'Leave cancelled and your days were returned.' : 'Request withdrawn.');
      await this.loadAll();
    } catch (err) {
      this.actionError.set(errorMessage(err));
    }
  }

  async openDocs(r: LeaveRequest): Promise<void> {
    this.docsFor.set(r);
    this.docs.set([]);
    this.docsError.set(null);
    await this.reloadDocs(r);
  }

  private async reloadDocs(r: LeaveRequest): Promise<void> {
    try {
      this.docs.set(await firstValueFrom(this.leave.getDocuments(r.id)));
    } catch (err) {
      this.docsError.set(errorMessage(err));
    }
  }

  async uploadDoc(event: Event): Promise<void> {
    const r = this.docsFor();
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!r || !file) return;

    this.docsBusy.set(true);
    this.docsError.set(null);
    try {
      await firstValueFrom(this.leave.uploadDocument(r.id, file));
      await this.reloadDocs(r);
      await this.loadAll();
    } catch (err) {
      this.docsError.set(errorMessage(err));
    } finally {
      this.docsBusy.set(false);
      input.value = '';
    }
  }

  async downloadDoc(d: LeaveAttachment): Promise<void> {
    const r = this.docsFor();
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
      this.docsError.set(errorMessage(err));
    }
  }

  async deleteDoc(d: LeaveAttachment): Promise<void> {
    const r = this.docsFor();
    if (!r || !confirm(`Remove ${d.fileName}?`)) return;
    try {
      await firstValueFrom(this.leave.deleteDocument(r.id, d.id));
      await this.reloadDocs(r);
      await this.loadAll();
    } catch (err) {
      this.docsError.set(errorMessage(err));
    }
  }
}
