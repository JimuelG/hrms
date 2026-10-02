export interface Branch {
    id: string;
    name: string;
    code: string;
    addressLine: string | null;
    city: string | null;
    country: string | null;
    timeZoneId: string | null;
    isActive: boolean;
    createdAtUtc: string;
}

export interface Department {
    id: string;
    name: string;
    code: string;
    description: string | null;
    isActive: boolean;
    createdAtUtc: string;
}

export interface BranchFormValue {
    name: string;
    code: string;
    addressLine?: string;
    city?: string;
    country?: string;
    timeZoneId?: string;
    isActive?: boolean;
}

export interface DepartmentFormValue {
    name: string;
    code: string;
    description?: string;
    isActive?: boolean;
}