namespace Zemanat.Core.Settings;

/// <summary>Checks the settings before they are saved from the admin screen.</summary>
public static class SystemSettingsValidator
{
    /// <summary>The page sizes offered in lists.</summary>
    public static readonly IReadOnlyList<int> AllowedPageSizes = [50, 100, 200, 300, 400, 500];

    /// <summary>Returns the Persian messages of every problem (empty when the settings are valid).</summary>
    public static IReadOnlyList<string> Validate(SystemSettings settings)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(settings.OrganizationName))
        {
            errors.Add("نام سازمان نمی‌تواند خالی باشد.");
        }

        if (settings.CreditPercent <= 0m || settings.CreditPercent > 1000m)
        {
            errors.Add("درصد مجاز باید بین ۰ و ۱۰۰۰ باشد.");
        }

        if (settings.CreditMonths < 1 || settings.CreditMonths > 120)
        {
            errors.Add("تعداد ماه باید بین ۱ و ۱۲۰ باشد.");
        }

        if (settings.ValidityDays < 1 || settings.ValidityDays > 3650)
        {
            errors.Add("مدت اعتبار سند باید بین ۱ و ۳۶۵۰ روز باشد.");
        }

        if (string.IsNullOrWhiteSpace(settings.DocumentNumberCode))
        {
            errors.Add("کد شماره‌ی دبیرخانه نمی‌تواند خالی باشد.");
        }

        if (string.IsNullOrEmpty(settings.DocumentNumberSeparator) || settings.DocumentNumberSeparator.Length > 3)
        {
            errors.Add("جداکننده‌ی شماره باید ۱ تا ۳ نویسه باشد.");
        }
        else if (!string.IsNullOrWhiteSpace(settings.DocumentNumberCode)
                 && settings.DocumentNumberCode.Contains(settings.DocumentNumberSeparator, StringComparison.Ordinal))
        {
            errors.Add("کد شماره‌ی دبیرخانه نباید شامل جداکننده باشد.");
        }

        if (!string.IsNullOrWhiteSpace(settings.WebsiteBaseUrl)
            && !(Uri.TryCreate(settings.WebsiteBaseUrl.Trim(), UriKind.Absolute, out var uri)
                 && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
        {
            errors.Add("آدرس سایت باید با http:// یا https:// شروع شود.");
        }

        if (!AllowedPageSizes.Contains(settings.DefaultPageSize))
        {
            errors.Add("تعداد ردیف هر صفحه باید یکی از ۵۰، ۱۰۰، ۲۰۰، ۳۰۰، ۴۰۰ یا ۵۰۰ باشد.");
        }

        if (string.IsNullOrWhiteSpace(settings.GuaranteeTitle)
            || string.IsNullOrWhiteSpace(settings.SalaryCertificateTitle)
            || string.IsNullOrWhiteSpace(settings.BankInquiryTitle))
        {
            errors.Add("عنوان هر سه نوع گواهی باید پر باشد.");
        }

        return errors;
    }
}
