export interface HeadcountCard {
    active: number;
    hiredThisMonth: number | null;
    leftThisMonth: number | null;
}

export interface AttendanceCard {
    expected: number;
    present: number;
    late: number;
    onLeave: number;
    notYetIn: number;
    notIn: number;
    noSchedule: number;
    latePeople: string[];
    notInPeople: string[];
}

export interface Away {
    name: string;
    from: string;
    to: string;
}

export interface LeaveCard {
    today: Away[];
    upcoming: Away[];
}

export interface MeCard {
    status: number;
    pendingLeaveRequests: number;
}

export interface Dashboard {
    scope: 'Company' | 'Team' | 'Personal';
    date: string;
    headcount: HeadcountCard | null;
    pendingApprovals: number;
    attendance: AttendanceCard | null;
    leave: LeaveCard;
    openJobPostings: number | null;
    me: MeCard | null;
}

export const PRESENCE_LABELS: Record<number, string> = {
    1: 'Not working today',
    2: 'On leave',
    3: 'Present',
    4: 'Late',
    5: 'Not in yet',
    6: 'Not clocked in',
    7: 'No schedule assigned'
}