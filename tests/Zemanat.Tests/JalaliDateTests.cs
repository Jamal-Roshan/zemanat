using Zemanat.Core.Dates;

namespace Zemanat.Tests;

public class JalaliDateTests
{
    // 1 Mehr 1404 is 23 September 2025 (first six Jalali months have 31 days).
    private static readonly DateTime FirstOfMehr1404 = new(2025, 9, 23);

    [Fact]
    public void Format_ReturnsJalaliDate()
    {
        Assert.Equal("1404/07/01", JalaliDate.Format(FirstOfMehr1404));
    }

    [Fact]
    public void Format_CanUsePersianDigits()
    {
        Assert.Equal("۱۴۰۴/۰۷/۰۱", JalaliDate.Format(FirstOfMehr1404, persianDigits: true));
    }

    [Fact]
    public void Format_ReturnsEmptyForDatesBeforeTheCalendarStarts()
    {
        Assert.Equal(string.Empty, JalaliDate.Format(DateTime.MinValue));
    }

    [Fact]
    public void Year_ReturnsJalaliYear()
    {
        Assert.Equal(1404, JalaliDate.Year(FirstOfMehr1404));
    }

    [Theory]
    [InlineData("1404/7/1")]
    [InlineData("1404-07-01")]
    [InlineData("1404.07.01")]
    [InlineData("۱۴۰۴/۰۷/۰۱")]
    public void TryParse_AcceptsCommonFormats(string text)
    {
        Assert.True(JalaliDate.TryParse(text, out var date));
        Assert.Equal(FirstOfMehr1404, date);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("1404/13/01")]
    [InlineData("1404/07/31")]
    [InlineData("1404/07")]
    public void TryParse_RejectsInvalidDates(string? text)
    {
        Assert.False(JalaliDate.TryParse(text, out _));
    }

    [Fact]
    public void ToGregorian_IsTheInverseOfParts()
    {
        Assert.Equal(FirstOfMehr1404, JalaliDate.ToGregorian(1404, 7, 1));
    }

    [Fact]
    public void IranClock_ConvertsUtcToIranTime()
    {
        var utc = new DateTime(2025, 9, 23, 20, 30, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2025, 9, 24, 0, 0, 0), IranClock.ToIran(utc));
    }
}
