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

    public static IEnumerable<(string Code, string Module, string Description)> All() =>
    [
        (Employees.Read, "Employees", "View employee records"),
        (Employees.Write, "Employees", "Create and edit employee records"),
        (Employees.Delete, "Employees", "Delete employee records"),
        (Tenant.ManageRoles, "Tenant", "Create and edit roles and permissions"),
        (Tenant.ManageSettings, "Tenant", "Edit tenant/company settings")
    ];
}