export enum IdentifierField {
    Tin = 1,
    Sss = 2,
    PhilHealth = 3,
    PagIbig = 4,
    BankAccountNumber = 5
}

export interface Identifiers {
    employeeId: string;
    tin: string | null;
    sss: string | null;
    philHealth: string | null;
    pagIbig: string | null;
    bankName: string | null;
    bankAccountName: string | null;
    bankAccountNumber: string | null;
    updatedAtUtc: string | null;
}

export type SecretKey = 'tin' | 'sss' | 'philHealth' | 'pagIbig' | 'bankAccountNumber';

export interface IdentifiersUpdate {
    tin: string | null;
    sss: string | null;
    philHealth: string | null;
    pagIbig: string | null;
    bankAccountNumber: string | null;
    bankName: string | null;
    bankAccountName: string | null;
}

export interface IdentifierAccess {
    field: IdentifierField;
    action: number;
    userName: string;
    createdAtUtc: string;
}

export const ACCESS_ACTION_LABELS: Record<number, string> = {
    1: 'Revealed',
    2: 'Changed',
    3: 'Cleared'
}