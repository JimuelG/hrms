using Core.Enums;

namespace Core.Common;
public static class WorkingDayExtensions
{
    public static WorkingDay ToWorkingDay(this DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => WorkingDay.Monday,
        DayOfWeek.Tuesday => WorkingDay.Tuesday,
        DayOfWeek.Wednesday => WorkingDay.Wednesday,
        DayOfWeek.Thursday => WorkingDay.Thursday,
        DayOfWeek.Friday => WorkingDay.Friday,
        DayOfWeek.Saturday => WorkingDay.Saturday,
        DayOfWeek.Sunday => WorkingDay.Sunday,
        _ => WorkingDay.None
    };
}