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
                if (scroll != null && scroll.AutoScroll) scroll.ScrollControlIntoView(target);
            }
        }
        internal static Control Focused(Control parent)
        {
            foreach (Control child in parent.Controls) if (child.ContainsFocus) return Focused(child);
            return parent.Focused ? parent : null;
        }
    }
}
