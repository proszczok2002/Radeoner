using System;
using System.Drawing;
using System.Windows.Forms;

namespace Radeoner
{
    // Keep the standard Button accessibility/keyboard behavior. Only its disabled
    // dark-state painting needs help because Framework uses system-light colors.
    internal sealed class ThemedButton : Button
    {
        internal bool DarkTheme;
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Enabled || !DarkTheme || SystemInformation.HighContrast) { base.OnPaint(e); return; }
            e.Graphics.Clear(BackColor);
            using (var pen = new Pen(Color.FromArgb(110, 110, 115))) e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Color.FromArgb(165, 165, 170),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.HidePrefix);
        }
    }
    internal sealed class ThemedCheckBox : CheckBox
    {
        internal bool DarkTheme;
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Enabled || !DarkTheme || SystemInformation.HighContrast) { base.OnPaint(e); return; }
            e.Graphics.Clear(BackColor);
            var glyph = new Rectangle(1, Math.Max(1, (Height - 13) / 2), 12, 12);
            using (var pen = new Pen(Color.FromArgb(165, 165, 170))) e.Graphics.DrawRectangle(pen, glyph);
            if (Checked) TextRenderer.DrawText(e.Graphics, "✓", Font, glyph, Color.FromArgb(165, 165, 170), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(18, 0, Math.Max(0, Width - 18), Height), Color.FromArgb(165, 165, 170), TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.HidePrefix);
        }
    }
    internal static class Appearance
    {
        internal static bool Dark(string theme) { return theme == "dark" && !SystemInformation.HighContrast; }
        internal static void Apply(Control root, string theme)
        {
            bool dark = Dark(theme), system = theme == "system" || SystemInformation.HighContrast;
            Color background = system ? SystemColors.Control : dark ? Color.FromArgb(30, 30, 32) : Color.FromArgb(248, 248, 248);
            Color field = system ? SystemColors.Window : dark ? Color.FromArgb(43, 43, 46) : Color.White;
            Color foreground = system ? SystemColors.ControlText : dark ? Color.FromArgb(242, 242, 242) : Color.FromArgb(24, 24, 24);
            ApplyRecursive(root, theme, background, field, foreground, system);
        }
        private static void ApplyRecursive(Control control, string theme, Color background, Color field, Color text, bool system)
        {
            control.BackColor = control is TextBoxBase || control is NumericUpDown || control is ComboBox ? field : background;
            control.ForeColor = text;
            var button = control as Button;
            if (button != null)
            {
                button.UseVisualStyleBackColor = system;
                button.FlatStyle = system ? FlatStyle.Standard : FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Dark(theme) ? Color.FromArgb(130, 130, 135) : Color.FromArgb(110, 110, 110);
            }
            var themed = control as ThemedButton;
            if (themed != null) themed.DarkTheme = Dark(theme);
            var checkbox = control as CheckBox;
            if (checkbox != null) { checkbox.UseVisualStyleBackColor = system; checkbox.FlatStyle = system ? FlatStyle.Standard : FlatStyle.Flat; }
            var themedCheck = control as ThemedCheckBox;
            if (themedCheck != null) themedCheck.DarkTheme = Dark(theme);
            var page = control as TabPage;
            if (page != null) page.UseVisualStyleBackColor = system;
            var tabs = control as TabControl;
            if (tabs != null) tabs.DrawMode = system ? TabDrawMode.Normal : TabDrawMode.OwnerDrawFixed;
            foreach (Control child in control.Controls) ApplyRecursive(child, theme, background, field, text, system);
            control.Invalidate();
        }
        internal static void DrawTab(TabControl tabs, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= tabs.TabPages.Count) return;
            bool selected = e.Index == tabs.SelectedIndex;
            Color background = tabs.TabPages[e.Index].BackColor;
            using (var brush = new SolidBrush(selected ? ControlPaint.Light(background, 0.1f) : background)) e.Graphics.FillRectangle(brush, e.Bounds);
            TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, tabs.Font, e.Bounds, tabs.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            if (selected && tabs.Focused) ControlPaint.DrawFocusRectangle(e.Graphics, e.Bounds, tabs.ForeColor, tabs.BackColor);
        }
    }
}
