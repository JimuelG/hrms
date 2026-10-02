import { Component, inject, signal } from '@angular/core';
import { DepartmentService } from '../../../../core/services/department.service';
import { Department, DepartmentFormValue } from '../../../../shared/models/organization';
import { extractApiError } from '../../../../shared/utils/api-error.util';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-departments',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './departments.component.html',
  styleUrl: './departments.component.scss',
})
export class DepartmentsComponent {
  private deparmentService = inject(DepartmentService);

  departments = signal<Department[]>([]);
  loading = signal(false);
  search = '';

  drawerOpen = signal(false);
  editing = signal<Department | null>(null);
  formError = signal<string | null>(null);
  saving = signal(false);

  form: DepartmentFormValue = this.emptyForm();

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.deparmentService.getAll(this.search || undefined).subscribe({
      next: (data) => { this.departments.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  openCreate(): void {
    this.editing.set(null);
    this.form = this.emptyForm();
    this.formError.set(null);
    this.drawerOpen.set(true);
  }

  openEdit(dept: Department): void {
    this.editing.set(dept);
    this.form = { name: dept.name, code: dept.code, description: dept.description ?? '', isActive: dept.isActive };
    this.formError.set(null);
    this.drawerOpen.set(true);
  }

  closeDrawer(): void { this.drawerOpen.set(false); }

  save(): void {
    this.formError.set(null);
    this.saving.set(true);
    const current = this.editing();
    const request = current ? this.deparmentService.update(current.id, this.form) : this.deparmentService.create(this.form);

    request.subscribe({
      next: () => { this.saving.set(false); this.closeDrawer(); this.load(); },
      error: (err) => { this.saving.set(false); this.formError.set(extractApiError(err)); }
    });
  }

  remove(dept: Department): void {
    if (!confirm(`Delete "${dept.name}"? This can't be undone from here.`)) return;
    this.deparmentService.delete(dept.id).subscribe(() => this.load());
  }

  private emptyForm(): DepartmentFormValue {
    return { name: '', code: '', description: '', isActive: true };
  }
}
