using Zemanat.Core.Documents;

namespace Zemanat.Tests;

public class DocumentNumberTests
{
    [Fact]
    public void Format_JoinsSequenceYearAndCode()
    {
        var number = new DocumentNumber(123, 1405, "106");

        Assert.Equal("123.1405.106", number.Format());
        Assert.Equal("123/1405/106", number.Format("/"));
    }

    [Fact]
    public void Format_CanUsePersianDigits()
    {
        var number = new DocumentNumber(123, 1405, "106");

        Assert.Equal("۱۲۳.۱۴۰۵.۱۰۶", number.Format(persianDigits: true));
    }

    [Fact]
    public void TryParse_RoundTripsWithPersianDigits()
    {
        var original = new DocumentNumber(7, 1405, "106");
        var text = original.Format(persianDigits: true);

        Assert.True(DocumentNumber.TryParse(text, ".", out var parsed));
        Assert.Equal(original, parsed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12.34")]
    [InlineData("0.1405.106")]
    [InlineData("a.1405.106")]
    [InlineData("12.1405.")]
    public void TryParse_RejectsInvalidText(string? text)
    {
        Assert.False(DocumentNumber.TryParse(text, ".", out _));
    }
}

public class TrackingCodeTests
{
    [Fact]
    public void Generate_ReturnsWellFormedCodes()
    {
        var code = TrackingCode.Generate();

        Assert.Equal(TrackingCode.CodeLength, code.Length);
        Assert.True(TrackingCode.IsWellFormed(code));
    }

    [Fact]
    public void Generate_ProducesDifferentCodes()
    {
        var codes = Enumerable.Range(0, 1000).Select(_ => TrackingCode.Generate()).ToHashSet();

        Assert.Equal(1000, codes.Count);
    }

    [Fact]
    public void Normalize_UpperCasesAndRemovesSeparators()
    {
        Assert.Equal("ABCDEFGHJKMN", TrackingCode.Normalize("abcd-efgh jkmn"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("SHORT")]
    [InlineData("ABCDEFGHJKM0")]
    public void IsWellFormed_RejectsBadCodes(string? code)
    {
        Assert.False(TrackingCode.IsWellFormed(code));
    }
}
