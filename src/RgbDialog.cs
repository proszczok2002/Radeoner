using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Radeoner
{
    internal sealed class RgbDialog : Form
    {
        private readonly ComboBox devices = new ComboBox(), modes = new ComboBox(), preset = new ComboBox();
        private readonly NumericUpDown port = Number("rgb.port", 1, 65535, 6742), red = Number("rgb.red", 0, 255, 255), green = Number("rgb.green", 0, 255, 255), blue = Number("rgb.blue", 0, 255, 255), brightness = Number("rgb.brightness", 0, 100, 100);
        private readonly Button connect = Button("rgb.connect"), apply = Button("rgb.apply"), off = Button("rgb.off"), close = Button("dialog.closeName");
        private readonly Button download = Button("rgb.download"), cancelDownload = Button("rgb.downloadCancel");
        private CancellationTokenSource downloadCancellation;
        private CancellationTokenSource connectionCancellation;
        private readonly TextBox status = new TextBox { ReadOnly = true, Multiline = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
        private readonly Label dimming = new Label { AutoSize = true, MaximumSize = new Size(680, 0) };
        private OpenRgbClient client;
        private bool busy, changing, fitting;
        private volatile bool closing;
        private readonly int[][] colors = { new[] { 255, 255, 255 }, new[] { 255, 0, 0 }, new[] { 0, 255, 0 }, new[] { 0, 0, 255 }, new[] { 255, 130, 0 }, new[] { 160, 0, 255 } };
        internal RgbDialog(Font font, string theme)
        {
            Text = L.T("rgb.title") + " — Radeoner v" + L.Version; AccessibleName = Text; Font = font;
            AutoScaleMode = AutoScaleMode.Dpi; Size = new Size(800, 700); MinimumSize = new Size(560, 520); StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false;
            var viewport = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), AutoScroll = true, AutoScrollMargin = new Size(0, 16) };
            var layout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, RowCount = 11, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 11; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(viewport); viewport.Controls.Add(layout);
            var instructions = new Label { Text = L.T("rgb.instructions"), AutoSize = true, MaximumSize = new Size(720, 0), Margin = new Padding(3, 3, 3, 12) };
            layout.Controls.Add(instructions, 0, 0); layout.SetColumnSpan(instructions, 2);
            var connection = new FlowLayoutPanel { AutoSize = true, WrapContents = true }; connection.Controls.Add(port); connection.Controls.Add(connect); Row(layout, 1, "rgb.port", connection);
            foreach (var box in new[] { devices, modes, preset }) { box.DropDownStyle = ComboBoxStyle.DropDownList; box.Dock = DockStyle.Fill; }
            devices.AccessibleName = L.T("rgb.device"); modes.AccessibleName = L.T("rgb.mode"); preset.AccessibleName = L.T("rgb.preset");
            Row(layout, 2, "rgb.device", devices); Row(layout, 3, "rgb.mode", modes); Row(layout, 4, "rgb.preset", preset);
            preset.Items.AddRange(new object[] { L.T("rgb.white"), L.T("rgb.red"), L.T("rgb.green"), L.T("rgb.blue"), L.T("rgb.orange"), L.T("rgb.purple"), L.T("rgb.custom") }); preset.SelectedIndex = 0;
            var channels = new FlowLayoutPanel { AutoSize = true, WrapContents = true };
            foreach (var pair in new[] { Tuple.Create("R", red), Tuple.Create("G", green), Tuple.Create("B", blue) })
            {
                var channel = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
                channel.Controls.Add(new Label { Text = pair.Item1, AutoSize = true, Margin = new Padding(3, 6, 3, 3) }); channel.Controls.Add(pair.Item2); channels.Controls.Add(channel);
            }
            Row(layout, 5, "rgb.channels", channels); Row(layout, 6, "rgb.brightness", brightness);
            layout.Controls.Add(dimming, 0, 7); layout.SetColumnSpan(dimming, 2);
            var actions = new FlowLayoutPanel { AutoSize = true, WrapContents = true }; actions.Controls.Add(apply); actions.Controls.Add(off); actions.Controls.Add(close);
            layout.Controls.Add(actions, 0, 8); layout.SetColumnSpan(actions, 2);
            var downloads = new FlowLayoutPanel { AutoSize = true, WrapContents = true }; downloads.Controls.Add(download); downloads.Controls.Add(cancelDownload);
            layout.Controls.Add(downloads, 0, 9); layout.SetColumnSpan(downloads, 2);
            status.AccessibleName = L.T("rgb.status"); status.Text = L.T("rgb.ready"); status.MinimumSize = new Size(0, Math.Max(100, Font.Height * 3)); layout.Controls.Add(status, 0, 10); layout.SetColumnSpan(status, 2);
            connect.Click += async (s, e) => await Connect(); apply.Click += async (s, e) => await Apply(false); off.Click += async (s, e) => await Apply(true);
            download.Click += async (s, e) => await Download(); cancelDownload.Click += (s, e) => { if (downloadCancellation != null) downloadCancellation.Cancel(); };
            close.DialogResult = DialogResult.Cancel; CancelButton = close;
            devices.SelectedIndexChanged += (s, e) => SelectDevice(); modes.SelectedIndexChanged += (s, e) =>
            {
                var mode = modes.SelectedItem as RgbMode;
                brightness.Value = mode != null && mode.NativeBrightness ? (decimal)Math.Max(0, Math.Min(100, Math.Round(((double)mode.Brightness - mode.BrightnessMin) * 100 / (mode.BrightnessMax - mode.BrightnessMin)))) : 100;
                UpdateActions();
            };
            preset.SelectedIndexChanged += (s, e) => { if (preset.SelectedIndex >= colors.Length || preset.SelectedIndex < 0) return; changing = true; var color = colors[preset.SelectedIndex]; red.Value = color[0]; green.Value = color[1]; blue.Value = color[2]; changing = false; };
            foreach (var channel in new[] { red, green, blue }) channel.ValueChanged += (s, e) => { if (!changing) preset.SelectedIndex = colors.Length; };
            FormClosing += (s, e) =>
            {
                closing = true;
                if (downloadCancellation != null) downloadCancellation.Cancel();
                if (connectionCancellation != null) connectionCancellation.Cancel();
                if (client != null) client.Dispose();
            };
            Shown += (s, e) => { Accessibility.FitNativeCombos(this); FitLayout(layout); connect.Focus(); };
            viewport.ClientSizeChanged += (s, e) => FitLayout(layout);
            FitLayout(layout);
            Accessibility.Order(this); Appearance.Apply(this, theme); Appearance.FollowSystem(this, theme); UpdateActions();
        }
        private void FitLayout(TableLayoutPanel layout)
        {
            if (fitting) return; fitting = true; layout.SuspendLayout();
            try
            {
                int width = Math.Max(360, layout.Parent.ClientSize.Width - layout.Parent.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 12);
                layout.MinimumSize = new Size(width, 0); layout.MaximumSize = new Size(width, 0);
                int labelWidth = Math.Min(210, width / 3);
                foreach (Control control in layout.Controls)
                {
                    int available = layout.GetColumnSpan(control) == 2 ? width : layout.GetColumn(control) == 0 ? labelWidth : Math.Max(130, width - labelWidth - 24);
                    if (control is Label || control is FlowLayoutPanel) control.MaximumSize = new Size(available, 0);
                    var flow = control as FlowLayoutPanel;
                    if (flow != null)
                    {
                        flow.MinimumSize = new Size(available, 0);
                        foreach (var button in flow.Controls.OfType<Button>()) button.MaximumSize = new Size(available - 8, 0);
                    }
                }
            }
            finally { layout.ResumeLayout(true); fitting = false; }
        }
        private static Button Button(string key) { return new ThemedButton { Text = L.T(key == "rgb.connect" ? "rgb.connectShort" : key == "rgb.download" ? "rgb.downloadShort" : key), AccessibleName = L.T(key), AutoSize = true, Padding = new Padding(8, 4, 8, 4) }; }
        private static NumericUpDown Number(string key, int min, int max, int value)
        { var control = new NumericUpDown { Minimum = min, Maximum = max, Value = value, Width = 115, AccessibleName = L.T(key), AccessibleDescription = L.T("ui.range", new IntRange(min, max, 1)) }; Accessibility.Number(control); return control; }
        private static void Row(TableLayoutPanel panel, int row, string key, Control control)
        { panel.Controls.Add(new Label { Text = L.T(key), AutoSize = true, Margin = new Padding(3, 7, 12, 6) }, 0, row); panel.Controls.Add(control, 1, row); }
        private void UpdateActions()
        {
            bool selected = devices.SelectedItem is RgbDevice, color = modes.SelectedItem is RgbMode && ((RgbMode)modes.SelectedItem).CanColor;
            connect.Enabled = port.Enabled = !busy; devices.Enabled = modes.Enabled = !busy && selected;
            download.Enabled = !busy; cancelDownload.Visible = downloadCancellation != null; cancelDownload.Enabled = downloadCancellation != null;
            preset.Enabled = red.Enabled = green.Enabled = blue.Enabled = brightness.Enabled = !busy && color;
            apply.Enabled = !busy && color; off.Enabled = !busy && selected && ((RgbDevice)devices.SelectedItem).Modes.Any(m => m.CanColor || string.Equals(m.Name, "Off", StringComparison.OrdinalIgnoreCase));
            dimming.Text = color ? L.T(((RgbMode)modes.SelectedItem).NativeBrightness ? "rgb.nativeBrightness" : "rgb.softwareBrightness") : "";
        }
        private void SelectDevice()
        {
            modes.Items.Clear(); var device = devices.SelectedItem as RgbDevice;
            if (device != null)
            {
                foreach (var mode in device.Modes) if (mode.CanColor) modes.Items.Add(mode);
                var preferred = device.Modes.FirstOrDefault(m => m.CanColor && (m.Name.Equals("Static", StringComparison.OrdinalIgnoreCase) || m.Name.Equals("Direct", StringComparison.OrdinalIgnoreCase)));
                if (preferred != null) modes.SelectedItem = preferred; else if (modes.Items.Count > 0) modes.SelectedIndex = 0;
                var selectedMode = modes.SelectedItem as RgbMode;
                uint color = selectedMode != null && selectedMode.ModeColor && selectedMode.Colors.Count > 0 ? selectedMode.Colors[0] : device.Colors.Count > 0 ? device.Colors[0] : 0xFFFFFF;
                changing = true; red.Value = color & 255; green.Value = color >> 8 & 255; blue.Value = color >> 16 & 255; changing = false; preset.SelectedIndex = colors.Length;
            }
            UpdateActions();
            if (device != null && modes.Items.Count == 0) status.Text = L.T("rgb.noColor");
        }
        private async Task Connect()
        {
            if (busy) return;
            busy = true;
            UpdateActions();
            status.Text = L.T("rgb.connecting");
            connectionCancellation = new CancellationTokenSource();
            var cancellation = connectionCancellation.Token;
            try
            {
                if (client != null) { client.Dispose(); client = null; }
                devices.Items.Clear();
                modes.Items.Clear();
                int selectedPort = (int)port.Value;
                var result = await Task.Run(() =>
                {
                    System.Collections.Generic.List<RgbDevice> found;
                    var connection = OpenRgbClient.Discover(selectedPort, cancellation, out found);
                    return Tuple.Create(connection, found);
                });
                if (closing || IsDisposed || Disposing) { result.Item1.Dispose(); return; }
                client = result.Item1;
                var foundDevices = result.Item2;
                devices.Items.AddRange(foundDevices.ToArray());
                if (devices.Items.Count > 0) devices.SelectedIndex = 0;
                status.Text = L.T(foundDevices.Count == 0 ? "rgb.noGpu" : modes.Items.Count == 0 ? "rgb.noColor" : "rgb.connected", foundDevices.Count);
            }
            catch (Exception ex)
            {
                if (!closing && !IsDisposed && !Disposing)
                {
                    if (client != null) { client.Dispose(); client = null; }
                    devices.Items.Clear();
                    modes.Items.Clear();
                    status.Text = L.T("rgb.failed", ex.Message);
                }
            }
            finally
            {
                connectionCancellation.Dispose();
                connectionCancellation = null;
                busy = false;
                if (!closing && !IsDisposed && !Disposing)
                {
                    UpdateActions();
                    if (devices.Items.Count > 0) devices.Focus();
                    else { status.Focus(); status.SelectAll(); }
                }
            }
        }
        private async Task Apply(bool turnOff)
        {
            var device = devices.SelectedItem as RgbDevice; var mode = modes.SelectedItem as RgbMode;
            if (busy || client == null || device == null || (!turnOff && mode == null)) return;
            var connection = client;
            busy = true; UpdateActions(); status.Text = L.T("rgb.sending");
            int index = mode == null ? -1 : device.Modes.IndexOf(mode), r = (int)red.Value, g = (int)green.Value, b = (int)blue.Value, level = (int)brightness.Value;
            try
            {
                await Task.Run(() => connection.Apply(device, index, r, g, b, level, turnOff));
                if (!closing && !IsDisposed && !Disposing) { status.Text = L.T("rgb.sent"); status.Focus(); }
            }
            catch (Exception ex) { if (!closing && !IsDisposed && !Disposing) { status.Text = L.T("rgb.failed", ex.Message); if (client != null) client.Dispose(); client = null; devices.Items.Clear(); modes.Items.Clear(); status.Focus(); status.SelectAll(); } }
            finally { busy = false; if (!closing && !IsDisposed && !Disposing) UpdateActions(); }
        }
        private async Task Download()
        {
            if (busy) return;
            string destination;
            using (var save = new SaveFileDialog { Title = L.T("rgb.downloadTitle"), Filter = L.T("rgb.downloadFilter"), FileName = OpenRgbDownload.FileName, DefaultExt = "zip", AddExtension = true, CheckPathExists = true, OverwritePrompt = true })
            { if (save.ShowDialog(this) != DialogResult.OK) return; destination = save.FileName; }
            busy = true; downloadCancellation = new CancellationTokenSource(); UpdateActions(); cancelDownload.Focus(); status.Text = L.T("rgb.downloading", 0);
            int lastPercent = -1; bool finished = false;
            var progress = new Progress<long>(bytes =>
            {
                if (finished || closing || IsDisposed || downloadCancellation == null) return;
                int percent = (int)(bytes * 100 / OpenRgbDownload.Size);
                if (percent / 10 != lastPercent / 10 || lastPercent < 0) { lastPercent = percent; status.Text = L.T("rgb.downloading", percent); }
            });
            var cancellation = downloadCancellation.Token;
            try
            {
                await Task.Run(() => OpenRgbDownload.DownloadAsync(destination, progress, cancellation));
                finished = true;
                if (!closing && !IsDisposed)
                {
                    status.Text = L.T("rgb.downloaded", destination); status.Focus();
                    SystemDialogs.Show(this, Text, L.T("rgb.downloadComplete"), status.Text, false);
                }
            }
            catch (Exception ex)
            {
                finished = true;
                if (!closing && !IsDisposed)
                {
                    status.Text = L.T(cancellation.IsCancellationRequested || ex is OperationCanceledException ? "rgb.downloadCancelled" : "rgb.failed", ex.GetBaseException().Message); status.Focus();
                    if (!cancellation.IsCancellationRequested) SystemDialogs.Show(this, Text, L.T("rgb.downloadTitle"), status.Text, false);
                }
            }
            finally { downloadCancellation.Dispose(); downloadCancellation = null; busy = false; if (!closing && !IsDisposed) { UpdateActions(); status.Focus(); } }
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keys)
        {
            if (keys == Keys.F6) { status.Focus(); status.SelectAll(); return true; }
            if (keys == (Keys.Control | Keys.Enter)) { if (apply.Enabled) apply.PerformClick(); return true; }
            return base.ProcessCmdKey(ref msg, keys);
        }
    }
}
