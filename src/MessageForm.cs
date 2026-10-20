// RtlMessageDialog — a Persian / RTL-friendly replacement for MessageBox (Windows Forms).
// Licensed under the MIT License. See the LICENSE file in the repository root.

using System;
using System.Drawing;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RtlMessageDialog
{
    /// <summary>Visual kind of a message (header colour and glyph).</summary>
    public enum MessageKind { Info, Success, Warning, Error, Question }

    /// <summary>Button sets that can be shown in the dialog.</summary>
    public enum MessageButtons { Ok, OkCancel, YesNo }

    /// <summary>
    /// A borderless, right-to-left message dialog that replaces <see cref="MessageBox"/>.
    /// <list type="bullet">
    /// <item>Coloured header with a glyph and the title.</item>
    /// <item>Message text: RTL and right-aligned (Persian / Arabic / Hebrew); scrolls when very long.</item>
    /// <item>Optional read-only <c>details</c> box: LTR and left-aligned, monospaced (ideal for exception text).</item>
    /// <item>Localizable buttons, system sounds, Ctrl+C to copy, per-monitor DPI sizing, thread-safe calls.</item>
    /// </list>
    /// The class has no dependencies other than the .NET Windows Forms libraries.
    /// </summary>
    public sealed class MessageForm : Form
    {
        // ---------- Layout constants (logical units at 96 DPI; converted with Px()) ----------
        private const int DialogWidth   = 500;
        private const int HeaderHeight  = 48;
        private const int FooterHeight  = 58;
        private const int DetailsHeight = 112;
        private const int DetailsGap    = 10;
        private const int PadX          = 18;
        private const int PadTop        = 14;
        private const int PadBottom     = 10;
        private const int BorderWidth   = 1;

        // ---------- Public customization ----------

        /// <summary>Text of the OK button. Default: «تأیید».</summary>
        public static string OkText { get; set; } = "تأیید";

        /// <summary>Text of the Cancel button. Default: «انصراف».</summary>
        public static string CancelText { get; set; } = "انصراف";

        /// <summary>Text of the Yes button. Default: «بله».</summary>
        public static string YesText { get; set; } = "بله";

        /// <summary>Text of the No button. Default: «خیر».</summary>
        public static string NoText { get; set; } = "خیر";

        private static string _fontName = "Segoe UI";

        /// <summary>
        /// Font family used for the title, message and buttons. Default: "Segoe UI".
        /// Set it once at startup (e.g. to "Vazirmatn" or "Tahoma"); it applies to dialogs shown afterwards.
        /// </summary>
        public static string FontName
        {
            get => _fontName;
            set
            {
                _fontName = string.IsNullOrWhiteSpace(value) ? "Segoe UI" : value;
                _baseFont = _titleFont = _questionFont = null; // rebuilt lazily with the new family
            }
        }

        // ---------- Shared fonts (created once, live for the whole process) ----------
        private static Font? _baseFont, _titleFont, _questionFont, _symbolFont, _detailsFont;
        private static Font BaseFont     => _baseFont     ??= new Font(FontName, 9F);
        private static Font TitleFont    => _titleFont    ??= new Font(FontName, 11F, FontStyle.Bold);
        private static Font QuestionFont => _questionFont ??= new Font(FontName, 14F, FontStyle.Bold);
        private static Font SymbolFont   => _symbolFont   ??= new Font("Segoe UI Symbol", 13F, FontStyle.Bold);
        private static Font DetailsFont  => _detailsFont  ??= new Font("Consolas", 9F);

        // ---------- Borderless-window dragging ----------
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 2;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        // ---------- Instance state ----------
        private readonly string _copyText;
        private readonly TextBox? _detailsBox;

        /// <summary>Converts a logical (96-DPI) size to real pixels for the current DPI.</summary>
        private int Px(int v) => (int)Math.Round(v * DeviceDpi / 96.0);

        private MessageForm(IWin32Window? owner, string title, string message, MessageKind kind,
            string? details, MessageButtons buttons)
        {
            message ??= "";
            _copyText = string.IsNullOrWhiteSpace(details)
                ? message
                : message + Environment.NewLine + Environment.NewLine + details;

            // The title bar is intentionally hidden (the coloured header shows the title).
            // Text is still set so the window has a name in Alt+Tab and for screen readers.
            Text = title;
            FormBorderStyle = FormBorderStyle.None;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            // Sizes are scaled manually with Px(); automatic form scaling would scale them twice.
            AutoScaleMode = AutoScaleMode.None;
            Font = BaseFont;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ShowIcon = false;
            // A thin outline replaces the missing window border: the form is grey, its content is inset by the border width.
            BackColor = Color.FromArgb(150, 150, 150);
            Padding = new Padding(Px(BorderWidth));

            // Header colour and glyph for the message kind
            string glyph;
            Color accent;
            Font glyphFont;
            switch (kind)
            {
                case MessageKind.Success:
                    accent = Color.FromArgb(39, 174, 96);
                    glyph = "✔";
                    glyphFont = SymbolFont;
                    break;
                case MessageKind.Warning:
                    accent = Color.FromArgb(230, 126, 34);
                    glyph = "⚠";
                    glyphFont = SymbolFont;
                    break;
                case MessageKind.Error:
                    accent = Color.FromArgb(192, 57, 43);
                    glyph = "✖";
                    glyphFont = SymbolFont;
                    break;
                case MessageKind.Question:
                    accent = Color.FromArgb(142, 68, 173);
                    glyph = "؟";
                    glyphFont = QuestionFont;
                    break;
                default: // Info
                    accent = Color.FromArgb(41, 128, 185);
                    glyph = "ℹ";
                    glyphFont = SymbolFont;
                    break;
            }

            // ---- Header: coloured bar with the glyph (right side) and the title ----
            var headerTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(Px(6), 0, Px(10), 0),
                Margin = new Padding(0),
            };
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Px(40)));
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            headerTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var glyphLabel = new Label
            {
                Text = glyph,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = glyphFont,
                AutoEllipsis = true,
            };
            var titleLabel = new Label
            {
                Text = title,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, // under RTL, "Left" renders on the right
                ForeColor = Color.White,
                Font = TitleFont,
                AutoEllipsis = true,
            };
            headerTable.Controls.Add(glyphLabel, 0, 0);
            headerTable.Controls.Add(titleLabel, 1, 0);

            var header = new Panel { Dock = DockStyle.Fill, BackColor = accent, Margin = new Padding(0) };
            header.Controls.Add(headerTable);

            // The header acts as the drag handle of the borderless window
            header.MouseDown += BeginDrag;
            headerTable.MouseDown += BeginDrag;
            glyphLabel.MouseDown += BeginDrag;
            titleLabel.MouseDown += BeginDrag;

            // ---- Message text: RTL, right-aligned; scrolls when very long ----
            var messageLabel = new Label
            {
                Text = message,
                AutoSize = false,
                RightToLeft = RightToLeft.Yes,
                TextAlign = ContentAlignment.TopLeft, // under RTL, "Left" renders on the right
                ForeColor = Color.FromArgb(40, 40, 40),
            };
            var messagePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                AutoScroll = true,
                Padding = new Padding(Px(PadX), Px(PadTop), Px(PadX), Px(PadBottom)),
                Margin = new Padding(0),
            };
            messagePanel.Controls.Add(messageLabel);

            // ---- Details (e.g. exception text): LTR, left-aligned, monospaced ----
            Panel? detailsHost = null;
            if (!string.IsNullOrWhiteSpace(details))
            {
                _detailsBox = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Text = details,
                    RightToLeft = RightToLeft.No,
                    TextAlign = HorizontalAlignment.Left,
                    Font = DetailsFont,
                    BackColor = Color.FromArgb(246, 246, 246),
                    ForeColor = Color.FromArgb(70, 70, 70),
                    BorderStyle = BorderStyle.FixedSingle,
                };
                _detailsBox.Select(0, 0); // don't open with all the text selected

                // Spacing comes from this panel's Padding (Margin has no effect on docked controls)
                detailsHost = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    Padding = new Padding(Px(PadX), 0, Px(PadX), Px(DetailsGap)),
                    Margin = new Padding(0),
                };
                detailsHost.Controls.Add(_detailsBox);
            }

            // ---- Footer: buttons flowing right-to-left ----
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0),
            };
            var footer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(244, 244, 244),
                Padding = new Padding(Px(14), Px(13), Px(14), 0),
                Margin = new Padding(0),
            };
            footer.Controls.Add(flow);

            Button MakeButton(string text, DialogResult result) => new Button
            {
                Text = text,
                DialogResult = result,
                Width = Px(96),
                Height = Px(32),
                Margin = new Padding(Px(6), 0, 0, 0),
            };

            switch (buttons)
            {
                case MessageButtons.OkCancel:
                {
                    var ok = MakeButton(OkText, DialogResult.OK);
                    var cancel = MakeButton(CancelText, DialogResult.Cancel);
                    flow.Controls.Add(ok);      // OK on the right (first in an RTL flow)
                    flow.Controls.Add(cancel);
                    AcceptButton = ok;
                    CancelButton = cancel;
                    break;
                }
                case MessageButtons.YesNo:
                {
                    var yes = MakeButton(YesText, DialogResult.Yes);
                    var no = MakeButton(NoText, DialogResult.No);
                    flow.Controls.Add(yes);
                    flow.Controls.Add(no);
                    AcceptButton = yes;
                    CancelButton = no;
                    break;
                }
                default: // OK only — Enter and Esc both close
                {
                    var okOnly = MakeButton(OkText, DialogResult.OK);
                    flow.Controls.Add(okOnly);
                    AcceptButton = okOnly;
                    CancelButton = okOnly;
                    break;
                }
            }

            // ---- Sizing: height follows the text, capped at 60% of the screen's working area ----
            Screen screen = owner is Control oc && oc.IsHandleCreated
                ? Screen.FromControl(oc)
                : Screen.FromPoint(Cursor.Position);

            int border = Px(BorderWidth);
            int clientW = Px(DialogWidth);
            int textW = clientW - Px(PadX) * 2;
            int fixedH = Px(HeaderHeight) + Px(FooterHeight) + Px(PadTop) + Px(PadBottom)
                         + (detailsHost != null ? Px(DetailsHeight + DetailsGap) : 0);
            int maxMsgH = Math.Max(Px(60), (int)(screen.WorkingArea.Height * 0.6) - fixedH);

            int msgH = messageLabel.GetPreferredSize(new Size(textW, 0)).Height;
            if (msgH > maxMsgH)
            {
                // A vertical scrollbar will appear: narrow the text and measure again
                textW -= SystemInformation.VerticalScrollBarWidth;
                msgH = messageLabel.GetPreferredSize(new Size(textW, 0)).Height;
            }
            messageLabel.Size = new Size(textW, msgH);
            messageLabel.Location = new Point(Px(PadX), Px(PadTop));

            // ---- Root: header + message (remaining space) + optional details + footer ----
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = detailsHost != null ? 4 : 3,
                Padding = new Padding(0),
                Margin = new Padding(0),
                BackColor = Color.White,
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Px(HeaderHeight)));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            if (detailsHost != null)
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, Px(DetailsHeight + DetailsGap)));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Px(FooterHeight)));

            int row = 0;
            root.Controls.Add(header, 0, row++);
            root.Controls.Add(messagePanel, 0, row++);
            if (detailsHost != null) root.Controls.Add(detailsHost, 0, row++);
            root.Controls.Add(footer, 0, row);
            Controls.Add(root);

            ClientSize = new Size(clientW + border * 2, fixedH + Math.Min(msgH, maxMsgH) + border * 2);
        }

        /// <summary>Starts a native window drag (used by the header of the borderless dialog).</summary>
        private void BeginDrag(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
        }

        /// <summary>
        /// Ctrl+C copies the message (and details) to the clipboard.
        /// If text is selected in the details box, the normal copy of that selection is used instead.
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.C)
                && !(_detailsBox != null && _detailsBox.Focused && _detailsBox.SelectionLength > 0))
            {
                try { Clipboard.SetText(_copyText); }
                catch { /* the clipboard may be temporarily locked by another process */ }
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // =====================================================================
        //  Public API
        // =====================================================================

        /// <summary>Shows a dialog with any kind/button combination (e.g. a warning with Yes/No).</summary>
        public static DialogResult Show(IWin32Window? owner, string title, string message,
            MessageKind kind, string? details = null, MessageButtons buttons = MessageButtons.Ok)
            => ShowCore(owner, title, message, kind, details, buttons);

        /// <summary>Information message (blue) with an OK button.</summary>
        public static DialogResult ShowInfo(IWin32Window? owner, string title, string message, string? details = null)
            => ShowCore(owner, title, message, MessageKind.Info, details, MessageButtons.Ok);

        /// <summary>Success message (green) with an OK button.</summary>
        public static DialogResult ShowSuccess(IWin32Window? owner, string title, string message, string? details = null)
            => ShowCore(owner, title, message, MessageKind.Success, details, MessageButtons.Ok);

        /// <summary>Warning message (orange) with an OK button.</summary>
        public static DialogResult ShowWarning(IWin32Window? owner, string title, string message, string? details = null)
            => ShowCore(owner, title, message, MessageKind.Warning, details, MessageButtons.Ok);

        /// <summary>Error message (red) with an OK button; optional <paramref name="details"/> (e.g. the full exception text).</summary>
        public static DialogResult ShowError(IWin32Window? owner, string title, string message, string? details = null)
            => ShowCore(owner, title, message, MessageKind.Error, details, MessageButtons.Ok);

        /// <summary>Question (purple) with Yes/No or OK/Cancel buttons.</summary>
        public static DialogResult ShowQuestion(IWin32Window? owner, string title, string message,
            MessageButtons buttons = MessageButtons.YesNo)
            => ShowCore(owner, title, message, MessageKind.Question, null, buttons);

        private static DialogResult ShowCore(
            IWin32Window? owner, string title, string message,
            MessageKind kind, string? details, MessageButtons buttons)
        {
            // Called from a non-UI thread: marshal to the owner's thread
            if (owner is Control c && c.InvokeRequired)
                return (DialogResult)c.Invoke(
                    new Func<DialogResult>(() => ShowCore(owner, title, message, kind, details, buttons)));

            PlaySound(kind);
            using var dlg = new MessageForm(owner, title, message, kind, details, buttons);
            return dlg.ShowDialog(owner);
        }

        private static void PlaySound(MessageKind kind)
        {
            switch (kind)
            {
                case MessageKind.Error:    SystemSounds.Hand.Play(); break;
                case MessageKind.Warning:  SystemSounds.Exclamation.Play(); break;
                case MessageKind.Question: SystemSounds.Question.Play(); break;
                default:                   SystemSounds.Asterisk.Play(); break;
            }
        }
    }
}
