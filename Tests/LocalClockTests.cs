using Core.Common;

namespace Tests;

public class LocalClockTests
{
    [Fact]
    public void Manila_Is_Eight_Hours_Ahead_Of_Utc_Across_Midnight()
    {
        // 17:00 UTC is 01:00 the next day in Manila
        Assert.Equal(new DateOnly(2026, 10, 11),
            LocalClock.LocalDate(new DateTime(2026, 10, 10, 17, 0, 0, DateTimeKind.Utc), "Asia/Manila"));
        Assert.Equal(new DateOnly(2026, 10, 10),
            LocalClock.LocalDate(new DateTime(2026, 10, 10, 15, 59, 0, DateTimeKind.Utc), "Asia/Manila"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Not/AZone")]
    public void Missing_Or_Unknown_Zone_Falls_Back_To_Utc(string? zone) =>
        Assert.Equal(new DateOnly(2026, 10, 10),
            LocalClock.LocalDate(new DateTime(2026, 10, 10, 20, 0, 0, DateTimeKind.Utc), zone));

    [Fact]
    public void IsValid_Recognizes_Real_And_Fake_Zones()
    {
        Assert.True(LocalClock.IsValid("Asia/Manila"));
        Assert.False(LocalClock.IsValid("Asia/Manilla"));
    }
}