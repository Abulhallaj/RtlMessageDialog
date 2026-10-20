using System;
using System.Windows.Forms;

namespace RtlMessageDialog.Demo
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new DemoForm());
        }
    }

    /// <summary>A small window with one button per dialog type.</summary>
    internal sealed class DemoForm : Form
    {
        public DemoForm()
        {
            Text = "RtlMessageDialog demo";
            ClientSize = new System.Drawing.Size(320, 330);
            StartPosition = FormStartPosition.CenterScreen;

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(16),
                WrapContents = false,
            };
            Controls.Add(flow);

            AddButton(flow, "Info", () =>
                MessageForm.ShowInfo(this, "اطلاعات", "فایل پرداخت با موفقیت بارگذاری شد."));

            AddButton(flow, "Success", () =>
                MessageForm.ShowSuccess(this, "انجام شد", "۱۲۸ ردیف پردازش و فایل خروجی ساخته شد."));

            AddButton(flow, "Warning", () =>
                MessageForm.ShowWarning(this, "هشدار", "برخی ردیف‌ها شمارهٔ شبا ندارند و نادیده گرفته می‌شوند."));

            AddButton(flow, "Error + details (LTR)", () =>
            {
                try
                {
                    _ = System.IO.File.ReadAllText(@"C:\no\such\file.txt");
                }
                catch (Exception ex)
                {
                    MessageForm.ShowError(this, "خطا", "خواندن فایل ناموفق بود.", ex.ToString());
                }
            });

            AddButton(flow, "Question (Yes/No)", () =>
            {
                var r = MessageForm.ShowQuestion(this, "تأیید", "آیا از حذف این مورد مطمئن هستید؟");
                // Always compare with == Yes (Esc / Alt+F4 return No / Cancel)
                MessageForm.ShowInfo(this, "نتیجه", r == DialogResult.Yes ? "بله انتخاب شد." : "بله انتخاب نشد.");
            });

            AddButton(flow, "Warning + Yes/No", () =>
                MessageForm.Show(this, "هشدار", "فایل موجود بازنویسی می‌شود. ادامه می‌دهید؟",
                    MessageKind.Warning, buttons: MessageButtons.YesNo));

            AddButton(flow, "Very long message (scrolls)", () =>
                MessageForm.ShowInfo(this, "پیام بلند",
                    string.Join(Environment.NewLine, System.Linq.Enumerable.Repeat(
                        "این یک خط نمونه از یک پیام بسیار طولانی است که باید اسکرول شود.", 60))));
        }

        private static void AddButton(FlowLayoutPanel parent, string text, Action onClick)
        {
            var b = new Button { Text = text, Width = 280, Height = 32 };
            b.Click += (_, _) => onClick();
            parent.Controls.Add(b);
        }
    }
}
