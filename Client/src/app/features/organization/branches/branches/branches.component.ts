import { Component, inject, signal } from '@angular/core';
import { BranchService } from '../../../../core/services/branch.service';
import { Branch, BranchFormValue } from '../../../../shared/models/organization';
import { extractApiError } from '../../../../shared/utils/api-error.util';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-branches',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './branches.component.html',
  styleUrl: './branches.component.scss',
})
export class BranchesComponent {
  private branchService = inject(BranchService);

  branches = signal<Branch[]>([]);
  loading = signal(false);
  search = '';

  drawerOpen = signal(false);
  editing = signal<Branch | null>(null);
  formError = signal<string | null>(null);
  saving = signal(false);

  form: BranchFormValue = this.emptyForm();

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.branchService.getAll(this.search || undefined).subscribe({
      next: (data) => { this.branches.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  openCreate(): void {
    this.editing.set(null);
    this.form = this.emptyForm();
    this.formError.set(null);
    this.drawerOpen.set(true);
  }

  openEdit(branch: Branch): void {
    this.editing.set(branch);
    this.form = {
      name: branch.name, code: branch.code, addressLine: branch.addressLine ?? '',
      city: branch.city ?? '', country: branch.country ?? '',
      timeZoneId: branch.timeZoneId ?? '', isActive: branch.isActive
    };
    this.formError.set(null);
    this.drawerOpen.set(true);
  }

  closeDrawer(): void {
    this.drawerOpen.set(false);
  }

  save(): void {
    this.formError.set(null);
    this.saving.set(true);
    const current = this.editing();

    const request = current
      ? this.branchService.update(current.id, this.form)
      : this.branchService.create(this.form);

    request.subscribe({
      next: () => { this.saving.set(false); this.closeDrawer(); this.load(); },
      error: (err) => { this.saving.set(false); this.formError.set(extractApiError(err)); }
    });
  }

  remove(branch: Branch): void {
    if (!confirm(`Delete "${branch.name}"? This can't be undone from here.`)) return;
    this.branchService.delete(branch.id).subscribe(() => this.load());
  }

  private emptyForm(): BranchFormValue {
    return { name: '', code: '', addressLine: '', city: '', country: '', timeZoneId: '', isActive: true };
  }
}
