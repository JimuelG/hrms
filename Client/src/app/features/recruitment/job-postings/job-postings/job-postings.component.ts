import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { JobPostingService } from '../../../../core/services/job-posting.service';
import { PositionService } from '../../../../core/services/position.service';
import { DepartmentService } from '../../../../core/services/department.service';
import { BranchService } from '../../../../core/services/branch.service';
import { EmploymentType, JOB_POSTING_STATUS_LABELS, JobPosting, JobPostingFormValue, JobPostingStatus } from '../../../../shared/models/recruitment';
import { Branch, Department, Position } from '../../../../shared/models/organization';
import { Router } from '@angular/router';
import { forkJoin, take } from 'rxjs';
import { extractApiError } from '../../../../shared/utils/api-error.util';

@Component({
  selector: 'app-job-postings',
  imports: [
    CommonModule,
    FormsModule,
  ],
  templateUrl: './job-postings.component.html',
  styleUrl: './job-postings.component.scss',
})
export class JobPostingsComponent implements OnInit{
  private service = inject(JobPostingService);
  private positionService = inject(PositionService);
  private departmentService = inject(DepartmentService);
  private branchService = inject(BranchService);
  private router = inject(Router);

  postings = signal<JobPosting[]>([]);
  positions = signal<Position[]>([]);
  departments = signal<Department[]>([]);
  branches = signal<Branch[]>([]);
  loading = signal(true);
  search = '';
  statusFilter: JobPostingStatus | null = null;
  statusLabel = JOB_POSTING_STATUS_LABELS;
  statuses = Object.entries(JOB_POSTING_STATUS_LABELS).map(([v, l]) => ({ value: +v as JobPostingStatus, label: l}));

  drawerOpen = signal(false);
  editing = signal<JobPosting | null>(null);
  formError = signal<string | null>(null);
  saving = signal(false);
  form: JobPostingFormValue = this.emptyForm();

  ngOnInit(): void {
    forkJoin({
      positions: this.positionService.getAll(),
      departments: this.departmentService.getAll(),
      branches: this.branchService.getAll()
    }).subscribe(({ positions, departments, branches }) => {
      this.positions.set(positions);
      this.departments.set(departments);
      this.branches.set(branches);
    })

    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.service.getAll(this.search || undefined, this.statusFilter ?? undefined).subscribe({
      next: (data) => {
        this.postings.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    })
  }

  openPipeline(p: JobPosting): void {
    this.router.navigate(['/recruitment/pipeline/', p.id])
  }

  openCreate(): void {
    this.editing.set(null);

    this.emptyForm();
    this.formError.set(null);
    this.drawerOpen.set(true);
  }

  openEdit(p: JobPosting, event: Event): void {
    event.stopPropagation();
    this.editing.set(p);

    this.form = {
      title: p.title,
      positionId: p.positionId,
      departmentId: p.departmentId,
      branchId: p.branchId,
      employmentType: p.employmentType,
      description: p.description,
      requirements: p.requirements ?? '',
      skills: p.skills ?? '',
      educationRequirement: p.educationRequirement ?? '',
      experienceRequirement: p.experienceRequirement ?? '',
      salaryMin: p.salaryMin,
      salaryMax: p.salaryMax,
      salaryVisible: p.salaryVisible,
      vacancies: p.vacancies,
      applicationDeadline: p.applicationDeadline,
      hiringManagerId: p.hiringManagerId,
      status: p.status
    }

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
    const request = current ? this.service.update(current.id, this.form)
                            : this.service.create(this.form);

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

  remove(p: JobPosting, event: Event): void {
    event.stopPropagation();
    if (!confirm(`Delete "${p.title}"`)) return;
    this.service.delete(p.id).subscribe(() => this.load());
  }

  private emptyForm(): JobPostingFormValue {
    return {
      title: '',
      positionId: '',
      departmentId: '',
      branchId: '',
      employmentType: EmploymentType.FullTime,
      description: '',
      salaryVisible: false,
      vacancies: 1,
      status: JobPostingStatus.Draft
    }
  }
}
