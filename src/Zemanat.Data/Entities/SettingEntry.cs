namespace Zemanat.Data.Entities;

/// <summary>One key/value row of the system settings.</summary>
public class SettingEntry
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
