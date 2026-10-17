export const PERMISSIONS = {
    employees: { read: 'employees.read', write: 'employees.write', linkUser: 'employees.link_user' },
    branches: { read: 'branches.read' },
    departments: { read: 'departments.read' },
    positions: { read: 'positions.read' },
    jobPostings: { read: 'jobpostings.read' },
    applicants: { read: 'applicants.read' },
    onboarding: { read: 'onboarding.read' },
    attendance: { clock: 'attendance.clock', read: 'attendance.read', approve: 'attendance.approve' },
    workSchedules: { read: 'workschedules.read', write: 'workschedules.write', delete: 'workschedules.delete' },
    workLocations: { read: 'worklocations.read', write: 'worklocations.write', delete: 'workschedules.delete'},
    tenant: { manageSettings: 'tenant.manage_settings'},
    leave: { request: 'leave.request', manage: 'leave.manage', approve: 'leave.approve' },
} as const;