using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Radeoner
{
    // Keep the standard Button accessibility/keyboard behavior. Only its disabled
    // dark-state painting needs help because Framework uses system-light colors.
    internal sealed class ThemedButton : Button
    {
        internal bool DarkTheme;
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Enabled || !DarkTheme || Radeoner.Appearance.HighContrast) { base.OnPaint(e); return; }
            e.Graphics.Clear(BackColor);
            using (var pen = new Pen(Color.FromArgb(110, 110, 115))) e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Color.FromArgb(165, 165, 170),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.HidePrefix);
        }
    }
    internal sealed class ThemedCheckBox : CheckBox
    {
        internal bool DarkTheme;
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Enabled || !DarkTheme || Radeoner.Appearance.HighContrast) { base.OnPaint(e); return; }
            e.Graphics.Clear(BackColor);
            var glyph = new Rectangle(1, Math.Max(1, (Height - 13) / 2), 12, 12);
            using (var pen = new Pen(Color.FromArgb(165, 165, 170))) e.Graphics.DrawRectangle(pen, glyph);
            if (Checked) TextRenderer.DrawText(e.Graphics, "✓", Font, glyph, Color.FromArgb(165, 165, 170), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(18, 0, Math.Max(0, Width - 18), Height), Color.FromArgb(165, 165, 170), TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.HidePrefix);
        }
    }
    internal static class Appearance
    {
        internal static Func<bool> TestSystemDark;
        internal static Func<bool> TestHighContrast;
        internal static bool HighContrast { get { return TestHighContrast != null ? TestHighContrast() : SystemInformation.HighContrast; } }
        internal static bool SystemDark()
        {
            if (TestSystemDark != null) return TestSystemDark();
            try { using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")) return key != null && Convert.ToInt32(key.GetValue("AppsUseLightTheme", 1)) == 0; }
            catch { return false; }
        }
        internal static bool Dark(string theme) { return !HighContrast && (theme == "dark" || theme == "system" && SystemDark()); }
        internal static void FollowSystem(Form form, string theme)
        {
            UserPreferenceChangedEventHandler changed = (sender, e) =>
            {
                if (form.IsDisposed || form.Disposing || !form.IsHandleCreated) return;
                try { form.BeginInvoke((Action)(() => { if (!form.IsDisposed && !form.Disposing) Apply(form, theme); })); }
                catch (InvalidOperationException) { }
            };
            SystemEvents.UserPreferenceChanged += changed;
            form.Disposed += (sender, e) => SystemEvents.UserPreferenceChanged -= changed;
        }
        internal static void Apply(Control root, string theme)
        {
            if (root.IsDisposed || root.Disposing) return;
            bool dark = Dark(theme), system = theme == "system" && !dark || HighContrast;
            Color background = system ? SystemColors.Control : dark ? Color.FromArgb(30, 30, 32) : Color.FromArgb(248, 248, 248);
            Color field = system ? SystemColors.Window : dark ? Color.FromArgb(43, 43, 46) : Color.White;
            Color foreground = system ? SystemColors.ControlText : dark ? Color.FromArgb(242, 242, 242) : Color.FromArgb(24, 24, 24);
            root.SuspendLayout();
            try { ApplyRecursive(root, dark, background, field, foreground, system); }
            finally { root.ResumeLayout(true); }
        }
        private static void ApplyRecursive(Control control, bool dark, Color background, Color field, Color text, bool system)
        {
            if (control.IsDisposed || control.Disposing) return;
            control.BackColor = control is TextBoxBase || control is NumericUpDown || control is ComboBox || control is ListView ? field : background;
            control.ForeColor = text;
            var button = control as Button;
            if (button != null)
            {
                button.UseVisualStyleBackColor = system;
                button.FlatStyle = system ? FlatStyle.Standard : FlatStyle.Flat;
                button.FlatAppearance.BorderColor = dark ? Color.FromArgb(130, 130, 135) : Color.FromArgb(110, 110, 110);
                button.FlatAppearance.MouseOverBackColor = field;
                button.FlatAppearance.MouseDownBackColor = dark ? Color.FromArgb(65, 65, 70) : Color.FromArgb(225, 225, 230);
            }
            var themed = control as ThemedButton;
            if (themed != null) themed.DarkTheme = dark;
            var checkbox = control as CheckBox;
            if (checkbox != null) { checkbox.UseVisualStyleBackColor = system; checkbox.FlatStyle = system ? FlatStyle.Standard : FlatStyle.Flat; }
            var themedCheck = control as ThemedCheckBox;
            if (themedCheck != null) themedCheck.DarkTheme = dark;
            var combo = control as ComboBox;
            if (combo != null) combo.FlatStyle = system ? FlatStyle.Standard : FlatStyle.Flat;
            var page = control as TabPage;
            if (page != null) page.UseVisualStyleBackColor = system;
            var tabs = control as TabControl;
            if (tabs != null) tabs.DrawMode = system ? TabDrawMode.Normal : TabDrawMode.OwnerDrawFixed;
            foreach (Control child in control.Controls) ApplyRecursive(child, dark, background, field, text, system);
            control.Invalidate();
        }
        internal static void DrawTab(TabControl tabs, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= tabs.TabPages.Count) return;
            bool selected = e.Index == tabs.SelectedIndex;
            Color background = tabs.TabPages[e.Index].BackColor;
            bool dark = background.GetBrightness() < 0.5;
            Color selectedColor = dark ? Color.FromArgb(55, 55, 60) : Color.White;
            using (var brush = new SolidBrush(selected ? selectedColor : background)) e.Graphics.FillRectangle(brush, e.Bounds);
            TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, tabs.Font, e.Bounds, tabs.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            if (selected)
            {
                using (var brush = new SolidBrush(dark ? Color.FromArgb(255, 143, 155) : Color.FromArgb(160, 24, 46))) e.Graphics.FillRectangle(brush, e.Bounds.Left + 2, e.Bounds.Bottom - 3, Math.Max(0, e.Bounds.Width - 4), 3);
                if (tabs.Focused) { var focus = e.Bounds; focus.Inflate(-4, -4); ControlPaint.DrawFocusRectangle(e.Graphics, focus, tabs.ForeColor, selectedColor); }
            }
        }
    }
}
