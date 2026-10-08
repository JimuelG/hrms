import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { OnboardingTaskTemplateService } from '../../../../core/services/onboarding-task-template.service';
import { OnboardingTaskTemplate, OnboardingTaskTemplateFormValue } from '../../../../shared/models/onboarding';
import { extractApiError } from '../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-onboarding-templates',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './onboarding-templates.component.html',
  styleUrl: './onboarding-templates.component.scss',
})
export class OnboardingTemplatesComponent implements OnInit{
  private service = inject(OnboardingTaskTemplateService);

  templates = signal<OnboardingTaskTemplate[]>([]);
  loading = signal(true);

  drawerOpen = signal(false);
  editing = signal<OnboardingTaskTemplate | null>(null);
  formError = signal<string | null>(null);
  saving = signal(false);
  form: OnboardingTaskTemplateFormValue = this.emptyForm();

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);

    this.service.getAll().subscribe({
      next: (data) => {
        this.templates.set(data.sort((a, b) => a.sortOrder - b.sortOrder));
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false)
      }
    })
  }

  openCreate(): void {
    this.editing.set(null);
    this.form = { ...this.emptyForm(), sortOrder: this.templates().length + 1 };
    this.formError.set(null);
    this.drawerOpen.set(true);
  }

  openEdit(t: OnboardingTaskTemplate): void {
    this.editing.set(t);
    this.form = {
      title: t.title,
      description: t.description ?? '',
      isRequired: t.isRequired,
      sortOrder: t.sortOrder,
      isActive: t.isActive
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
    const request = current ? this.service.update(current.id, this.form) : this.service.create(this.form);
    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.closeDrawer();
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(extractApiError(err));
      }
    })
  }

  remove(t: OnboardingTaskTemplate): void {
    if (!confirm(`Delete "${t.title}"?`)) return;
    this.service.delete(t.id).subscribe(() => this.load());
  }

  private emptyForm(): OnboardingTaskTemplateFormValue {
    return {
      title: '',
      description: '',
      isRequired: true,
      sortOrder: 1,
      isActive: true
    };
  }
}
