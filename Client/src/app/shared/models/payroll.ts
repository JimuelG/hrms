export enum PayFrequency {
    SemiMonthly = 1,
    Monthly = 2
}

export enum ContributionDeduction {
    SplitEvenly = 1,
    SecondPeriodOnly = 2
}

export enum PayrollRunStatus {
    Draft = 1,
    Approved = 2,
    Finalized = 3,
    Cancelled = 4
}

export enum PayrollLineKind {
    Earning = 1,
    Deduction = 2
}

export enum StatutoryKind {
    Sss = 1,
    PhilHealth = 2,
    PagIbig = 3,
    WithholdingTax = 4
}

export enum StatutoryMethod {
    Brackets = 1,
    Rate = 2
}

export const RUN_STATUS_LABELS: Record<PayrollRunStatus, string> = {
    [PayrollRunStatus.Draft]: 'Draft',
    [PayrollRunStatus.Approved]: 'Approved',
    [PayrollRunStatus.Finalized]: 'Finalized',
    [PayrollRunStatus.Cancelled]: 'Cancelled'
}

export const KIND_LABELS: Record<StatutoryKind, string> = {
    [StatutoryKind.Sss]: "SSS",
    [StatutoryKind.PhilHealth]: "PhilHealth",
    [StatutoryKind.PagIbig]: 'Pag-IBIG',
    [StatutoryKind.WithholdingTax]: 'Withholding tax'
}

export const FREQUENCY_LABELS: Record<PayFrequency, string> = {
    [PayFrequency.SemiMonthly]: 'Semi-monthly',
    [PayFrequency.Monthly]: 'Monthly'
}

export const POLICY_LABELS: Record<ContributionDeduction, string> = {
    [ContributionDeduction.SplitEvenly]: 'Split evenly across the month',
    [ContributionDeduction.SecondPeriodOnly]: 'All in the second period'
}

export function runStattusClass(s: PayrollRunStatus): string {
   switch (s) {
    case PayrollRunStatus.Draft: return 'bg-amber-50 text-amber-700 dark:bg-amber-950 dark:text-amber-400';
    case PayrollRunStatus.Approved: return 'bg-indigo-50 text-indigo-700 dark:bg-indigo-950 dark:text-indigo-400';
    case PayrollRunStatus.Finalized: return 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-400';
    default: return 'bg-slate-100 text-slate-500 dark:bg-slate-800 dark:text-slate-400';
  }
}

export interface PayrollLine {
    code: string;
    label: string;
    amount: number;
    kind: PayrollLineKind;
}

export interface PayrollResult {
    id: string;
    employeeId: string;
    employeeNumber: string;
    employeeName: string;
    departmentName: string | null;
    positionTitle: string | null;
    monthlyBasic: number;
    gross: number;
    taxableIncome: number;
    totalEarnings: number;
    totalDeductions: number;
    net: number;
    employerContributions: number;
    lines: PayrollLine[];
    notes: string[];
    warnings: string[];
    errorMessage: string | null;
}

export interface PayrollRunSummary {
    id: string;
    periodStart: string;
    periodEnd: string;
    status: PayrollRunStatus;
    employeeCount: number;
    errorCount: number;
    totalGross: number;
    totalDeductions: number;
    totalNet: number;
    totalEmployerContributions: number;
    createdAtUtc: string;
    finalizedAtUtc: string | null; 
}

export interface PayrollRunDetail {
    run: PayrollRunSummary;
    results: PayrollResult[];

}

export interface PayrollAdjustment {
    id: string;
    employeeId: string;
    label: string;
    amount: number;
}

export interface PayrollSettings {
    frequency: PayFrequency;
    contributionPolicy: ContributionDeduction;
    workingDaysFactorFiveDay: number;
    workingDaysFactorSixDay: number;
    hoursPerDay: number;
    ordinaryOvertimeMultiplier: number;
}

export interface Bracket {
    order: number;
    from: number;
    to: number | null;
    employeeFixed: number;
    employerFixed: number;
    employeeRateOnExcess: number;
}

export interface RuleSet {
    id: string;
    kind: StatutoryKind;
    method: StatutoryMethod;
    effectiveFrom: string;
    basis: PayFrequency | null;
    employeeRate: number;
    employerRate: number;
    baseFloor: number | null;
    baseCeiling: number | null;
    source: string;
    notes: string | null;
    setByName: string;
    createdAtUtc: string;
    isInForce: boolean;
    brackets: Bracket[];
}

export interface RuleSetForm {
    kind: StatutoryKind;
    method: StatutoryMethod;
    effectiveFrom: string;
    basis: PayFrequency | null;
    employeeRate: number;
    employerRate: number;
    baseFloor: number | null;
    baseCeiling: number | null;
    source: string;
    notes: string | null;
    bracketsCsv: string | null;
}

export interface StatutoryLine {
    code: string;
    label: string;
    employee: number;
    employer: number;
    ruleEffectiveFrom: string | null;
    note: string | null;
}

export interface StatutoryPreview {
    date: string;
    lines: StatutoryLine[];
}

export interface Compensation {
    id: string;
    employeeId: string;
    effectiveFrom: string;
    monthlyBasicSalary: number;
    reason: string;
    setByName: string;
    createdAtUtc: string;
    isCurrent: boolean;
}