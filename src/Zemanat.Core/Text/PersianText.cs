using System.Globalization;
using System.Text;

namespace Zemanat.Core.Text;

/// <summary>
/// Helpers for normalising Persian text and converting digits.
/// All user text is normalised before being stored (Arabic yeh/kaf become Persian yeh/kaf).
/// </summary>
public static class PersianText
{
    private const char ArabicYeh = 'ي';
    private const char AlefMaksura = 'ى';
    private const char ArabicKaf = 'ك';
    private const char PersianYeh = 'ی';
    private const char PersianKaf = 'ک';

    /// <summary>Replaces Arabic yeh / alef maksura / kaf with their Persian forms.</summary>
    public static string NormalizeLetters(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            builder.Append(ch switch
            {
                ArabicYeh => PersianYeh,
                AlefMaksura => PersianYeh,
                ArabicKaf => PersianKaf,
                _ => ch
            });
        }

        return builder.ToString();
    }

    /// <summary>
    /// Normalises letters, removes directional marks, converts odd whitespace to a plain space,
    /// collapses repeated spaces and trims. The zero-width non-joiner is kept on purpose.
    /// </summary>
    public static string CleanText(string? input)
    {
        var text = NormalizeLetters(input);
        if (text.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        var previousWasSpace = false;
        foreach (var ch in text)
        {
            if (ch is '‎' or '‏' or '‪' or '‫' or '‬' or '‭' or '‮' or '﻿')
            {
                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                if (!previousWasSpace)
                {
                    builder.Append(' ');
                }

                previousWasSpace = true;
                continue;
            }

            previousWasSpace = false;
            builder.Append(ch);
        }

        return builder.ToString().Trim();
    }

    /// <summary>Converts Persian and Arabic-Indic digits to Latin digits (0-9).</summary>
    public static string ToLatinDigits(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (ch >= '۰' && ch <= '۹')
            {
                builder.Append((char)('0' + (ch - '۰')));
            }
            else if (ch >= '٠' && ch <= '٩')
            {
                builder.Append((char)('0' + (ch - '٠')));
            }
            else
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }

    /// <summary>Converts Latin and Arabic-Indic digits to Persian digits.</summary>
    public static string ToPersianDigits(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (ch >= '0' && ch <= '9')
            {
                builder.Append((char)('۰' + (ch - '0')));
            }
            else if (ch >= '٠' && ch <= '٩')
            {
                builder.Append((char)('۰' + (ch - '٠')));
            }
            else
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Parses a whole, non-negative amount typed by a user. Accepts Persian/Arabic/Latin digits and
    /// the usual thousands separators (comma, Persian comma, Arabic thousands separator, spaces).
    /// </summary>
    public static bool TryParseAmount(string? input, out long value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var builder = new StringBuilder(input.Length);
        foreach (var ch in ToLatinDigits(input))
        {
            if (ch is ',' or '،' or '٬' or '‌' || char.IsWhiteSpace(ch))
            {
                continue;
            }

            builder.Append(ch);
        }

        return long.TryParse(builder.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }
}
