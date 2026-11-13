namespace Zemanat.Core.Security;

/// <summary>
/// The "complex password" rule that applies when the manager turns it on in the settings:
/// at least 8 characters with at least one letter and one digit.
/// Lockout and session numbers are fixed by the requirements.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int LockoutMinutes = 15;
    public const int MaxFailedAttempts = 5;
    public const int SessionMinutes = 60;

    /// <summary>Returns the Persian messages of every broken rule (empty when the password is acceptable).</summary>
    public static IReadOnlyList<string> Validate(string? password)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
        {
            errors.Add("رمز عبور باید حداقل ۸ کاراکتر باشد.");
        }

        if (string.IsNullOrEmpty(password) || !password.Any(char.IsLetter))
        {
            errors.Add("رمز عبور باید حداقل یک حرف داشته باشد.");
        }

        if (string.IsNullOrEmpty(password) || !password.Any(char.IsDigit))
        {
            errors.Add("رمز عبور باید حداقل یک عدد داشته باشد.");
        }

        return errors;
    }
}
