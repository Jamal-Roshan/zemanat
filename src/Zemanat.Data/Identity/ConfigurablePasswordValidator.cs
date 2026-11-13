using Microsoft.AspNetCore.Identity;
using Zemanat.Core.Security;
using Zemanat.Data.Entities;

namespace Zemanat.Data.Identity;

/// <summary>
/// Applies the complex-password rule only when it is switched on in the settings (default off).
/// The switch is read through <see cref="IPasswordRuleSwitch"/> so the validator stays testable.
/// </summary>
public class ConfigurablePasswordValidator : IPasswordValidator<AppUser>
{
    private readonly IPasswordRuleSwitch _switch;

    public ConfigurablePasswordValidator(IPasswordRuleSwitch ruleSwitch) => _switch = ruleSwitch;

    public async Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user, string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return IdentityResult.Failed(new IdentityError { Code = "PasswordEmpty", Description = "رمز عبور نمی‌تواند خالی باشد." });
        }

        if (!await _switch.IsComplexRequiredAsync())
        {
            return IdentityResult.Success;
        }

        var errors = PasswordPolicy.Validate(password);
        return errors.Count == 0
            ? IdentityResult.Success
            : IdentityResult.Failed(errors.Select(e => new IdentityError { Code = "PasswordPolicy", Description = e }).ToArray());
    }
}

/// <summary>Tells whether the complex-password rule is currently on.</summary>
public interface IPasswordRuleSwitch
{
    Task<bool> IsComplexRequiredAsync();
}
