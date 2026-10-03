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
    ];
}