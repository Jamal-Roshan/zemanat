using Zemanat.Core.Text;

namespace Zemanat.Tests;

public class NumberWordsTests
{
    [Theory]
    [InlineData(0L, "صفر")]
    [InlineData(1L, "یک")]
    [InlineData(10L, "ده")]
    [InlineData(11L, "یازده")]
    [InlineData(21L, "بیست و یک")]
    [InlineData(100L, "صد")]
    [InlineData(101L, "صد و یک")]
    [InlineData(1000L, "یک هزار")]
    [InlineData(1250000L, "یک میلیون و دویست و پنجاه هزار")]
    [InlineData(2000000000L, "دو میلیارد")]
    [InlineData(123456789L, "صد و بیست و سه میلیون و چهارصد و پنجاه و شش هزار و هفتصد و هشتاد و نه")]
    [InlineData(-5L, "منفی پنج")]
    public void ToWords_ReturnsExpectedText(long value, string expected)
    {
        Assert.Equal(expected, NumberWords.ToWords(value));
    }

    [Fact]
    public void ToWords_HandlesLongMinAndMaxWithoutThrowing()
    {
        Assert.StartsWith("منفی", NumberWords.ToWords(long.MinValue));
        Assert.Contains("کوینتیلیون", NumberWords.ToWords(long.MaxValue));
    }
}
