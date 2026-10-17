import { DayPortion } from "../models/leave";

export const formatDays = (d: number): string => `${Number.isInteger(d) ? d : d.toFixed(1)} day${d === 1 ? '' : 's'}`;

export function formatBytes(n: number): string {
    if (n < 1024) return `${n} B`;
    return n < 1024 * 1024 ? `${(n / 1024).toFixed(0)} KB` : `${(n / (1024 * 1024)).toFixed(1)} MB`;
}

export function portionLabel(p: DayPortion): string {
    return p === DayPortion.Morning ? 'morning only' : p === DayPortion.Afternoon ? 'afternoon only' : '';
}

export function shortDate(iso: string): string {
    const [y, m, d] = iso.split('-').map(Number);
    return new Date(y, m - 1, d).toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
}