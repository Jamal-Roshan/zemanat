using Zemanat.Core.Text;

namespace Zemanat.Tests;

public class PersianTextTests
{
    [Fact]
    public void NormalizeLetters_ReplacesArabicYehAndKaf()
    {
        // "علي كريمي" written with Arabic yeh and kaf
        var input = "علي كريمي";
        var expected = "علی کریمی";

        Assert.Equal(expected, PersianText.NormalizeLetters(input));
    }

    [Fact]
    public void NormalizeLetters_HandlesNullAndEmpty()
    {
        Assert.Equal(string.Empty, PersianText.NormalizeLetters(null));
        Assert.Equal(string.Empty, PersianText.NormalizeLetters(string.Empty));
    }

    [Fact]
    public void CleanText_TrimsAndCollapsesSpaces()
    {
        Assert.Equal("رضا احمدی", PersianText.CleanText("  رضا   احمدی \t"));
    }

    [Fact]
    public void CleanText_RemovesDirectionalMarksButKeepsZeroWidthNonJoiner()
    {
        var input = "‏می‌خواهم‎";
        Assert.Equal("می‌خواهم", PersianText.CleanText(input));
    }

    [Fact]
    public void ToPersianDigits_ConvertsLatinAndArabicIndic()
    {
        Assert.Equal("۱۴۰۵/۰۷", PersianText.ToPersianDigits("1405/07"));
        Assert.Equal("۰۱۲", PersianText.ToPersianDigits("٠١٢"));
    }

    [Fact]
    public void ToLatinDigits_ConvertsPersianAndArabicIndic()
    {
        Assert.Equal("1405", PersianText.ToLatinDigits("۱۴۰۵"));
        Assert.Equal("012", PersianText.ToLatinDigits("٠١٢"));
    }

    [Theory]
    [InlineData("1,250,000", 1250000L)]
    [InlineData("۱٬۲۵۰٬۰۰۰", 1250000L)]
    [InlineData("  500 000 ", 500000L)]
    [InlineData("0", 0L)]
    public void TryParseAmount_AcceptsCommonInputs(string input, long expected)
    {
        Assert.True(PersianText.TryParseAmount(input, out var value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-5")]
    [InlineData("12.5")]
    public void TryParseAmount_RejectsInvalidInputs(string? input)
    {
        Assert.False(PersianText.TryParseAmount(input, out _));
    }
}
