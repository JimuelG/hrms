import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApplicantService } from '../../../../core/services/applicant.service';
import { Applicant, ApplicantFormValue } from '../../../../shared/models/recruitment';
import { extractApiError } from '../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-applicants',
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './applicants.component.html',
  styleUrl: './applicants.component.scss',
})
export class ApplicantsComponent implements OnInit{
  private service = inject(ApplicantService);

  applicants = signal<Applicant[]>([]);
  loading = signal(false);
  search = '';

  drawerOpen = signal(false);
  editing = signal<Applicant | null>(null);
  formError = signal<string | null>(null);
  saving = signal(false);
  form: ApplicantFormValue = this.emptyForm();
  pendingFile: File | null = null;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service.getAll(this.search || undefined).subscribe({
      next: (data) => {
        this.applicants.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    })
  }

  openCreate(): void {
    this.editing.set(null);
    this.form = this.emptyForm();
    this.pendingFile = null;
    this.formError.set(null);
    this.drawerOpen.set(true);
  }

  openEdit(a: Applicant): void {
    this.editing.set(a);
    this.form ={
      firstName: a.firstName,
      lastName: a.lastName,
      email: a.email,
      phone: a.phone ?? '',
      skillsSummary: a.skillsSummary ?? '',
      educationSummary: a.educationSummary ?? '',
      experienceSummary: a.experienceSummary ?? ''
    };

    this.pendingFile = null;
    this.formError.set(null);
    this.drawerOpen.set(true);
  }

  closeDrawer(): void {
    this.drawerOpen.set(false)
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.pendingFile = input.files?.[0] ?? null;
  }

  save(): void {
    this.formError.set(null);
    this.saving.set(true);
    const current = this.editing();
    const request = current ? this.service.update(current.id, this.form)
                            : this.service.create(this.form);

    request.subscribe({
      next: (saved) => {
        if (this.pendingFile) {
          this.service.uploadResume(saved.id, this.pendingFile).subscribe({
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
        } else {
          this.saving.set(false);
          this.closeDrawer();
          this.load();
        }
      }
    })
  }

  downloadResume(a: Applicant, event: Event): void {
    event.stopPropagation();
    this.service.downloadResume(a.id).subscribe((blob) => {
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = a.resumeOriginalFileName ?? 'resume';
      link.click();
      window.URL.revokeObjectURL(url);
    });
  }

  remove(a: Applicant, event: Event): void {
    event.stopPropagation();
    if (!confirm(`Delete ${a.firstName} ${a.lastName}?`)) return;
    this.service.delete(a.id).subscribe(() => this.load());
  }

  private emptyForm(): ApplicantFormValue {
    return {
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      skillsSummary: '',
      educationSummary: '',
      experienceSummary: ''
    }
  }
}
