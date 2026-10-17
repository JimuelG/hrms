import { ApprovalStatus } from "./attendance";

export enum ApprovalSubjectType {
    LeaveRequest =1
}

export enum ApprovalStepStatus {
    Waiting = 1,
    Pending = 2,
    Approved = 3,
    Rejected = 4,
    Skipped = 5,
    Voided = 6
}

export enum ApprovalEventType {
    Submitted = 1,
    StepSkipped = 2,
    StepApproved = 3,
    Rejected = 4,
    Commented = 5,
    Cancelled = 6
}

export const STEP_STATUS_LABELS: Record<ApprovalStepStatus, string> = {
    [ApprovalStepStatus.Waiting]: 'Waiting',
    [ApprovalStepStatus.Pending]: 'Needs a decision',
    [ApprovalStepStatus.Approved]: 'Approved',
    [ApprovalStepStatus.Rejected]: 'Rejected',
    [ApprovalStepStatus.Skipped]: 'Skipped',
    [ApprovalStepStatus.Voided]: 'Not needed'
}

export const EVENT_TYPE_LABELS: Record<ApprovalEventType, string> = {
    [ApprovalEventType.Submitted]: 'Submitted',
    [ApprovalEventType.StepSkipped]: 'Step skipped',
    [ApprovalEventType.StepApproved]: 'Approved',
    [ApprovalEventType.Rejected]: 'Rejected',
    [ApprovalEventType.Commented]: 'Comment',
    [ApprovalEventType.Cancelled]: 'Cancelled'
}

export function stepStatusClass(s: ApprovalStepStatus): string {
    switch (s) {
        case ApprovalStepStatus.Approved: return 'bg-emerald-500';
        case ApprovalStepStatus.Rejected: return 'bg-red-500';
        case ApprovalStepStatus.Pending: return 'bg-amber-500';
        default: return 'bg-slate-300 dark:bg-slate-600';
    }
}

export interface ApprovalSummary {
    id: string;
    subjectType: ApprovalSubjectType;
    subjectId: string;
    requesterEmployeeId: string;
    requesterName: string;
    requesterNumber: string;
    title: StorageManager;
    details: string | null;
    status: ApprovalStatus;
    currentStepLabel: string | null;
    createdAtUtc: string;
    completedAtUtc: string | null; 
}

export interface ApprovalStep {
    order: number;
    kind: number;
    label: string;
    status: ApprovalStepStatus;
    approverName: string | null;
    skipReason: string | null;
    decidedByName: string | null;
    decidedAtUtc: string | null;
    note: string | null;
}

export interface ApprovalEvent {
    type: ApprovalEventType;
    stepOrder: number | null;
    actorName: string | null;
    note: string | null;
    createdAtUtc: string;
}

export interface ApprovalAccess {
    isRequester: boolean;
    canDecide: boolean;
    canComment: boolean;
    canCancel: boolean;
}

export interface ApprovalDetail {
    request: ApprovalSummary;
    steps: ApprovalStep[];
    events: ApprovalEvent[];
    access: ApprovalAccess;
}