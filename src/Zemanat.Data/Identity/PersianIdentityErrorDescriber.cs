using Microsoft.AspNetCore.Identity;

namespace Zemanat.Data.Identity;

public class PersianIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => new() { Code = nameof(DefaultError), Description = "خطای ناشناخته رخ داد." };
    public override IdentityError PasswordMismatch() => new() { Code = nameof(PasswordMismatch), Description = "رمز عبور فعلی نادرست است." };
    public override IdentityError InvalidUserName(string? userName) => new() { Code = nameof(InvalidUserName), Description = "نام کاربری نامعتبر است." };
    public override IdentityError DuplicateUserName(string userName) => new() { Code = nameof(DuplicateUserName), Description = "این نام کاربری قبلاً ثبت شده است." };
    public override IdentityError DuplicateRoleName(string role) => new() { Code = nameof(DuplicateRoleName), Description = "این نقش قبلاً ثبت شده است." };
    public override IdentityError UserAlreadyInRole(string role) => new() { Code = nameof(UserAlreadyInRole), Description = "کاربر قبلاً این نقش را دارد." };
    public override IdentityError UserNotInRole(string role) => new() { Code = nameof(UserNotInRole), Description = "کاربر این نقش را ندارد." };
    public override IdentityError UserLockoutNotEnabled() => new() { Code = nameof(UserLockoutNotEnabled), Description = "قفل شدن برای این کاربر فعال نیست." };
}
