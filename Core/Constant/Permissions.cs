using Core.Entities;

namespace Core.Constant;
public static class Permissions
{
    public static class Employees
    {
        public const string Read = "employees.read";
        public const string Write = "employees.write";
        public const string Delete = "employees.delete";
        public const string LinkUser = "employees.link_user";
    }

    public static class Tenant
    {
        public const string ManageRoles = "tenant.manage_roles";
        public const string ManageSettings = "tenant.manage_settings";
    }

    public static class Branches
    {
        public const string Read = "branches.read";
        public const string Write = "branches.write";
        public const string Delete = "branches.delete";
    }

    public static class Departments
    {
        public const string Read = "departments.read";
        public const string Write = "departments.write";
        public const string Delete = "deparments.delete";
    }

    public static class Positions
    {
        public const string Read = "positions.read";
        public const string Write = "positions.write";
        public const string Delete = "positions.delete";
    }

    public static class JobPosting
    {
        public const string Read = "jobpostings.read";
        public const string Write = "jobpostings.write";
        public const string Delete = "jobpostings.delete";
    }

    public static class Applicants
    {
        public const string Read = "applicants.read";
        public const string Write = "applicants.write";
        public const string Delete = "applicant.delete";
    }

    public static class Applications
    {
        public const string Read = "applications.read";
        public const string Write = "applications.write";
    }

    public static class Interviews
    {
        public const string Read = "interviews.read";
        public const string Write = "interviews.write";
    }

    public static class JobOffers
    {
        public const string Read = "joboffers.read";
        public const string Write = "joboffers.write";
    }

    public static class Onboarding
    {
        public const string Read = "onboarding.read";
        public const string Write = "onboarding.write";
    }

    public static class WorkSchedules
    {
        public const string Read = "workschedules.read";
        public const string Write = "workschedules.write";
        public const string Delete = "workschedules.delete";
    }

    public static class WorkLocations
    {
        public const string Read = "worklocations.read";
        public const string Write = "worklocations.write";
        public const string Delete = "worklocations.delete";
    }

    public static class Attendance
    {
        public const string Clock = "attendance.clock";
        public const string Read = "attendance.read";
        public const string Approve = "attendance.approve";
    }

    public static class Leave
    {
        public const string Request = "leave.request";
        public const string Manage = "leave.manage";
        public const string Approve = "leave.approve";
    }

    public static class Compensation
    {
        public const string Read = "compensation.read";
        public const string Write = "compensation.write";
    }
    
    public static class Payroll
    {
        public const string ReadRules = "payroll.read_rules";
        public const string ManageRules = "payroll.manage_rules";
        public const string Read = "payroll.read";
        public const string Run = "payroll.run";
        public const string Approve = "payroll.approve";
        public const string Finalize = "payroll.finalize";
        public const string ManageSettings = "payroll.manage_settings";
        public const string ViewOwn = "payroll.view_own";
    }

    public static class Identifiers
    {
        public const string Read = "identifiers.read";
        public const string Write = "identifiers.write";
        public const string Reveal = "identifiers.reveal";
    }

    public static IEnumerable<(string Code, string Module, string Description)> All() =>
    [
        (Employees.Read, "Employees", "View employee records"),
        (Employees.Write, "Employees", "Create and edit employee records"),
        (Employees.Delete, "Employees", "Delete employee records"),
        (Employees.LinkUser, "Employees", "Link employee to a user account"),
        (Tenant.ManageRoles, "Tenant", "Create and edit roles and permissions"),
        (Tenant.ManageSettings, "Tenant", "Edit tenant/company settings"),
        (Branches.Read, "Branches", "View branches"),
        (Branches.Write, "Branches", "Create and edit branches"),
        (Branches.Delete, "Branches", "Delete branches"),
        (Departments.Read, "Deparments", "View deparments"),
        (Departments.Write, "Deparments", "Create and edit deparments"),
        (Departments.Delete, "Deparments", "Delete deparments"),
        (Positions.Read, "Positions", "View positions"),
        (Positions.Write, "Positions", "Create and edit positions"),
        (Positions.Delete, "Positions", "Delete positions"),
        (JobPosting.Read, "Recruitment", "View job postings"),
        (JobPosting.Write, "Recruitment", "Create and edit job postings"),
        (JobPosting.Delete, "Recruitment", "Delete job postings"),
        (Applicants.Read, "Applicants", "View applicants"),
        (Applicants.Write, "Applicants", "Create and edit applicants"),
        (Applicants.Delete, "Applicants", "Delete applicants"),
        (Applications.Read, "Applications", "View Applcations"),
        (Applications.Write, "Applications", "Create and Edit Applcations"),
        (Interviews.Read, "Interviews", "View interviews"),
        (Interviews.Write, "Interviews", "Create and edit interviews"),
        (JobOffers.Read, "JobOffers", "View job offers"),
        (JobOffers.Write, "JobOffers", "Create and edit job offers"),
        (Onboarding.Read, "Onboarding", "View onboarding"),
        (Onboarding.Write, "Onboarding", "Create and edit onboarding"),
        (WorkSchedules.Read, "WorkSchedules", "View work schedules"),
        (WorkSchedules.Write, "WorkSchedules", "Create and edit work schedules"),
        (WorkSchedules.Delete, "WorkSchedules", "Delete work schedules"),
        (WorkLocations.Read, "WorkLocations", "View work locations"),
        (WorkLocations.Write, "WorkLocations", "Create and edit work locations"),
        (WorkLocations.Delete, "WorkLocations", "Delete work locations"),
        (Attendance.Clock, "Attendance", "Clock in and out for yourself"),
        (Attendance.Read, "Attendance", "View attendance for all employees"),
        (Attendance.Approve, "Attendance", "Review attendance corrections and overtime requests"),
        (Leave.Request, "Leave", "Request leave and view leave types and holidays"),
        (Leave.Manage, "Leave", "Configure leave types, holidays and balances"),
        (Leave.Approve, "Leave", "Approve or reject leave requests at the HR step"),
        (Compensation.Read, "Compensation", "View salary history"),
        (Compensation.Write, "Compensation", "Set salaries"),
        (Payroll.ReadRules, "Payroll", "View statutory contribution and tax tables"),
        (Payroll.ManageRules, "Payroll", "Load and change statutory contribution and tax tables"),
        (Payroll.Read, "Payroll", "View payroll runs and payslip figures"),
        (Payroll.Run, "Payroll", "Start and recompute payroll runs"),
        (Payroll.Approve, "Payroll", "Approve or send back payroll runs"),
        (Payroll.Finalize, "Payroll", "Finalize payroll runs, which locks them permanently"),
        (Payroll.ManageSettings, "Payroll", "Change payroll settings such as pay frequency"),
        (Payroll.ViewOwn, "Payroll", "View your own finalized payslips"),
        (Identifiers.Read, "Employees", "View masked government IDs and bank details"),
        (Identifiers.Write, "Employees", "Set government IDs and bank details"),
        (Identifiers.Reveal, "Employees", "Reveal full government IDs and bank details"),

    ];
}