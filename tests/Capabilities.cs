using System;
using System.Linq;
using System.Windows.Forms;
using Radeoner;

internal static partial class Tests
{
    private static void RunCapabilityTests()
    {
        Test("A GPU without supported sensors retains diagnostic reasons and a readable polling error", () => WithDriver((driver, backend) =>
        {
            driver.NoSensors = true; var snapshot = backend.Read(0);
            Check(snapshot.Metrics.Count == 0 && snapshot.Notes.Count >= 10, "Unsupported sensor reasons were lost");
            Expect(() => backend.ReadSensors(0), L.T("driver.062"));
            Check(driver.Writes == 0, "Unsupported sensor discovery changed tuning");
        }));
        Test("Capability tabs retain supported integrated controls and rebuild safely when changing GPU", () => WithDriver((driver, backend) =>
        {
            using (var form = TestForm("capability-matrix"))
            {
                SetAuditSnapshot(form, backend.Read(0)); form.Opacity = 0; form.ShowInTaskbar = false; form.Show();
                var tabs = Field<TabControl>(form, "tabs");
                foreach (int type in new[] { 1, 2, 0 }) foreach (int mask in Enumerable.Range(0, 16))
                {
                    var snapshot = backend.Read(0); snapshot.Gpu.Type = type;
                    if ((mask & 1) == 0) snapshot.Settings.Clear();
                    if ((mask & 2) == 0) snapshot.Fan.Clear();
                    if ((mask & 4) == 0) snapshot.ZeroRpm = null;
                    snapshot.CanReset = (mask & 8) != 0;
                    SetField(form, "snapshot", snapshot); Call(form, "Render");
                    var expected = new[] { 0, 1, 2, 3, 4 }.Where(p => p != 1 || (mask & 9) != 0 || type != 1).Where(p => p != 2 || (mask & 6) != 0).ToArray();
                    Check(tabs.TabPages.Cast<TabPage>().Select(p => (int)p.Tag).SequenceEqual(expected), "Wrong capability tabs: " + type + "/" + mask);
                    Check(Field<System.Collections.Generic.Dictionary<string, NumericUpDown>>(form, "editors").Count == snapshot.Settings.Count, "Supported settings were lost");
                    foreach (int logical in expected)
                    {
                        Call(form, "SelectPage", logical); tabs.Focus(); int before = tabs.SelectedIndex;
                        bool handled = ProcessTab(tabs, false);
                        Check(handled && tabs.SelectedIndex == (before + 1) % tabs.TabCount, "Ctrl Tab failed at GPU=" + type + ", capabilities=" + mask + ", logical page=" + logical + ", before=" + before + ", after=" + tabs.SelectedIndex + ", handled=" + handled);
                        Check(ProcessTab(tabs, true) && (int)tabs.SelectedTab.Tag == logical, "Ctrl Shift Tab lost the logical page");
                    }
                    Call(form, "SelectPage", 1);
                    Check(Field<Button>(form, "rgb").Visible == (type != 1), "RGB availability does not follow GPU type");
                    Check(Field<Button>(form, "factory").Visible == snapshot.CanReset, "Factory reset hidden despite driver support");
                    Check(Field<Button>(form, "export").Enabled == ((mask & 7) != 0), "Profile actions expose unsupported settings");
                    Call(form, "SelectPage", 4);
                }
                SetField(form, "exitRequested", true); form.Close();
            }
            Check(driver.Writes == 0, "Capability discovery wrote GPU settings");
        }));
        Test("Tray omits integrated or invalid fan data in every language and retains discrete Zero RPM", () =>
        {
            foreach (string language in L.Languages)
            {
                L.Load(language);
                var snapshot = new Snapshot { Gpu = new GpuInfo { Type = 1, Name = "AMD integrated GPU" } };
                snapshot.MetricValues["temperature"] = 40; snapshot.MetricValues["power"] = 2; snapshot.MetricValues["fan"] = 333;
                Check(!TrayText.Format(snapshot, false).Contains("RPM"), "Integrated fan announced: " + language);
                snapshot.Gpu.Type = 2;
                foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, -1 })
                { snapshot.MetricValues["fan"] = invalid; Check(!TrayText.Format(snapshot, false).Contains("RPM"), "Invalid RPM announced: " + language); }
                snapshot.MetricValues["fan"] = 0; Check(TrayText.Format(snapshot, false).Contains("0 RPM"), "Valid stopped fan omitted: " + language);
                snapshot.MetricValues["fan"] = 333; Check(TrayText.Format(snapshot, false).Contains("333 RPM"), "Supported fan omitted: " + language);
                snapshot.MetricValues.Remove("fan"); Check(!TrayText.Format(snapshot, false).Contains("RPM"), "Missing fan announced: " + language);
            }
            L.Load("pl");
        });
    }
}
