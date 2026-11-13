namespace Zemanat.Data;

/// <summary>Reads the connection string from the OS environment variable so no secret is ever stored in the repository.</summary>
public static class ConnectionStringProvider
{
    public const string EnvironmentVariableName = "ZEMANAT_CONNECTION_STRING";

    public static string Get()
    {
        var value = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"متغیر محیطی {EnvironmentVariableName} تنظیم نشده است. رشته‌ی اتصال بانک اطلاعاتی را در آن قرار دهید.");
        }

        return value;
    }
}
