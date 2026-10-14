export interface WorkSchedule {
    id: string;
    name: string;
    startTime: string;
    endTime: string;
    workingDays: number;
    isActive: boolean;
    createdAtUtc: string;
}

export enum WorkLocationType {
    Office = 1,
    Branch = 2,
    ClientSite = 3,
    Remote = 4,
    Temporary = 5
}

export interface MyWorkLocation { 
    id: string;
    name: string;
    type: WorkLocationType;
}

export interface ClockRequest {
    latitude?: number;
    longitude?: number;
    accuracyMeters?: number;
    workLocationId?: string;
}

export enum AttendanceFlags {
    None = 0,
    ClockInLocationUnverified = 1,
    ClockOutLocationUnverified = 2,
    LongShift = 4,
    ManuallyCorrected = 8
}

export type FlagTone = 'warn' | 'danger' | 'neutral';

export function describeFlags(flags: number): { label: string; tone: FlagTone}[] {
    const out: { label: string; tone: FlagTone}[] = [];
    if (flags & AttendanceFlags.ClockInLocationUnverified) out.push({ label: 'Clock-in location unverified', tone: 'warn'});
    if (flags & AttendanceFlags.ClockOutLocationUnverified) out.push({ label: 'Clock-out location unverified', tone: 'warn'});
    if (flags & AttendanceFlags.LongShift) out.push({ label: 'Long shift', tone: 'danger'});
    if (flags & AttendanceFlags.ManuallyCorrected) out.push({ label: 'Corrected', tone: 'neutral'});
    return out;
}

export interface AttendanceRecord {
    id: string;
    employeeId: string;
    employeeNumber: string;
    employeeName: string;
    date: string;
    clockInUtc: string;
    clockInLocationName: string | null;
    clockInDistanceMeters: number | null;
    clockInAccuracyMeters: number | null;
    clockInLatitude: number | null;
    clockInLongitude: number | null;
    clockOutUtc: string | null;
    clockOutLocationName: string | null;
    clockOutDistanceMeters: number | null;
    clockOutAccuracyMeters: number | null;
    clockOutLatitude: number | null;
    clockOutLongitude: number | null;
    workedMinutes: number | null;
    flags: number;
}

export enum ApprovalStatus {
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}

export const APPROVAL_STATUS_LABELS: Record<ApprovalStatus, string> = {
    [ApprovalStatus.Pending]: 'Pending',
    [ApprovalStatus.Approved]: 'Approved',
    [ApprovalStatus.Rejected]: 'Rejected',
    [ApprovalStatus.Cancelled]: 'Cancelled',
}

export interface AttendanceCorrection {
    id: string;
    employeeId: string;
    employeeNumber: string;
    employeeName: string;
    date: string;
    requestedClockIn: string | null;
    requestedClockOut: string | null;
    reason: string;
    status: ApprovalStatus;
    enteredByApprover: boolean;
    createdAtUtc: string;
    reviewedAtUtc: string | null;
    reviewNote: string | null;
}

export interface CreateCorrectionValue {
    date: string;
    clockIn?: string;
    clockOut?: string;
    reason: string;
}

export interface OvertimeRequest {
    id: string;
    employeeId: string;
    employeeNumber: string;
    employeeName: string;
    date: string;
    startTime: string;
    endTime: string;
    requestedMinutes: number;
    approvedMinutes: number | null;
    reason: string;
    status: ApprovalStatus;
    createdAtUtc: string;
    reviewedAtUtc: string | null;
    reviewNote: string | null;
}

export interface CreateOvertimeValue {
    date: string;
    startTime: string;
    endTime: string;
    reason: string;
}

export interface PagedResult<T> {
    items: T[];
    total: number;
    page: number;
    pageSize: number;
}

export const WORK_LOCATION_TYPE_LABELS: Record<WorkLocationType, string> = {
    [WorkLocationType.Office]: 'Office',
    [WorkLocationType.Branch]: 'Branch',
    [WorkLocationType.ClientSite]: 'Client site',
    [WorkLocationType.Remote]: 'Remote',
    [WorkLocationType.Temporary]: 'Temporary'
}

export interface WorkLocation {
    id: string;
    name: string;
    type: WorkLocationType;
    latitude: number | null;
    longitude: number | null;
    radiusMeters: number | null;
    branchId: string | null;
    branchName: string | null;
    validFrom: string | null;
    validTo: string | null;
    isActive: boolean;
    createdAtUtc: string;
}

export interface WorkLocationFormValue {
    name: string;
    type: WorkLocationType;
    latitude: number | null;
    longitude: number | null;
    radiusMeters: number | null;
    branchId: string | null;
    validFrom: string | null;
    validTo: string | null;
    isActive?: boolean;
}

export interface WorkScheduleFormValue {
    name: string;
    startTime: string;
    endTime: string;
    workingDays: number;
    isActive?: boolean;
}

export interface AttendanceQuery {
    from: string;
    to: string;
    employeeId?: string;
    needsReview?: boolean;
    page?: number;
    pageSize?: number;
}