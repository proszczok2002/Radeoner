using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Linq;
using System.Windows.Forms;
using Radeoner;

internal static partial class Tests
{
    [StructLayout(LayoutKind.Explicit, Size = 40)]
    private struct KeyInput
    {
        [FieldOffset(0)] internal uint Type;
        [FieldOffset(8)] internal ushort Key;
        [FieldOffset(12)] internal uint Flags;
    }
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int command);
    [DllImport("user32.dll")] private static extern uint SendInput(uint count, KeyInput[] input, int size);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    private static KeyInput Key(ushort code, bool up = false) { return new KeyInput { Type = 1, Key = code, Flags = up ? 2u : 0 }; }
    private static bool UserModifierHeld() { return new[] { 0x10, 0x11, 0x12, 0x5B, 0x5C, 0x2D, 0x14 }.Any(k => (GetAsyncKeyState(k) & 0x8000) != 0); }
    private static async Task KeyboardTab(MainForm form, bool reverse)
    {
        for (int wait = 0; wait < 30 && UserModifierHeld(); wait++) await Task.Delay(100);
        Check(GetForegroundWindow() == form.Handle, "Interactive tour lost its own foreground window; no keys sent");
        Check(!UserModifierHeld(), "A user modifier is held; no keys sent");
        // After foreground activation the GUI queue can retain an old Alt state
        // although the hardware key is up. Clear only this thread's cached bits.
        var state = new byte[256]; GetKeyboardState(state);
        foreach (int key in new[] { 0x10, 0x11, 0x12, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5 }) state[key] &= 0x7F;
        SetKeyboardState(state);
        var keys = reverse ? new[] { Key(0x11), Key(0x10), Key(9), Key(9, true), Key(0x10, true), Key(0x11, true) } : new[] { Key(0x11), Key(9), Key(9, true), Key(0x11, true) };
        try { Check(SendInput((uint)keys.Length, keys, Marshal.SizeOf(typeof(KeyInput))) == keys.Length, "Windows refused keyboard input"); }
        finally { SendInput(2, new[] { Key(0x10, true), Key(0x11, true) }, Marshal.SizeOf(typeof(KeyInput))); }
    }
    private static async Task<string> VisibleKeyboardTour(MainForm form)
    {
        Call(form, "ShowFromTray"); form.Opacity = 1; form.Location = new System.Drawing.Point(100, 100); ShowWindow(form.Handle, 1); form.Activate(); SetForegroundWindow(form.Handle);
        await Task.Delay(400);
        // Wait for manual activation when Windows refuses the request. Do not
        // send an activation key to whichever application currently owns focus.
        for (int wait = 0; wait < 300 && GetForegroundWindow() != form.Handle; wait++) await Task.Delay(100);
        Check(GetForegroundWindow() == form.Handle, "Windows did not grant foreground to the test window");
        var tabs = Field<TabControl>(form, "tabs"); tabs.SelectedIndex = 0; Field<ComboBox>(form, "gpu").Focus();
        for (int i = 0; i < tabs.TabCount; i++)
        {
            int before = tabs.SelectedIndex; await KeyboardTab(form, false); await Task.Delay(450);
            Check(tabs.SelectedIndex == (before + 1) % tabs.TabCount && tabs.Focused, "Real Ctrl Tab input failed");
            int page = (int)tabs.SelectedTab.Tag;
            if (page == 1 && Field<Button>(form, "rgb").Visible) Field<Button>(form, "rgb").Focus();
            else if (page == 3) Field<TextBox>(form, "report").Focus();
            else if (page == 4) { var language = Field<ComboBox>(form, "language"); language.Focus(); language.DroppedDown = true; }
            else if (page == 0) Field<SensorView>(form, "overview").Focus();
        }
        for (int i = 0; i < tabs.TabCount; i++)
        { int before = tabs.SelectedIndex; await KeyboardTab(form, true); await Task.Delay(450); Check(tabs.SelectedIndex == (before + tabs.TabCount - 1) % tabs.TabCount && tabs.Focused, "Real Ctrl Shift Tab input failed"); }
        Call(form, "SelectPage", 4); int dialogs = 0; long maximum = 0; var watch = new Stopwatch();
        try
        {
            SystemDialogs.TestCreated = window =>
            {
                maximum = Math.Max(maximum, watch.ElapsedMilliseconds); dialogs++;
                var close = new System.Windows.Forms.Timer { Interval = 1200 };
                close.Tick += (s, e) => { close.Stop(); close.Dispose(); SystemDialogs.PostMessage(window, 0x466, new IntPtr(8), IntPtr.Zero); }; close.Start();
            };
            watch.Restart(); Field<Button>(form, "aboutProgram").PerformClick();
            watch.Restart(); Field<Button>(form, "supportProject").PerformClick();
            Check(dialogs == 2, "Visible About/support dialogs were not created");
        }
        finally { SystemDialogs.TestCreated = null; }
        bool nvda = Process.GetProcessesByName("nvda").Length > 0;
        return "Real SendInput Ctrl+Tab/Ctrl+Shift+Tab: PASS; visible native About/support dialogs: PASS; maximum create time: " + maximum + " ms; NVDA running: " + nvda + ". Speech output was not captured or listened to.";
    }
}
