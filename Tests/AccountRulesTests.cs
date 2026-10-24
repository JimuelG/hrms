using Core.Common;
using Core.Enums;

namespace Tests;

public class AccountRulesTests
{
    [Fact] public void A_Temporary_Password_Has_The_Default_Length() =>
        Assert.Equal(14, TemporaryPassword.Generate().Length);

    [Fact] public void A_Temporary_Password_Meets_The_Password_Rules()
    {
        for (var i = 0; i < 200; i++)
        {
            var p = TemporaryPassword.Generate();
            Assert.Contains(p, char.IsUpper);
            Assert.Contains(p, char.IsLower);
            Assert.Contains(p, char.IsDigit);
        }
    }

    [Fact] public void A_Temporary_Password_Has_No_Look_Alike_Characters()
    {
        for (var i = 0; i < 200; i++)
            Assert.DoesNotContain(TemporaryPassword.Generate(), c => "0O1lI".Contains(c));
    }

    [Fact] public void Temporary_Passwords_Differ() =>
        Assert.NotEqual(TemporaryPassword.Generate(), TemporaryPassword.Generate());

    [Fact] public void Too_Short_A_Password_Is_Refused() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => TemporaryPassword.Generate(8));

    private static readonly DateTime Now = new(2026, 10, 24, 8, 0, 0, DateTimeKind.Utc);

    [Fact] public void No_Expiry_Means_Not_Expired() => Assert.False(AccountRules.TemporaryPasswordExpired(null, Now));
    [Fact] public void Before_The_Expiry_It_Still_Works() => Assert.False(AccountRules.TemporaryPasswordExpired(Now.AddMinutes(1), Now));
    [Fact] public void At_The_Expiry_It_Has_Expired() => Assert.True(AccountRules.TemporaryPasswordExpired(Now, Now));

    private static readonly DateOnly Today = new(2026, 10, 24);

    [Fact] public void A_Leaver_Keeps_Access_On_The_Last_Day() =>
        Assert.False(AccountRules.ShouldLoseAccess(EmployeeStatus.Resigned, Today, Today));

    [Fact] public void A_Leaver_Loses_Access_The_Day_After() =>
        Assert.True(AccountRules.ShouldLoseAccess(EmployeeStatus.Terminated, Today.AddDays(-1), Today));

    [Fact] public void A_Leaver_With_No_Last_Day_Is_Not_Switched_Off_Automatically() =>
        Assert.False(AccountRules.ShouldLoseAccess(EmployeeStatus.Resigned, null, Today));

    [Fact] public void A_Current_Employee_Never_Loses_Access() =>
        Assert.False(AccountRules.ShouldLoseAccess(EmployeeStatus.Regular, Today.AddDays(-30), Today));

    [Fact] public void A_One_Company_Employee_Can_Be_Reset() => Assert.Null(AccountRules.ResetRefusal(false, 1));
    [Fact] public void A_Multi_Company_Person_Cannot_Be_Reset() => Assert.Contains("another company", AccountRules.ResetRefusal(false, 2));
    [Fact] public void A_Platform_Admin_Cannot_Be_Reset() => Assert.Contains("platform administrator", AccountRules.ResetRefusal(true, 1));
}