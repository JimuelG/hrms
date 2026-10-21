using Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;

namespace Tests;

public class SecretProtectorTests
{
    private static readonly Guid TenantA = Guid.NewGuid(), TenantB = Guid.NewGuid();
    private static DataProtectionSecretProtector Create() => new(new EphemeralDataProtectionProvider());

    [Fact] public void A_Value_Round_Trips()
    {
        var p = Create();
        Assert.True(p.TryUnprotect(TenantA, p.Protect(TenantA, "123456789"), out var back));
        Assert.Equal("123456789", back);
    }

    [Fact] public void The_Stored_Text_Does_Not_Contain_The_Value() =>
        Assert.DoesNotContain("123456789", Create().Protect(TenantA, "123456789"));

    [Fact] public void Another_Tenant_Cannot_Read_It()
    {
        var p = Create();
        Assert.False(p.TryUnprotect(TenantB, p.Protect(TenantA, "123456789"), out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a protected value")]
    public void Garbage_Fails_Safely_Without_Throwing(string? input) =>
        Assert.False(Create().TryUnprotect(TenantA, input, out _));
}