using System;
using System.Drawing;
using System.Windows.Forms;

namespace Radeoner
{
    internal sealed class DetailsDialog : Form
    {
        private readonly TextBox body;
        private readonly string theme;
        internal DetailsDialog(string title, string text, Font font, string selectedTheme)
        {
            theme = selectedTheme; Text = AccessibleName = title; Font = font;
            AutoScaleMode = AutoScaleMode.Dpi; Size = new Size(700, 540); MinimumSize = new Size(420, 300);
            StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 2 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Text = text, AccessibleName = title };
            var close = new ThemedButton { Text = L.T("dialog.close"), AccessibleName = L.T("dialog.closeName"), AutoSize = true, Padding = new Padding(10, 6, 10, 6), Anchor = AnchorStyles.Right, DialogResult = DialogResult.Cancel };
            layout.Controls.Add(body, 0, 0); layout.Controls.Add(close, 0, 1); Controls.Add(layout); CancelButton = close;
            Shown += (s, e) => { body.Focus(); body.Select(0, 0); };
            Accessibility.Order(this); Appearance.Apply(this, theme); Appearance.FollowSystem(this, theme);
        }
        protected override bool ProcessCmdKey(ref Message message, Keys keys)
        {
            if (keys == Keys.F6) { body.Focus(); body.SelectAll(); return true; }
            return base.ProcessCmdKey(ref message, keys);
        }
        protected override void OnSystemColorsChanged(EventArgs e)
        { base.OnSystemColorsChanged(e); if (theme != null) Appearance.Apply(this, theme); }
    }
}
