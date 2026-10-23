export enum AlertKind { StaleApproval = 1, DocumentExpiry = 2, OpenAttendance = 3 }

export interface Alert {
    id: string;
    kind: AlertKind;
    title: string;
    message: string;
    subjectId: string | null;
    createdAtUtc: string;
}

export const ALERT_KIND_LABELS: Record<AlertKind, string> = {
    [AlertKind.StaleApproval]: 'Approvals waiting',
    [AlertKind.DocumentExpiry]: 'Documents',
    [AlertKind.OpenAttendance]: 'Attendance'
}