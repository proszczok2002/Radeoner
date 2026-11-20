using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Radeoner;

// Opt-in only. Reads actual Speech Viewer output; never substitutes MSAA names
// for NVDA speech, changes NVDA settings or installs a reader add-on.
internal sealed class NvdaSpeech
{
    private delegate bool WindowCallback(IntPtr window, IntPtr data);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowCallback callback, IntPtr data);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr parent, WindowCallback callback, IntPtr data);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder text, int count);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr SendMessageTimeout(IntPtr window, uint message, IntPtr parameter, StringBuilder text, uint flags, uint timeout, out IntPtr result);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    private readonly IntPtr edit;
    private readonly string output;
    private readonly StringBuilder log = new StringBuilder();
    internal NvdaSpeech(string path)
    {
        output = Path.GetFullPath(path); IntPtr viewer = IntPtr.Zero, found = IntPtr.Zero;
        var ids = new HashSet<int>(Process.GetProcessesByName("nvda").Select(p => p.Id));
        EnumWindows((window, data) =>
        {
            uint pid; GetWindowThreadProcessId(window, out pid); if (!ids.Contains((int)pid)) return true;
            var title = new StringBuilder(1024); GetWindowText(window, title, title.Capacity);
            string name = title.ToString();
            if (name.IndexOf("speech viewer", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("podgląd mowy", StringComparison.OrdinalIgnoreCase) >= 0) viewer = window;
            return true;
        }, IntPtr.Zero);
        if (viewer != IntPtr.Zero) EnumChildWindows(viewer, (window, data) =>
        {
            var name = new StringBuilder(256); GetClassName(window, name, name.Capacity);
            if (name.ToString().IndexOf("RichEdit", StringComparison.OrdinalIgnoreCase) >= 0) found = window;
            return true;
        }, IntPtr.Zero);
        if (found == IntPtr.Zero) throw new InvalidOperationException("NVDA Speech Viewer is unavailable. Enable NVDA menu > Tools > Speech Viewer. No NVDA speech check was completed.");
        edit = found; Read();
        log.AppendLine("Actual NVDA Speech Viewer capture; app " + L.Version + "; " + DateTime.UtcNow.ToString("o")); Save();
    }
    private string Read()
    {
        IntPtr length, copied;
        if (SendMessageTimeout(edit, 0x000E, IntPtr.Zero, null, 2, 3000, out length) == IntPtr.Zero) throw new InvalidOperationException("Windows refused reading NVDA Speech Viewer (error " + Marshal.GetLastWin32Error() + "); no speech result can be verified.");
        int count = length.ToInt32(); if (count < 0 || count > 1000000) throw new InvalidOperationException("Clear Speech Viewer before the audit (history is too large).");
        var text = new StringBuilder(count + 1);
        if (SendMessageTimeout(edit, 0x000D, new IntPtr(text.Capacity), text, 2, 3000, out copied) == IntPtr.Zero) throw new InvalidOperationException("Windows refused reading NVDA Speech Viewer (error " + Marshal.GetLastWin32Error() + ").");
        return text.ToString();
    }
    private static string Normalize(string text) { return Regex.Replace(text.ToLowerInvariant(), @"[^\p{L}\p{N}]+", " ").Trim(); }
    // NVDA may synchronously query the owned STA window while processing focus.
    // Await the cross-process text read so that window can service those queries.
    internal Task<string> SnapshotAsync() { return Task.Run((Func<string>)Read); }
    private static string ForegroundContext(IntPtr expected)
    {
        IntPtr current = GetForegroundWindow(); uint pid; GetWindowThreadProcessId(current, out pid);
        var name = new StringBuilder(256); GetClassName(current, name, name.Capacity);
        return " expected=" + expected.ToString("X") + " current=" + current.ToString("X") + " pid=" + pid + " class=" + name;
    }
    internal async Task<string> Native(IntPtr window, string before, string context, string expected, bool requireForeground = true)
    {
        string spoken = "";
        await Task.Delay(400);
        for (int poll = 0; poll < 30; poll++)
        {
            if (requireForeground && GetForegroundWindow() != window) throw new InvalidOperationException("Native audit lost its own foreground window: " + context + ForegroundContext(window));
            string current = await SnapshotAsync();
            if (requireForeground && GetForegroundWindow() != window) throw new InvalidOperationException("Native audit lost its own foreground window: " + context + ForegroundContext(window));
            if (!current.StartsWith(before, StringComparison.Ordinal)) throw new InvalidOperationException("Speech history changed: " + context);
            spoken = current.Substring(before.Length);
            if (spoken.Trim().Length > 0 && (expected.Length == 0 || Normalize(spoken).Contains(Normalize(expected)))) break;
            await Task.Delay(150);
        }
        log.AppendLine("\r\nCHECKPOINT " + context + "\r\nExpected name: " + expected + "\r\nActual NVDA text:\r\n" + spoken); Save();
        if (spoken.Trim().Length == 0 || expected.Length > 0 && !Normalize(spoken).Contains(Normalize(expected))) throw new InvalidOperationException("Native NVDA speech not verified: " + context);
        return spoken;
    }
    internal async Task Focus(Form form, Control control, string context, string expected = null, Func<Task> activate = null)
    {
        form.Activate(); SetForegroundWindow(form.Handle);
        for (int wait = 0; wait < 300 && GetForegroundWindow() != form.Handle; wait++) await Task.Delay(100);
        if (GetForegroundWindow() != form.Handle) throw new InvalidOperationException("Audit window lost foreground; no NVDA checkpoint recorded: " + context);
        if (control.ContainsFocus)
        {
            var alternate = Controls(form).FirstOrDefault(c => c is Button && c.CanFocus && c != control);
            if (alternate != null) { alternate.Focus(); await Task.Delay(600); }
        }
        string before = await SnapshotAsync();
        if (GetForegroundWindow() != form.Handle) throw new InvalidOperationException("Audit window lost foreground before focus: " + context);
        if (activate != null) await activate();
        else if (!control.Focus() && !control.ContainsFocus) throw new InvalidOperationException("Cannot focus " + context);
        string spoken = "", name = expected ?? control.AccessibleName;
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Unnamed NVDA checkpoint: " + context);
        await Task.Delay(400);
        for (int poll = 0; poll < 20; poll++)
        {
            await Task.Delay(150);
            if (GetForegroundWindow() != form.Handle) throw new InvalidOperationException("Foreground changed; no speech from other applications was recorded at " + context);
            string current = await SnapshotAsync();
            if (GetForegroundWindow() != form.Handle) throw new InvalidOperationException("Foreground changed; no speech from other applications was recorded at " + context);
            if (!current.StartsWith(before, StringComparison.Ordinal)) throw new InvalidOperationException("Speech Viewer history changed during the audit; repeat checkpoint " + context);
            spoken = current.Substring(before.Length);
            if (Normalize(spoken).Contains(Normalize(name))) break;
        }
        log.AppendLine("\r\nCHECKPOINT " + context + "\r\nExpected name: " + name + "\r\nActual NVDA text:\r\n" + spoken); Save();
        if (!Normalize(spoken).Contains(Normalize(name))) throw new InvalidOperationException("NVDA did not report the expected name at " + context + ". Review the actual transcript.");
    }
    private static IEnumerable<Control> Controls(Control root)
    { foreach (Control child in root.Controls) { yield return child; foreach (Control nested in Controls(child)) yield return nested; } }
    private void Save() { File.WriteAllText(output, log.ToString(), new UTF8Encoding(true)); }
}

internal static partial class Tests
{
    private static int NvdaNumbers(string output)
    {
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            var speech = new NvdaSpeech(output); Exception failure = null;
            using (var form = new Form { Text = "Radeoner — test etykiet NVDA", Size = new Size(650, 650) })
            {
                var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoScroll = true, WrapContents = false }; form.Controls.Add(layout);
                var numbers = new List<NumericUpDown>(); int failed = 0;
                foreach (var role in new[] { AccessibleRole.SpinButton, AccessibleRole.Client, AccessibleRole.Client })
                {
                    string label = "Pole próbne " + role + " " + numbers.Count;
                    layout.Controls.Add(new Label { AutoSize = true, Text = label });
                    var number = new NumericUpDown { Value = 42, AccessibleName = label, AccessibleDescription = "Zakres od 0 do 100", AccessibleRole = role, Width = 300 };
                    foreach (Control child in number.Controls) { child.AccessibleName = label; child.AccessibleDescription = number.AccessibleDescription; }
                    if (numbers.Count == 2) foreach (var child in number.Controls.OfType<TextBox>()) child.AccessibleRole = AccessibleRole.SpinButton;
                    layout.Controls.Add(number); numbers.Add(number);
                }
                var close = new Button { Text = "Zamknij", AccessibleName = "Zamknij", AutoSize = true }; close.Click += (s, e) => form.Close(); layout.Controls.Add(close);
                form.Shown += async (s, e) =>
                {
                    try
                    {
                        foreach (var number in numbers)
                        {
                            await speech.Focus(form, number, number.AccessibleName);
                            try
                            {
                                await speech.Focus(form, number, number.AccessibleName + ": Up arrow changes and announces value", "43", async () =>
                                {
                                    number.Focus(); await Task.Delay(500);
                                    Check(GetForegroundWindow() == form.Handle && !UserModifierHeld(), "Unsafe keyboard state");
                                    var state = new byte[256]; GetKeyboardState(state);
                                    foreach (int key in new[] { 0x10, 0x11, 0x12, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5 }) state[key] &= 0x7F;
                                    SetKeyboardState(state);
                                    var keys = new[] { Key(0x26), Key(0x26, true) };
                                    keys[0].Flags |= 1; keys[1].Flags |= 1; // E0 arrow, not NVDA's numpad review key.
                                    Check(SendInput(2, keys, Marshal.SizeOf(typeof(KeyInput))) == 2, "Up arrow input refused");
                                    await Task.Delay(300);
                                    File.AppendAllText(output + ".values", number.AccessibleName + ": " + number.Value + "\r\n");
                                });
                                Check(number.Value == 43, "Native numeric keyboard interaction changed");
                            }
                            catch (Exception ex) { failed++; File.AppendAllText(output + ".values", number.AccessibleName + ": " + ex.Message + "\r\n"); }
                        }
                        Check(failed == 0, "Numeric NVDA keyboard probes failed; review .values evidence");
                    }
                    catch (Exception ex) { failure = ex; }
                    finally { form.Close(); }
                };
                Application.Run(form); if (failure != null) throw failure;
            }
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 2; }
    }
    private static int LiveNvdaAmd(string output)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            var speech = new NvdaSpeech(output);
            foreach (string language in L.Languages)
            {
                L.Load(language);
                using (var dialog = new DriverUpdateDialog(new GpuInfo { Name = "Radeon test GPU", Id = TestInstanceId, Driver = "32.0.12345.67" },
                    SystemFonts.MessageBoxFont, "system"))
                {
                    Exception failure = null;
                    dialog.Shown += async (sender, args) =>
                    {
                        try
                        {
                            var details = Descendants(dialog).OfType<TextBox>().Single();
                            var offers = Descendants(dialog).OfType<ComboBox>().Single();
                            offers.Items.Add(TestDriverOffer(false));
                            offers.Items.Add(TestDriverOffer());
                            offers.SelectedIndex = 0;
                            await speech.Focus(dialog, offers, language + "/driver list");
                            var license = Descendants(dialog).OfType<Button>().Single(b => b.Enabled && b.AccessibleName == L.T("wud.license"));
                            await speech.Focus(dialog, license, language + "/license");
                            offers.SelectedIndex = 1;
                            foreach (var button in Descendants(dialog).OfType<Button>().Where(b => b.Enabled))
                                await speech.Focus(dialog, button, language + "/" + button.AccessibleName);
                            await speech.Focus(dialog, details, language + "/driver information");
                        }
                        catch (Exception ex) { failure = ex; }
                        finally { dialog.Close(); }
                    };
                    Application.Run(dialog);
                    if (failure != null) throw failure;
                }
            }
            File.AppendAllText(output, "\r\nPASS NVDA AMD driver updates dialog in six languages.\r\n");
            return 0;
        }
        catch (Exception ex)
        {
            File.AppendAllText(output, "\r\nNOT VERIFIED: " + ex.Message + "\r\n");
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }

    private static int LiveNvdaRgb(string output)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            var speech = new NvdaSpeech(output);
            foreach (string language in L.Languages)
            {
                L.Load(language);
                using (var server = new RgbServer(topologyNotices: 1))
                using (var dialog = new RgbDialog(SystemFonts.MessageBoxFont, "system"))
                {
                    Exception failure = null;
                    dialog.Shown += async (sender, args) =>
                    {
                        try
                        {
                            var connect = Descendants(dialog).OfType<Button>().Single(b => b.AccessibleName == L.T("rgb.connect"));
                            var port = Descendants(dialog).OfType<NumericUpDown>().Single(n => n.AccessibleName == L.T("rgb.port"));
                            var status = Descendants(dialog).OfType<TextBox>().Single(t => t.AccessibleName == L.T("rgb.status"));
                            var device = Descendants(dialog).OfType<ComboBox>().Single(c => c.AccessibleName == L.T("rgb.device"));
                            var connectAsync = typeof(RgbDialog).GetMethod("Connect", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                            var unused = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
                            unused.Start();
                            try { port.Value = ((System.Net.IPEndPoint)unused.LocalEndpoint).Port; }
                            finally { unused.Stop(); }
                            await speech.Focus(dialog, connect, language + "/connect");
                            string before = await speech.SnapshotAsync();
                            await (Task)connectAsync.Invoke(dialog, null);
                            Check(status.Focused, "Connection error did not focus status");
                            await speech.Native(dialog.Handle, before, language + "/unavailable", L.T("rgb.noServer"));
                            port.Value = server.Port;
                            connect.Focus();
                            before = await speech.SnapshotAsync();
                            await (Task)connectAsync.Invoke(dialog, null);
                            Check(device.Focused && server.Connections == 2 && server.Updates == 0, "RGB discovery did not restore the GPU list");
                            await speech.Native(dialog.Handle, before, language + "/recovered", L.T("rgb.device"));
                            server.CheckHealthy();
                        }
                        catch (Exception ex) { failure = ex; }
                        finally { dialog.Close(); }
                    };
                    Application.Run(dialog);
                    if (failure != null) throw failure;
                }
            }
            File.AppendAllText(output, "\r\nPASS NVDA RGB error and rescan recovery in six languages.\r\n");
            return 0;
        }
        catch (Exception ex)
        {
            File.AppendAllText(output, "\r\nNOT VERIFIED: " + ex.Message + "\r\n");
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }

    private static int LiveNvda(string output)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            var speech = new NvdaSpeech(output);
            WithDriver((driver, backend) =>
            {
                foreach (string language in L.Languages) foreach (string theme in new[] { "light", "dark", "system" }) foreach (int scale in new[] { 200 })
                {
                    string path = TestFile("nvda.settings.json"); new AppSettings { Language = language, Theme = theme, TextScale = scale, CheckUpdates = false }.Save(path);
                    using (var form = new MainForm(false, path, new StartupRegistration(Application.ExecutablePath, @"Software\Radeoner\Tests", "Unused")) { SuppressAutomaticUpdateChecks = true })
                    {
                        SetAuditSnapshot(form, backend.Read(0)); form.Text += " — audyt / test data"; form.AccessibleName = form.Text; Exception failure = null;
                        form.Shown += async (sender, e) =>
                        {
                            try
                            {
                                string context = language + "/" + theme + "/" + scale; var tabs = Field<TabControl>(form, "tabs");
                                for (int tab = 0; tab < 5; tab++)
                                {
                                    tabs.SelectedIndex = (tab + 4) % 5;
                                    string caption = tabs.TabPages[tab].Text;
                                    await speech.Focus(form, tabs, context + "/tab" + tab, caption, () => KeyboardTab(form, false));
                                    Check(tabs.SelectedIndex == tab && tabs.Focused, "Actual Ctrl Tab did not focus the target tab");
                                    foreach (var control in Descendants(tabs.SelectedTab).Where(c => c.Enabled && c.Visible && c.TabStop && (c is NumericUpDown || c is ComboBox || c is CheckBox || c is Button || c is ListView || c is TextBox)).Where(c => !(c is TextBox && c.Parent is NumericUpDown)).ToList()) await speech.Focus(form, control, context + "/" + control.AccessibleName);
                                }
                                using (var details = new DetailsDialog(L.T("ui.helpTitle"), L.T("ui.helpText", L.Version) + "\r\n\r\n" + L.Guide(), form.Font, theme))
                                    NvdaDialog(form, details, async () => { foreach (var c in Descendants(details).Where(c => c is TextBox || c is Button)) await speech.Focus(details, c, context + "/details/" + c.AccessibleName); });
                                using (var server = new RgbServer()) using (var rgb = new RgbDialog(form.Font, theme))
                                    NvdaDialog(form, rgb, async () =>
                                    {
                                        Descendants(rgb).OfType<NumericUpDown>().Single(c => c.AccessibleName == L.T("rgb.port")).Value = server.Port;
                                        await (Task)typeof(RgbDialog).GetMethod("Connect", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(rgb, null);
                                        foreach (var c in Descendants(rgb).Where(c => c.Enabled && c.Visible && c.TabStop && (c is Button || c is NumericUpDown || c is ComboBox || c is TextBox)).Where(c => !(c is TextBox && c.Parent is NumericUpDown))) await speech.Focus(rgb, c, context + "/RGB/" + c.AccessibleName);
                                    });
                                var integrated = backend.Read(0); integrated.Gpu.Type = 1; integrated.Settings.Clear(); integrated.Fan.Clear(); integrated.ZeroRpm = null; integrated.CanReset = false;
                                SetField(form, "snapshot", integrated); Call(form, "Render");
                                Check(tabs.TabCount == 3, "Unsupported integrated pages remained visible");
                                for (int tab = 0; tab < tabs.TabCount; tab++)
                                {
                                    tabs.SelectedIndex = (tab + tabs.TabCount - 1) % tabs.TabCount;
                                    await speech.Focus(form, tabs, context + "/integrated/tab" + tab, tabs.TabPages[tab].Text, () => KeyboardTab(form, false));
                                    Check(tabs.SelectedIndex == tab, "Real Ctrl Tab included an unavailable integrated page");
                                }
                                await speech.Focus(form, tabs, context + "/integrated/reverse", L.T("ui.reportTab"), () => KeyboardTab(form, true));
                                Check((int)tabs.SelectedTab.Tag == 3, "Reverse integrated navigation failed");
                                var supported = backend.Read(0); supported.Gpu.Type = 1; SetField(form, "snapshot", supported); Call(form, "Render");
                                Check(tabs.TabCount == 5, "Supported integrated controls were hidden");
                                Call(form, "SelectPage", 1);
                                await speech.Focus(form, Field<Dictionary<string, NumericUpDown>>(form, "editors").Values.First(), context + "/integrated/supported setting");
                                backend.Gpus[0].Type = 2;
                            }
                            catch (Exception ex) { failure = ex; }
                            finally { SetField(form, "exitRequested", true); form.Close(); }
                        };
                        Application.Run(form); if (failure != null) throw failure;
                    }
                }
                Check(driver.Writes == 0, "NVDA audit changed physical GPU settings");
            });
            File.AppendAllText(output, "\r\nPASS captured NVDA names for the tested controls. Review transcripts for duplicate/unclear speech. Native file dialogs, tray and physical GPU coverage require separate review.\r\n"); return 0;
        }
        catch (Exception ex) { File.AppendAllText(output, "\r\nNOT VERIFIED: " + ex.Message + "\r\n"); Console.Error.WriteLine(ex.Message); return 2; }
    }
    private static void NvdaDialog(Form owner, Form dialog, Func<Task> action)
    {
        Exception failure = null;
        dialog.Shown += async (sender, e) => { try { await action(); } catch (Exception ex) { failure = ex; } finally { dialog.Close(); } };
        dialog.ShowDialog(owner); if (failure != null) throw failure;
    }
}
