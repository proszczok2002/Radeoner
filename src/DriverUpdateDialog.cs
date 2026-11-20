using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Radeoner
{
    internal sealed class DriverUpdateDialog : Form
    {
        private readonly TextBox details = new TextBox
        {
            ReadOnly = true, Multiline = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill
        };
        private readonly ComboBox offers = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        private readonly Button search = Action("wud.check"), license = Action("wud.license"),
            install = Action("wud.install"), website = Action("amd.website"), close = Action("dialog.close");
        private readonly string introduction, instanceId, theme;
        private readonly bool draftChanges;
        private DriverOffer reviewed;
        private bool checking, installing, closing;
        internal bool RefreshDriver { get; private set; }

        internal DriverUpdateDialog(GpuInfo gpu, Font font, string theme, bool draftChanges = false)
        {
            this.theme = theme;
            this.draftChanges = draftChanges;
            instanceId = gpu == null ? null : gpu.Id;
            Text = AccessibleName = L.T("amd.title");
            Font = font;
            AutoScaleMode = AutoScaleMode.Dpi;
            Size = new Size(820, 580);
            MinimumSize = new Size(480, 430);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;

            string driver = gpu == null ? L.T("amd.noGpu") : L.T("amd.installed", gpu.Name, gpu.DriverDescription);
            introduction = driver + "\r\n\r\n" + L.T(WindowsDriverUpdates.IsAmdInstance(instanceId) ? "wud.instructions" : "wud.noGpu");
            details.AccessibleName = L.T("amd.details");
            details.Text = introduction;
            offers.AccessibleName = L.T("wud.offers");
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(12), ColumnCount = 1, RowCount = 4
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, Math.Max(208, Font.Height * 5)));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
            actions.Controls.AddRange(new Control[] { search, license, install, website, close });
            table.Controls.Add(details, 0, 0);
            table.Controls.Add(new Label { Text = L.T("wud.offers"), AutoSize = true }, 0, 1);
            table.Controls.Add(offers, 0, 2);
            table.Controls.Add(actions, 0, 3);
            scroll.Controls.Add(table);
            Controls.Add(scroll);
            close.DialogResult = DialogResult.Cancel;
            CancelButton = close;
            search.Click += async (s, e) => await Check();
            install.Click += async (s, e) => await Install();
            license.Click += (s, e) =>
            {
                var offer = offers.SelectedItem as DriverOffer;
                if (offer == null || offer.LicenseAccepted) return;
                using (var dialog = new DetailsDialog(L.T("wud.license"), offer.License, Font, this.theme))
                    dialog.ShowDialog(this);
                reviewed = offer;
                UpdateActions();
            };
            offers.SelectedIndexChanged += (s, e) =>
            {
                reviewed = null;
                var offer = offers.SelectedItem as DriverOffer;
                if (offer != null) SetDetails(Describe(offer));
                UpdateActions();
            };
            website.Click += (s, e) =>
            {
                try { using (Process.Start(new ProcessStartInfo(WindowsDriverUpdates.SupportUrl) { UseShellExecute = true })) { } }
                catch (Exception ex) { FocusStatus(L.T("wud.failed", ex.Message)); }
            };
            FormClosing += (s, e) =>
            {
                if (installing && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; return; }
                closing = true;
            };
            Action fitButtons = () =>
            {
                int width = table.ClientSize.Width - table.Padding.Horizontal - actions.Margin.Horizontal;
                foreach (Button button in actions.Controls) button.MaximumSize = new Size(Math.Max(1, width - button.Margin.Horizontal - 4), 0);
            };
            scroll.ClientSizeChanged += (s, e) => fitButtons();
            Shown += (s, e) => { fitButtons(); Accessibility.FitNativeCombos(this); details.Focus(); details.Select(0, 0); };
            Accessibility.Order(this);
            Appearance.Apply(this, theme);
            Appearance.FollowSystem(this, theme);
            UpdateActions();
        }

        private static Button Action(string key)
        {
            return new ThemedButton
            {
                Text = L.T(key), AccessibleName = L.T(key).Replace("&", ""),
                AutoSize = true, Padding = new Padding(7, 5, 7, 5)
            };
        }

        private void UpdateActions()
        {
            bool ready = !checking && !installing;
            var offer = offers.SelectedItem as DriverOffer;
            search.Enabled = ready && WindowsDriverUpdates.IsAmdInstance(instanceId);
            website.Enabled = ready;
            offers.Enabled = ready && offers.Items.Count > 0;
            license.Enabled = ready && offer != null && !offer.LicenseAccepted;
            install.Enabled = ready && offer != null && (offer.LicenseAccepted || reviewed == offer);
            close.Enabled = !installing;
        }

        private static string Describe(DriverOffer offer)
        {
            return L.T("wud.offer", offer.Title, offer.Date.ToString("d", L.Culture));
        }

        private void SetDetails(string message)
        {
            details.Text = message + "\r\n\r\n" + introduction;
            details.Select(0, 0);
        }

        private void FocusStatus(string message)
        {
            SetDetails(message);
            details.Focus();
        }

        private async Task Check()
        {
            if (!search.Enabled) return;
            checking = true;
            offers.Items.Clear();
            offers.AccessibleDescription = null;
            UpdateActions();
            FocusStatus(L.T("wud.checking"));
            string message;
            try
            {
                var found = await Task.Run(() => WindowsDriverUpdates.Check(instanceId));
                if (closing) return;
                offers.Items.AddRange(found.ToArray());
                message = found.Count == 0 ? L.T("wud.none") : L.T("wud.found", found.Count);
            }
            catch (Exception ex) { message = L.T("wud.failed", ex.Message); }
            finally
            {
                checking = false;
                if (!closing) UpdateActions();
            }
            if (closing) return;
            FocusStatus(message);
            if (offers.Items.Count == 0) SystemDialogs.Info(this, message, Text);
            else
            {
                offers.AccessibleDescription = message;
                offers.Focus();
            }
        }

        private async Task Install()
        {
            var offer = offers.SelectedItem as DriverOffer;
            if (!install.Enabled || offer == null) return;
            string confirm = L.T(offer.LicenseAccepted ? "wud.confirm" : "wud.confirmLicense", offer.Title);
            if (draftChanges) confirm += "\r\n\r\n" + L.T("wud.discardDraft");
            if (!SystemDialogs.Confirm(this, confirm, Text)) return;
            installing = true;
            UpdateActions();
            FocusStatus(L.T("wud.installing"));
            string message;
            try
            {
                int code = await Task.Run(() => WindowsDriverUpdates.RequestInstall(instanceId, offer));
                RefreshDriver |= code == WindowsDriverUpdates.Installed || code == WindowsDriverUpdates.RestartRequired;
                message = InstallStatus(code);
                if (!closing) offers.Items.Clear();
            }
            catch (Win32Exception ex) { message = ex.NativeErrorCode == 1223 ? L.T("wud.cancelled") : L.T("wud.failed", ex.Message); }
            catch (Exception ex) { message = L.T("wud.failed", ex.Message); }
            finally
            {
                installing = false;
                if (!closing) UpdateActions();
            }
            if (!closing)
            {
                FocusStatus(message);
                SystemDialogs.Info(this, message, Text);
            }
        }

        private static string InstallStatus(int code)
        {
            switch (code)
            {
                case WindowsDriverUpdates.Installed: return L.T("wud.installed");
                case WindowsDriverUpdates.RestartRequired: return L.T("wud.restart");
                case WindowsDriverUpdates.OfferChanged: return L.T("wud.changed");
                case WindowsDriverUpdates.WindowsBusy: return L.T("wud.busy");
                default: return L.T("wud.failed", "0x" + code.ToString("X8"));
            }
        }

        protected override bool ProcessCmdKey(ref Message message, Keys keys)
        {
            if (keys == Keys.F6) { details.Focus(); details.Select(0, 0); return true; }
            return base.ProcessCmdKey(ref message, keys);
        }
    }
}
