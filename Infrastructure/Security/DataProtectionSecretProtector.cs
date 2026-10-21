using System.Security.Cryptography;
using Core.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace Infrastructure.Security;

public class DataProtectionSecretProtector(IDataProtectionProvider provider) : ISecretProtector
{
    private IDataProtector For(Guid scope) => provider.CreateProtector("hrms.identifiers.v1", scope.ToString("N"));
    public string Protect(Guid scope, string plainText) => For(scope).Protect(plainText);

    public bool TryUnprotect(Guid scope, string? protectedValue, out string value)
    {
        value = "";
        if (string.IsNullOrEmpty(protectedValue)) return false;
        try
        {
            value = For(scope).Unprotect(protectedValue);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}