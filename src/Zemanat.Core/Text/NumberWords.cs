namespace Zemanat.Core.Text;

/// <summary>Converts whole numbers to Persian words (for the "amount in words" shown while typing and on printed forms).</summary>
public static class NumberWords
{
    private static readonly string[] Ones =
        ["", "یک", "دو", "سه", "چهار", "پنج", "شش", "هفت", "هشت", "نه"];

    private static readonly string[] Teens =
        ["ده", "یازده", "دوازده", "سیزده", "چهارده", "پانزده", "شانزده", "هفده", "هجده", "نوزده"];

    private static readonly string[] Tens =
        ["", "", "بیست", "سی", "چهل", "پنجاه", "شصت", "هفتاد", "هشتاد", "نود"];

    private static readonly string[] Hundreds =
        ["", "صد", "دویست", "سیصد", "چهارصد", "پانصد", "ششصد", "هفتصد", "هشتصد", "نهصد"];

    private static readonly string[] Scales =
        ["", "هزار", "میلیون", "میلیارد", "تریلیون", "کوادریلیون", "کوینتیلیون"];

    /// <summary>Returns the Persian words for <paramref name="value"/>, e.g. 1250000 becomes "یک میلیون و دویست و پنجاه هزار".</summary>
    public static string ToWords(long value)
    {
        if (value == 0)
        {
            return "صفر";
        }

        var negative = value < 0;
        var magnitude = negative ? (ulong)(-(value + 1)) + 1UL : (ulong)value;

        var groups = new List<string>();
        var scaleIndex = 0;
        while (magnitude > 0)
        {
            var chunk = (int)(magnitude % 1000UL);
            magnitude /= 1000UL;
            if (chunk != 0)
            {
                var words = ChunkToWords(chunk);
                groups.Add(scaleIndex == 0 ? words : words + " " + Scales[scaleIndex]);
            }

            scaleIndex++;
        }

        groups.Reverse();
        var result = string.Join(" و ", groups);
        return negative ? "منفی " + result : result;
    }

    private static string ChunkToWords(int number)
    {
        var parts = new List<string>(3);
        var hundreds = number / 100;
        var rest = number % 100;

        if (hundreds > 0)
        {
            parts.Add(Hundreds[hundreds]);
        }

        if (rest >= 20)
        {
            parts.Add(Tens[rest / 10]);
            if (rest % 10 > 0)
            {
                parts.Add(Ones[rest % 10]);
            }
        }
        else if (rest >= 10)
        {
            parts.Add(Teens[rest - 10]);
        }
        else if (rest > 0)
        {
            parts.Add(Ones[rest]);
        }

        return string.Join(" و ", parts);
    }
}
