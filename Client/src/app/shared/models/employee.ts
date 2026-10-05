export enum EmploymentType {
    FullTime = 1,
    PartTime = 2,
    Contractual = 3,
    Intern = 4
}

export enum EmployeeStatus {
    Applicant = 1,
    PreOnboarding = 2,
    Probationary = 3,
    Regular = 4,
    OnLeave = 5,
    Suspended = 6,
    Resigned = 7,
    Terminated = 8,
    Archived = 9
}

export const EMPLOYMENT_TYPE_LABELS: Record<EmploymentType, string> = {
    [EmploymentType.FullTime]: 'Full-time',
    [EmploymentType.PartTime]: 'Part-time',
    [EmploymentType.Contractual]: 'Contractual',
    [EmploymentType.Intern]: 'Intern'
};

export const EMPLOYEE_STATUS_LABELS: Record<EmployeeStatus, string> = {
    [EmployeeStatus.Applicant]: 'Applicant',
    [EmployeeStatus.PreOnboarding]: 'Pre-onboarding',
    [EmployeeStatus.Probationary]: 'Probationary',
    [EmployeeStatus.Regular]: 'Regular',
    [EmployeeStatus.OnLeave]: 'On leave',
    [EmployeeStatus.Suspended]: 'Suspended',
    [EmployeeStatus.Resigned]: 'Resigned',
    [EmployeeStatus.Terminated]: 'Terminated',
    [EmployeeStatus.Archived]: 'Archived'
}

export interface Employee {
    id: string;
    employeeNumber: string;
    firstName: string;
    lastName: string;
    email: string;
    phone: string | null;
    dateOfBirth: string | null;
    branchId: string;
    branchName: string;
    departmentId: string;
    departmentName: string;
    positionId: string;
    positionTitle: string;
    managerId: string | null;
    managerName: string | null;
    employmentType: EmploymentType;
    hireDate: string;
    status: EmployeeStatus;
    createdAtUtc: string;
}

export interface EmployeeFormValue {
    employeeNumber?: string;
    firstName: string;
    lastName: string;
    email: string;
    phone?: string;
    dateOfBirth?: string | null;
    branchId: string;
    departmentId: string;
    positionId: string;
    managerId?: string | null;
    employmentType: EmploymentType;
    hireDate: string;
    status: EmployeeStatus;
}

export interface EmergencyContact {
    id: string;
    name: string;
    relationship: string;
    phone: string;
    alternatePhone: string | null;
    address: string | null;
    isPrimary: boolean;
}

export interface EmergencyContactFormValue {
    name: string;
    relationship: string;
    phone: string;
    alternatePhone?: string;
    address?: string;
    isPrimary: boolean;
}

export enum DocumentStatus {
    Pending = 1,
    Verified = 2,
    Rejected = 3
}

export interface EmployeeDocument {
    id: string;
    documentType: string;
    originalFileName: string;
    contentType: string;
    sizeBytes: number;
    status: DocumentStatus;
    verifiedAtUtc: string | null;
    rejectionReason: string | null;
    expirationDate: string | null;
    uploadedAtUtc: string;
}

export enum TimelineEventType {
    Hired = 1,
    StatusChanged = 2,
    BranchChanged = 3,
    DepartmentChanged = 4,
    PositionChanged = 5,
    ManagerChanged = 6,
    DocumentUpload = 7,
    DocumentVerified = 8,
    DocumentRejected = 9,
    Note = 99
}

export const TIMELINE_ICONS: Record<TimelineEventType, string> = {
    [TimelineEventType.Hired]: 'fa-handshake',
    [TimelineEventType.StatusChanged]: 'fa-arrows-rotate',
    [TimelineEventType.BranchChanged]: 'fa-building',
    [TimelineEventType.DepartmentChanged]: 'fa-sitemap',
    [TimelineEventType.PositionChanged]: 'fa-id-badge',
    [TimelineEventType.ManagerChanged]: 'fa-user-tie',
    [TimelineEventType.DocumentUpload]: 'fa-file-arrow-up',
    [TimelineEventType.DocumentVerified]: 'fa-file-circle-check',
    [TimelineEventType.DocumentRejected]: 'fa-file-circle-xmark',
    [TimelineEventType.Note]: 'fa-note-sticky'
}

export interface TimelineEvent {
    id : string;
    eventType: TimelineEventType;
    title: string;
    description: string | null;
    eventDateUtc: string;
}

export interface AddTimelimeNoteValue {
    title: string;
    description?: string;
    eventDateUtc?: string;
}

export interface EmployeeSummary {
    id: string;
    fullName: string;
    positionTitle: string;
}