import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApplicationService } from '../../../../core/services/application.service';
import { JobPostingService } from '../../../../core/services/job-posting.service';
import { ALL_APPLICATION_STATUSES, Applicant, ApplicationItem, ApplicationStatus, JobPosting, PIPELINE_COLUMNS, SIDE_STATUSES } from '../../../../shared/models/recruitment';
import { extractApiError } from '../../../../shared/utils/api-error.util';
import { ApplicantService } from '../../../../core/services/applicant.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-pipeline',
  imports: [
    CommonModule,
    RouterLink,
    FormsModule
  ],
  templateUrl: './pipeline.component.html',
  styleUrl: './pipeline.component.scss',
})
export class PipelineComponent implements OnInit{
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private applicationService = inject(ApplicationService);
  private applicantService = inject(ApplicantService);
  private jobPostingService = inject(JobPostingService);

  jobPostingId = this.route.snapshot.paramMap.get('jobPostingId')!;
  posting = signal<JobPosting | null>(null);
  applications = signal<ApplicationItem[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  pipelineColumns = PIPELINE_COLUMNS;
  sideStatuses = SIDE_STATUSES;
  allStatuses = ALL_APPLICATION_STATUSES;

  addOpen = signal(false);
  applicants = signal<Applicant[]>([]);
  applicantSearch = '';
  selectedApplicantId = '';
  adding = signal(false);
  addError = signal<string | null>(null);

  openMenuFor = signal<string | null>(null);

  ngOnInit(): void {
    this.jobPostingService.getById(this.jobPostingId).subscribe((p) => this.posting.set(p));
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.applicationService.getByPosting(this.jobPostingId).subscribe({
      next: (data) => {
        this.applications.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    })
  }

  columnItems(status: ApplicationStatus): ApplicationItem[] {
    return this.applications().filter((a) => a.status === status);
  }

  openCard(app: ApplicationItem): void {
    this.router.navigate(['/recruitment/applications', app.id]);
  }

  toggleMenu(appId: string, event: Event): void {
    event.stopPropagation();
    this.openMenuFor.set(this.openMenuFor() === appId ? null : appId);
  }

  changeStatus(app: ApplicationItem, status: ApplicationStatus, event: Event): void {
    event.stopPropagation;
    this.openMenuFor.set(null);

    this.applicationService.updateStatus(app.id, status).subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(extractApiError(err))
    });
  }

  openAddApplicant(): void {
    this.addError.set(null);
    this.selectedApplicantId = '';
    this.applicantService.getAll(this.applicantSearch || undefined).subscribe((a) => this.applicants.set(a));
    this.addOpen.set(true);
  }

  searchApplicants(): void {
    this.applicantService.getAll(this.applicantSearch || undefined).subscribe((a) => this.applicants.set(a));
  }

  addApplicantToPipeline(): void {
    if (!this.selectedApplicantId) return;

    this.addError.set(null);
    this.adding.set(true);

    this.applicationService.create(this.selectedApplicantId, this.jobPostingId).subscribe({
      next: () => {
        this.adding.set(false);
        this.addOpen.set(false);
        this.load();
      },
      error: (err) => {
        this.adding.set(false);
        this.addError.set(extractApiError(err));
      }
    })
  }
}
