using Core.Entities;

namespace Core.Constant;
public static class Permissions
{
    public static class Employees
    {
        public const string Read = "employees.read";
        public const string Write = "employees.write";
        public const string Delete = "employees.delete";
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

    public static IEnumerable<(string Code, string Module, string Description)> All() =>
    [
        (Employees.Read, "Employees", "View employee records"),
        (Employees.Write, "Employees", "Create and edit employee records"),
        (Employees.Delete, "Employees", "Delete employee records"),
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
    ];
}