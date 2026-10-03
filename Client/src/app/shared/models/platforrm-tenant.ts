export enum TenantStatus {
    Active = 1,
    Suspended = 2,
    Closed = 3
}

export interface PlatformTenant {
    id: string;
    name: string;
    slug: string;
    status: TenantStatus;
    createdAtUtc: string;
}

export interface CreateTenantValue {
    name: string;
    slug: string;
}