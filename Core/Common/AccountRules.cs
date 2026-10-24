using System.Security.Cryptography;
using Core.Enums;

namespace Core.Common;

public static class TemporaryPassword
{
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnpqrstuvwxyz";
    private const string Digits = "23456789";
    public const int DefaultLength = 14;

    public static string Generate(int length = DefaultLength)
    {
        if (length < 10) throw new ArgumentOutOfRangeException(nameof(length), "At least 10 characters.");
        var all = Upper + Lower + Digits;
        var chars = new char[length];
        chars[0] = Pick(Upper);
        chars[1] = Pick(Lower);
        chars[2] = Pick(Digits);
        for (var i = 3; i < length; i++) chars[i] = Pick(all);
        RandomNumberGenerator.Shuffle(chars.AsSpan());
        return new string(chars);

    }
    
    private static char Pick(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];
}

public static class AccountRules
{
    public static readonly TimeSpan TemporaryPasswordLifetime = TimeSpan.FromDays(7);

    public static bool TemporaryPasswordExpired(DateTime? expiresAtUtc, DateTime nowUtc) =>
        expiresAtUtc is DateTime e && nowUtc >= e;

    public static bool ShouldLoseAccess(EmployeeStatus status, DateOnly? lastWorkingDay, DateOnly today) =>
        status is EmployeeStatus.Resigned or EmployeeStatus.Terminated
        && lastWorkingDay is DateOnly d && d < today;

    public static string? ResetRefusal(bool isPlatformAdmin, int activeCompanies) =>
        isPlatformAdmin ? "This login belongs to a platform administrator and can't be reset here."
        : activeCompanies > 1 ? "This person also signs in to another company, so their password can't be reset from here. They can use their own password."
        : null;
}