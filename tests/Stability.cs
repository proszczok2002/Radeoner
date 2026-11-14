using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Radeoner;

internal static partial class Tests
{
    private sealed class FlakySensors : IBackend, ISensorBackend
    {
        internal int Calls, FullReads;
        internal Snapshot Initial;
        public List<GpuInfo> Gpus { get { return new List<GpuInfo> { Initial.Gpu }; } }
        public string Status { get { return "synthetic sensor failure"; } }
        public Snapshot Read(int index) { FullReads++; return Initial; }
        public Snapshot ReadSensors(int index)
        {
            if (++Calls == 1) throw new IOException("synthetic transient driver failure");
            var data = new Snapshot { Gpu = Initial.Gpu }; data.Metrics.Add("Temperatura GPU: 90 °C"); data.MetricValues["temperature"] = 90; return data;
        }
        public void WriteSetting(int g, string id, int v) { throw new Exception("Unexpected write"); }
        public void WriteStates(int g, string id, IDictionary<string, int> v) { throw new Exception("Unexpected write"); }
        public void WriteFan(int g, IList<FanPoint> p) { throw new Exception("Unexpected write"); }
        public void WriteZeroRpm(int g, bool v) { throw new Exception("Unexpected write"); }
        public void Reset(int g) { throw new Exception("Unexpected write"); }
        public void Dispose() { }
    }
    private sealed class RpcFixture : IBackend, ISensorBackend
    {
        private readonly FakeDriver driver = new FakeDriver();
        private readonly AmdBackend backend;
        public List<GpuInfo> Gpus { get; private set; }
        public string Status { get { return "fixture"; } }
        internal RpcFixture()
        {
            backend = new AmdBackend(driver.System.Pointer); Gpus = new List<GpuInfo>(backend.Gpus);
            Gpus.Add(new GpuInfo { Index = 1, Name = "Crash fixture", Id = "crash" });
            Gpus.Add(new GpuInfo { Index = 2, Name = "Timeout fixture", Id = "timeout" });
        }
        public Snapshot Read(int index) { return backend.Read(0); }
        public Snapshot ReadSensors(int index)
        {
            if (index == 1) Environment.Exit(31);
            if (index == 2) Thread.Sleep(30000);
            return backend.ReadSensors(0);
        }
        public void WriteSetting(int gpu, string id, int value) { backend.WriteSetting(0, id, value); }
        public void WriteStates(int gpu, string prefix, IDictionary<string, int> values) { backend.WriteStates(0, prefix, values); }
        public void WriteFan(int gpu, IList<FanPoint> points) { backend.WriteFan(0, points); }
        public void WriteZeroRpm(int gpu, bool value) { backend.WriteZeroRpm(0, value); }
        public void Reset(int gpu) { backend.Reset(0); }
        public void Dispose() { backend.Dispose(); driver.CheckReleased(); driver.Dispose(); }
    }
    private static void Pump(Task task)
    {
        var watch = Stopwatch.StartNew();
        while (!task.IsCompleted && watch.ElapsedMilliseconds < 10000) { Application.DoEvents(); Thread.Sleep(1); }
        Check(task.IsCompleted, "UI operation stalled"); task.GetAwaiter().GetResult();
    }
    [DllImport("user32.dll")] private static extern bool GetKeyboardState(byte[] state);
    [DllImport("user32.dll")] private static extern bool SetKeyboardState(byte[] state);
    private static bool ProcessTab(Control control, bool backwards)
    {
        var original = new byte[256]; Check(GetKeyboardState(original), "Cannot read test-thread keyboard state"); var state = (byte[])original.Clone();
        foreach (int key in new[] { 0x10, 0x11, 0x12, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5 }) state[key] = 0;
        state[0x11] = state[0xA2] = 0x80; state[0x10] = state[0xA0] = backwards ? (byte)0x80 : (byte)0;
        try { Check(SetKeyboardState(state), "Cannot set test-thread modifiers"); Check(Control.ModifierKeys == (backwards ? Keys.Control | Keys.Shift : Keys.Control), "Unexpected synthetic test modifiers: " + Control.ModifierKeys); var message = Message.Create(control.Handle, 0x100, new IntPtr(9), new IntPtr(1)); return control.PreProcessMessage(ref message); }
        finally { SetKeyboardState(original); }
    }
    private static void RunStabilityTests()
    {
        Test("Sensor-only polling bypasses tuning discovery and retains real typed metrics", () => WithDriver((driver, backend) =>
        {
            var data = backend.ReadSensors(0);
            Check(data.Settings.Count == 0 && data.Fan.Count == 0 && data.MetricValues["temperature"] == 42.5 && driver.Writes == 0, "Sensor path performed tuning operations");
            Check(backend.Read(0).Settings.Count == 7, "Full settings read changed");
        }));
        Test("A transient sensor error retains the timer and recovers live values without discarding draft tuning", () => WithDriver((driver, backend) =>
        {
            using (var form = TestForm("sensor-recovery"))
            {
                var initial = backend.Read(0); SetField(form, "snapshot", initial); SetField(form, "backend", new FlakySensors { Initial = initial }); Call(form, "Render"); form.Stage(Change("power", -5));
                var timer = Field<System.Windows.Forms.Timer>(form, "timer"); timer.Enabled = true;
                Pump((Task)Call(form, "RefreshSensors", false)); Check(timer.Enabled && Field<bool>(form, "sensorFailed"), "One error stopped monitoring");
                Pump((Task)Call(form, "RefreshSensors", false)); Check(timer.Enabled && !Field<bool>(form, "sensorFailed") && Field<Snapshot>(form, "sensors").MetricValues["temperature"] == 90, "Live monitoring did not recover");
                Check(((Changes)Call(form, "Collect")).Values["power"] == -5 && driver.Writes == 0, "Polling lost or applied the draft");
                SetField(form, "dirty", false); SetField(form, "started", true); Field<TabControl>(form, "tabs").SelectedIndex = 4; Call(form, "UpdateActions");
                var message = Message.Create(form.Handle, 0x100, new IntPtr(0x74), IntPtr.Zero);
                typeof(MainForm).GetMethod("ProcessCmdKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { message, Keys.F5 });
                var deadline = Stopwatch.StartNew(); while (Field<bool>(form, "busy") && deadline.ElapsedMilliseconds < 3000) { Application.DoEvents(); Thread.Sleep(1); }
                Check(((FlakySensors)Field<IBackend>(form, "backend")).FullReads == 1, "F5 stopped working when its toolbar button was hidden in Settings");
            }
        }));
        Test("Driver IPC preserves complete snapshots, fan/range types and verified tuning transactions", () =>
        {
            var originalEncoding = Console.InputEncoding;
            try
            {
            // A UTF-8 console can prefix redirected stdin with a BOM. The host
            // must accept it instead of terminating on the first Base64 frame.
            Console.InputEncoding = new UTF8Encoding(true);
            using (var backend = new IsolatedBackend(Assembly.GetExecutingAssembly().Location, "--driver-fixture", 3000))
            {
                var s = backend.Read(0);
                Check(s.Settings.Count == 7 && s.Fan[0].Temperature == 50 && s.Settings[0].Range.Step == 10 && s.MetricValues["temperature"] == 42.5, "IPC lost typed data");
                var changes = Change("power", -5); changes.Fan = new List<FanPoint> { new FanPoint(55, 30), new FanPoint(85, 90) }; changes.ZeroRpm = false;
                var applied = TuningTransaction.Apply(backend, s, changes);
                Check(applied.After.Settings.Single(x => x.Id == "power").Value == -5 && applied.After.Fan[0].Speed == 30 && applied.After.ZeroRpm == false, "IPC setters/readback failed");
                TuningTransaction.Apply(backend, applied.After, applied.Undo);
                Check(backend.Read(0).Fan[0].Speed == 20 && backend.ReadSensors(0).Settings.Count == 0, "IPC undo or fast polling failed");
            }
            }
            finally { Console.InputEncoding = originalEncoding; }
        });
        Test("Driver child crash is contained; reconnect is read-only until an explicit settings refresh", () =>
        {
            using (var backend = new IsolatedBackend(Assembly.GetExecutingAssembly().Location, "--driver-fixture", 1500))
            {
                backend.Read(0); Expect(() => backend.ReadSensors(1), "Okno programu pozostaje otwarte");
                Expect(() => backend.WriteSetting(0, "power", -5), "F5");
                Thread.Sleep(2100); Check(backend.ReadSensors(0).MetricValues["temperature"] == 42.5, "Driver did not reconnect");
                Expect(() => backend.WriteSetting(0, "power", -5), "F5");
                backend.Read(0); backend.WriteSetting(0, "power", -5);
                Check(backend.Read(0).Settings.Single(x => x.Id == "power").Value == -5, "Explicit refresh did not restore writes");
            }
        });
        Test("Hung driver child times out without blocking the UI indefinitely", () =>
        {
            using (var backend = new IsolatedBackend(Assembly.GetExecutingAssembly().Location, "--driver-fixture", 300))
            {
                var watch = Stopwatch.StartNew(); Expect(() => backend.ReadSensors(2), "wymaganym czasie");
                Check(watch.ElapsedMilliseconds < 3000, "Timeout was not bounded");
            }
        });
        Test("Ctrl Tab and Ctrl Shift Tab bubble correctly from lists, edits, numeric fields and top-level buttons", () => WithDriver((driver, backend) =>
        {
            using (var form = TestForm("tab-bubbling"))
            {
                SetField(form, "started", true); SetField(form, "snapshot", backend.Read(0)); Call(form, "Render");
                form.Opacity = 0; form.ShowInTaskbar = false; form.Show(); var tabs = Field<TabControl>(form, "tabs");
                var controls = new Control[] { Field<SensorView>(form, "overview"), Field<TextBox>(form, "report"), Field<ComboBox>(form, "language"), Field<Button>(form, "help"), Field<Dictionary<string, NumericUpDown>>(form, "editors")["gfx.min"].Controls.OfType<TextBox>().Single(), Field<TextBox>(form, "status") };
                foreach (var control in controls)
                {
                    int start = control == controls[0] ? 0 : control == controls[1] ? 3 : control == controls[2] ? 4 : control == controls[4] ? 1 : 2;
                    tabs.SelectedIndex = start; control.Focus();
                    Check(ProcessTab(control, false) && tabs.SelectedIndex == (start + 1) % 5 && tabs.Focused, "Forward tab failed from " + control.GetType());
                    Check(ProcessTab(control, true) && tabs.SelectedIndex == start && tabs.Focused, "Backward tab failed from " + control.GetType());
                }
                tabs.SelectedIndex = 4; var language = Field<ComboBox>(form, "language"); language.Focus(); language.DroppedDown = true;
                Check(ProcessTab(language, false) && !language.DroppedDown && tabs.SelectedIndex == 0, "Expanded combo blocked Ctrl Tab");
                SetField(form, "exitRequested", true); form.Close(); Check(driver.Writes == 0, "Navigation wrote tuning");
            }
        }));
        Test("Sensor list updates selected rows live without losing selection; titles and tray include version", () =>
        {
            using (var view = new SensorView())
            {
                view.Localize(); view.Text = "Temperatura GPU: 40 °C\r\nMoc GPU: 2 W"; view.CreateControl(); view.Items[0].Selected = true;
                view.Text = "Temperatura GPU: 77 °C\r\nMoc GPU: 180 W";
                Check(view.Items[0].Selected && view.Items[0].SubItems[1].Text == "77 °C" && view.Items[1].SubItems[1].Text == "180 W", "Live row update lost current data or selection");
            }
            using (var form = TestForm("version-title")) Check(form.Text == "Radeoner v" + L.Version && form.AccessibleName == form.Text, "Window version missing");
            Check(TrayText.Format(null, false).StartsWith("Radeoner v" + L.Version), "Tray version missing");
        });
        Test("Settings remain reachable with keyboard at 100 and 200 percent text in a small window", () =>
        {
            using (var form = TestForm("settings-layout"))
            {
                SetField(form, "started", true); form.Opacity = 0; form.ShowInTaskbar = false; form.Size = new Size(760, 650); Field<TabControl>(form, "tabs").SelectedIndex = 4; form.Show();
                foreach (int scale in new[] { 0, 4 })
                {
                    Field<ComboBox>(form, "textScale").Focus(); Field<ComboBox>(form, "textScale").SelectedIndex = scale; Application.DoEvents();
                    var fields = Field<FlowLayoutPanel>(form, "personalFields"); var button = Field<Button>(form, "supportProject"); button.Focus(); Application.DoEvents();
                    var viewport = fields.RectangleToScreen(fields.ClientRectangle); var bounds = button.RectangleToScreen(button.ClientRectangle);
                    using (var bitmap = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size)); bitmap.Save(TestFile("settings-" + scale + ".png")); }
                    Check(viewport.Contains(bounds), "Settings action is clipped at scale " + scale + ": " + bounds + " outside " + viewport + "; tab=" + Field<TabControl>(form, "tabs").SelectedIndex);
                    Field<TabControl>(form, "tabs").SelectedIndex = 1;
                    foreach (var action in new[] { Field<Button>(form, "rgb"), Field<Button>(form, "exit") })
                    {
                        action.Focus(); Application.DoEvents(); var window = form.RectangleToScreen(form.ClientRectangle);
                        Check(window.Contains(action.RectangleToScreen(action.ClientRectangle)), "Large text made an action unreachable: " + action.AccessibleName);
                    }
                    Field<TabControl>(form, "tabs").SelectedIndex = 4;
                }
                SetField(form, "exitRequested", true); form.Close();
            }
        });
        Test("Visible keyboard mnemonics are unique on each tab in all six languages", () => WithDriver((driver, backend) =>
        {
            using (var form = TestForm("mnemonics"))
            {
                SetField(form, "started", true); SetField(form, "snapshot", backend.Read(0)); Call(form, "Render"); form.Opacity = 0; form.ShowInTaskbar = false; form.Show();
                foreach (int language in Enumerable.Range(0, L.Languages.Length))
                {
                    Field<ComboBox>(form, "language").SelectedIndex = language;
                    for (int page = 0; page < 5; page++)
                    {
                        Field<TabControl>(form, "tabs").SelectedIndex = page; var used = new Dictionary<char, string>();
                        foreach (var control in Descendants(form).Where(c => c.Visible && (c is Button || c is CheckBox || c is Label)))
                        {
                            int marker = control.Text.IndexOf('&'); if (marker < 0 || marker == control.Text.Length - 1) continue; char key = char.ToLowerInvariant(control.Text[marker + 1]);
                            Check(!used.ContainsKey(key), "Duplicate Alt+" + key + " on tab " + page + ": " + (used.ContainsKey(key) ? used[key] : "") + " / " + control.Text); used[key] = control.Text;
                        }
                    }
                }
                SetField(form, "exitRequested", true); form.Close();
            }
            L.Load("pl"); Check(driver.Writes == 0, "Mnemonic audit wrote hardware");
        }));
    }
}
