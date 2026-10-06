export enum EmploymentType {
    FullTime = 1,
    PartTime = 2,
    Contractual = 3,
    Intern = 4
}

export enum JobPostingStatus {
    Draft = 1,
    Open = 2,
    OnHold = 3,
    Closed = 4,
    Filled = 5,
    Cancelled = 6
}

export const JOB_POSTING_STATUS_LABELS: Record<JobPostingStatus, string> = {
    [JobPostingStatus.Draft]: 'Draft',
    [JobPostingStatus.Open]: 'Open',
    [JobPostingStatus.OnHold]: 'On hold',
    [JobPostingStatus.Closed]: 'Closed',
    [JobPostingStatus.Filled]: 'Filled',
    [JobPostingStatus.Cancelled]: 'Cancelled',
}

export interface JobPosting {
    id: string;
    title: string;
    positionId: string;
    positionTitle: string;
    departmentId: string;
    departmentName: string;
    branchId: string;
    branchName: string;
    employmentType: EmploymentType;
    description: string;
    requirements: string | null;
    skills: string | null;
    educationRequirement: string | null;
    experienceRequirement: string | null;
    salaryMin: number | null;
    salaryMax: number;
    salaryVisible: boolean;
    vacancies: number;
    applicationDeadline: string | null;
    hiringManagerId: string | null;
    hiringManagerName: string | null;
    status: JobPostingStatus;
    createdAtUtc: string;
}

export interface JobPostingFormValue {
    title: string;
    positionId: string;
    departmentId: string;
    branchId: string;
    employmentType: EmploymentType;
    description: string;
    requirements?: string;
    skills?: string;
    educationRequirement?: string;
    experienceRequirement?: string;
    salaryMin?: number | null;
    salaryMax?: number | null;
    salaryVisible: boolean;
    vacancies: number;
    applicationDeadline?: string | null;
    hiringManagerId?: string | null;
    status?: JobPostingStatus;
}

export interface Applicant {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    phone: string | null;
    skillsSummary: string | null;
    educationSummary: string | null;
    experienceSummary: string | null;
    resumeOriginalFileName: string | null;
    createdAtUtc: string;
}

export interface ApplicantFormValue {
    firstName: string;
    lastName: string;
    email: string;
    phone?: string;
    skillsSummary?: string;
    educationSummary?: string;
    experienceSummary?: string;
}

export enum ApplicationStatus {
    Applied = 1,
    Screening = 2,
    AtsEvaluation = 3,
    HrInterview = 4,
    DepartmentInterview = 5,
    Assessment = 6,
    FinalInterview = 7,
    JobOffer = 8,
    Requirements = 9,
    OnBoarding = 10,
    Hired = 11,
    Rejected = 12,
    Withdrawn = 13,
    OnHold = 14,
    TalentPool = 15
}

export const PIPELINE_COLUMNS: { status: ApplicationStatus; label: string } [] = [
    { status: ApplicationStatus.Applied, label: 'Applied'},
    { status: ApplicationStatus.Screening, label: 'Screening'},
    { status: ApplicationStatus.AtsEvaluation, label: 'Ats Evaluation'},
    { status: ApplicationStatus.HrInterview, label: 'Hr Interview'},
    { status: ApplicationStatus.DepartmentInterview, label: 'Department Interview'},
    { status: ApplicationStatus.Assessment, label: 'Assessment'},
    { status: ApplicationStatus.FinalInterview, label: 'Final Interview'},
    { status: ApplicationStatus.JobOffer, label: 'Job Offer'},
    { status: ApplicationStatus.Requirements, label: 'Requirements'},
    { status: ApplicationStatus.OnBoarding, label: 'On Boarding'},
    { status: ApplicationStatus.Hired, label: 'Hired'},
];

export const SIDE_STATUSES: { status: ApplicationStatus; label: string } [] = [
    { status: ApplicationStatus.Rejected, label: 'Rejected'},
    { status: ApplicationStatus.Withdrawn, label: 'Withdrawn'},
    { status: ApplicationStatus.OnHold, label: 'On Hold'},
    { status: ApplicationStatus.TalentPool, label: 'Talent Pool'},
]

export const ALL_APPLICATION_STATUSES = [ ...PIPELINE_COLUMNS, ...SIDE_STATUSES];

export const APPLICATION_STATUS_LABELS: Record<ApplicationStatus, string> =
    Object.fromEntries(ALL_APPLICATION_STATUSES.map((s) => [s.status, s.label])) as Record<ApplicationStatus, string>;

export interface ApplicationItem {
    id: string;
    applicantId: string;
    applicantName: string;
    applicantEmail: string;
    jobPostingId: string;
    jobPostingTitle: string;
    status: ApplicationStatus;
    notes: string | null;
    appliedAtUtc: string;
}

export enum InterviewType {
    Screening = 1,
    HrInterview = 2,
    DepartmentInterview = 3,
    FinalInterview = 4
};

export const INTERVIEW_TYPE_LABELS: Record<InterviewType, string> = {
    [InterviewType.Screening]: 'Screening',
    [InterviewType.HrInterview]: 'Hr Interview',
    [InterviewType.DepartmentInterview]: 'Department Interview',
    [InterviewType.FinalInterview]: 'Final Interview',
};

export enum InterviewStatus {
    Scheduled = 1,
    Completed = 2,
    Cancelled = 3,
    NoShow = 4,
}

export enum InterviewRecommendation {
    StrongNo = 1,
    No = 2,
    Maybe = 3,
    Yes = 4,
    StrongYes = 5
}

export const RECOMMENDATION_LABELS: Record<InterviewRecommendation, string> = {
    [InterviewRecommendation.StrongNo]: 'Strong No',
    [InterviewRecommendation.No]: 'No',
    [InterviewRecommendation.Maybe]: 'Maybe',
    [InterviewRecommendation.Yes]: 'Yes',
    [InterviewRecommendation.StrongYes]: 'Strong Yes',
}

export interface InterviewEvaluation {
    id: string;
    rating: number;
    recommendation: InterviewRecommendation;
    strengths: string | null;
    concerns: string | null;
    notes: string | null;
    submittedByUserId: string;
    submittedAtUtc: string;
}

export interface Interview {
    id: string;
    applicationId: string;
    applicantName: string;
    jobPostingTitle: string;
    type: InterviewType;
    scheduledAtUtc: string;
    durationMinutes: number;
    location: string | null;
    interviewerId: string;
    interviewerName: string;
    status: InterviewStatus;
    cancellationReason: string | null;
    evaluation: InterviewEvaluation | null;
}

export interface ScheduleInterviewValue {
    applicationId: string;
    type: InterviewType;
    scheduledAtUtc: string;
    durationMinutes: number;
    location?: string;
    interviewerId: string;
}

export interface SubmitEvaluationValue {
    rating: number;
    recommendation: InterviewRecommendation;
    strengths?: string;
    concerns?: string;
    notes?: string;
}

export enum JobOfferStatus {
    Draft = 1,
    Sent = 2,
    Accepted = 3,
    Declined = 4,
    Withdrawn = 5,
    Expired = 6
}

export const OFFER_STATUS_LABELS: Record<JobOfferStatus, string> = {
    [JobOfferStatus.Draft]: 'Draft',
    [JobOfferStatus.Sent]: 'Sent',
    [JobOfferStatus.Accepted]: 'Accepted',
    [JobOfferStatus.Declined]: 'Declined',
    [JobOfferStatus.Withdrawn]: 'Withdrawn',
    [JobOfferStatus.Expired]: 'Expired'
}

export interface JobOffer {
    id: string;
    applicationId: string;
    applicantName: string;
    jobPostingTitle: string;
    proposedSalary: number;
    currency: string;
    proposedStartDate: string;
    expiresOn: string | null;
    terms: string | null;
    status: JobOfferStatus;
    sentAtUtc: string | null;
    respondedAtUtc: string | null;
    declineReason: string | null;
    createdAtUtc: string;
}

export interface CreateJobOfferValue {
    applicationId: string;
    proposedSalary: number;
    currency: string;
    proposedStartDate: string;
    expiresOn?: string | null;
    terms?: string;
}
