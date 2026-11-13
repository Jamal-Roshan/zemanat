using System.Globalization;
using System.Reflection;

namespace Zemanat.Core.Settings;

/// <summary>
/// Converts <see cref="SystemSettings"/> to and from the key/value rows stored in the database.
/// A missing or unreadable value keeps the default, so the app never breaks because of a bad row.
/// </summary>
public static class SystemSettingsSerializer
{
    private static readonly PropertyInfo[] Properties = typeof(SystemSettings)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead && p.CanWrite)
        .ToArray();

    /// <summary>The setting keys (the property names).</summary>
    public static IReadOnlyList<string> Keys { get; } = Properties.Select(p => p.Name).ToArray();

    /// <summary>Writes every setting as text using the invariant culture.</summary>
    public static IReadOnlyDictionary<string, string> ToDictionary(SystemSettings settings)
    {
        var result = new Dictionary<string, string>(Properties.Length);
        foreach (var property in Properties)
        {
            var value = property.GetValue(settings);
            result[property.Name] = value switch
            {
                null => string.Empty,
                bool flag => flag ? "true" : "false",
                IFormattable formattable when value is not Enum => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? string.Empty
            };
        }

        return result;
    }

    /// <summary>Builds the settings from stored rows. Anything missing or invalid keeps its default value.</summary>
    public static SystemSettings FromDictionary(IReadOnlyDictionary<string, string> values)
    {
        var settings = new SystemSettings();
        foreach (var property in Properties)
        {
            if (!values.TryGetValue(property.Name, out var text) || text is null)
            {
                continue;
            }

            if (TryConvert(text, property.PropertyType, out var converted))
            {
                property.SetValue(settings, converted);
            }
        }

        return settings;
    }

    private static bool TryConvert(string text, Type type, out object? value)
    {
        value = null;

        if (type == typeof(string))
        {
            value = text;
            return true;
        }

        if (type == typeof(bool))
        {
            if (bool.TryParse(text, out var flag))
            {
                value = flag;
                return true;
            }

            return false;
        }

        if (type == typeof(int))
        {
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            {
                value = number;
                return true;
            }

            return false;
        }

        if (type == typeof(decimal))
        {
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
            {
                value = number;
                return true;
            }

            return false;
        }

        if (type.IsEnum)
        {
            if (Enum.TryParse(type, text, ignoreCase: true, out var parsed) && Enum.IsDefined(type, parsed!))
            {
                value = parsed;
                return true;
            }

            return false;
        }

        return false;
    }
}
