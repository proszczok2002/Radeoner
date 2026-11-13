using System;
using System.Linq;
using System.Windows.Forms;

namespace Radeoner
{
    internal static class Accessibility
    {
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
                Order(child);
            }
        }
        internal static Control Focused(Control parent)
        {
            foreach (Control child in parent.Controls) if (child.ContainsFocus) return Focused(child);
            return parent.Focused ? parent : null;
        }
    }
}
