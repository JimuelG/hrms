import { ApprovalStatus } from "./attendance";

export enum LeaveEntitlementMode {
    Annual = 1,
    HrGranted = 2,
    Unlimited = 3
}

export enum DayPortion {
    Full = 1,
    Morning = 2,
    Afternoon = 3
}

export enum LeaveEntryType {
    Grant = 1,
    Adjustment = 2,
    CarryOver = 3,
    Usage = 4,
    Reversal = 5,
    Expiry = 6,
}

export enum LeaveDayStatus {
    Counted = 1,
    NonWorkingDay = 2,
    Holiday = 3
}

export enum HolidayType {
    Public = 1,
    Special = 2,
    Company = 3
}

export const HOLIDAY_TYPE_LABELS: Record<HolidayType, string> = {
    [HolidayType.Public]: 'Public',
    [HolidayType.Special]: 'Special',
    [HolidayType.Company]: 'Company'
}

export const ENTITLEMENT_MODE_LABELS: Record<LeaveEntitlementMode, string> = {
    [LeaveEntitlementMode.Annual]: 'Annual (granted each year)',
    [LeaveEntitlementMode.HrGranted]: 'HR-granted (per employee)',
    [LeaveEntitlementMode.Unlimited]: 'Unlimited (no balance)',
}

export const ENTRY_TYPE_LABELS: Record<LeaveEntryType, string> = {
    [LeaveEntryType.Grant]: 'Grant',
    [LeaveEntryType.Adjustment]: 'Adjustment',
    [LeaveEntryType.CarryOver]: 'Carried over',
    [LeaveEntryType.Usage]: 'Leave taken',
    [LeaveEntryType.Reversal]: 'Returned',
    [LeaveEntryType.Expiry]: 'Expired'
};

export interface LeaveType {
    id: string;
    code: string;
    name: string;
    description: string | null;
    isPaid: boolean;
    entitlementMode: LeaveEntitlementMode;
    defaultDays: number;
    allowsHalfDay: boolean;
    requiresDocumentAfterDays: number | null;
    isActive: boolean;
    prorateFirstYear: boolean;
    maxCarryOverDays: number;
    minServiceMonths: number;
    createdAtUtc: string;
}

export interface LeaveBalance {
    leaveTypeId: string;
    code: string;
    name: string;
    isPaId: boolean;
    entitlementMode: LeaveEntitlementMode;
    year: number;
    granted: number;
    carriedOver: number;
    adjusted: number;
    used: number;
    balance: number;
    eligibleFrom: string | null;
    isEligible: boolean;
}

export interface LeaveBalances {
    year: number;
    asOf: string;
    items: LeaveBalance[];
}

export interface LedgerEntry {
    id: string;
    leaveTypeId: string;
    leaveTypeCode: string;
    leaveTypeName: string;
    year: number;
    entryType: LeaveEntryType;
    days: number;
    description: string;
    isAutomatic: boolean;
    createdAtUtc: string;
}

export interface PreviewLine {
    date: string;
    valaue: number;
    status: LeaveDayStatus;
    holidayName: string | null;
}

export interface LeavePreview {
    days: number;
    documentRequired: boolean;
    isPaid: boolean;
    lines: PreviewLine[];
}

export interface LeaveRequest {
    id: string;
    employeeId: string;
    employeeName: string;
    employeeNumber: string;
    leaveTypeId: string;
    leaveTypeCode: string;
    leaveTypeName: string;
    isPaid: boolean;
    startDate: string;
    endDate: string;
    startPortion: DayPortion;
    endPortion: DayPortion;
    days: number;
    leaveYear: number;
    reason: string;
    status: ApprovalStatus;
    createdAtUtc: string;
    decidedAtUtc: string | null;
    cancelledAtUtc: string | null;
    cancellationReason: string | null;
    documentRequired: boolean;
    attachmentCount: number;
    approvalRequestId: string | null;
    currentStepLabel: string | null;
}

export interface LeaveFilingResult {
    request: LeaveRequest;
    notes: string[];
}

export interface LeaveAttachment {
    id: string;
    fileName: string;
    contentType: string;
    sizeBytes: number;
    uploadedByName: string;
    uploadedAtUtc: string;
}

export interface CreateLeaveRequestValve {
    leaveTypeId: string;
    startDate: string;
    endDate: string;
    startPortion: DayPortion;
    endPortion: DayPortion;
    reason: string;
}

export interface Holiday {
    id: string;
    date: string;
    name: string;
    type: HolidayType;
}

export interface HolidayFormValue {
    date: string;
    name: string;
    type: HolidayType
}

export interface LeaveTypeFormValue {
    code: string;
    name: string;
    description: string | null;
    isPaid: boolean;
    entitlementMode: LeaveEntitlementMode;
    defaultDays: number;
    prorateFirstYear: boolean;
    maxCarryOverDays: number;
    minServiceMonths: number;
    allowsHalfDay: boolean;
    reqiuresDocumentAfterDays: number | null;
    isActive?: boolean;
}

export interface PostLedgerValue {
    employeeId: string;
    leaveTypeId: string;
    year: number;
    days: number;
    reason: string;
}