<div dir="rtl">

# RtlMessageDialog

جایگزینی مدرن، بدون نوار عنوان و **راست‌به‌چپ** برای `MessageBox` در Windows Forms، مناسب برنامه‌های فارسی، عربی و عبری.

> 🇬🇧 [English version](README.md)

کل کتابخانه **فقط یک فایل** است (`src/MessageForm.cs`) و به چیزی جز کتابخانه‌های Windows Forms وابسته نیست. فایل را در پروژه‌ات کپی کن و استفاده کن.

## امکانات

- **RTL واقعی:** متن پیام راست‌چین و راست‌به‌چپ است و چیدمان و ترتیب دکمه‌ها هم آینه‌ای می‌شود.
- **جعبهٔ جزئیات LTR:** رشتهٔ اختیاری `details` (مثلاً `ex.ToString()`) در یک جعبهٔ فقط‌خواندنی، **چپ‌چین و تک‌فاصله** نمایش داده می‌شود تا مسیر فایل، stack trace و نقطه‌گذاری متن لاتین درست دیده شود.
- **پنج نوع پیام:** اطلاعات، موفقیت، هشدار، خطا و پرسش، هرکدام با رنگ و نمادِ هدر مخصوص خودش.
- **بدون نوار عنوان:** عنوان تکراری ندارد؛ هدر رنگی عنوان را نشان می‌دهد و با آن می‌شود پنجره را جابه‌جا کرد.
- **پیام بلند مشکلی ندارد:** ارتفاع پنجره با متن تنظیم می‌شود، سقف آن ۶۰٪ ارتفاع صفحه است و بعد از آن متن اسکرول می‌شود.
- **سازگار با DPI بالا:** همهٔ اندازه‌ها با DPI مانیتور مقیاس می‌شوند.
- **Ctrl+C:** پیام و جزئیات را در کلیپ‌بورد کپی می‌کند (برای ارسال گزارش خطا مفید است).
- **صدای سیستمی** مثل `MessageBox` استاندارد (خطا، هشدار، پرسش، اطلاعات).
- **امن در برابر ترد:** اگر از تردِ پس‌زمینه صدا زده شود (و `owner` یک `Control` باشد)، خودکار به تردِ UI منتقل می‌شود.
- **قابل شخصی‌سازی:** متن دکمه‌ها و فونت را با یک خط تغییر بده.

## پیش‌نیازها

- ویندوز با Windows Forms
- .NET 6 یا بالاتر (`net6.0-windows` / `net8.0-windows`)
- C# نسخهٔ ۸ به بالا با nullable reference types

.NET Framework 4.7.2 به بالا هم باید کار کند (کد از `Form.DeviceDpi` استفاده می‌کند)، ولی تست نشده است.

## نصب

۱. فایل `src/MessageForm.cs` را در پروژه‌ات کپی کن.
۲. (اختیاری) `namespace` را با پروژهٔ خودت هماهنگ کن.

همین؛ بدون بستهٔ NuGet و بدون فایل دیگر.

## شروع سریع

</div>

```csharp
using RtlMessageDialog;

// اطلاعات / موفقیت / هشدار
MessageForm.ShowInfo(this, "اطلاعات", "فایل با موفقیت بارگذاری شد.");
MessageForm.ShowSuccess(this, "انجام شد", "عملیات با موفقیت پایان یافت.");
MessageForm.ShowWarning(this, "هشدار", "برخی ردیف‌ها نادیده گرفته شدند.");

// خطا همراه با جزئیات فنی (LTR و چپ‌چین)
try
{
    DoWork();
}
catch (Exception ex)
{
    MessageForm.ShowError(this, "خطا", "انجام عملیات ناموفق بود.", ex.ToString());
}

// پرسش
if (MessageForm.ShowQuestion(this, "تأیید", "آیا مطمئن هستید؟") == DialogResult.Yes)
{
    // ...
}

// هر ترکیبی، مثلاً هشدار با دکمه‌های بله/خیر
var result = MessageForm.Show(this, "هشدار", "فایل بازنویسی می‌شود. ادامه می‌دهید؟",
    MessageKind.Warning, buttons: MessageButtons.YesNo);
```

<div dir="rtl">

> **نکته:** همیشه با نتیجهٔ مثبت مقایسه کن (`== DialogResult.Yes` یا `== DialogResult.OK`)، نه با `!= DialogResult.No`. کلید Esc مقدار `No` یا `Cancel` برمی‌گرداند و Alt+F4 مقدار `Cancel`.

## API

همهٔ متدها `static` هستند و `DialogResult` برمی‌گردانند. `owner` می‌تواند `null` باشد.

| متد | توضیح |
| --- | --- |
| `ShowInfo(owner, title, message, details = null)` | آبی، دکمهٔ تأیید |
| `ShowSuccess(owner, title, message, details = null)` | سبز، دکمهٔ تأیید |
| `ShowWarning(owner, title, message, details = null)` | نارنجی، دکمهٔ تأیید |
| `ShowError(owner, title, message, details = null)` | قرمز، دکمهٔ تأیید |
| `ShowQuestion(owner, title, message, buttons = YesNo)` | بنفش، `YesNo` یا `OkCancel` |
| `Show(owner, title, message, kind, details = null, buttons = Ok)` | هر نوع با هر دکمه |

Enumها:

- `MessageKind`: ‏`Info`, `Success`, `Warning`, `Error`, `Question`
- `MessageButtons`: ‏`Ok`, `OkCancel`, `YesNo`

مقادیر بازگشتی: `Ok` ← `OK`؛ `OkCancel` ← `OK` / `Cancel`؛ `YesNo` ← `Yes` / `No`.

## شخصی‌سازی

این مقادیر را یک بار در شروع برنامه تنظیم کن:

</div>

```csharp
MessageForm.FontName   = "Vazirmatn";   // پیش‌فرض: "Segoe UI"
MessageForm.OkText     = "تأیید";
MessageForm.CancelText = "انصراف";
MessageForm.YesText    = "بله";
MessageForm.NoText     = "خیر";
```

<div dir="rtl">

فونت برای عنوان، پیام و دکمه‌ها استفاده می‌شود. برای زبان دیگر کافی است متن دکمه‌ها را عوض کنی.

برای تغییرات عمیق‌تر (رنگ‌ها، عرض، ارتفاع‌ها)، ثابت‌های ابتدای `MessageForm.cs` را ویرایش کن؛ همهٔ اندازه‌های چیدمان همان‌جا به واحد ۹۶ DPI تعریف شده‌اند.

## نکات رفتاری

- **صفحه‌کلید:** Enter دکمهٔ پیش‌فرض (تأیید / بله) را فعال می‌کند. Esc انصراف می‌دهد (انصراف / خیر؛ در پیام‌های فقط‌تأیید فقط می‌بندد). Ctrl+C متن را کپی می‌کند.
- **جابه‌جایی:** پنجره نوار عنوان ندارد؛ آن را با هدر رنگی بکش.
- **جعبهٔ جزئیات:** فقط وقتی `details` خالی نباشد نمایش داده می‌شود. اگر داخل آن متنی را انتخاب کنی و Ctrl+C بزنی، فقط همان انتخاب کپی می‌شود.
- **عنوان پنجره:** `title` به‌عنوان نام پنجره هم ثبت می‌شود، پس در Alt+Tab و برای صفحه‌خوان‌ها دیده می‌شود.
- **DPI چندمانیتوری:** اندازه‌ها بر اساس DPI لحظهٔ ساخت دیالوگ محاسبه می‌شوند.

## برنامهٔ نمونه

یک برنامهٔ نمونهٔ کوچک در پوشهٔ `demo/` هست:

</div>

```bash
dotnet run --project demo
```

<div dir="rtl">

برای هر نوع دیالوگ یک دکمه دارد، از جمله پیام بسیار بلند و خطا همراه با جزئیات.

## ساختار مخزن

</div>

```
src/MessageForm.cs                 کل کتابخانه
demo/                              برنامهٔ نمونه
README.md / README.fa.md           مستندات
LICENSE                            MIT
```

<div dir="rtl">

## مشارکت

گزارش مشکل و pull request خوش‌آمد است. لطفاً کتابخانه را بدون وابستگی و تک‌فایل نگه دار و تغییرات را متمرکز بنویس.

## مجوز

[MIT](LICENSE)

</div>
