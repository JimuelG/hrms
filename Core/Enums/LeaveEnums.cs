namespace Core.Enums;
public enum LeaveEntitlementMode
{
    Annual = 1,
    HrGranted = 2,
    Unlimited = 3
}

public enum HolidayType
{
    Public = 1,
    Special = 2,
    Company = 3
}

public enum DayPortion
{
    Full = 1,
    Morning = 2,
    Afternoon = 3
}

public enum LeaveEntryType
{
    Grant = 1,
    Adjustment = 2,
    CarryOver = 3,
    Usage = 4,
    Reversal = 5,
    Expiry = 6
}