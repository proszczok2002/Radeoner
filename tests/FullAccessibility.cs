using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Radeoner;
using Appearance = Radeoner.Appearance;

internal static partial class Tests
{
    private static void SetAuditSnapshot(MainForm form, Snapshot snapshot)
    {
        SetField(form, "started", true); SetField(form, "loadingSelection", true);
        var gpu = Field<ComboBox>(form, "gpu"); gpu.Items.Add(snapshot.Gpu); gpu.SelectedIndex = 0;
        SetField(form, "loadingSelection", false); SetField(form, "snapshot", snapshot); Call(form, "Render"); Call(form, "Announce", L.T("ui.ready"));
    }
    private static double Luminance(Color color)
    {
        Func<byte, double> linear = value => { double c = value / 255.0; return c <= .04045 ? c / 12.92 : Math.Pow((c + .055) / 1.055, 2.4); };
        return .2126 * linear(color.R) + .7152 * linear(color.G) + .0722 * linear(color.B);
    }
    private static double Contrast(Color first, Color second)
    { double a = Luminance(first), b = Luminance(second); return (Math.Max(a, b) + .05) / (Math.Min(a, b) + .05); }
    private static void CheckFocusVisible(Control control, string context)
    {
        Check(control.Focus() || control.ContainsFocus, context + ": cannot focus " + control.AccessibleName);
        Radeoner.Accessibility.EnsureVisible(control); Application.DoEvents();
        Rectangle bounds = control.RectangleToScreen(control.ClientRectangle);
        for (Control parent = control.Parent; parent != null; parent = parent.Parent)
        {
            var viewport = parent.RectangleToScreen(parent.ClientRectangle);
            if (!viewport.Contains(bounds))
            {
                using (var bitmap = new Bitmap(control.FindForm().Width, control.FindForm().Height)) { control.FindForm().DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size)); bitmap.Save(TestFile("audit-clipped.png")); }
                var scroll = parent as ScrollableControl;
                throw new Exception(context + ": clipped " + control.AccessibleName + " " + bounds + " outside " + viewport + " in " + parent.GetType().Name + (scroll == null ? "" : " position=" + scroll.AutoScrollPosition + " display=" + scroll.DisplayRectangle + " max=" + scroll.VerticalScroll.Maximum + " large=" + scroll.VerticalScroll.LargeChange + " value=" + scroll.VerticalScroll.Value));
            }
        }
        Check(!string.IsNullOrWhiteSpace(control.AccessibilityObject.Name), context + ": empty MSAA name");
    }
    private static void RunFullAccessibilityTests()
    {
        Test("Closing RGB during device discovery completes without touching disposed controls", () =>
        {
            using (var requested = new System.Threading.ManualResetEventSlim())
            using (var release = new System.Threading.ManualResetEventSlim())
            using (var server = new RgbServer())
            using (var rgb = new RgbDialog(SystemFonts.MessageBoxFont, "dark"))
            {
                server.ListRequested = requested; server.ListRelease = release;
                rgb.Opacity = 0; rgb.ShowInTaskbar = false; rgb.Show();
                Descendants(rgb).OfType<NumericUpDown>().Single(c => c.AccessibleName == L.T("rgb.port")).Value = server.Port;
                var task = (System.Threading.Tasks.Task)typeof(RgbDialog).GetMethod("Connect", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(rgb, null);
                var watch = System.Diagnostics.Stopwatch.StartNew();
                while (!requested.IsSet && watch.ElapsedMilliseconds < 2500) { Application.DoEvents(); System.Threading.Thread.Sleep(1); }
                Check(requested.IsSet, "RGB discovery did not reach controlled response");
                rgb.Close(); release.Set(); Pump(task);
                Check(rgb.IsDisposed && !rgb.IsHandleCreated, "Closed RGB dialog recreated a handle");
            }
        });
        Test("Invalid numeric input returns focus to its named field without writing the GPU", () => WithDriver((driver, backend) =>
        {
            using (var form = TestForm("invalid-focus"))
            {
                SetAuditSnapshot(form, backend.Read(0)); form.Opacity = 0; form.ShowInTaskbar = false; form.Show();
                var tabs = Field<TabControl>(form, "tabs"); var number = Field<System.Collections.Generic.Dictionary<string, NumericUpDown>>(form, "editors")["power"];
                tabs.SelectedIndex = 1; number.Focus(); var edit = number.Controls.OfType<TextBox>().Single(); edit.Text = "abc"; bool rejected = false;
                try { Call(form, "Collect"); } catch (System.Reflection.TargetInvocationException ex) { rejected = ex.InnerException is InvalidOperationException; }
                Check(rejected && tabs.SelectedIndex == 1 && number.ContainsFocus && edit.SelectionStart == 0 && edit.SelectionLength == edit.TextLength && driver.Writes == 0, "Invalid field did not retain selected input: rejected=" + rejected + ", tab=" + tabs.SelectedIndex + ", focus=" + number.ContainsFocus + ", text=" + number.Text);
                SetField(form, "exitRequested", true); form.Close();
            }
        }));
        Test("All tabs retain named focusable controls and visible editors at 100/200 percent in six languages and all themes", () => WithDriver((driver, backend) =>
        {
            foreach (string language in L.Languages) foreach (string theme in new[] { "light", "dark", "system" }) foreach (int scale in new[] { 100, 200 })
            {
                string path = TestFile("full-audit.settings.json"); new AppSettings { Language = language, Theme = theme, TextScale = scale, CheckUpdates = false }.Save(path);
                using (var form = new MainForm(false, path, new StartupRegistration(Application.ExecutablePath, @"Software\Radeoner\Tests", "Unused")) { SuppressAutomaticUpdateChecks = true })
                {
                    SetAuditSnapshot(form, backend.Read(0));
                    form.Opacity = 0; form.ShowInTaskbar = false; form.Size = new Size(760, 650); form.Show(); var tabs = Field<TabControl>(form, "tabs");
                    for (int tab = 0; tab < 5; tab++)
                    {
                        tabs.SelectedIndex = tab; Application.DoEvents();
                        string context = language + "/" + theme + "/" + scale + "/tab" + tab;
                        foreach (var control in Descendants(form).Where(c => c.Visible && c.Enabled && c.TabStop && (c is Button || c is ComboBox || c is CheckBox || c is NumericUpDown || c is TextBox || c is ListView)).ToList()) CheckFocusVisible(control, context);
                        Check(tabs.AccessibilityObject.Role == AccessibleRole.PageTabList, "Native tab role was lost");
                        Check(tabs.DisplayRectangle.Height >= 120, context + ": toolbars collapsed the tab content");
                        using (var image = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size)); image.Save(TestFile("audit-" + language + "-" + theme + "-" + scale + "-" + tab + ".png")); }
                    }
                    foreach (var control in Descendants(form).Where(c => c.Enabled && (c is Label || c is TextBox || c is Button || c is CheckBox || c is NumericUpDown || c is ComboBox || c is ListView)))
                        Check(Contrast(control.ForeColor, control.BackColor) >= 4.5, "Low text contrast: " + control.AccessibleName);
                    SetField(form, "exitRequested", true); form.Close();
                }
            }
            L.Load("pl"); Check(driver.Writes == 0, "Audit changed GPU tuning");
        }));
        Test("Unsupported integrated GPU omits unavailable tuning/fan pages", () => WithDriver((driver, backend) =>
        {
            using (var form = TestForm("unsupported-audit"))
            {
                SetAuditSnapshot(form, backend.Read(0));
                var tabs = Field<TabControl>(form, "tabs");
                Check(tabs.TabCount == 3 && tabs.TabPages.Cast<TabPage>().Select(p => (int)p.Tag).SequenceEqual(new[] {0, 3, 4}), "Unsupported tabs are exposed");
                Check(!Field<Button>(form, "export").Enabled && !Field<Button>(form, "rgb").Visible, "Unavailable controls are exposed");
            }
        }, true));
        Test("High contrast overrides every app theme and restores native tab/button rendering", () =>
        {
            try
            {
                Appearance.TestHighContrast = () => true;
                foreach (string theme in new[] { "system", "light", "dark" })
                    using (var form = TestForm("high-contrast"))
                    {
                        Appearance.Apply(form, theme);
                        Check(form.BackColor == SystemColors.Control && Field<TextBox>(form, "report").BackColor == SystemColors.Window, "High contrast colors overridden");
                        Check(Field<TabControl>(form, "tabs").DrawMode == TabDrawMode.Normal && Field<Button>(form, "help").FlatStyle == FlatStyle.Standard, "High contrast native rendering overridden");
                    }
            }
            finally { Appearance.TestHighContrast = null; }
        });
        Test("Details and RGB dialogs preserve names, native roles, focus visibility and text at 200 percent", () =>
        {
            foreach (string language in L.Languages) foreach (string theme in new[] { "light", "dark", "system" })
            using (var font = new Font(SystemFonts.MessageBoxFont.FontFamily, SystemFonts.MessageBoxFont.Size * 2))
            {
                L.Load(language);
                using (var details = new DetailsDialog("Audit details", "Readable complete text", font, theme))
                {
                    details.Opacity = 0; details.ShowInTaskbar = false; details.Show(); Application.DoEvents();
                    var body = Descendants(details).OfType<TextBox>().Single();
                    Check(body.ReadOnly && body.Text == "Readable complete text" && body.Font.Size == font.Size, "Details text/font lost");
                    Check(Contrast(body.ForeColor, body.BackColor) >= 4.5, "Details text contrast");
                    foreach (var control in Descendants(details).Where(c => c is TextBox || c is Button)) CheckFocusVisible(control, "details/" + language + "/" + theme);
                    using (var image = new Bitmap(details.Width, details.Height)) { details.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size)); image.Save(TestFile("audit-details-" + language + "-" + theme + ".png")); }
                    details.Close();
                }
                using (var server = new RgbServer())
                using (var rgb = new RgbDialog(font, theme))
                {
                    rgb.Size = new Size(600, 600); rgb.Opacity = 0; rgb.ShowInTaskbar = false; rgb.Show(); Application.DoEvents();
                    Descendants(rgb).OfType<NumericUpDown>().Single(c => c.AccessibleName == L.T("rgb.port")).Value = server.Port;
                    Pump((System.Threading.Tasks.Task)typeof(RgbDialog).GetMethod("Connect", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(rgb, null));
                    foreach (var control in Descendants(rgb).Where(c => c.Visible && c.Enabled && c.TabStop && (c is Button || c is NumericUpDown || c is TextBox || c is ComboBox))) CheckFocusVisible(control, "RGB/" + language + "/" + theme);
                    using (var image = new Bitmap(rgb.Width, rgb.Height)) { rgb.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size)); image.Save(TestFile("audit-rgb-" + language + "-" + theme + ".png")); }
                    rgb.Close();
                }
            }
            L.Load("pl");
        });
    }
}
