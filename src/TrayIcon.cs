using System;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Radeoner
{
    public static class TrayText
    {
        private static string Value(Snapshot snapshot, string id, string unit)
        {
            double value;
            return snapshot != null && snapshot.MetricValues.TryGetValue(id, out value)
                ? value.ToString("0.#", L.Culture) + " " + unit : L.T("tray.missing");
        }
        public static string Format(Snapshot snapshot, bool stale)
        {
            if (snapshot == null) return "Radeoner — " + L.T("tray.waiting");
            string suffix = L.T("tray.sensors", Value(snapshot, "temperature", "°C"), Value(snapshot, "fan", "RPM"),
                Value(snapshot, snapshot.MetricValues.ContainsKey("boardPower") ? "boardPower" : "power", "W"));
            if (stale) suffix += ", " + L.T("tray.lastRead", snapshot.CapturedAt.ToLocalTime().ToString("HH:mm"));
            const string prefix = "Radeoner, ";
            int budget = Math.Max(1, 127 - prefix.Length - 2 - suffix.Length);
            string name = snapshot.Gpu.Name.Replace('\r', ' ').Replace('\n', ' ');
            if (name.Length > budget) name = name.Substring(0, Math.Max(0, budget - 1)) + "…";
            string text = prefix + name + ", " + suffix;
            return text.Length > 127 ? text.Substring(0, 126) + "…" : text;
        }
    }
    // A native shell notification icon supports 127 Unicode characters. .NET Framework
    // NotifyIcon.Text truncates at 63, which cannot hold the GPU name and sensor data.
    internal sealed class TrayIcon : NativeWindow, IDisposable
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct IconData
        {
            internal uint Size;
            internal IntPtr Window;
            internal uint Id, Flags, Callback;
            internal IntPtr Icon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] internal string Tip;
            internal uint State, StateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] internal string Info;
            internal uint Version;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] internal string InfoTitle;
            internal uint InfoFlags;
            internal Guid Guid;
            internal IntPtr BalloonIcon;
        }
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "Shell_NotifyIconW")]
        [return: MarshalAs(UnmanagedType.Bool)] private static extern bool Notify(uint message, ref IconData data);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint RegisterWindowMessage(string name);
        [DllImport("user32.dll")] private static extern bool SendNotifyMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
        [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr icon);
        private const uint CallbackMessage = 0x8001;
        private static readonly uint ShowMessage = RegisterWindowMessage("Radeoner.ShowWindow.v1");
        private readonly uint taskbarCreated = RegisterWindowMessage("TaskbarCreated");
        private readonly ContextMenuStrip menu;
        private readonly Icon icon;
        private bool added, disposed;
        private string text = "Radeoner";
        internal event Action ShowRequested;
        internal bool Visible { get { return added; } }
        internal TrayIcon(ContextMenuStrip contextMenu)
        {
            menu = contextMenu; icon = CreateIcon();
            CreateHandle(new CreateParams { Caption = "Radeoner.Tray", Style = 0 });
        }
        private static Icon CreateIcon()
        {
            using (var bitmap = new Bitmap(32, 32))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var brush = new SolidBrush(Color.FromArgb(178, 35, 50)))
            using (var font = new Font(SystemFonts.MessageBoxFont.FontFamily, 19, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                graphics.Clear(Color.Transparent); graphics.FillEllipse(brush, 1, 1, 30, 30);
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    graphics.DrawString("R", font, Brushes.White, new RectangleF(0, 0, 32, 32), format);
                IntPtr handle = bitmap.GetHicon();
                try { using (var temporary = Icon.FromHandle(handle)) return (Icon)temporary.Clone(); }
                finally { DestroyIcon(handle); }
            }
        }
        private IconData Data(uint flags)
        {
            return new IconData { Size = (uint)Marshal.SizeOf(typeof(IconData)), Window = Handle, Id = 1, Flags = flags,
                Callback = CallbackMessage, Icon = icon.Handle, Tip = text, Info = "", InfoTitle = "" };
        }
        internal bool EnsureVisible()
        {
            if (disposed) return false;
            if (!added) { var data = Data(1 | 2 | 4); added = Notify(0, ref data); }
            return added;
        }
        internal void Update(string value)
        {
            text = value.Length <= 127 ? value : value.Substring(0, 127);
            if (EnsureVisible()) { var data = Data(4); if (!Notify(1, ref data)) added = false; }
        }
        internal void Notice(string title, string body)
        {
            if (!EnsureVisible()) return;
            var data = Data(16); data.InfoTitle = title.Length > 63 ? title.Substring(0, 63) : title;
            data.Info = body.Length > 255 ? body.Substring(0, 255) : body; data.InfoFlags = 1; Notify(1, ref data);
        }
        internal static void SignalShow() { SendNotifyMessage(new IntPtr(0xffff), ShowMessage, IntPtr.Zero, IntPtr.Zero); }
        protected override void WndProc(ref Message m)
        {
            if ((uint)m.Msg == taskbarCreated) { added = false; EnsureVisible(); }
            else if ((uint)m.Msg == ShowMessage) { if (ShowRequested != null) ShowRequested(); }
            else if ((uint)m.Msg == CallbackMessage)
            {
                int action = m.LParam.ToInt32();
                if (action == 0x0202 || action == 0x0203 || action == 0x0400 || action == 0x0401)
                { if (ShowRequested != null) ShowRequested(); }
                else if (action == 0x0205 || action == 0x007b)
                { SetForegroundWindow(Handle); menu.Show(Cursor.Position); }
            }
            base.WndProc(ref m);
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (added) { var data = Data(0); Notify(2, ref data); added = false; }
            DestroyHandle(); icon.Dispose();
        }
    }
}
