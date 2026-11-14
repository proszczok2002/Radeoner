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
        if (SendMessageTimeout(edit, 0x000E, IntPtr.Zero, null, 2, 1000, out length) == IntPtr.Zero) throw new InvalidOperationException("Windows refused reading NVDA Speech Viewer; no speech result can be verified.");
        int count = length.ToInt32(); if (count < 0 || count > 1000000) throw new InvalidOperationException("Clear Speech Viewer before the audit (history is too large).");
        var text = new StringBuilder(count + 1);
        if (SendMessageTimeout(edit, 0x000D, new IntPtr(text.Capacity), text, 2, 1000, out copied) == IntPtr.Zero) throw new InvalidOperationException("Windows refused reading NVDA Speech Viewer.");
        return text.ToString();
    }
    private static string Normalize(string text) { return Regex.Replace(text.ToLowerInvariant(), @"[^\p{L}\p{N}]+", " ").Trim(); }
    internal async Task Focus(Form form, Control control, string context, string expected = null)
    {
        form.Activate(); SetForegroundWindow(form.Handle);
        for (int wait = 0; wait < 300 && GetForegroundWindow() != form.Handle; wait++) await Task.Delay(100);
        if (GetForegroundWindow() != form.Handle) throw new InvalidOperationException("Audit window lost foreground; no NVDA checkpoint recorded: " + context);
        if (control.ContainsFocus)
        {
            var alternate = Controls(form).FirstOrDefault(c => c is Button && c.CanFocus && c != control);
            if (alternate != null) { alternate.Focus(); await Task.Delay(250); }
        }
        string before = Read(); if (!control.Focus() && !control.ContainsFocus) throw new InvalidOperationException("Cannot focus " + context);
        string spoken = "", name = expected ?? control.AccessibleName;
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Unnamed NVDA checkpoint: " + context);
        for (int poll = 0; poll < 20; poll++)
        {
            await Task.Delay(150);
            if (GetForegroundWindow() != form.Handle) throw new InvalidOperationException("Foreground changed; no speech from other applications was recorded at " + context);
            string current = Read();
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
    private static int LiveNvda(string output)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            var speech = new NvdaSpeech(output);
            WithDriver((driver, backend) =>
            {
                foreach (string language in new[] { "pl", "en" }) foreach (string theme in new[] { "light", "dark", "system" }) foreach (int scale in new[] { 100, 200 })
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
                                    tabs.SelectedIndex = tab; await speech.Focus(form, tabs, context + "/tab" + tab, tabs.SelectedTab.Text);
                                    foreach (var control in Descendants(tabs.SelectedTab).Where(c => c.Enabled && c.Visible && c.TabStop && (c is NumericUpDown || c is ComboBox || c is CheckBox || c is Button || c is ListView || c is TextBox)).Where(c => !(c is TextBox && c.Parent is NumericUpDown)).ToList()) await speech.Focus(form, control, context + "/" + control.AccessibleName);
                                }
                                using (var details = new DetailsDialog(L.T("ui.helpTitle"), L.T("ui.helpText", L.Version), form.Font, theme))
                                    NvdaDialog(form, details, async () => { foreach (var c in Descendants(details).Where(c => c is TextBox || c is Button)) await speech.Focus(details, c, context + "/details/" + c.AccessibleName); });
                                using (var server = new RgbServer()) using (var rgb = new RgbDialog(form.Font, theme))
                                    NvdaDialog(form, rgb, async () =>
                                    {
                                        Descendants(rgb).OfType<NumericUpDown>().Single(c => c.AccessibleName == L.T("rgb.port")).Value = server.Port;
                                        await (Task)typeof(RgbDialog).GetMethod("Connect", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(rgb, null);
                                        foreach (var c in Descendants(rgb).Where(c => c.Enabled && c.Visible && c.TabStop && (c is Button || c is NumericUpDown || c is ComboBox || c is TextBox)).Where(c => !(c is TextBox && c.Parent is NumericUpDown))) await speech.Focus(rgb, c, context + "/RGB/" + c.AccessibleName);
                                    });
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
