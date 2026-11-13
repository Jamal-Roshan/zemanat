using Zemanat.Core.Finance;

namespace Zemanat.Tests;

public class MoneyFormatterTests
{
    [Fact]
    public void Format_Rial_UsesPersianDigitsAndSeparators()
    {
        Assert.Equal("۱٬۵۰۰٬۰۰۰", MoneyFormatter.Format(1_500_000, MoneyUnit.Rial));
    }

    [Fact]
    public void Format_Toman_DividesByTen()
    {
        Assert.Equal("۱۵۰٬۰۰۰", MoneyFormatter.Format(1_500_000, MoneyUnit.Toman));
    }

    [Fact]
    public void Format_Latin_WhenRequested()
    {
        Assert.Equal("1٬500٬000", MoneyFormatter.Format(1_500_000, MoneyUnit.Rial, persianDigits: false));
    }

    [Fact]
    public void Format_Toman_ShowsFractionWhenNotWhole()
    {
        Assert.Equal("1٫5", MoneyFormatter.Format(15, MoneyUnit.Toman, persianDigits: false));
    }

    [Theory]
    [InlineData(150000L, MoneyUnit.Toman, 1500000L)]
    [InlineData(150000L, MoneyUnit.Rial, 150000L)]
    public void ToRial_ConvertsFromTheChosenUnit(long amount, MoneyUnit unit, long expected)
    {
        Assert.Equal(expected, MoneyFormatter.ToRial(amount, unit));
    }

    [Fact]
    public void AmountInWords_AppendsTheUnit()
    {
        Assert.Equal("صد و پنجاه هزار تومان", MoneyFormatter.AmountInWords(1_500_000, MoneyUnit.Toman));
        Assert.Equal("یک میلیون و پانصد هزار ریال", MoneyFormatter.AmountInWords(1_500_000, MoneyUnit.Rial));
    }

    [Fact]
    public void FormatWithUnit_AddsTheUnitTitle()
    {
        Assert.EndsWith(" تومان", MoneyFormatter.FormatWithUnit(1_500_000, MoneyUnit.Toman));
    }
}
