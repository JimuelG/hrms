export interface WorkSchedule {
    id: string;
    name: string;
    startTime: string;
    endTime: string;
    workingDays: number;
    isActive: boolean;
    createdAtUtc: string;
}