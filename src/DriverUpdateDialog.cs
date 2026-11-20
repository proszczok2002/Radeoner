using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
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
        private readonly Button download = Action("amd.download"), run = Action("amd.run"), website = Action("amd.website"), close = Action("dialog.close");
        private readonly string introduction;
        private CancellationTokenSource operation;
        private string installerPath, installerHash;
        private bool closing;

        internal DriverUpdateDialog(GpuInfo gpu, Font font, string theme)
        {
            Text = AccessibleName = L.T("amd.title");
            Font = font;
            AutoScaleMode = AutoScaleMode.Dpi;
            Size = new Size(820, 550);
            MinimumSize = new Size(480, 390);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;

            introduction = (gpu == null ? L.T("amd.noGpu") : L.T("amd.installed", gpu.Name, gpu.DriverDescription))
                + "\r\n\r\n" + L.T("amd.instructions");
            details.AccessibleName = L.T("amd.details");
            details.Text = introduction;
            var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 2 };
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
            actions.Controls.AddRange(new Control[] { download, run, website, close });
            table.Controls.Add(details, 0, 0);
            table.Controls.Add(actions, 0, 1);
            Controls.Add(table);
            close.DialogResult = DialogResult.Cancel;
            CancelButton = close;
            download.Click += async (s, e) => await Download();
            run.Click += async (s, e) => await RunInstaller();
            website.Click += (s, e) =>
            {
                try { using (Process.Start(new ProcessStartInfo(AmdInstaller.SupportUrl) { UseShellExecute = true })) { } }
                catch (Exception ex) { ShowStatus(L.T("amd.error", ex.Message)); }
            };
            FormClosing += (s, e) => { closing = true; if (operation != null) operation.Cancel(); };
            Action fitButtons = () =>
            {
                int width = table.ClientSize.Width - table.Padding.Horizontal - actions.Margin.Horizontal;
                foreach (Button button in actions.Controls) button.MaximumSize = new Size(Math.Max(1, width - button.Margin.Horizontal - 4), 0);
            };
            SizeChanged += (s, e) => fitButtons();
            Shown += (s, e) => { fitButtons(); details.Focus(); details.Select(0, 0); };
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
            download.Enabled = website.Enabled = operation == null;
            run.Enabled = operation == null && installerPath != null;
        }

        private void ShowStatus(string message)
        {
            details.Text = message + "\r\n\r\n" + introduction;
            details.Focus();
            details.Select(0, 0);
        }

        private async Task Download()
        {
            if (operation != null) return;
            string destination;
            using (var save = new SaveFileDialog
            {
                Title = L.T("amd.download"), Filter = L.T("amd.filter"), FileName = "AMD-Auto-Detect.exe",
                DefaultExt = "exe", AddExtension = true, CheckPathExists = true, OverwritePrompt = true
            })
            {
                if (save.ShowDialog(this) != DialogResult.OK) return;
                destination = save.FileName;
            }
            var cancellation = operation = new CancellationTokenSource();
            UpdateActions();
            ShowStatus(L.T("amd.downloading", 0));
            long last = 0;
            var progress = new Progress<long>(bytes =>
            {
                long mb = bytes / (1024 * 1024);
                if (operation != cancellation || closing || mb <= last) return;
                last = mb;
                details.Text = L.T("amd.downloading", mb) + "\r\n\r\n" + introduction;
            });
            string result;
            try
            {
                string hash = await AmdInstaller.DownloadAsync(destination, progress, cancellation.Token);
                installerPath = destination;
                installerHash = hash;
                result = L.T("amd.downloaded");
            }
            catch (Exception ex)
            {
                result = cancellation.IsCancellationRequested ? L.T("amd.cancelled") : L.T("amd.error", ex.Message);
            }
            finally
            {
                cancellation.Dispose();
                operation = null;
            }
            if (!closing) { UpdateActions(); ShowStatus(result); }
        }

        private async Task RunInstaller()
        {
            if (operation != null || installerPath == null) return;
            if (!SystemDialogs.Confirm(this, L.T("amd.confirm"), Text)) return;
            var cancellation = operation = new CancellationTokenSource();
            var token = cancellation.Token;
            UpdateActions();
            ShowStatus(L.T("amd.verifying"));
            string result;
            try
            {
                await Task.Run(() => AmdInstaller.Launch(installerPath, installerHash, token), token);
                result = L.T("amd.started");
            }
            catch (Exception ex) { result = L.T("amd.error", ex.Message); }
            finally
            {
                cancellation.Dispose();
                operation = null;
            }
            if (!closing) { UpdateActions(); ShowStatus(result); }
        }

        protected override bool ProcessCmdKey(ref Message message, Keys keys)
        {
            if (keys == Keys.F6) { details.Focus(); details.Select(0, 0); return true; }
            return base.ProcessCmdKey(ref message, keys);
        }
    }
}
