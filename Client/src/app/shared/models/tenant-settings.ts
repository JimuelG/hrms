export enum WorkingDay {
    None = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64
}

export const WORKING_DAY_OPTIONS: { label: string; value: WorkingDay }[] = [
    { label: 'Mon', value: WorkingDay.Monday },
    { label: 'Tue', value: WorkingDay.Tuesday },
    { label: 'Wed', value: WorkingDay.Wednesday },
    { label: 'Thu', value: WorkingDay.Thursday },
    { label: 'Fri', value: WorkingDay.Friday },
    { label: 'Sat', value: WorkingDay.Saturday },
    { label: 'Sun', value: WorkingDay.Sunday },
]

export interface TenantSettings {
    companyLegalName: string | null;
    logoUrl: string | null;
    primaryColor: string | null;
    addressLine: string | null;
    city: string | null;
    country: string | null;
    contactEmail: string | null;
    contactPhone: string | null;
    timeZoneId: string;
    currency: string;
    dateFormat: string;
    workingDays: number;
    updatedAtUtc: string | null;
}

export type UpdateTenantSettings = Omit<TenantSettings, 'updatedAtUtc'>;