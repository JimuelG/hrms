import { ApprovalStatus, AttendanceRecord, FlagTone } from "../models/attendance";

export const toApiTime = (v: string | null | undefined): string | undefined =>
    v ? (v.length === 5 ? `${v}:00` : v) : undefined;

export function formatTimeOnly(v: string | null | undefined): string {
    if (!v) return '-';
    const [hh, mm] = v.split(':').map(Number);
    return `${hh % 12 || 12}:${String(mm).padStart(2, '0')} ${hh < 12 ? 'AM' : 'PM'}`
}

export function formatMinutes(total: number | null | undefined): string {
    if (total === null || total === undefined) return '-';
    const h = Math.floor(total / 60);
    const m = total % 60;
    return h > 0 ? `${h}h ${String(m).padStart(2, '0')}m` : `${m}m`;
}

export function minutesBetween(start: string, end: string): number {
    const [sh, sm] = start.split(':').map(Number);
    const [eh, em] = end.split(':').map(Number);
    return eh * 60 + em - (sh * 60 + sm);
}

export function localDateIso(d: Date = new Date()): string {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

export function approvalStatusClass(status: ApprovalStatus): string {
    switch (status) {
        case ApprovalStatus.Pending: return 'bg-amber-50 text-amber-700 dark:bg-amber-950 dark:text-amber-400';
        case ApprovalStatus.Approved: return 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-400';
        case ApprovalStatus.Rejected: return 'bg-red-50 text-red-700 dark:bg-red-950 dark:text-red-400';
        default: return 'bg-slate-100 text-slate-500 dark:bg-slate-800 dark:text-slate-400';
    }
}

export function flagToneClass(tone: FlagTone): string {
    switch (tone) {
        case 'warn': return 'bg-amber-50 text-amber-700 dark:bg-amber-950 dark:text-amber-400';
        case 'danger': return 'bg-red-50 text-red-700 dark:bg-red-950 dark:text-red-400';
        default: return 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400';
    }
}

export function isStaleOpen(r: AttendanceRecord, nowMs: number): boolean {
    return !r.clockOutUtc && nowMs - Date.parse(r.clockInUtc) > 16 * 60 * 60 * 1000;
}

export const mapUrl = (lat: number, lon: number): string =>
    `https://www.openstreetmap.org/?mlat=${lat}&mlon=${lon}#map=18/${lat}/${lon}`;