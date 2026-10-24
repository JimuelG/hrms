using System.Reflection;
using Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Tests;

public class EndpointSecurityTests
{
    // Reviewed: reachable without signing in (for example sign-in and token refresh).
    private static readonly HashSet<string> Anonymous = ["Auth.Login", "Auth.Refresh", "Auth.Logout", "Auth.ChangeTemporaryPassword"];
    

    // Reviewed: open to any signed-in user, because the service decides what each person sees.
    private static readonly HashSet<string> SignedInOnly = 
    [
        "Dashboard.Get",
        "Auth.Me",
        "Auth.MyPermissions",
        "Approvals.Get",
        "Approvals.GetBySubject",
        "Approvals.Inbox",
        "Approvals.Mine",
        "Approvals.Approve",
        "Approvals.Reject",
        "Approvals.Comment"
    ];

    private static readonly string[] PermissionAttributes = ["HasPermissionAttribute", "PlatformAdminOnlyAttribute"];

    private static IEnumerable<(string Key, MethodInfo Method, Type Controller)> Actions() =>
        typeof(Program).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes<HttpMethodAttribute>(true).Any())
                .Select(m => ($"{t.Name.Replace("Controller", "")}.{m.Name}", m, t)));

    private static bool Has(MethodInfo m, Type t, Func<object, bool> match) =>
        m.GetCustomAttributes(true).Any(match) || t.GetCustomAttributes(true).Any(match);

    [Fact]
    public void Every_Endpoint_Is_Protected_Or_Deliberately_Open()
    {
        var problems = new List<string>();

        foreach (var (key, method, controller) in Actions())
        {
            var anonymous = Has(method, controller, a => a is IAllowAnonymous);
            var permission = Has(method, controller, a => PermissionAttributes.Contains(a.GetType().Name));
            var signedIn = Has(method, controller, a => a is IAuthorizeData);

            if (anonymous)
            {
                if (!Anonymous.Contains(key)) problems.Add($"{key} allows anonymous access but isn't on the reviewed anonymous list.");
            }
            else if (permission)
            {
                // fine: a specific permission is required
            }
            else if (signedIn)
            {
                if (!SignedInOnly.Contains(key)) problems.Add($"{key} is open to any signed-in user but isn't on the reviewed list.");
            }
            else
            {
                problems.Add($"{key} has no authorization at all.");
            }
        }

        Assert.True(problems.Count == 0, Environment.NewLine + string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void The_Reviewed_Lists_Contain_No_Endpoints_That_No_Longer_Exist()
    {
        var keys = Actions().Select(a => a.Key).ToHashSet();
        Assert.Empty(Anonymous.Concat(SignedInOnly).Where(k => !keys.Contains(k)));
    }

    [Fact]
    public void Every_Hidden_Entity_Name_Is_A_Real_Entity()
    {
        var names = typeof(Core.Entities.Employee).Assembly.GetTypes().Select(t => t.Name).ToHashSet();
        Assert.Empty(AuditRules.HiddenEntities.Where(n => !names.Contains(n)));
    }
}