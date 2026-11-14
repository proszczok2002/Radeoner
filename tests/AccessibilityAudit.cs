using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Radeoner;
using Appearance = Radeoner.Appearance;

internal static partial class Tests
{
    private static void RunAccessibilityTests()
    {
        Test("Five native tabs, Settings actions and inner numeric edit labels in both languages", () => WithDriver((driver, backend) =>
        {
            using (var form = TestForm("accessibility"))
            {
                SetField(form, "snapshot", backend.Read(0)); Call(form, "Render");
                var tabs = Field<TabControl>(form, "tabs");
                Check(tabs.TabPages.Count == 5 && tabs.TabPages[3].Text == "Raport" && tabs.TabPages[4].Text == "Ustawienia", "Tab captions/count incorrect");
                foreach (var number in Descendants(form).OfType<NumericUpDown>())
                {
                    var edit = number.Controls.OfType<TextBox>().Single();
                    Check(edit.AccessibleName == number.AccessibleName && edit.AccessibleDescription == number.AccessibleDescription, "Inner numeric edit lost label/range");
                    Check(number.AccessibilityObject.Role == AccessibleRole.Client && edit.AccessibilityObject.Role == AccessibleRole.SpinButton, "Numeric focus role repeats the labelled container or loses numeric semantics");
                }
                Check(Field<Button>(form, "aboutProgram").AccessibleName == L.T("info.aboutName") && Field<Button>(form, "supportProject").TabStop, "Information actions inaccessible");
                Field<ComboBox>(form, "language").SelectedIndex = 1;
                Check(tabs.TabPages[4].AccessibilityObject.Name == "Settings" && Field<Button>(form, "supportProject").AccessibleName == L.T("info.supportName"), "Settings not translated in MSAA");
                Field<ComboBox>(form, "language").SelectedIndex = 0;
                SetField(form, "started", true); form.Opacity = 0; form.ShowInTaskbar = false; tabs.SelectedIndex = 1; form.Show();
                var fields = Field<Dictionary<string, NumericUpDown>>(form, "editors");
                fields["gfx.min"].Focus(); var inner = fields["gfx.min"].Controls.OfType<TextBox>().Single(); inner.Select(1, 2);
                Call(form, "SetBusy", true); Call(form, "Render"); Call(form, "SetBusy", false);
                var restored = Field<Dictionary<string, NumericUpDown>>(form, "editors")["gfx.min"];
                Check(restored.ContainsFocus && restored.Controls.OfType<TextBox>().Single().SelectionStart == 1, "Rebuilding disposed numeric edit lost focus/caret");
                SetField(form, "exitRequested", true); form.Close();
                Check(driver.Writes == 0, "Accessibility audit changed GPU");
            }
        }));
        Test("System theme follows Windows application theme and explicit theme overrides it", () =>
        {
            try
            {
                Appearance.TestSystemDark = () => true;
                Check(Appearance.Dark("system") == !SystemInformation.HighContrast && !Appearance.Dark("light"), "Dark system setting ignored");
                Appearance.TestSystemDark = () => false;
                Check(!Appearance.Dark("system") && Appearance.Dark("dark") == !SystemInformation.HighContrast, "Explicit dark override incorrect");
            }
            finally { Appearance.TestSystemDark = null; }
        });
        Test("Update prompt exposes release notes before confirmation and No never downloads", () =>
        {
            using (var form = TestForm("update-prompt"))
            {
                var update = new AvailableUpdate { Manifest = new UpdateManifest { Version = "99.0.0.0", DisplayVersion = "99.0" }, ReleaseNotes = "## Polski\n- **Dostępność** i [kontakt](https://t.me/proszczok)" };
                SetField(form, "availableUpdate", update); bool shown = false;
                try
                {
                    SystemDialogs.TestShow = (title, content, question) => { shown = true; Check(question && content.Contains("Dostępność") && content.Contains("https://t.me/proszczok") && !content.Contains("**"), "Changelog absent or unreadable"); return 7; };
                    // A hidden form must not be shown by the test; only exercise the prompt on a created window.
                    form.Opacity = 0; form.ShowInTaskbar = false; SetField(form, "started", true);
                    ((Task)Call(form, "DownloadAndInstallUpdate")).GetAwaiter().GetResult();
                    Check(shown && !Field<bool>(form, "updateInstalling") && !Field<bool>(form, "busy"), "No began an installation");
                }
                finally { SystemDialogs.TestShow = null; SetField(form, "exitRequested", true); form.Close(); }
            }
            Check(UpdateFeed.ReadableNotes(null) == L.T("update.noNotes"), "Missing notes have no accessible fallback");
        });
    }
    private delegate bool ChildCallback(IntPtr window, IntPtr data);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr window, ChildCallback callback, IntPtr data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder text, int maximum);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int maximum);
    [DllImport("user32.dll")] private static extern int GetDlgCtrlID(IntPtr window);
    [DllImport("oleacc.dll")] private static extern int AccessibleObjectFromWindow(IntPtr window, uint objectId, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out Accessibility.IAccessible accessible);
    private static void AccessibleText(Accessibility.IAccessible node, StringBuilder text, int depth)
    {
        if (depth > 12) return;
        try { text.AppendLine(node.get_accName(0)); } catch (COMException) { } catch (ArgumentException) { }
        try { text.AppendLine(node.get_accValue(0)); } catch (COMException) { } catch (ArgumentException) { }
        int count = Math.Min(500, node.accChildCount);
        for (int i = 1; i <= count; i++)
        {
            try { var child = node.get_accChild(i) as Accessibility.IAccessible; if (child != null) AccessibleText(child, text, depth + 1); else text.AppendLine(node.get_accName(i)); }
            catch (COMException) { } catch (ArgumentException) { }
        }
    }
    private static int LiveDialogs(string output)
    {
        string result; int count = 0; Exception failure = null;
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); L.Load("pl");
        try
        {
            bool question = true;
            SystemDialogs.TestCreated = window =>
            {
                var probe = new System.Windows.Forms.Timer { Interval = 200 };
                probe.Tick += (sender, e) =>
                {
                probe.Stop(); probe.Dispose();
                try
                {
                    count++; var cls = new StringBuilder(256); GetClassName(window, cls, cls.Capacity); Check(cls.ToString() == "#32770", "Not a native dialog");
                    var ids = new List<int>(); var content = new StringBuilder();
                    EnumChildWindows(window, (child, data) => { ids.Add(GetDlgCtrlID(child)); var text = new StringBuilder(16000); GetWindowText(child, text, text.Capacity); content.AppendLine(text.ToString()); return true; }, IntPtr.Zero);
                    var iid = new Guid("618736E0-3C3D-11CF-810C-00AA00389B71"); Accessibility.IAccessible accessible;
                    Check(AccessibleObjectFromWindow(window, unchecked((uint)-4), ref iid, out accessible) == 0, "Native MSAA provider unavailable");
                    var msaa = new StringBuilder(); AccessibleText(accessible, msaa, 0);
                    EnumChildWindows(window, (child, data) => { Accessibility.IAccessible childAccessible; if (AccessibleObjectFromWindow(child, unchecked((uint)-4), ref iid, out childAccessible) == 0) AccessibleText(childAccessible, msaa, 0); return true; }, IntPtr.Zero);
                    content.Append(msaa); string textContent = content.ToString().Replace("&", "");
                    if (question) Check(msaa.ToString().Contains("Tak") && msaa.ToString().Contains("Nie") && msaa.ToString().Contains("Wszystkie zmiany") && msaa.ToString().Contains("Zmiana testowa"), "Prompt has no accessible Yes/No, full-notes button or changelog: " + msaa);
                    else Check(msaa.ToString().Contains(count == 3 ? "PayPal" : L.T("info.source")) && msaa.ToString().Contains(count == 3 ? "Revolut" : L.T("info.website")) && textContent.Contains("Zamknij"), "Native accessible link/close buttons missing: " + textContent);
                }
                catch (Exception ex) { failure = ex; }
                SystemDialogs.PostMessage(window, 0x466, new IntPtr(question ? 7 : 8), IntPtr.Zero);
                };
                probe.Start();
            };
            int answer = SystemDialogs.Show(null, "Radeoner", "Aktualizacja", "Zmiana testowa\r\nCzy zainstalować?", true, new DialogAction { Id = 101, Text = "Wszystkie zmiany" });
            Check(answer == 7, "No did not cancel native prompt"); question = false;
            using (var form = TestForm("native-dialogs"))
            {
                SetField(form, "started", true); form.Opacity = 0; form.ShowInTaskbar = false; Field<TabControl>(form, "tabs").SelectedIndex = 4; form.Show();
                // Invoke registered click handlers without opening a browser or payment page.
                Field<Button>(form, "aboutProgram").PerformClick();
                Field<Button>(form, "supportProject").PerformClick();
                SetField(form, "exitRequested", true); form.Close();
            }
            if (failure != null) throw failure;
            Check(count == 3, "Expected three actual TaskDialog windows, got " + count);
            result = "PASS native Windows TaskDialog: changelog, Yes/No cancellation, About and support/contact buttons. No external links opened.";
        }
        catch (Exception ex) { result = "FAIL " + ex; }
        finally { SystemDialogs.TestCreated = null; }
        File.WriteAllText(output, result); Console.WriteLine(result); return result.StartsWith("PASS") ? 0 : 1;
    }
}
