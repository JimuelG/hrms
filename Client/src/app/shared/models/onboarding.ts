export interface OnboardingTaskTemplate {
    id: string;
    title: string;
    description: string | null;
    isRequired: boolean;
    sortOrder: number;
    isActive: boolean;
}

export interface OnboardingTaskTemplateFormValue {
    title: string;
    description?: string;
    isRequired: boolean;
    sortOrder: number;
    isActive?: boolean;
}

export enum OnboardingCaseStatus {
    InProgress = 1,
    ReadyToConvert = 2,
    Completed = 3,
    Cancelled = 4
}

export const ONBOARDING_STATUS_LABELS: Record<OnboardingCaseStatus, string> = {
    [OnboardingCaseStatus.InProgress]: 'In progress',
    [OnboardingCaseStatus.ReadyToConvert]: 'Ready to convert',
    [OnboardingCaseStatus.Completed]: 'Completed',
    [OnboardingCaseStatus.Cancelled]: 'Cancelled'
}

export interface OnboardingTask {
    id: string;
    title: string;
    description: string | null;
    isRequired: boolean;
    isCompleted: boolean;
    completedAtUtc: string | null;
}

export interface OnboardingCase {
    id: string;
    applicationId: string;
    applicantName: string;
    jobPostingTitle: string;
    status: OnboardingCaseStatus;
    startedAtUtc: string;
    completedAtUtc: string | null;
    createdEmployeedId: string | null;
    tasks: OnboardingTask[];
}

export interface ConvertToEmployeeValue {
    employeeNumber: string;
    branchId: string;
    departmentId: string;
    positionId: string;
    managerId?: string | null;
    employmentType: number;
    hireDate: string;
}