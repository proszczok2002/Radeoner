using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Radeoner;

internal static partial class Tests
{
    private static int passed;
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Test(string name, Action body) { body(); passed++; Console.WriteLine("PASS " + name); }
    private static void Expect(Action body, string fragment)
    {
        try { body(); } catch (InvalidOperationException ex) { Check(ex.Message.Contains(fragment), "Wrong error: " + ex.Message); return; }
        throw new Exception("Expected an error containing " + fragment);
    }
    private static void WithDriver(Action<FakeDriver, AmdBackend> body, bool unsupported = false, bool legacy = false)
    {
        using (var driver = new FakeDriver { Unsupported = unsupported, Legacy = legacy })
        {
            using (var backend = new AmdBackend(driver.System.Pointer)) body(driver, backend);
            driver.CheckReleased();
        }
    }
    private static Changes Change(params object[] pairs)
    {
        var result = new Changes();
        for (int i = 0; i < pairs.Length; i += 2) result.Values.Add((string)pairs[i], (int)pairs[i + 1]);
        return result;
    }
    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls) { yield return child; foreach (Control d in Descendants(child)) yield return d; }
    }
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 2 && (args[0] == "--live-ui" || args[0] == "--live-tray" || args[0] == "--live-keyboard")) return LiveUi(args[1], args[0] == "--live-tray", args[0] == "--live-keyboard");
        if (args.Length == 2 && args[0] == "--live-dialogs") return LiveDialogs(args[1]);
        if (args.Length == 2 && args[0] == "--driver-fixture") return DriverHost.Run(int.Parse(args[1]), () => new RpcFixture());
        try
        {
            if (args.Length == 2 && args[0] == "--download-openrgb")
            {
                OpenRgbDownload.DownloadAsync(args[1], null, System.Threading.CancellationToken.None).GetAwaiter().GetResult();
                Console.WriteLine("PASS official OpenRGB portable download: " + args[1]); return 0;
            }
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            Test("ADLX integer range ABI, inclusive bounds and step", () =>
            {
                Check(Marshal.SizeOf(typeof(IntRange)) == 12, "Range must be three int32 fields");
                var range = new IntRange(-20, 20, 5);
                Check(range.Contains(-20) && range.Contains(20) && !range.Contains(1) && !range.Contains(21), "Step validation failed");
                Check(!new IntRange(0, 100, 0).Contains(10), "Zero step must not enable editing");
                Check(new IntRange(int.MinValue, int.MaxValue, 1).Contains(int.MaxValue), "Range arithmetic overflowed");
            });
            Test("Native discovery, typed list, sensor types, missing sensor and no writes", () => WithDriver((driver, backend) =>
            {
                Check(backend.Gpus.Count == 1 && backend.Gpus[0].Type == 2, "Discrete GPU detection failed");
                var s = backend.Read(0);
                Check(s.Settings.Count == 7 && s.Fan.Count == 2 && s.ZeroRpm == true && s.AtFactory == true, "Capabilities failed");
                Check(s.Metrics.Any(x => x.StartsWith("Temperatura GPU: 42")), "double sensor marshaling failed");
                Check(s.Metrics.Contains("Zegar GPU: 800 MHz") && s.Metrics.Contains("Temperatura hotspot: niedostępne"), "int or support marshaling failed");
                Check(driver.Writes == 0, "Discovery/read must never change tuning");
            }));
            Test("Unsupported integrated GPU remains read-only", () => WithDriver((driver, backend) =>
            {
                var s = backend.Read(0);
                Check(s.Gpu.Type == 1 && s.Settings.Count == 0 && s.Fan.Count == 0 && !s.CanReset, "Unsupported features enabled");
                Expect(() => TuningTransaction.Apply(backend, s, Change("power", 1)), "niedostępne");
                Check(driver.Writes == 0, "Unsupported GPU was written");
            }, true));
            Test("Out-of-range, step and inverted clocks rejected before native writes", () => WithDriver((driver, backend) =>
            {
                var s = backend.Read(0);
                Expect(() => TuningTransaction.Apply(backend, s, Change("gfx.voltage", 1300)), "wpisz wartość");
                Expect(() => TuningTransaction.Apply(backend, s, Change("gfx.min", 1001)), "wpisz wartość");
                Expect(() => TuningTransaction.Apply(backend, s, Change("gfx.min", 2100)), "Minimalny zegar");
                Check(driver.Writes == 0, "Invalid values reached driver");
            }));
            Test("Scalar native setters, readback and undo", () => WithDriver((driver, backend) =>
            {
                var s = backend.Read(0);
                var result = TuningTransaction.Apply(backend, s, Change("power", -5, "gfx.min", 900, "gfx.max", 1900, "gfx.voltage", 950, "vram.max", 1400, "fan.min", 1100, "fan.target", 2100));
                Check(driver.Writes == 7 && driver.Values["gfx.voltage"] == 950 && result.After.AtFactory == false, "Setters did not reach driver");
                var undone = TuningTransaction.Apply(backend, result.After, result.Undo);
                Check(driver.Values["power"] == 0 && driver.Values["gfx.voltage"] == 1000 && driver.Values["vram.max"] == 1500, "Undo did not restore originals");
            }));
            Test("Clock boundaries expand and shrink in a valid order; rollback respects bounds", () => WithDriver((driver, backend) =>
            {
                var s = backend.Read(0);
                var expanded = TuningTransaction.Apply(backend, s, Change("gfx.min", 2100, "gfx.max", 2200));
                Check(driver.Values["gfx.min"] == 2100 && driver.Values["gfx.max"] == 2200, "Maximum must expand before minimum");
                var shrunk = TuningTransaction.Apply(backend, expanded.After, Change("gfx.min", 700, "gfx.max", 800));
                Check(driver.Values["gfx.min"] == 700 && driver.Values["gfx.max"] == 800, "Minimum must shrink before maximum");
                driver.PartialFailOnceId = "gfx.voltage";
                Expect(() => TuningTransaction.Apply(backend, shrunk.After, Change("gfx.min", 1500, "gfx.max", 2000, "gfx.voltage", 950)), "odtworzone i potwierdzone");
                Check(driver.Values["gfx.min"] == 700 && driver.Values["gfx.max"] == 800, "Clock rollback crossed boundaries");
            }));
            Test("Offset maximum frequency is not compared to absolute minimum", () =>
            {
                var s = new Snapshot();
                s.Settings.Add(new Setting { Id = "gfx.min", Name = "min", Range = new IntRange(500, 3000, 10), Value = 1000 });
                s.Settings.Add(new Setting { Id = "gfx.max", Name = "offset", Range = new IntRange(-500, 500, 10), Value = 0 });
                Validation.Validate(s, Change("gfx.min", 1500, "gfx.max", -100));
            });
            Test("Partial write failure restores already applied AND failing setting", () => WithDriver((driver, backend) =>
            {
                var s = backend.Read(0); driver.PartialFailOnceId = "gfx.voltage";
                Expect(() => TuningTransaction.Apply(backend, s, Change("power", -5, "gfx.voltage", 950)), "odtworzone i potwierdzone");
                Check(driver.Values["power"] == 0 && driver.Values["gfx.voltage"] == 1000, "Rollback missed partial write");
            }));
            Test("Ignored driver write fails readback and restores values", () => WithDriver((driver, backend) =>
            {
                var s = backend.Read(0); driver.IgnoreOnceId = "gfx.voltage";
                Expect(() => TuningTransaction.Apply(backend, s, Change("power", -5, "gfx.voltage", 950)), "nie potwierdził");
                Check(driver.Values["power"] == 0 && driver.Values["gfx.voltage"] == 1000, "Rollback after ignored write failed");
            }));
            Test("Rollback rejection is reported as unconfirmed", () => WithDriver((driver, backend) =>
            {
                var s = backend.Read(0); driver.RejectWrites = true;
                Expect(() => TuningTransaction.Apply(backend, s, Change("power", -5)), "Nie udało się potwierdzić pełnego");
            }));
            Test("External changes rejected without overwriting driver values", () => WithDriver((driver, backend) =>
            {
                var s = backend.Read(0); driver.Values["power"] = -1;
                Expect(() => TuningTransaction.Apply(backend, s, Change("power", -5)), "poza programem");
                Check(driver.Values["power"] == -1 && driver.Writes == 0, "Stale state overwrote external change");
            }));
            Test("Unrestorable original values outside driver step remain unavailable", () => WithDriver((driver, backend) =>
            {
                driver.Values["gfx.voltage"] = 951;
                var s = backend.Read(0);
                Check(!s.Settings.Any(x => x.Id == "gfx.voltage"), "A value that cannot be restored must not be editable");
                Expect(() => TuningTransaction.Apply(backend, s, Change("gfx.voltage", 950)), "niedostępne");
                Check(driver.Writes == 0, "Unrestorable value was overwritten");
            }));
            Test("Fan curve native copy, validation, Zero RPM and undo", () => WithDriver((driver, backend) =>
            {
                var s = backend.Read(0);
                var invalid = new Changes { Fan = new List<FanPoint> { new FanPoint(80, 50), new FanPoint(70, 40) } };
                Expect(() => TuningTransaction.Apply(backend, s, invalid), "muszą rosnąć");
                Check(driver.Writes == 0, "Invalid fan curve reached the hardware setter");
                var changes = new Changes { Fan = new List<FanPoint> { new FanPoint(55, 30), new FanPoint(85, 90) }, ZeroRpm = false };
                var result = TuningTransaction.Apply(backend, s, changes);
                Check(driver.Fan[0].Temperature == 55 && driver.Fan[1].Speed == 90 && !driver.ZeroRpm && driver.Writes == 2, "Fan settings not committed");
                TuningTransaction.Apply(backend, result.After, result.Undo);
                Check(driver.Fan[0].Temperature == 50 && driver.Fan[1].Speed == 80 && driver.ZeroRpm, "Fan undo failed");
            }));
            Test("Legacy GPU and VRAM tables read, edit atomically, verify and undo", () => WithDriver((driver, backend) =>
            {
                var s = backend.Read(0);
                Check(s.Settings.Any(x => x.Id == "gfx.state.1.voltage") && s.Settings.Any(x => x.Id == "vram.state.1.frequency"), "Legacy tables missing");
                Check(!s.Settings.Any(x => x.Id == "gfx.min"), "New interface incorrectly used on legacy card");
                Check(driver.Writes == 0, "Legacy discovery changed state");
                var result = TuningTransaction.Apply(backend, s, Change("gfx.state.1.frequency", 1300, "gfx.state.1.voltage", 950, "vram.state.1.frequency", 1500));
                Check(driver.Writes == 2 && driver.GfxStates[1][0] == 1300 && driver.GfxStates[1][1] == 950 && driver.VramStates[1][0] == 1500, "Legacy writes not grouped by table");
                TuningTransaction.Apply(backend, result.After, result.Undo);
                Check(driver.GfxStates[1][0] == 1200 && driver.GfxStates[1][1] == 1000 && driver.VramStates[1][0] == 1400, "Legacy undo failed");
            }, false, true));
            Test("Factory reset calls explicit native action only", () => WithDriver((driver, backend) =>
            {
                backend.Read(0); Check(driver.Writes == 0, "Unexpected automatic reset");
                driver.Factory = false; backend.Reset(0);
                Check(driver.Writes == 1 && backend.Read(0).AtFactory == true, "Factory action/readback failed");
            }));
            Test("WinForms control names, text reports, keyboard fields and no writes", () => WithDriver((driver, backend) =>
            {
                Application.EnableVisualStyles();
                using (var form = new MainForm())
                {
                    typeof(MainForm).GetField("snapshot", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(form, backend.Read(0));
                    typeof(MainForm).GetMethod("Render", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, null);
                    var controls = Descendants(form).ToList();
                    foreach (Control c in controls.Where(x => x is NumericUpDown || x is Button || x is ComboBox || x is CheckBox || x is TextBox && ((TextBox)x).ReadOnly))
                    {
                        Check(!string.IsNullOrWhiteSpace(c.AccessibleName), "Unlabeled accessible control: " + c.GetType().Name);
                        Check(c.TabStop, "Interactive/report control skipped by Tab: " + c.AccessibleName);
                    }
                    var numbers = controls.OfType<NumericUpDown>().ToList();
                    Check(numbers.Count == 12 && numbers.All(n => !string.IsNullOrWhiteSpace(n.AccessibleDescription)), "Numeric fields or accessible ranges missing");
                    Check(controls.OfType<ListView>().Any(t => t.Items.Cast<ListViewItem>().Any(i => i.Text.Contains("Temperatura GPU"))), "Readable sensor rows missing");
                    var automatic = controls.OfType<CheckBox>().Single(x => x.AccessibleName == L.T("ui.monitoringName"));
                    Check(automatic.Checked && driver.Writes == 0, "Tray monitoring must be available without tuning writes");
                    numbers[0].Text = "900";
                    Check(controls.OfType<Button>().Single(x => x.AccessibleName.StartsWith("Zastosuj")).Enabled, "Typing did not enable Apply");
                }
            }));
            RunFeatureTests();
            RunUpdateTests();
            RunAccessibilityTests();
            RunStabilityTests();
            RunRgbTests();
            RunRgbDownloadTests();
            Console.WriteLine("Passed: " + passed + ". No real driver was loaded; no real tuning settings were changed.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static int LiveUi(string output, bool startInTray, bool keyboard = false)
    {
        // Run the real message loop and read-only initialization off screen. This is
        // a UI startup check, not a screen-reader listening session.
        string result = null;
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        using (var form = new MainForm(startInTray, TestFile("live.settings.json"), new StartupRegistration(Application.ExecutablePath, @"Software\Radeoner\Tests", "Unused")) { SuppressAutomaticUpdateChecks = true })
        using (var timer = new System.Windows.Forms.Timer { Interval = 100 })
        {
            form.ShowInTaskbar = keyboard; form.Opacity = keyboard ? 1 : 0; form.StartPosition = FormStartPosition.Manual; form.Location = keyboard ? new Point(100, 100) : new Point(-20000, -20000);
            var watch = Stopwatch.StartNew();
            timer.Tick += async (sender, e) =>
            {
                bool busy = (bool)typeof(MainForm).GetField("busy", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
                var snapshot = (Snapshot)typeof(MainForm).GetField("snapshot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
                if (watch.ElapsedMilliseconds < 30000 && (busy || snapshot == null)) return;
                timer.Stop();
                try
                {
                    Check(!busy && snapshot != null, "Real UI initialization did not finish");
                    var controls = Descendants(form).ToList();
                    var selected = controls.OfType<ComboBox>().Single(x => x.AccessibleName == L.T("ui.gpu"));
                    Check(selected.SelectedItem != null && selected.AccessibilityObject.Name == "Karta AMD", "GPU selection inaccessible");
                    var text = controls.OfType<SensorView>().Single();
                    Check(text.Text.Contains(snapshot.Gpu.Name) && text.AccessibilityObject.Role == AccessibleRole.List && text.Items.Count > 4, "Native sensor list missing from MSAA");
                    var actions = controls.OfType<Button>().ToList();
                    Check(actions.All(x => !string.IsNullOrWhiteSpace(x.AccessibilityObject.Name)), "Button MSAA name missing");
                    Check(actions.Single(x => x.AccessibleName.StartsWith("Zastosuj")).Enabled == false, "Apply enabled without edits");
                    var tray = Field<TrayIcon>(form, "tray"); Check(tray != null && tray.Visible, "Shell tray icon missing");
                    if (startInTray) Check(!form.Visible && !form.ShowInTaskbar, "Startup did not stay in tray");
                    form.Close(); Check(!form.IsDisposed && !form.Visible && !form.ShowInTaskbar, "Close did not hide the window");
                    await Task.Delay(3600);
                    var sensors = Field<Snapshot>(form, "sensors"); Check(sensors.CapturedAt > snapshot.CapturedAt, "Background tray readings did not update");
                    Call(form, "ShowFromTray"); Check(form.Visible && form.ShowInTaskbar && !form.IsDisposed, "Tray restore failed"); form.Opacity = 0;
                    var tabControl = Field<TabControl>(form, "tabs"); tabControl.SelectedIndex = 3;
                    var reportBox = Field<TextBox>(form, "report"); reportBox.Focus(); reportBox.Select(4, 8);
                    Call(form, "SetBusy", true); Call(form, "Render"); Call(form, "SetBusy", false);
                    Check(reportBox.Focused && reportBox.SelectionStart == 4 && reportBox.SelectionLength == 8, "Read/rebuild lost report focus or selection");
                    for (int i = 0; i < 5; i++)
                    {
                        var message = Message.Create(form.Handle, 0, IntPtr.Zero, IntPtr.Zero);
                        typeof(MainForm).GetMethod("ProcessCmdKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { message, Keys.Control | (Keys)((int)Keys.D1 + i) });
                        Check(tabControl.SelectedIndex == i && tabControl.Focused, "Direct tab shortcut failed");
                    }
                    string tour = keyboard ? await VisibleKeyboardTour(form) : "";
                    result = "PASS live WinForms/MSAA, native tray icon, close-to-tray, background sensor updates, restore and explicit exit" + (startInTray ? ", startup hidden" : "") + ". " + tour + "\r\n" + sensors.Report();
                }
                catch (Exception ex) { result = "FAIL " + ex; }
                // Wait for initialization before Close so disposal cannot race the driver.
                if (!busy) await (Task)Call(form, "RequestExit");
                else Application.ExitThread();
            };
            form.Shown += (sender, e) => timer.Start();
            Application.Run(form);
        }
        File.WriteAllText(output, result ?? "FAIL no result");
        Console.WriteLine((result ?? "FAIL no result").Split('\r', '\n')[0]);
        return result != null && result.StartsWith("PASS") ? 0 : 1;
    }
}
