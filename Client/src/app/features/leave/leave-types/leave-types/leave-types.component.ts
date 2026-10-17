import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LeaveAdminService } from '../../../../core/services/leave-admin.service';
import { ENTITLEMENT_MODE_LABELS, LeaveEntitlementMode, LeaveType, LeaveTypeFormValue } from '../../../../shared/models/leave';
import { errorMessage } from '../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-leave-types',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './leave-types.component.html',
  styleUrl: './leave-types.component.scss',
})
export class LeaveTypesComponent implements OnInit {
  private admin = inject(LeaveAdminService);

  readonly Mode = LeaveEntitlementMode;
  readonly modeLabel = ENTITLEMENT_MODE_LABELS;
  readonly modes = [LeaveEntitlementMode.Annual, LeaveEntitlementMode.HrGranted, LeaveEntitlementMode.Unlimited];

  types = signal<LeaveType[]>([]);
  loading = signal(true);
  loadError = signal<string | null>(null);
  actionError = signal<string | null>(null);

  drawerOpen = signal(false);
  editing = signal<LeaveType | null>(null);
  formError = signal<string | null>(null);
  saving = signal(false);
  form: LeaveTypeFormValue = this.emptyForm();
  needsDocument = false;
  documentAfterDays = 2;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.admin.getTypes().subscribe({
      next: (t) => {
        this.types.set(t);
        this.loadError.set(null);
        this.loading.set(false);
      },
      error: (err) => {
        this.loadError.set(errorMessage(err));
        this.loading.set(false);
      }
    })
  }

  openCreate(): void {
    this.editing.set(null);
    this.form = this.emptyForm();
    this.needsDocument = false;
    this.documentAfterDays = 2;
    this.formError.set(null);
    this.drawerOpen.set(true);
  }

  openEdit(t: LeaveType): void {
    this.editing.set(t);
    this.form = {
      code: t.code,
      name: t.name,
      description: t.description,
      isPaid: t.isPaid,
      entitlementMode: t.entitlementMode,
      defaultDays: t.defaultDays,
      prorateFirstYear: t.prorateFirstYear,
      maxCarryOverDays: t.maxCarryOverDays,
      minServiceMonths: t.minServiceMonths,
      allowsHalfDay: t.allowsHalfDay,
      reqiuresDocumentAfterDays: t.requiresDocumentAfterDays,
      isActive: t.isActive
    };
    this.needsDocument = t.requiresDocumentAfterDays !== null;
    this.documentAfterDays = t.requiresDocumentAfterDays ?? 2;
    this.formError.set(null);
    this.drawerOpen.set(true);
  }

  closeDrawer(): void {
    this.drawerOpen.set(false);
  }

  setMode(mode: LeaveEntitlementMode): void {
    this.form.entitlementMode = mode;
    if (mode === LeaveEntitlementMode.Unlimited) this.form.defaultDays = 0;
    if (mode !== LeaveEntitlementMode.Annual) {
      this.form.prorateFirstYear = false;
      this.form.maxCarryOverDays = 0;
    }
  }

  save(): void {
    const f = this.form;
    if (!f.code.trim() || !f.name.trim()) {
      this.formError.set('Enter a code and a name.');
      return;
    }

    this.formError.set(null);
    this.saving.set(true);

    const payload: LeaveTypeFormValue = {
      ...f,
      code: f.code.trim(),
      name: f.name.trim(),
      description: f.description?.trim() || null,
      reqiuresDocumentAfterDays: this.needsDocument ?  this.documentAfterDays : null
    };

    const current = this.editing();
    const request = current 
                  ? this.admin.updateType(current.id, payload) 
                  : this.admin.createType(payload);

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
    })
  }

  remove(t: LeaveType): void {
    if (!confirm(`Delete "${t.name}"? Types with history can only be deactiveated.`)) return;
    this.actionError.set(null);
    this.admin.deleteType(t.id).subscribe({
      next: () => this.load(),
      error: (err) => this.actionError.set(errorMessage(err))
    })
  }

  private emptyForm(): LeaveTypeFormValue {
    return {
      code: '',
      name: '',
      description: null,
      isPaid: true,
      entitlementMode: LeaveEntitlementMode.Annual,
      defaultDays: 10,
      prorateFirstYear: true,
      maxCarryOverDays: 0,
      minServiceMonths: 0,
      allowsHalfDay: true,
      reqiuresDocumentAfterDays: null,
      isActive: true
    };
  }
}
