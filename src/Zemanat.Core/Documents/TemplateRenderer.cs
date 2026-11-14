using System.Net;
using System.Text.RegularExpressions;

namespace Zemanat.Core.Documents;

/// <summary>
/// Fills a print template. Placeholders look like {{FullName}}; values are HTML-encoded unless the key is listed as raw
/// (the QR code and signature are ready-made HTML). Unknown placeholders become empty text.
/// </summary>
public static partial class TemplateRenderer
{
    public static readonly IReadOnlySet<string> RawKeys = new HashSet<string> { "QR", "Sign" };

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex Token();

    public static string Render(string template, IReadOnlyDictionary<string, string> values)
    {
        return Token().Replace(template, m =>
        {
            var key = m.Groups[1].Value;
            if (!values.TryGetValue(key, out var value)) return string.Empty;
            return RawKeys.Contains(key) ? value : WebUtility.HtmlEncode(value);
        });
    }

    /// <summary>The placeholder names used inside a template (for the editor help and for tests).</summary>
    public static IReadOnlyList<string> PlaceholdersIn(string template)
        => Token().Matches(template).Select(m => m.Groups[1].Value).Distinct().ToList();
}
