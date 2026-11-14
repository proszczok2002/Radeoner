using System;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.CompilerServices;

namespace Radeoner
{
    internal static class Accessibility
    {
        private static readonly ConditionalWeakTable<Control, object> wired = new ConditionalWeakTable<Control, object>();
        internal static void Number(NumericUpDown number)
        {
            number.AccessibleRole = AccessibleRole.SpinButton;
            foreach (Control child in number.Controls)
            {
                child.AccessibleName = number.AccessibleName;
                child.AccessibleDescription = number.AccessibleDescription;
            }
        }
        internal static void FitNativeCombos(Control root)
        {
            if (root.IsDisposed || root.Disposing) return;
            var combo = root as ComboBox;
            if (combo != null)
            {
                int width = combo.MinimumSize.Width;
                combo.MinimumSize = new System.Drawing.Size(width, 0);
                combo.Height = combo.PreferredHeight;
                // Native themed combos can be taller than Framework's cached
                // preferred size. Make their actual height part of layout.
                combo.MinimumSize = new System.Drawing.Size(width, combo.Height);
                var flow = combo.Parent as FlowLayoutPanel;
                if (flow != null) flow.MinimumSize = new System.Drawing.Size(flow.MinimumSize.Width, combo.Height + combo.Margin.Vertical + flow.Padding.Vertical);
                var table = combo.Parent as TableLayoutPanel;
                if (table != null)
                {
                    int row = table.GetRow(combo), height = combo.Height + combo.Margin.Vertical;
                    foreach (Control peer in table.Controls) if (peer != combo && table.GetRow(peer) == row) height = Math.Max(height, peer.Height + peer.Margin.Vertical);
                    while (table.RowStyles.Count <= row) table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    table.RowStyles[row].SizeType = SizeType.Absolute; table.RowStyles[row].Height = height;
                }
            }
            foreach (Control child in root.Controls) FitNativeCombos(child);
        }
        internal static void Order(Control parent)
        {
            int i = 0;
            foreach (Control child in parent.Controls)
            {
                child.TabIndex = i++;
                if (child is Label || child is Panel || child is GroupBox) child.TabStop = false;
                object marker;
                if (!wired.TryGetValue(child, out marker)) { wired.Add(child, new object()); child.Enter += (s, e) => EnsureVisible((Control)s); }
                Order(child);
            }
        }
        internal static void EnsureVisible(Control target)
        {
            if (target == null || target.IsDisposed || !target.Visible) return;
            for (Control parent = target.Parent; parent != null; parent = parent.Parent)
            {
                var scroll = parent as ScrollableControl;
                if (scroll != null && scroll.AutoScroll)
                {
                    scroll.ScrollControlIntoView(target);
                    // Framework may align an oversized containing row instead of
                    // the focused editor. Correct the remaining clipped pixels.
                    var bounds = scroll.RectangleToClient(target.RectangleToScreen(target.ClientRectangle));
                    int dx = bounds.Left < 0 ? bounds.Left : bounds.Right > scroll.ClientSize.Width ? bounds.Right - scroll.ClientSize.Width : 0;
                    int dy = bounds.Top < 0 ? bounds.Top : bounds.Bottom > scroll.ClientSize.Height ? bounds.Bottom - scroll.ClientSize.Height : 0;
                    if (dx != 0 || dy != 0) scroll.AutoScrollPosition = new System.Drawing.Point(Math.Max(0, -scroll.AutoScrollPosition.X + dx), Math.Max(0, -scroll.AutoScrollPosition.Y + dy));
                }
            }
        }
        internal static Control Focused(Control parent)
        {
            foreach (Control child in parent.Controls) if (child.ContainsFocus) return Focused(child);
            return parent.Focused ? parent : null;
        }
    }
}
