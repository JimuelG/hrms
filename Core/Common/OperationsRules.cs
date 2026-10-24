namespace Core.Common;
public static class RetentionRules
{
    public const int MinimumAuditDays = 365;
    public const int DefaultAuditDays = 1095;
    public const int ResolvedAlertDays = 90;

    public static DateTime? Cutoff(DateTime nowUtc, int days, int minimumDays)
    {
        if (days <= 0) return null;
        return nowUtc.AddDays(-Math.Max(days, minimumDays));
    }
}

public enum KeysFolderState { Healthy = 0, Empty = 1, Missing = 2 }

public static class KeysRules
{
    public static KeysFolderState State(bool folderExists, int keyFileCount) =>
        !folderExists ? KeysFolderState.Missing
        : keyFileCount == 0 ? KeysFolderState.Empty
        : KeysFolderState.Healthy;
}