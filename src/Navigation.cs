using System;
using System.Text;
using System.Windows.Forms;

namespace Radeoner
{
    internal sealed class NavigationComboBox : ComboBox
    {
        internal Action<int> Navigate;
        protected override bool ProcessCmdKey(ref Message msg, Keys keys)
        {
            int direction = NavigationTabs.Direction(keys);
            if (direction != 0 && Navigate != null) { DroppedDown = false; Navigate(direction); return true; }
            return base.ProcessCmdKey(ref msg, keys);
        }
    }
    internal sealed class NavigationTabs : TabControl
    {
        internal Action<int> Navigate;
        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);
            // Preserve the native tab provider and keyboard handling.
            bool print = message.Msg == 0x0317 || message.Msg == 0x0318;
            if ((message.Msg == 0x000F || print && message.WParam != IntPtr.Zero) && DrawMode == TabDrawMode.OwnerDrawFixed && TabCount > 0 && !IsDisposed && !Disposing && !Radeoner.Appearance.HighContrast)
            {
                var last = GetTabRect(TabCount - 1);
                if (last.Right < ClientSize.Width - 8)
                    using (var graphics = print ? System.Drawing.Graphics.FromHdc(message.WParam) : System.Drawing.Graphics.FromHwnd(Handle))
                    using (var brush = new System.Drawing.SolidBrush(BackColor)) graphics.FillRectangle(brush, last.Right + 1, 0, ClientSize.Width - last.Right - 1, last.Bottom + 1);
            }
        }
        internal static int Direction(Keys keys)
        { return keys == (Keys.Control | Keys.Tab) ? 1 : keys == (Keys.Control | Keys.Shift | Keys.Tab) ? -1 : 0; }
        protected override bool ProcessCmdKey(ref Message msg, Keys keys)
        {
            int direction = Direction(keys);
            if (direction != 0 && Navigate != null) { Navigate(direction); return true; }
            return base.ProcessCmdKey(ref msg, keys);
        }
    }
    internal sealed class SensorView : ListView
    {
        private string summary = "";
        internal SensorView()
        {
            View = View.Details; FullRowSelect = true; HideSelection = false; MultiSelect = true;
            HeaderStyle = ColumnHeaderStyle.Nonclickable; Dock = DockStyle.Fill;
            Columns.Add("", 260); Columns.Add("", 500);
            SizeChanged += (s, e) => { if (!Disposing && !IsDisposed && Columns.Count >= 2 && ClientSize.Width > 80) { Columns[0].Width = Math.Max(100, ClientSize.Width * 45 / 100); Columns[1].Width = Math.Max(100, ClientSize.Width - Columns[0].Width - 24); } };
        }
        internal void Localize()
        { Columns[0].Text = L.T("sensors.parameter"); Columns[1].Text = L.T("sensors.value"); AccessibleName = L.T("ui.overviewName"); }
        public override string Text
        {
            get { return summary; }
            set
            {
                summary = value ?? ""; string[] rows = summary.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                BeginUpdate();
                try
                {
                    for (int i = 0; i < rows.Length; i++)
                    {
                        int split = rows[i].IndexOf(':'); string name = split < 0 ? rows[i] : rows[i].Substring(0, split);
                        string reading = split < 0 ? "" : rows[i].Substring(split + 1).Trim();
                        if (i >= Items.Count) Items.Add(new ListViewItem(new[] { name, reading }));
                        else { if (Items[i].Text != name) Items[i].Text = name; if (Items[i].SubItems[1].Text != reading) Items[i].SubItems[1].Text = reading; }
                    }
                    while (Items.Count > rows.Length) Items.RemoveAt(Items.Count - 1);
                }
                finally { EndUpdate(); }
            }
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keys)
        {
            if (keys == (Keys.Control | Keys.A)) { foreach (ListViewItem item in Items) item.Selected = true; return true; }
            if (keys == (Keys.Control | Keys.C))
            {
                var text = new StringBuilder(); foreach (ListViewItem item in SelectedItems) text.AppendLine(item.Text + ": " + item.SubItems[1].Text);
                if (text.Length > 0) try { Clipboard.SetText(text.ToString()); } catch (System.Runtime.InteropServices.ExternalException) { }
                return true;
            }
            return base.ProcessCmdKey(ref msg, keys);
        }
    }
}
