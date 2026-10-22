export interface CountRow { label: string; count: number; }
export interface Celebration { name: string; month: number; day: number; next: string; years: number | null; }

export interface WorkforceReport {
    date: string;
    total: number;
    byDepartment: CountRow[];
    byBranch: CountRow[];
    byPosition: CountRow[];
    byEmploymentType: CountRow[];
    byStatus: CountRow[];
    tenure: CountRow[];
    birthdays: Celebration[];
    anniversaries: Celebration[];
}

export type Breakdown = 'department' | 'branch' | 'position' | 'type' | 'status' | 'tenure' | 'birthdays' | 'anniversaries';

export interface AttendanceRow {
    employeeId: string;
    employeeNumber: string;
    name: string;
    department: string | null;
    branch: string | null;
    scheduledDays: number;
    presentDays: number;
    absentDays: number;
    lateCount: number;
    lateMinutes: number;
    undertimeMinutes: number;
    overtimeMinutes: number;
    openRecords: number;
    noSchedule: boolean;
}

export interface AttendanceTotals {
    scheduledDays: number;
    presentDays: number;
    absentDays: number;
    lateCount: number;
    lateMinutes: number;
    undertimeMinutes: number;
    overtimeMinutes: number;
    openRecords: number;
}

export interface AttendanceReport {
    from: string;
    to: string;
    graceMinutes: number;
    rows: AttendanceRow[];
    totals: AttendanceTotals;
    noScheduleCount: number;
}

export interface UsageRow {
    employeeId: string;
    employeeNumber: string;
    name: string;
    department: string | null;
    leaveTypeCode: string;
    leaveTypeName: string;
    approved: number;
    pending: number;
    balance: number | null;
}

export interface TypeTotal {
    code: string;
    name: string;
    approved: number;
    pending: number;
}

export interface LeaveUsageReport {
    year: number;
    rows: UsageRow[];
    totals: TypeTotal[];
}

export interface CalendayDay {
    date: string;
    names: string[];
}

export interface LeaveCalendar {
    from: string;
    to: string;
    days: CalendayDay[];
}

export interface AttendanceQuery {
    from: string;
    to: string;
    graceMinutes: number;
    departmentId: string;
    branchId: string;
}