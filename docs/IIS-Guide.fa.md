# راهنمای نصب روی IIS (ویندوز سرور)

## پیش‌نیازها
1. **ASP.NET Core Hosting Bundle** نسخه‌ی .NET 10 را نصب کنید (روی سرور اینترنت لازم نیست؛ فایل نصب را از جای دیگر ببرید). پس از نصب، یک‌بار `iisreset` بزنید.
2. در Server Manager، ویژگی‌ی **WebSocket Protocol** را در IIS فعال کنید (Blazor Server به آن نیاز دارد):
   Add Roles and Features ← Web Server ← Application Development ← WebSocket Protocol.
3. SQL Server 2016 یا بالاتر و یک پایگاه داده‌ی خالی (یا نام پایگاه داده‌ای که برنامه بسازد).

## انتشار برنامه
روی ماشین توسعه:

```
dotnet publish src/Zemanat.Web -c Release -o publish
```

پوشه‌ی `publish` را روی سرور مثلاً در `C:\inetpub\zemanat` کپی کنید.

## ساخت سایت
1. در IIS Manager یک **Application Pool** جدید بسازید: `.NET CLR version = No Managed Code`.
2. یک **Site** جدید بسازید که Physical path آن `C:\inetpub\zemanat` و Pool آن همان Pool باشد.
3. به حساب Pool (`IIS AppPool\<نام Pool>`) روی پوشه‌ی سایت دسترسی Read & Execute بدهید.

## رشته‌ی اتصال (متغیر محیطی)
رشته‌ی اتصال از متغیر محیطی `ZEMANAT_CONNECTION_STRING` خوانده می‌شود، نه از فایل تنظیمات. روی سرور، در PowerShell با دسترسی Administrator:

```
setx ZEMANAT_CONNECTION_STRING "Server=.;Database=Zemanat;Integrated Security=True;TrustServerCertificate=True" /M
iisreset
```

نکته‌ها:
- `/M` متغیر را برای کل ماشین تنظیم می‌کند تا Pool آن را ببیند.
- حساب Pool باید در SQL Server ورود (Login) داشته باشد: `CREATE LOGIN [IIS APPPOOL\<نام Pool>] FROM WINDOWS` و سپس در پایگاه داده نقش `db_owner` (برای ساخت خودکار جدول‌ها در اولین اجرا).
- اگر از SQL Login استفاده می‌کنید: `User Id=...;Password=...` را در رشته بگذارید.

## اولین اجرا
با اولین باز شدن سایت، جدول‌ها خودکار ساخته می‌شوند و کاربر `admin` با رمز `admin` ایجاد می‌شود. وارد شوید و:
1. از «تنظیمات»، نشانی سایت (برای QR)، نام سازمان، امضا و کد دبیرخانه را تنظیم کنید.
2. از «بارگذاری پرسنل» فایل Excel کارکنان را وارد کنید.
3. یک نفر را «مدیر» کنید؛ از آن لحظه کاربر `admin` غیرفعال می‌شود.

## فونت‌های چاپ
فونت‌های B Nazanin و B Titr را روی **رایانه‌ی کاربرانی که چاپ می‌کنند** نصب کنید؛ در غیر این صورت وزیر جایگزین می‌شود.

## عیب‌یابی
- خطای 500.30 یا 500.19: Hosting Bundle نصب نیست یا پس از نصب `iisreset` زده نشده است.
- صفحه باز می‌شود ولی بی‌پاسخ است: WebSocket فعال نیست.
- خطای اتصال به پایگاه داده: متغیر محیطی با `/M` تنظیم نشده یا حساب Pool دسترسی ندارد. گزارش خطا در پوشه‌ی `logs` یا Event Viewer است.
