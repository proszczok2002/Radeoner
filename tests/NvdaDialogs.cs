using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Radeoner;

internal static partial class Tests
{
    private static int NvdaTray(string output, string language = "pl", bool integrated = false)
    {
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            var speech = new NvdaSpeech(output); Exception failure = null;
            WithDriver((driver, backend) =>
            {
                using (var form = TestForm("nvda-tray"))
                {
                    L.Load(language); Call(form, "ApplyLanguage"); var snapshot = backend.Read(0); snapshot.Gpu.Type = integrated ? 1 : 2;
                    snapshot.Gpu.Name = integrated ? "AMD integrated GPU" : "AMD discrete GPU"; snapshot.MetricValues["fan"] = 333;
                    if (integrated) { snapshot.Settings.Clear(); snapshot.Fan.Clear(); snapshot.ZeroRpm = null; snapshot.CanReset = false; }
                    SetAuditSnapshot(form, snapshot); var tray = new TrayIcon(Field<ContextMenuStrip>(form, "trayMenu")); SetField(form, "tray", tray);
                    tray.ShowRequested += () => Call(form, "ShowFromTray");
                    form.Shown += async (s, e) =>
                    {
                        try
                        {
                            Check(tray.EnsureVisible(), "Native tray icon was not added"); Call(form, "UpdateTray", false); await Task.Delay(300);
                            Check((bool)Call(form, "HideToTray"), "Hide to tray failed");
                            string before = speech.Snapshot(); File.WriteAllText(output + ".ready", TrayText.Format(Field<Snapshot>(form, "snapshot"), false));
                            var watch = Stopwatch.StartNew();
                            while (!File.Exists(output + ".focus") && watch.ElapsedMilliseconds < 60000) await Task.Delay(100);
                            Check(File.Exists(output + ".focus"), "Waiting for UI Automation to focus this exact tray icon timed out");
                            // Shell notification focus can move between Explorer
                            // HWNDs; the external harness verifies the exact UIA
                            // focused icon name before acknowledging this step.
                            string iconSpeech = await speech.Native(GetForegroundWindow(), before, language + (integrated ? "/integrated" : "/discrete") + "/tray/actual icon focus", "Radeoner v" + L.Version, false);
                            Check(integrated ? iconSpeech.IndexOf("RPM", StringComparison.OrdinalIgnoreCase) < 0 : iconSpeech.Contains("333 RPM"), "Actual NVDA tray fan announcement differs from GPU capabilities");
                            before = speech.Snapshot(); File.WriteAllText(output + ".focused", "ready");
                            while (!form.Visible && watch.ElapsedMilliseconds < 90000) await Task.Delay(100);
                            Check(form.Visible, "Actual tray icon activation did not restore the app");
                            await speech.Native(form.Handle, before, "tray/actual icon restores main window", "Radeoner v" + L.Version);
                            var menu = Field<ContextMenuStrip>(form, "trayMenu"); before = speech.Snapshot(); menu.Show(form, new System.Drawing.Point(20, 20)); menu.Items[0].Select();
                            await speech.Native(GetForegroundWindow(), before, "tray/menu/show", menu.Items[0].Text.Replace("&", ""), false);
                            foreach (int index in new[] { 1, 2, 4 })
                            {
                                before = speech.Snapshot();
                                Check((GetForegroundWindow() == form.Handle || GetForegroundWindow() == menu.Handle) && !UserModifierHeld(), "Own tray menu lost focus");
                                var keys = new[] { Key(0x28), Key(0x28, true) }; keys[0].Flags |= 1; keys[1].Flags |= 1;
                                Check(SendInput(2, keys, Marshal.SizeOf(typeof(KeyInput))) == 2, "Own menu Down key refused");
                                await speech.Native(GetForegroundWindow(), before, "tray/menu/" + index, menu.Items[index].Text.Replace("&", ""), false);
                            }
                            menu.Close();
                            File.AppendAllText(output, "\r\nPASS native tray tooltip speech, hide and actual Shell icon activation restore.\r\n");
                        }
                        catch (Exception ex) { failure = ex; }
                        finally { SetField(form, "exitRequested", true); form.Close(); }
                    };
                    Application.Run(form); if (failure != null) throw failure;
                    Check(driver.Writes == 0, "Tray test changed GPU settings");
                }
            }); return 0;
        }
        catch (Exception ex) { File.AppendAllText(output, "\r\nNOT VERIFIED: " + ex.Message + "\r\n"); return 2; }
    }
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    private static async Task NativeTab(IntPtr window)
    {
        Check(GetForegroundWindow() == window && !UserModifierHeld(), "Native dialog keyboard focus changed; no keys sent");
        var state = new byte[256]; GetKeyboardState(state);
        foreach (int key in new[] { 0x10, 0x11, 0x12, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5 }) state[key] &= 0x7F;
        SetKeyboardState(state);
        Check(SendInput(2, new[] { Key(9), Key(9, true) }, Marshal.SizeOf(typeof(KeyInput))) == 2, "Native dialog Tab refused");
        await Task.Delay(100);
    }
    private static int NvdaNativeDialogs(string output)
    {
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            var speech = new NvdaSpeech(output); Exception failure = null;
            WithDriver((driver, backend) =>
            {
                using (var form = TestForm("nvda-native"))
                {
                    SetAuditSnapshot(form, backend.Read(0)); form.Text += " — dialogi audytu / test data";
                    form.Shown += async (s, e) =>
                    {
                        try
                        {
                            for (int wait = 0; wait < 300 && GetForegroundWindow() != form.Handle; wait++) { form.Activate(); await Task.Delay(100); }
                            Check(GetForegroundWindow() == form.Handle, "Native audit owner did not become foreground");
                            foreach (string language in L.Languages)
                            {
                                L.Load(language); Call(form, "ApplyLanguage"); Field<TabControl>(form, "tabs").SelectedIndex = 4;
                                foreach (string kind in new[] { "aboutProgram", "supportProject", "update" })
                                {
                                    string before = speech.Snapshot(); Exception dialogFailure = null; bool question = kind == "update"; int created = 0;
                                    SystemDialogs.TestCreated = window =>
                                    {
                                        created++; var timer = new System.Windows.Forms.Timer { Interval = 300 };
                                        timer.Tick += async (sender, args) =>
                                        {
                                            timer.Stop(); timer.Dispose();
                                            try
                                            {
                                                await speech.Native(window, before, language + "/native/" + kind, question ? "99.0" : kind == "aboutProgram" ? "Radeoner" : L.T("info.supportTitle"));
                                                int count = question ? 3 : kind == "aboutProgram" ? 3 : 5;
                                                for (int i = 0; i < count; i++) { string start = speech.Snapshot(); await NativeTab(window); await speech.Native(window, start, language + "/native/" + kind + "/Tab" + i, ""); }
                                            }
                                            catch (Exception ex) { dialogFailure = ex; }
                                            finally { SystemDialogs.PostMessage(window, 0x466, new IntPtr(question ? 7 : 8), IntPtr.Zero); }
                                        }; timer.Start();
                                    };
                                    try
                                    {
                                        if (question)
                                        {
                                            SetField(form, "availableUpdate", new AvailableUpdate { Manifest = new UpdateManifest { Version = "99.0.0.0", DisplayVersion = "99.0" }, ReleaseNotes = "## Polski\nZmiana testowa dostępności.\n## English\nAccessibility test change." });
                                            await (Task)Call(form, "DownloadAndInstallUpdate");
                                            Check(!Field<bool>(form, "updateInstalling"), "No started an update installation");
                                        }
                                        else Field<Button>(form, kind).PerformClick();
                                        Check(created == 1, "Native dialog did not open"); if (dialogFailure != null) throw dialogFailure;
                                    }
                                    finally { SystemDialogs.TestCreated = null; }
                                }
                                string folder = TestFile("native-file-data"); Directory.CreateDirectory(folder); File.WriteAllText(Path.Combine(folder, "audit-profile.json"), "{}");
                                foreach (string kind in new[] { "export", "import", "report", "openrgb" })
                                {
                                    string title = L.T(kind == "export" ? "profile.exportTitle" : kind == "import" ? "profile.importTitle" : kind == "report" ? "ui.reportTitle" : "rgb.downloadTitle");
                                    string before = speech.Snapshot(); Exception fileFailure = null; int seen = 0;
                                    using (var timer = new System.Windows.Forms.Timer { Interval = 150 })
                                    using (FileDialog dialog = kind == "import" ? (FileDialog)new OpenFileDialog() : new SaveFileDialog())
                                    {
                                        dialog.Title = title; dialog.InitialDirectory = folder; dialog.FileName = kind == "report" ? "audit.txt" : kind == "openrgb" ? OpenRgbDownload.FileName : "audit-profile.json"; dialog.RestoreDirectory = true;
                                        timer.Tick += async (sender, args) =>
                                        {
                                            IntPtr window = GetForegroundWindow(); uint pid; GetWindowThreadProcessId(window, out pid);
                                            var cls = new StringBuilder(256); GetClassName(window, cls, cls.Capacity);
                                            if (pid != Process.GetCurrentProcess().Id || cls.ToString() != "#32770") return;
                                            timer.Stop(); seen++;
                                            try
                                            {
                                                await speech.Native(window, before, language + "/file/" + kind, title);
                                                for (int i = 0; i < 3; i++) { string start = speech.Snapshot(); await NativeTab(window); await speech.Native(window, start, language + "/file/" + kind + "/Tab" + i, ""); }
                                            }
                                            catch (Exception ex) { fileFailure = ex; }
                                            finally { SystemDialogs.PostMessage(window, 0x10, IntPtr.Zero, IntPtr.Zero); }
                                        };
                                        timer.Start(); Check(dialog.ShowDialog(form) == DialogResult.Cancel, "File audit did not cancel");
                                        Check(seen == 1, "Native file dialog was not observed"); if (fileFailure != null) throw fileFailure;
                                    }
                                }
                            }
                        }
                        catch (Exception ex) { failure = ex; }
                        finally { SystemDialogs.TestCreated = null; SetField(form, "exitRequested", true); form.Close(); }
                    };
                    Application.Run(form); if (failure != null) throw failure;
                    Check(driver.Writes == 0, "Native dialog audit invoked GPU writes");
                }
            });
            File.AppendAllText(output, "\r\nPASS actual NVDA native-dialog and file-dialog capture; links not invoked, files cancelled, update rejected.\r\n"); return 0;
        }
        catch (Exception ex) { File.AppendAllText(output, "\r\nNOT VERIFIED: " + ex + "\r\n"); return 2; }
    }
}
