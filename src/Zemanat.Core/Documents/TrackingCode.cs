using System.Security.Cryptography;

namespace Zemanat.Core.Documents;

/// <summary>
/// The random, unguessable code printed in the QR of a document and used by the public authenticity page.
/// 12 characters from a 32-symbol alphabet (no 0/O/1/I) give 60 bits of randomness.
/// </summary>
public static class TrackingCode
{
    public const int CodeLength = 12;

    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>Creates a new cryptographically random code.</summary>
    public static string Generate() => RandomNumberGenerator.GetString(Alphabet, CodeLength);

    /// <summary>Upper-cases the text and removes spaces and dashes, so a code typed by hand still matches.</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return new string(text
            .Where(ch => !char.IsWhiteSpace(ch) && ch != '-')
            .ToArray())
            .ToUpperInvariant();
    }

    /// <summary>True when the text has the right length and only uses the allowed symbols.</summary>
    public static bool IsWellFormed(string? code) =>
        code is not null && code.Length == CodeLength && code.All(ch => Alphabet.Contains(ch));
}
