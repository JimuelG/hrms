namespace Core.Enums;

[Flags]
public enum PlanFeature
{
    None = 0,
    Ats = 1,
    Payroll = 2,
    Benefits = 4,
    AdvancedAnalytics = 8,
    BiometricAttendance = 16,
    Sso = 32
}