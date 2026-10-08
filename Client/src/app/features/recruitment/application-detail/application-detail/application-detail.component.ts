import { CommonModule, DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApplicationService } from '../../../../core/services/application.service';
import { InterviewService } from '../../../../core/services/interview.service';
import { JobOfferService } from '../../../../core/services/job-offer.service';
import { EmployeeService } from '../../../../core/services/employee.service';
import { APPLICATION_STATUS_LABELS, ApplicationItem, Interview, INTERVIEW_TYPE_LABELS, InterviewRecommendation, InterviewStatus, InterviewType, JobOffer, JobOfferStatus, OFFER_STATUS_LABELS, RECOMMENDATION_LABELS, ScheduleInterviewValue } from '../../../../shared/models/recruitment';
import { Employee, EMPLOYMENT_TYPE_LABELS, EmploymentType } from '../../../../shared/models/employee';
import { extractApiError } from '../../../../shared/utils/api-error.util';
import { OnboardingService } from '../../../../core/services/onboarding.service';
import { BranchService } from '../../../../core/services/branch.service';
import { DepartmentService } from '../../../../core/services/department.service';
import { PositionService } from '../../../../core/services/position.service';
import { ConvertToEmployeeValue, ONBOARDING_STATUS_LABELS, OnboardingCase, OnboardingCaseStatus } from '../../../../shared/models/onboarding';
import { Branch, Department, Position } from '../../../../shared/models/organization';

type Tab = 'overview' | 'interviews' | 'offer' | 'onboarding';

@Component({
  selector: 'app-application-detail',
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    DatePipe
  ],
  templateUrl: './application-detail.component.html',
  styleUrl: './application-detail.component.scss',
})
export class ApplicationDetailComponent implements OnInit{
  private route = inject(ActivatedRoute);
  private applicationService = inject(ApplicationService);
  private interviewService = inject(InterviewService);
  private offerService = inject(JobOfferService);
  private employeeService = inject(EmployeeService);
  private onboardingService = inject(OnboardingService);
  private branchService = inject(BranchService);
  private departmentService = inject(DepartmentService);
  private positionService = inject(PositionService);

  onboardingCase = signal<OnboardingCase | null>(null);
  onboardingStatusLabel = ONBOARDING_STATUS_LABELS;
  OnboardingCaseStatus = OnboardingCaseStatus;
  onboardingNotStarted = signal(false);
  startingOnboarding = signal(false);

  applicationId = this.route.snapshot.paramMap.get('id')!;
  application = signal<ApplicationItem | null>(null);
  tab = signal<Tab>('overview');

  interviewTypeLabel = INTERVIEW_TYPE_LABELS;
  recommendationLabel = RECOMMENDATION_LABELS;
  offerStatusLabel = OFFER_STATUS_LABELS;
  applicationStatusLabel = APPLICATION_STATUS_LABELS;
  employmentTypeLabel = EMPLOYMENT_TYPE_LABELS;
  InterviewStatus = InterviewStatus;
  JobOfferStatus = JobOfferStatus;

  interviewTypeOptions = Object.values(InterviewType).filter((v): v is InterviewType => typeof v === 'number');
  recommendationsOptions = Object.values(InterviewRecommendation).filter((v): v is InterviewRecommendation => typeof v === 'number');

  interviews = signal<Interview[]>([]);
  employees = signal<Employee[]>([]);
  scheduleOpen = signal(false);
  scheduleError = signal<string | null>(null);
  scheduleForm: Partial<ScheduleInterviewValue> = { type: InterviewType.Screening, durationMinutes: 30};
  
  branches = signal<Branch[]>([]);
  departments = signal<Department[]>([]);
  positions = signal<Position[]>([]);

  employmentTypeOptions = Object.values(EmploymentType).filter((v): v is EmploymentType => typeof v === 'number');

  convertOpen = signal(false);
  convertError = signal<string | null>(null);
  converting = signal(false);
  convertForm: ConvertToEmployeeValue = this.emptyConvertForm();

  evaluatingInterviewId = signal<string | null>(null);
  evalRating = 3;
  evalRecommendation: InterviewRecommendation = InterviewRecommendation.Maybe;
  evalStrengths = '';
  evalConcerns = '';
  evalNotes = '';

  offers = signal<JobOffer[]>([]);
  offerFormOpen = signal(false);
  offerError = signal<string | null>(null);
  offerForm = {
    proposedSalary: 0,
    currency: 'PHP',
    proposedStartDate: '',
    expiresOn: '',
    terms: ''
  }

  ngOnInit(): void {
    this.applicationService.getById(this.applicationId).subscribe((a) => this.application.set(a));
    this.loadInterviews();
    this.loadOffers();
    this.loadOnboarding();
    this.employeeService.getAll().subscribe((e) => this.employees.set(e)); 
    this.branchService.getAll().subscribe((b) => this.branches.set(b));
    this.departmentService.getAll().subscribe((d) => this.departments.set(d));
    this.positionService.getAll().subscribe((p) => this.positions.set(p));
  }

  setTab(t: Tab): void {
    this.tab.set(t);
  }

  loadInterviews(): void {
    this.interviewService.getByApplication(this.applicationId).subscribe((i) => this.interviews.set(i));
  }

  loadOffers(): void {
    this.offerService.getByApplication(this.applicationId).subscribe((o) => this.offers.set(o));
  }

  openSchedule(): void {
    this.scheduleForm = { 
      applicationId: this.applicationId,
      type: InterviewType.Screening,
      durationMinutes: 30
    };
    this.scheduleError.set(null);
    this.scheduleOpen.set(true);
  }

  submitSchedule(): void {
    this.scheduleError.set(null);
    this.interviewService.schedule(this.scheduleForm as ScheduleInterviewValue).subscribe({
      next: () => {
        this.scheduleOpen.set(false);
        this.loadInterviews();
      },
      error: (err) => {
        this.scheduleError.set(extractApiError(err));
      }
    })
  }

  completeInterview(i: Interview): void {
    this.interviewService.complete(i.id).subscribe({
      next: () => {
        this.loadInterviews();
      }
    })
  }

  cancelInterview(i: Interview): void {
    const reason = prompt('Cancellation reason:');
    if (!reason) return;
    this.interviewService.cancel(i.id, reason).subscribe({
      next: () => {
        this.loadInterviews();
      },
      error: (err) => {
        alert(extractApiError(err));
      }
    }) 
  }


  openEvaluate(i: Interview): void {
    this.evaluatingInterviewId.set(i.id);
    this.evalRating = 3;
    this.evalRecommendation = InterviewRecommendation.Maybe;
    this.evalStrengths = '';
    this.evalConcerns = '';
    this.evalNotes = '';
  }

  submitEvaluation(): void {
    const id = this.evaluatingInterviewId();

    if (!id) return;

    this.interviewService.submitEvaluation(id, {
      rating: this.evalRating,
      recommendation: this.evalRecommendation,
      strengths: this.evalStrengths || undefined,
      concerns: this.evalConcerns || undefined,
      notes: this.evalNotes || undefined
    }).subscribe({
      next: () => {
        this.evaluatingInterviewId.set(null);
        this.loadInterviews();
      },
      error: (err) => {
        alert(extractApiError(err));
      }
    })
  }

  openCreateOffer(): void {
    this.offerForm = {
      proposedSalary: 0,
      currency: 'PHP',
      proposedStartDate: '',
      expiresOn: '',
      terms: ''
    };
    this.offerError.set(null);
    this.offerFormOpen.set(true);
  }

  submitOffer(): void {
    this.offerError.set(null);
    this.offerService.create({ applicationId: this.applicationId, ...this.offerForm }).subscribe({
      next: () => {
        this.offerFormOpen.set(false);
        this.loadOffers();
      },
      error: (err) => {
        this.offerError.set(extractApiError(err));
      }
    })
  }

  sendOffer(o: JobOffer): void {
    this.offerService.send(o.id).subscribe({
      next: () => {
        this.loadOffers();
      },
      error: (err) => {
        alert(extractApiError(err));
      }
    })
  }

  acceptOffer(o: JobOffer): void {
    this.offerService.accept(o.id).subscribe({
      next: () => {
        this.loadOffers();
      },
      error: (err) => {
        alert(extractApiError(err));
      }
    })
  }

  declineOffer(o: JobOffer): void {
    const reason = prompt('Decline reason (optional):') ?? undefined;
    this.offerService.decline(o.id, reason).subscribe({
      next: () => {
        this.loadOffers()
      },
      error: (err) => {
        alert(extractApiError(err))
      }
    })
  }

  withdrawOffer(o: JobOffer): void {
    this.offerService.withdraw(o.id).subscribe({
      next: () => {
        this.loadOffers();
      },
      error: (err) => {
        alert(extractApiError(err));
      }
    })
  }

  loadOnboarding(): void {
    this.onboardingService.getByApplication(this.applicationId).subscribe({
      next: (c) => {
        this.onboardingCase.set(c);
        this.onboardingNotStarted.set(false);
      },
      error: () => {
        this.onboardingCase.set(null);
        this.onboardingNotStarted.set(true);
      }
    })
  }

  startOnboarding(): void {
    this.startingOnboarding.set(true);
    this.onboardingService.start(this.applicationId).subscribe({
      next: () => {
        this.startingOnboarding.set(false);
        this.loadOnboarding();
      },
      error: (err) => {
        this.startingOnboarding.set(false);
        alert(extractApiError(err));
      }
    })
  }

  toggleTask(task: { id: string; isCompleted: boolean }): void {
    const c = this.onboardingCase();
    if (!c) return;

    const request = task.isCompleted
      ? this.onboardingService.reopenTask(c.id, task.id)
      : this.onboardingService.completeTask(c.id, task.id);
    
      request.subscribe({
        next: (updated) => {
          this.onboardingCase.set(updated);
        },
        error: (err) => {
          alert(extractApiError(err))
        }
      })
  }

  openConvert(): void {
    this.convertForm = this.emptyConvertForm();
    this.convertError.set(null);
    this.convertOpen.set(true);
  }

  submitConvert(): void {
    const c = this.onboardingCase();
    if (!c) return;

    this.convertError.set(null);
    this.converting.set(true);

    this.onboardingService.convert(c.id, this.convertForm).subscribe({
      next: () => {
        this.converting.set(false);
        this.convertOpen.set(false);
        this.loadOnboarding();
      },
      error: (err) => {
        this.converting.set(false);
        this.convertError.set(extractApiError(err));
      }
    })
  }

  private emptyConvertForm(): ConvertToEmployeeValue {
    return {
      employeeNumber: '',
      branchId: '',
      departmentId: '',
      positionId: '',
      employmentType: 1,
      hireDate: ''
    }
  }
}
