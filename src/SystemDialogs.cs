using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Radeoner
{
    internal sealed class DialogAction
    {
        internal int Id;
        internal string Text;
        internal Action<IntPtr> Invoke;
    }
    internal static class SystemDialogs
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        private struct Config
        {
            internal uint Size;
            internal IntPtr Parent, Instance;
            internal uint Flags, CommonButtons;
            [MarshalAs(UnmanagedType.LPWStr)] internal string Title;
            internal IntPtr MainIcon;
            [MarshalAs(UnmanagedType.LPWStr)] internal string Instruction;
            [MarshalAs(UnmanagedType.LPWStr)] internal string Content;
            internal uint ButtonCount;
            internal IntPtr Buttons;
            internal int DefaultButton;
            internal uint RadioCount;
            internal IntPtr Radios;
            internal int DefaultRadio;
            [MarshalAs(UnmanagedType.LPWStr)] internal string Verification;
            [MarshalAs(UnmanagedType.LPWStr)] internal string Expanded;
            [MarshalAs(UnmanagedType.LPWStr)] internal string ExpandedText;
            [MarshalAs(UnmanagedType.LPWStr)] internal string CollapsedText;
            internal IntPtr FooterIcon;
            [MarshalAs(UnmanagedType.LPWStr)] internal string Footer;
            internal Callback Callback;
            internal IntPtr CallbackData;
            internal uint Width;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct NativeButton { internal int Id; internal IntPtr Text; }
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Callback(IntPtr window, uint notification, IntPtr wparam, IntPtr lparam, IntPtr data);
        [DllImport("comctl32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int TaskDialogIndirect(ref Config config, out int button, out int radio, [MarshalAs(UnmanagedType.Bool)] out bool verification);
        [DllImport("user32.dll")] internal static extern bool PostMessage(IntPtr window, uint message, IntPtr wparam, IntPtr lparam);
        internal static Action<IntPtr> TestCreated;
        internal static Func<string, string, bool, int> TestShow;
        internal static int Show(IWin32Window owner, string title, string instruction, string content, bool question, params DialogAction[] actions)
        {
            if (TestShow != null) return TestShow(instruction, content, question);
            var strings = new List<IntPtr>(); IntPtr buttons = IntPtr.Zero;
            try
            {
                int size = Marshal.SizeOf(typeof(NativeButton));
                if (actions.Length > 0)
                {
                    buttons = Marshal.AllocHGlobal(actions.Length * size);
                    for (int i = 0; i < actions.Length; i++)
                    {
                        var text = Marshal.StringToHGlobalUni(actions[i].Text); strings.Add(text);
                        Marshal.StructureToPtr(new NativeButton { Id = actions[i].Id, Text = text }, IntPtr.Add(buttons, i * size), false);
                    }
                }
                Callback callback = (window, notification, wp, lp, data) =>
                {
                    if (notification == 0 && TestCreated != null) TestCreated(window);
                    if (notification == 2)
                        foreach (var action in actions) if (wp.ToInt32() == action.Id && action.Invoke != null)
                        {
                            try { action.Invoke(window); }
                            catch (Exception ex) { MessageBox.Show(new WindowOwner(window), ex.Message, "Radeoner", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                            return 1; // Keep the native dialog open after a link/action.
                        }
                    return 0;
                };
                var config = new Config { Size = (uint)Marshal.SizeOf(typeof(Config)), Parent = owner == null ? IntPtr.Zero : owner.Handle,
                    Flags = 8 | 0x1000, CommonButtons = question ? 2u | 4u : 32u, Title = title, Instruction = instruction, Content = content,
                    ButtonCount = (uint)actions.Length, Buttons = buttons, DefaultButton = question ? 7 : 8, Callback = callback, Width = 460 };
                int selected, radio; bool verify;
                int result = TaskDialogIndirect(ref config, out selected, out radio, out verify);
                GC.KeepAlive(callback);
                if (result < 0) Marshal.ThrowExceptionForHR(result);
                return selected;
            }
            catch (EntryPointNotFoundException) { return Fallback(owner, title, instruction, content, question); }
            catch (System.Runtime.InteropServices.COMException) { return Fallback(owner, title, instruction, content, question); }
            finally { foreach (var ptr in strings) Marshal.FreeHGlobal(ptr); if (buttons != IntPtr.Zero) Marshal.FreeHGlobal(buttons); }
        }
        private static int Fallback(IWin32Window owner, string title, string instruction, string content, bool question)
        {
            return (int)MessageBox.Show(owner, instruction + "\r\n\r\n" + content, title, question ? MessageBoxButtons.YesNo : MessageBoxButtons.OK,
                question ? MessageBoxIcon.Question : MessageBoxIcon.Information, question ? MessageBoxDefaultButton.Button2 : MessageBoxDefaultButton.Button1);
        }
        internal static bool Confirm(IWin32Window owner, string text, string title)
        { return Show(owner, title, title, text, true) == 6; }
        internal static void Info(IWin32Window owner, string text, string title)
        { Show(owner, title, title, text, false); }
        internal static void Details(IWin32Window owner, string title, string text)
        {
            using (var form = new Form { Text = title, AccessibleName = title, Size = new System.Drawing.Size(700, 540), MinimumSize = new System.Drawing.Size(400, 300), StartPosition = FormStartPosition.CenterParent, ShowInTaskbar = false })
            {
                form.AutoScaleMode = AutoScaleMode.Dpi;
                foreach (Form open in Application.OpenForms) if (open is MainForm) { form.Font = open.Font; break; }
                var box = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Text = text, AccessibleName = title, SelectionStart = 0, SelectionLength = 0 };
                var close = new Button { Text = L.T("dialog.close"), AccessibleName = L.T("dialog.closeName"), Dock = DockStyle.Bottom, Height = 40, DialogResult = DialogResult.Cancel };
                form.Controls.Add(box); form.Controls.Add(close); form.CancelButton = close;
                form.Shown += (s, e) => { box.Focus(); box.Select(0, 0); }; form.ShowDialog(owner);
            }
        }
        internal sealed class WindowOwner : IWin32Window
        {
            private readonly IntPtr window;
            internal WindowOwner(IntPtr handle) { window = handle; }
            public IntPtr Handle { get { return window; } }
        }
    }
}
