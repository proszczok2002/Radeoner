using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Radeoner
{
    internal sealed class DriverUpdateDialog : Form
    {
        private readonly TextBox details = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
        private readonly TextBox page = new TextBox { Dock = DockStyle.Fill };
        private readonly Button check = Action("amd.check"), download = Action("amd.prepare"), install = Action("amd.install"),
            website = Action("amd.website"), terms = Action("amd.license"), close = Action("dialog.close");
        private readonly string introduction, instanceId;
        private readonly Version installedVersion;
        private readonly bool draftChanges;
        private CancellationTokenSource currentWork;
        private AmdDriverOffer offer;
        private AmdDriverPackage prepared;
        private bool installing, closeAfterWork;
        internal bool RefreshDriver { get; private set; }

        internal DriverUpdateDialog(GpuInfo gpu, Font font, string theme, bool draftChanges = false)
        {
            this.draftChanges = draftChanges;
            instanceId = gpu == null ? null : gpu.Id;
            Version.TryParse(gpu == null ? null : gpu.Driver, out installedVersion);
            Text = AccessibleName = L.T("amd.title");
            Font = font;
            AutoScaleMode = AutoScaleMode.Dpi;
            Size = new Size(820, 580);
            MinimumSize = new Size(500, 430);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;

            introduction = (gpu == null ? L.T("amd.noGpu") : L.T("amd.installed", gpu.Name, gpu.DriverDescription))
                + "\r\n\r\n" + L.T("amd.instructions");
            details.AccessibleName = L.T("amd.details");
            details.Text = introduction;
            page.AccessibleName = L.T("amd.page");
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(12), ColumnCount = 1, RowCount = 4
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, Math.Max(200, Font.Height * 5)));
            for (int i = 0; i < 3; i++) table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
            actions.Controls.AddRange(new Control[] { check, terms, download, install, website, close });
            var label = new Label { Text = page.AccessibleName, AutoSize = true };
            table.Controls.Add(details, 0, 0);
            table.Controls.Add(label, 0, 1);
            table.Controls.Add(page, 0, 2);
            table.Controls.Add(actions, 0, 3);
            scroll.Controls.Add(table);
            Controls.Add(scroll);
            CancelButton = close;
            close.Click += (s, e) => Close();
            website.Click += (s, e) => OpenSite(AmdDriverPackage.SupportUrl);
            terms.Click += (s, e) => OpenSite(AmdDriverPackage.LicenseUrl);
            check.Click += async (s, e) => await Check();
            download.Click += async (s, e) => await Prepare();
            install.Click += async (s, e) => await Install();
            page.TextChanged += (s, e) => { offer = null; ClearPrepared(); UpdateActions(); };
            FormClosing += (s, e) =>
            {
                if (currentWork == null) return;
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    if (!installing) { closeAfterWork = true; currentWork.Cancel(); }
                }
                else if (!installing) currentWork.Cancel();
            };
            Action fit = () =>
            {
                int width = table.ClientSize.Width - table.Padding.Horizontal - actions.Margin.Horizontal;
                label.MaximumSize = new Size(Math.Max(1, width - label.Margin.Horizontal), 0);
                foreach (Button button in actions.Controls)
                    button.MaximumSize = new Size(Math.Max(1, width - button.Margin.Horizontal - 4), 0);
            };
            scroll.ClientSizeChanged += (s, e) => fit();
            Shown += (s, e) => { fit(); FocusDetails(); };
            Shown += async (s, e) =>
            {
                string suggestion = await Task.Run(() => AmdDriverPackage.SuggestedPage(gpu));
                if (!IsDisposed && currentWork == null && page.TextLength == 0) page.Text = suggestion;
            };
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
            bool ready = currentWork == null;
            page.ReadOnly = !ready;
            check.Enabled = ready && AmdDisplayInf.IsAmdInstance(instanceId);
            download.Enabled = ready && offer != null && prepared == null && !RefreshDriver
                && (installedVersion == null || offer.StoreVersions.Length == 0 || offer.StoreVersions.Any(v => v > installedVersion));
            install.Enabled = ready && prepared != null && !RefreshDriver;
            terms.Enabled = website.Enabled = ready;
            close.Enabled = !installing;
        }

        private void FocusDetails() { details.Focus(); details.Select(0, 0); }
        private void Status(string message, bool focus = true)
        {
            details.Text = message + "\r\n\r\n" + introduction;
            if (focus) FocusDetails();
            else details.Select(0, 0);
        }

        private void OpenSite(string url)
        {
            try { using (Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })) { } }
            catch (Win32Exception ex) { Status(L.T("amd.error", ex.Message)); }
        }

        private async Task Run(string initial, Func<CancellationToken, Task<string>> work)
        {
            using (var cancellation = new CancellationTokenSource())
            {
                currentWork = cancellation;
                UpdateActions();
                Status(initial);
                string message;
                try { message = await work(cancellation.Token); }
                catch (OperationCanceledException) { message = L.T("amd.cancelled"); }
                catch (Win32Exception ex) { message = ex.NativeErrorCode == 1223 ? L.T("amd.cancelled") : L.T("amd.error", ex.Message); }
                catch (Exception ex) { message = L.T("amd.error", ex.Message); }
                if (IsDisposed) return;
                currentWork = null; installing = false;
                if (closeAfterWork) { Close(); return; }
                UpdateActions();
                Status(message);
                SystemDialogs.Info(this, message, Text);
            }
        }

        private Task Check()
        {
            Uri address;
            if (!Uri.TryCreate(page.Text.Trim(), UriKind.Absolute, out address) || !AmdDriverPackage.IsProductPage(address))
            {
                Status(L.T("amd.pageError")); SystemDialogs.Info(this, L.T("amd.pageError"), Text); page.Focus();
                return Task.FromResult(0);
            }
            ClearPrepared(); offer = null;
            return Run(L.T("amd.checking"), async token =>
            {
                offer = await AmdDriverPackage.Check(address, token);
                string versions = offer.StoreVersions.Length == 0 ? L.T("common.unavailable") : string.Join(", ", offer.StoreVersions.Select(v => v.ToString()));
                string message = L.T("amd.available", offer.Release, versions);
                if (installedVersion != null && offer.StoreVersions.Length > 0 && offer.StoreVersions.All(v => v <= installedVersion))
                    message += "\r\n" + L.T("amd.current");
                return message;
            });
        }

        private Task Prepare()
        {
            var found = offer;
            if (found == null || !SystemDialogs.Confirm(this, L.T("amd.agree", found.Release, AmdDriverPackage.LicenseUrl), Text))
                return Task.FromResult(0);
            return Run(L.T("amd.downloading", 0), async token =>
            {
                AmdDriverPackage stage = new AmdDriverPackage();
                try
                {
                    var progress = new Progress<long>(bytes =>
                    {
                        if (!token.IsCancellationRequested && currentWork != null && !IsDisposed) Status(L.T("amd.downloading", bytes), false);
                    });
                    await stage.Download(found, progress, token);
                    if (!IsDisposed) Status(L.T("amd.verifying"));
                    await Task.Run(() => stage.Prepare(instanceId, null, token));
                    token.ThrowIfCancellationRequested();
                    if (stage.Driver == null) return L.T("amd.incompatible");
                    if (!stage.Driver.Newer) return L.T("amd.notNewer", stage.Driver.Version, stage.Driver.CurrentVersion);
                    prepared = stage; stage = null;
                    return L.T("amd.ready", prepared.Driver.CurrentVersion, prepared.Driver.Version);
                }
                finally { if (stage != null) stage.Dispose(); }
            });
        }

        private Task Install()
        {
            var selected = prepared;
            if (selected == null) return Task.FromResult(0);
            string question = L.T("amd.confirm", selected.Driver.Version);
            if (draftChanges) question += "\r\n\r\n" + L.T("amd.discardDraft");
            if (!SystemDialogs.Confirm(this, question, Text)) return Task.FromResult(0);
            installing = true;
            return Run(L.T("amd.installing"), async token =>
            {
                int result;
                try { result = await Task.Run(() => AmdDriverPackage.RequestInstall(instanceId, selected)); }
                finally { ClearPrepared(); }
                RefreshDriver = result == AmdDisplayInf.Installed || result == AmdDisplayInf.RestartRequired;
                switch (result)
                {
                    case AmdDisplayInf.Installed: return L.T("amd.installedOk");
                    case AmdDisplayInf.RestartRequired: return L.T("amd.restart");
                    case AmdDisplayInf.Changed: return L.T("amd.changed");
                    case AmdDisplayInf.NotBetter: return L.T("amd.current");
                    default: return L.T("amd.error", "0x" + result.ToString("X8"));
                }
            });
        }

        private void ClearPrepared()
        {
            if (prepared == null) return;
            prepared.Dispose(); prepared = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !installing) ClearPrepared();
            base.Dispose(disposing);
        }

        protected override bool ProcessCmdKey(ref Message message, Keys keys)
        {
            if (keys == Keys.F6) { FocusDetails(); return true; }
            return base.ProcessCmdKey(ref message, keys);
        }
    }
}
