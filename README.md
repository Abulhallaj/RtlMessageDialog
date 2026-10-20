# RtlMessageDialog

A modern, borderless, **right-to-left** replacement for `MessageBox` in Windows Forms — built for Persian (Farsi), Arabic and Hebrew applications.

> 🇮🇷 [نسخهٔ فارسی](README.fa.md)

The whole library is **one file** (`src/MessageForm.cs`) with **no dependencies** beyond .NET Windows Forms. Copy it into your project and use it.

## Features

- **Proper RTL**: message text is right-aligned and RTL; the button order and layout are mirrored.
- **LTR details box**: pass an optional `details` string (for example `ex.ToString()`) and it is shown in a read-only, **left-aligned, monospaced** box, so file paths, stack traces and punctuation in Latin text are displayed correctly.
- **Five message kinds**: Info, Success, Warning, Error, Question — each with its own header colour and glyph.
- **Borderless design**: no duplicated title bar; the coloured header shows the title and works as a drag handle.
- **Long messages are safe**: window height follows the text, is capped at 60% of the screen, and the text scrolls beyond that.
- **High-DPI aware**: all sizes scale with the monitor DPI.
- **Ctrl+C** copies the message and details to the clipboard (handy for bug reports).
- **System sounds** like the standard `MessageBox` (error, warning, question, info).
- **Thread-safe**: calling from a background thread is marshalled to the owner's UI thread automatically (when `owner` is a `Control`).
- **Localizable**: change the button texts and the font family in one line each.

## Requirements

- Windows, with Windows Forms
- .NET 6 or later (`net6.0-windows` / `net8.0-windows`)
- C# 8+ with nullable reference types

.NET Framework 4.7.2+ should also work (the code uses `Form.DeviceDpi`), but it has not been tested.

## Installation

1. Copy `src/MessageForm.cs` into your project.
2. (Optional) Change the `namespace` to match your project.

That's it — no NuGet packages, no other files.

## Quick start

```csharp
using RtlMessageDialog;

// Information / success / warning
MessageForm.ShowInfo(this, "اطلاعات", "فایل با موفقیت بارگذاری شد.");
MessageForm.ShowSuccess(this, "انجام شد", "عملیات با موفقیت پایان یافت.");
MessageForm.ShowWarning(this, "هشدار", "برخی ردیف‌ها نادیده گرفته شدند.");

// Error with technical details (shown LTR, left-aligned)
try
{
    DoWork();
}
catch (Exception ex)
{
    MessageForm.ShowError(this, "خطا", "انجام عملیات ناموفق بود.", ex.ToString());
}

// Question
if (MessageForm.ShowQuestion(this, "تأیید", "آیا مطمئن هستید؟") == DialogResult.Yes)
{
    // ...
}

// Any combination, e.g. a warning with Yes/No buttons
var result = MessageForm.Show(this, "هشدار", "فایل بازنویسی می‌شود. ادامه می‌دهید؟",
    MessageKind.Warning, buttons: MessageButtons.YesNo);
```

> **Tip:** always compare against the positive result (`== DialogResult.Yes` or `== DialogResult.OK`) rather than `!= DialogResult.No`. Pressing Esc returns `No`/`Cancel`, and Alt+F4 returns `Cancel`.

## API

All methods are `static` and return a `DialogResult`. `owner` can be `null`.

| Method | Description |
| --- | --- |
| `ShowInfo(owner, title, message, details = null)` | Blue, OK button |
| `ShowSuccess(owner, title, message, details = null)` | Green, OK button |
| `ShowWarning(owner, title, message, details = null)` | Orange, OK button |
| `ShowError(owner, title, message, details = null)` | Red, OK button |
| `ShowQuestion(owner, title, message, buttons = YesNo)` | Purple, `YesNo` or `OkCancel` |
| `Show(owner, title, message, kind, details = null, buttons = Ok)` | Any kind with any buttons |

Enums:

- `MessageKind`: `Info`, `Success`, `Warning`, `Error`, `Question`
- `MessageButtons`: `Ok`, `OkCancel`, `YesNo`

Returned values: `Ok` → `OK`; `OkCancel` → `OK` / `Cancel`; `YesNo` → `Yes` / `No`.

## Customization

Set these once at application startup:

```csharp
MessageForm.FontName   = "Vazirmatn";   // default: "Segoe UI"
MessageForm.OkText     = "تأیید";
MessageForm.CancelText = "انصراف";
MessageForm.YesText    = "بله";
MessageForm.NoText     = "خیر";
```

The font family is used for the title, message and buttons. For another language, just change the button texts.

For deeper changes (colours, width, heights), edit the constants at the top of `MessageForm.cs` — all layout sizes are defined there in 96-DPI units.

## Behaviour notes

- **Keyboard**: Enter activates the default button (OK / Yes). Esc cancels (Cancel / No; for OK-only dialogs it simply closes). Ctrl+C copies the text.
- **Dragging**: the dialog has no title bar; drag it by the coloured header.
- **Details box**: shown only when `details` is not empty. Selecting text inside it and pressing Ctrl+C copies just the selection.
- **Window title**: the `title` is also set as the window's name, so it still appears in Alt+Tab and to screen readers.
- **Multi-monitor DPI**: sizes are computed from the DPI at the moment the dialog is created.

## Demo

A small demo app lives in `demo/`:

```bash
dotnet run --project demo
```

It has one button per dialog type, including a very long message and an error with details.

## Repository layout

```
src/MessageForm.cs                 the whole library
demo/                              sample app
README.md / README.fa.md           documentation
LICENSE                            MIT
```

## Contributing

Issues and pull requests are welcome. Please keep the library dependency-free and single-file, and keep changes focused.

## License

[MIT](LICENSE)
