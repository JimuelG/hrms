import { CommonModule, DatePipe } from '@angular/common';
import { Component, effect, inject, input, OnDestroy, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IdentifierService } from '../../../../core/services/identifier.service';
import { PermissionService } from '../../../../core/services/permission.service';
import { PERMISSIONS } from '../../../../shared/constants/permissions';
import { ACCESS_ACTION_LABELS, IdentifierAccess, IdentifierField, Identifiers, SecretKey } from '../../../../shared/models/identifiers';
import { firstValueFrom } from 'rxjs';
import { errorMessage } from '../../../../shared/utils/api-error.util';

const REAVEL_SECONDS = 15;

@Component({
  selector: 'app-employee-identifiers',
  imports: [
    CommonModule,
    FormsModule,
    DatePipe
  ],
  templateUrl: './employee-identifiers.component.html',
  styleUrl: './employee-identifiers.component.scss',
})
export class EmployeeIdentifiersComponent implements OnDestroy {
  private service = inject(IdentifierService);
  readonly permissions = inject(PermissionService);
  readonly P = PERMISSIONS;
  readonly actionLabel = ACCESS_ACTION_LABELS;

  employeeId = input.required<string>();

  readonly fields: { key: SecretKey; field: IdentifierField; label: string; hint: string }[] = [
    { key: 'tin', field: IdentifierField.Tin, label: 'TIN', hint: '9 or 12 digits' },
    { key: 'sss', field: IdentifierField.Sss, label: 'SSS number', hint: '10 digits' },
    { key: 'philHealth', field: IdentifierField.PhilHealth, label: 'PhilHealth number', hint: '12 digits' },
    { key: 'pagIbig', field: IdentifierField.PagIbig, label: 'Pag-IBIG number', hint: '12 digits' },
    { key: 'bankAccountNumber', field: IdentifierField.BankAccountNumber, label: 'Bank account number', hint: '5 to 20 digits' }
  ];

  ids = signal<Identifiers | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  revealed = signal<Record<string, string>>({});
  private timers = new Map<string, ReturnType<typeof setTimeout>>();

  log = signal<IdentifierAccess[] | null>(null);

  formOpen = signal(false);
  form: Record<SecretKey, string> & { bankName: string; bankAccountName: string } =
    { tin: '', sss: '', philHealth: '', pagIbig: '', bankAccountNumber: '', bankName: '', bankAccountName: ''};
  clearing = signal<ReadonlySet<SecretKey>>(new Set());
  formError = signal<string | null>(null);
  saving = signal(false);

  constructor() {
    effect(() => { this.employeeId(); void this.load(); })
  }

  ngOnDestroy(): void {
    this.timers.forEach((t) => clearTimeout(t));
  }

  async load(): Promise<void> {
    this.loading.set(true);
    try {
      this.ids.set(await firstValueFrom(this.service.get(this.employeeId())));
      this.error.set(null);
    } catch (err) {
      this.error.set(errorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  async reveal(f: { key: SecretKey; field: IdentifierField; label: string }): Promise<void> {
    if (!confirm(`Show the full ${f.label}? This is recorded.`)) return;
    this.error.set(null);
    try {
      const r = await firstValueFrom(this.service.reveal(this.employeeId(), f.field));
      this.revealed.update((m) => ({ ...m, [f.key]: r.value }));
      clearTimeout(this.timers.get(f.key));
      this.timers.set(f.key, setTimeout(() => this.hide(f.key), REAVEL_SECONDS * 1000));
    } catch (err) {
      this.error.set(errorMessage(err));
    }
  }

  hide(key: SecretKey): void {
    clearTimeout(this.timers.get(key));
    this.revealed.update((m) => { const n = { ...m }; delete n[key]; return n; });
  }

  async toggleLog(): Promise<void> {
    if (this.log() !== null) { this.log.set(null); return; }
    try {
      this.log.set(await firstValueFrom(this.service.accessLog(this.employeeId())));
    } catch (err) {
      this.error.set(errorMessage(err));
    }
  }

  openForm(): void {
    const d = this.ids();
    this.form = { tin: '', sss: '', philHealth: '', pagIbig: '', bankAccountNumber: '', bankName: d?.bankName ?? '', bankAccountName: d?.bankAccountName ?? '' };
    this.clearing.set(new Set());
    this.formError.set(null);
    this.formOpen.set(true);
  }

  toggleClear(key: SecretKey): void {
    this.clearing.update((s) => { const n = new Set(s); if (!n.delete(key)) n.add(key); return n; });
  }

  async save(): Promise<void> {
    const pick = (key: SecretKey): string | null =>
        this.clearing().has(key) ? '' : (this.form[key].trim() || null);

    this.saving.set(true);
    this.formError.set(null);
    try {
      this.ids.set(await firstValueFrom(this.service.save(this.employeeId(), {
        tin: pick('tin'),
        sss: pick('sss'),
        philHealth: pick('philHealth'),
        pagIbig: pick('pagIbig'),
        bankAccountNumber: pick('bankAccountNumber'),
        bankName: this.form.bankName.trim(),
        bankAccountName: this.form.bankAccountName.trim()
      })));
      this.formOpen.set(false);
      this.log.set(null);
    } catch (err) {
      this.formError.set(errorMessage(err));
    } finally {
      this.saving.set(false);
    }
  }

  fieldName(f: number): string {
    return this.fields.find((x) => x.field === f)?.label ?? 'Field';
  }
}
