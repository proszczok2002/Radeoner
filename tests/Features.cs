using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Radeoner;

internal static partial class Tests
{
    private static string TestFile(string name)
    {
        var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "feature-tests");
        Directory.CreateDirectory(folder); return Path.Combine(folder, name);
    }
    private static T Field<T>(MainForm form, string name) { return (T)typeof(MainForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form); }
    private static void SetField(MainForm form, string name, object value) { typeof(MainForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(form, value); }
    private static object Call(MainForm form, string method, params object[] args) { return typeof(MainForm).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, args); }
    private static MainForm TestForm(string name)
    {
        string file = TestFile(name + ".settings.json"); if (File.Exists(file)) File.Delete(file);
        return new MainForm(false, file, new StartupRegistration(Application.ExecutablePath, @"Software\Radeoner\Tests", "Unused")) { SuppressAutomaticUpdateChecks = true };
    }
    private static void RunFeatureTests()
    {
        Test("JSON profiles roundtrip complete scalar, fan and Zero RPM values without writes", () => WithDriver((driver, backend) =>
        {
            var s = backend.Read(0); var p = Profiles.Capture(s, new Changes()); p.GpuName = "Radeon Łódź ąęść";
            string file = TestFile("roundtrip.json"); Profiles.Save(file, p); var loaded = Profiles.Load(file);
            Check(loaded.GpuName == p.GpuName && loaded.Settings.Count == 7 && loaded.Fan.Count == 2 && loaded.ZeroRpm == true, "Incomplete JSON roundtrip");
            Check(Profiles.Prepare(s, loaded).Empty && driver.Writes == 0, "Loading a profile must not write GPU settings");
            Check(File.ReadAllText(file).Contains("\n  "), "Profile JSON should be readable and indented");
            // Simulate a later session with different settings and restore the complete backup.
            driver.Values["power"] = -5; driver.Values["gfx.voltage"] = 950; driver.ZeroRpm = false; driver.Fan[0].Speed = 30;
            var now = backend.Read(0); var restore = Profiles.Prepare(now, loaded);
            Check(restore.Values.Count == 2 && restore.Fan != null && restore.ZeroRpm == true && driver.Writes == 0, "Restore was not staged");
            TuningTransaction.Apply(backend, now, restore);
            Check(driver.Values["power"] == 0 && driver.Values["gfx.voltage"] == 1000 && driver.ZeroRpm && driver.Fan[0].Speed == 20, "Profile restore failed through the existing verified write path");
        }));
        Test("Export includes unapplied editor values while preserving hardware state", () => WithDriver((driver, backend) =>
        {
            var s = backend.Read(0); var p = Profiles.Capture(s, Change("power", -5, "gfx.voltage", 950));
            var staged = Profiles.Prepare(s, p);
            Check(staged.Values["power"] == -5 && staged.Values["gfx.voltage"] == 950, "Unapplied values missing");
            Check(driver.Values["power"] == 0 && driver.Values["gfx.voltage"] == 1000 && driver.Writes == 0, "Export changed hardware");
        }));
        Test("Profile identity rejects other devices and includes subsystem but ignores instance location", () =>
        {
            var a = new GpuInfo { Id = @"PCI\VEN_1002&DEV_150E&SUBSYS_3A191043&REV_E3\old-instance" };
            var b = new GpuInfo { Id = @"PCI\VEN_1002&DEV_150E&SUBSYS_3A191043&REV_E3\new-instance" };
            var c = new GpuInfo { Id = @"PCI\VEN_1002&DEV_150E&SUBSYS_00001043&REV_E3\new-instance" };
            Check(Profiles.HardwareKey(a) == Profiles.HardwareKey(b) && Profiles.HardwareKey(a) != Profiles.HardwareKey(c), "Hardware identity is unstable or too permissive");
            WithDriver((driver, backend) =>
            {
                var s = backend.Read(0); var p = Profiles.Capture(s, new Changes()); p.HardwareId = "other-device";
                Expect(() => Profiles.Prepare(s, p), "innej karty"); Check(driver.Writes == 0, "Wrong GPU profile reached a setter");
            });
        });
        Test("Profile validation rejects unsupported, duplicate, changed-unit and changed-range settings", () => WithDriver((driver, backend) =>
        {
            var s = backend.Read(0); var p = Profiles.Capture(s, new Changes()); p.Settings.Add(p.Settings[0]);
            Expect(() => Profiles.Prepare(s, p), "Nieprawidłowy");
            p = Profiles.Capture(s, new Changes()); p.Settings[0].Unit = "W"; Expect(() => Profiles.Prepare(s, p), "jednostka");
            p = Profiles.Capture(s, new Changes()); p.Settings[0].Range.Min -= 10; Expect(() => Profiles.Prepare(s, p), "zakres");
            p = Profiles.Capture(s, new Changes()); p.Settings[0].Value = int.MaxValue; Expect(() => Profiles.Prepare(s, p), "spoza zakresu");
            p = Profiles.Capture(s, new Changes()); p.Settings[0].Id = "unknown"; Expect(() => Profiles.Prepare(s, p), "niedostępne");
            p = Profiles.Capture(s, new Changes()); p.Version = 999; Expect(() => Profiles.Prepare(s, p), "nieobsługiwana");
            Check(driver.Writes == 0, "Invalid profile changed hardware");
        }));
        Test("Malformed and oversized JSON, missing fields, fan count and curve ordering are rejected", () => WithDriver((driver, backend) =>
        {
            string file = TestFile("invalid.json"); File.WriteAllText(file, "{\"Version\":1}"); Expect(() => Profiles.Load(file), "Nieprawidłowy");
            File.WriteAllText(file, "{"); Expect(() => Profiles.Load(file), "Nieprawidłowy");
            File.WriteAllText(file, new string(' ', 1024 * 1024 + 1)); Expect(() => Profiles.Load(file), "zbyt duży");
            var s = backend.Read(0); var p = Profiles.Capture(s, new Changes()); p.Fan.RemoveAt(0); Expect(() => Profiles.Prepare(s, p), "Liczba punktów");
            p = Profiles.Capture(s, new Changes()); p.Fan[1].Speed = 0; Expect(() => Profiles.Prepare(s, p), "nie mogą maleć");
            Check(driver.Writes == 0, "Invalid fan profile changed hardware");
        }));
        Test("Personalization settings persist and recover from invalid files", () =>
        {
            string file = TestFile("preferences.json");
            new AppSettings { Language = "en", Theme = "dark", TextScale = 175, RefreshSensors = false }.Save(file);
            var s = AppSettings.Load(file); Check(s.Language == "en" && s.Theme == "dark" && s.TextScale == 175 && !s.RefreshSensors, "Preferences did not persist");
            s.Theme = "light"; s.Save(file); Check(AppSettings.Load(file).Theme == "light", "Atomic replacement failed");
            File.WriteAllText(file, "{"); s = AppSettings.Load(file); Check(s.Language == "pl" && s.TextScale == 100, "Corrupt preferences prevented startup");
            new AppSettings { Language = "xx", Theme = "unknown", TextScale = int.MaxValue }.Save(file);
            s = AppSettings.Load(file); Check(s.Language == "pl" && s.Theme == "system" && s.TextScale == 100, "Invalid preference values accepted");
        });
        Test("Autostart registry enable, quoted path, tray argument and removal use an isolated test key", () =>
        {
            string key = @"Software\Radeoner\Tests\" + Guid.NewGuid().ToString("N");
            try
            {
                var registration = new StartupRegistration(@"C:\Radeoner folder\Radeoner.exe", key, "TestStartup");
                Check(!registration.Enabled, "Unexpected initial test startup"); registration.SetEnabled(true);
                Check(registration.Enabled && registration.UsesCurrentLocation && registration.Command == "\"C:\\Radeoner folder\\Radeoner.exe\" --tray", "Invalid autostart command");
                var moved = new StartupRegistration(@"C:\Other\Radeoner.exe", key, "TestStartup"); Check(moved.Enabled && !moved.UsesCurrentLocation, "Moved portable path not detected");
                registration.SetEnabled(false); Check(!registration.Enabled, "Startup removal failed");
            }
            finally { Registry.CurrentUser.DeleteSubKey(key, false); }
        });
        Test("LNG files cover all keys in both languages and fallback works without external files", () =>
        {
            var assembly = typeof(L).Assembly; Dictionary<string, string> pl, en;
            using (var reader = new StreamReader(assembly.GetManifestResourceStream("Radeoner.Languages.pl.lng"))) pl = L.Parse(reader.ReadToEnd());
            using (var reader = new StreamReader(assembly.GetManifestResourceStream("Radeoner.Languages.en.lng"))) en = L.Parse(reader.ReadToEnd());
            Check(pl.Keys.OrderBy(x => x).SequenceEqual(en.Keys.OrderBy(x => x)), "Language keys differ");
            foreach (string key in pl.Keys)
            { string.Format(pl[key], Enumerable.Repeat<object>(1, 8).ToArray()); string.Format(en[key], Enumerable.Repeat<object>(1, 8).ToArray()); }
            string folder = TestFile("external-language"); Directory.CreateDirectory(folder); File.WriteAllText(Path.Combine(folder, "en.lng"), "ui.reportTab=Custom report\n");
            L.Load("en", folder); Check(L.T("ui.reportTab") == "Custom report" && L.T("ui.personalTab") == "Personalization", "External language overrides/fallback failed");
            L.Load("en", "no-such-folder"); Check(L.T("ui.reportTab") == "Report", "Embedded fallback failed"); L.Load("pl");
        });
        Test("Switching language refreshes cached backend descriptions without tuning writes", () => WithDriver((driver, backend) =>
        {
            L.Load("pl"); var s = backend.Read(0); Check(s.Settings[0].Name == "Minimalny zegar GPU", "Polish name missing");
            L.Load("en"); var english = backend.Read(0);
            Check(english.Settings[0].Name == "Minimum GPU clock" && english.Report().Contains("Driver:") && english.Metrics.Any(x => x.StartsWith("GPU temperature:")), "Backend report is partially untranslated");
            Check(!english.Report().Contains("ustawienia") && !english.Report().Contains("niedostęp") && driver.Writes == 0, "Language change wrote hardware or retained Polish text"); L.Load("pl");
        }));
        Test("Tray tooltip uses actual readings, missing values, locale and bounded full GPU text", () =>
        {
            var s = new Snapshot { Gpu = new GpuInfo { Name = "AMD Radeon RX 9070 XT" } };
            s.MetricValues["temperature"] = 40; s.MetricValues["fan"] = 333; s.MetricValues["power"] = 2;
            string text = TrayText.Format(s, false); Check(text.Contains("temperatura: 40 °C") && text.Contains("333 RPM") && text.Contains("moc: 2 W") && text.Length <= 127, "Tray measurements incorrect");
            s.MetricValues.Remove("fan"); Check(!TrayText.Format(s, false).Contains("wentylator"), "Unsupported fan is announced");
            s.Gpu.Name = new string('G', 300); text = TrayText.Format(s, true); Check(text.Length <= 127 && text.Contains("odczyt") && text.Contains("2 W"), "Long GPU name displaced readings or stale timestamp");
            L.Load("en"); Check(TrayText.Format(s, false).Contains("temperature"), "Tray not translated"); L.Load("pl");
        });
        Test("Profile staging, theme, text size and live language preserve unapplied edits", () => WithDriver((driver, backend) =>
        {
            using (var form = TestForm("ui-preferences"))
            {
                Exception uiError = null; form.TestFailure = ex => uiError = ex;
                SetField(form, "snapshot", backend.Read(0)); Call(form, "Render");
                var changes = new Changes { Fan = new List<FanPoint> { new FanPoint(55, 30), new FanPoint(85, 90) }, ZeroRpm = false }; changes.Values.Add("power", -5); form.Stage(changes);
                Check(driver.Writes == 0 && Field<bool>(form, "dirty"), "Staging applied settings or lost draft status");
                Field<ComboBox>(form, "theme").SelectedIndex = 2; Field<ComboBox>(form, "textScale").SelectedIndex = 2;
                Check(form.Font.Size > SystemFonts.MessageBoxFont.Size * 1.45, "Text scaling not applied");
                if (!SystemInformation.HighContrast) Check(form.BackColor.GetBrightness() < 0.2, "Dark theme not applied");
                Field<ComboBox>(form, "language").SelectedIndex = 1;
                if (uiError != null) throw new Exception("Switch to English failed", uiError);
                var collected = (Changes)Call(form, "Collect");
                Check(collected.Values["power"] == -5 && collected.Fan[0].Temperature == 55 && collected.ZeroRpm == false && Field<bool>(form, "dirty"), "Personalization discarded edits");
                Check(Field<TabControl>(form, "tabs").TabPages[3].Text == "Report" && Field<Button>(form, "apply").AccessibleName.Contains("Apply"), "Language switch failed");
                Field<ComboBox>(form, "language").SelectedIndex = 0;
                if (uiError != null) throw new Exception("Switch to Polish failed", uiError);
                Check(Field<TabControl>(form, "tabs").TabPages[3].Text == "Raport", "Wrong tab caption after switch to Polish: " + Field<TabControl>(form, "tabs").TabPages[3].Text + "; language " + L.Language);
                Check(driver.Writes == 0, "Personalization wrote GPU settings");
            }
        }));
    }
}
