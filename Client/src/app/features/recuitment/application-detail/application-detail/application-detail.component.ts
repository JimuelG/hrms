import { CommonModule, DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApplicationService } from '../../../../core/services/application.service';
import { ApplicantService } from '../../../../core/services/applicant.service';
import { InterviewService } from '../../../../core/services/interview.service';
import { JobOfferService } from '../../../../core/services/job-offer.service';
import { EmployeeService } from '../../../../core/services/employee.service';
import { APPLICATION_STATUS_LABELS, ApplicationItem, Interview, INTERVIEW_TYPE_LABELS, InterviewRecommendation, InterviewStatus, InterviewType, JobOffer, JobOfferStatus, OFFER_STATUS_LABELS, RECOMMENDATION_LABELS, ScheduleInterviewValue } from '../../../../shared/models/recruitment';
import { Employee } from '../../../../shared/models/employee';
import { extractApiError } from '../../../../shared/utils/api-error.util';

type Tab = 'overview' | 'interviews' | 'offer';

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
  private applicantService = inject(ApplicantService);
  private interviewService = inject(InterviewService);
  private offerService = inject(JobOfferService);
  private employeeService = inject(EmployeeService);

  applicationId = this.route.snapshot.paramMap.get('id')!;
  application = signal<ApplicationItem | null>(null);
  tab = signal<Tab>('overview');

  interviewTypeLabel = INTERVIEW_TYPE_LABELS;
  recommendationLabel = RECOMMENDATION_LABELS;
  offerStatusLabel = OFFER_STATUS_LABELS;
  applicationStatusLabel = APPLICATION_STATUS_LABELS;
  InterviewStatus = InterviewStatus;
  JobOfferStatus = JobOfferStatus;

  interviewTypeOptions = Object.values(InterviewType).filter((v): v is InterviewType => typeof v === 'number');
  recommendationsOptions = Object.values(InterviewRecommendation).filter((v): v is InterviewRecommendation => typeof v === 'number');

  interviews = signal<Interview[]>([]);
  employees = signal<Employee[]>([]);
  scheduleOpen = signal(false);
  scheduleError = signal<string | null>(null);
  scheduleForm: Partial<ScheduleInterviewValue> = { type: InterviewType.Screening, durationMinutes: 30};

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
    this.employeeService.getAll().subscribe((e) => this.employees.set(e)); 
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
      proposedStartDate: '0',
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
}
