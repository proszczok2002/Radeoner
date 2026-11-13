using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Radeoner
{
    public sealed partial class MainForm : Form
    {
        private IBackend backend;
        private Snapshot snapshot, sensors;
        private ApplyResult lastApply;
        private bool busy, loadingSelection, dirty, sensorReading, changingPreferences, exitRequested, disposed, started;
        private Task<Snapshot> sensorTask;
        private readonly bool startInTray;
        private readonly string settingsPath;
        private AppSettings settings;
        private readonly StartupRegistration startup;
        private Font applicationFont;
        private readonly Dictionary<int, Font> fonts = new Dictionary<int, Font>();
        private TrayIcon tray;
        private readonly ContextMenuStrip trayMenu = new ContextMenuStrip();
        private readonly ToolStripMenuItem trayShow = new ToolStripMenuItem(), trayRead = new ToolStripMenuItem(), trayPersonalize = new ToolStripMenuItem(), trayExit = new ToolStripMenuItem();
        private readonly ComboBox gpu = new ComboBox(), language = new ComboBox(), theme = new ComboBox(), textScale = new ComboBox();
        private readonly Label gpuLabel = new Label(), languageLabel = new Label(), themeLabel = new Label(), textLabel = new Label(), preferencesNote = new Label();
        private readonly TabControl tabs = new TabControl();
        private readonly TextBox overview = ReadOnlyText(), report = ReadOnlyText(), status = ReadOnlyText();
        private readonly FlowLayoutPanel tuningFields = new FlowLayoutPanel(), fanFields = new FlowLayoutPanel(), personalFields = new FlowLayoutPanel();
        private readonly Button refresh = Button(), apply = Button(), undo = Button(), factory = Button(), save = Button(), export = Button(), import = Button(), help = Button(), exit = Button();
        private readonly CheckBox monitoring = new ThemedCheckBox { AutoSize = true }, autoStart = new ThemedCheckBox { AutoSize = true };
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 3000 };
        private readonly Dictionary<string, NumericUpDown> editors = new Dictionary<string, NumericUpDown>();
        private readonly List<NumericUpDown> temperatures = new List<NumericUpDown>(), speeds = new List<NumericUpDown>();
        private CheckBox zeroRpm;
        internal Action<Exception> TestFailure;
        internal bool SuppressAutomaticUpdateChecks;
        private bool initializedSuccessfully;
        private Control savedFocus;
        private string savedEditor;
        private int savedSelectionStart, savedSelectionLength;
        [DllImport("user32.dll")] private static extern void NotifyWinEvent(uint evt, IntPtr hwnd, int objectId, int childId);

        public MainForm() : this(false) { }
        public MainForm(bool trayOnStartup) : this(trayOnStartup, AppSettings.DefaultPath, new StartupRegistration(Application.ExecutablePath)) { }
        internal MainForm(bool trayOnStartup, string preferencesPath, StartupRegistration startupRegistration)
        {
            settingsPath = preferencesPath; settings = AppSettings.Load(settingsPath); startup = startupRegistration;
            L.Load(settings.Language); startInTray = trayOnStartup;
            AutoScaleMode = AutoScaleMode.Dpi; MinimumSize = new Size(720, 520); Size = new Size(1040, 780);
            StartPosition = FormStartPosition.CenterScreen; KeyPreview = true;
            if (startInTray) { Opacity = 0; ShowInTaskbar = false; }
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(12) };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(layout);
            var top = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 2 };
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            gpuLabel.AutoSize = true; gpuLabel.Anchor = AnchorStyles.Left; top.Controls.Add(gpuLabel, 0, 0);
            gpu.DropDownStyle = ComboBoxStyle.DropDownList; gpu.Dock = DockStyle.Fill; top.Controls.Add(gpu, 1, 0); layout.Controls.Add(top, 0, 0);
            var tools = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
            tools.Controls.AddRange(new Control[] { refresh, export, import, save, monitoring }); layout.Controls.Add(tools, 0, 1);
            tabs.Dock = DockStyle.Fill; tabs.Multiline = true; tabs.DrawItem += (s, e) => Appearance.DrawTab(tabs, e);
            ConfigureFields(tuningFields); ConfigureFields(fanFields); ConfigureFields(personalFields);
            AddTab(overview); AddTab(tuningFields); AddTab(fanFields); AddTab(report); AddTab(personalFields); layout.Controls.Add(tabs, 0, 2);
            var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
            actions.Controls.AddRange(new Control[] { apply, undo, factory, help, exit }); layout.Controls.Add(actions, 0, 3);
            status.Dock = DockStyle.Fill; status.MinimumSize = new Size(0, 60); layout.Controls.Add(status, 0, 4);
            BuildPersonalization(); BuildUpdates(); BuildInformation();
            trayMenu.Items.AddRange(new ToolStripItem[] { trayShow, trayRead, trayPersonalize, new ToolStripSeparator(), trayExit });
            trayShow.Click += (s, e) => ShowFromTray(); trayRead.Click += async (s, e) => await RefreshSensors(true);
            trayPersonalize.Click += (s, e) => { ShowFromTray(); tabs.SelectedIndex = 4; language.Focus(); }; trayExit.Click += async (s, e) => await RequestExit();
            refresh.Click += async (s, e) => await RefreshAll(true); apply.Click += async (s, e) => await ApplyChanges();
            undo.Click += async (s, e) => await UndoChanges(); factory.Click += async (s, e) => await ResetFactory();
            save.Click += (s, e) => SaveReport(); export.Click += (s, e) => ExportProfile(); import.Click += async (s, e) => await ImportProfile();
            help.Click += (s, e) => ShowHelp(); exit.Click += async (s, e) => await RequestExit();
            gpu.SelectedIndexChanged += async (s, e) => { if (!loadingSelection && !busy) await SelectGpu(); };
            monitoring.Checked = settings.RefreshSensors; monitoring.CheckedChanged += (s, e) => ChangeMonitoring();
            timer.Tick += async (s, e) => await RefreshSensors(false);
            Shown += async (s, e) =>
            {
                if (started) return; started = true;
                tray = new TrayIcon(trayMenu); tray.ShowRequested += ShowFromTray; tray.EnsureVisible();
                if (startInTray) HideToTray(); Opacity = 1;
                await Initialize(); if (disposed) return; timer.Enabled = monitoring.Checked;
                if (UpdateCompletionPlan != null && initializedSuccessfully) { await CompleteUpdate(); if (disposed) return; }
                if (UpdateWasRolledBack) Announce(L.T("update.rolledBack"));
                StartUpdateChecks();
            };
            FormClosing += HandleClosing; SystemEvents.UserPreferenceChanged += SystemPreferencesChanged;
            ApplyLanguage(); ApplyAppearance(); Accessibility.Order(this); status.Text = L.T("ui.connecting"); UpdateActions();
        }
        private static TextBox ReadOnlyText() { return new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, WordWrap = true }; }
        private static Button Button() { return new ThemedButton { AutoSize = true, Padding = new Padding(8, 4, 8, 4), UseVisualStyleBackColor = true }; }
        private static void ConfigureFields(FlowLayoutPanel panel) { panel.Dock = DockStyle.Fill; panel.AutoScroll = true; panel.FlowDirection = FlowDirection.TopDown; panel.WrapContents = false; panel.Padding = new Padding(8); }
        private void AddTab(Control content) { var page = new TabPage { Padding = new Padding(8), UseVisualStyleBackColor = true }; page.Controls.Add(content); tabs.TabPages.Add(page); }
        private static Label Note(string text) { return new Label { Text = text, AutoSize = true, MaximumSize = new Size(820, 0), Margin = new Padding(3, 5, 3, 10) }; }
        private void BuildPersonalization()
        {
            foreach (var box in new[] { language, theme, textScale }) { box.DropDownStyle = ComboBoxStyle.DropDownList; box.Width = 280; }
            language.Items.AddRange(new object[] { "Polski", "English" }); language.SelectedIndex = settings.Language == "en" ? 1 : 0;
            theme.Items.AddRange(new object[] { "", "", "" }); theme.SelectedIndex = settings.Theme == "dark" ? 2 : settings.Theme == "light" ? 1 : 0;
            foreach (int scale in new[] { 100, 125, 150, 175, 200 }) textScale.Items.Add(scale + " %"); textScale.SelectedIndex = (settings.TextScale - 100) / 25;
            foreach (var pair in new[] { Tuple.Create(languageLabel, language), Tuple.Create(themeLabel, theme), Tuple.Create(textLabel, textScale) })
            {
                var row = new FlowLayoutPanel { AutoSize = true, WrapContents = true }; pair.Item1.AutoSize = true; pair.Item1.Margin = new Padding(3, 6, 12, 3);
                row.Controls.Add(pair.Item1); row.Controls.Add(pair.Item2); personalFields.Controls.Add(row);
            }
            try { autoStart.Checked = startup.Enabled; } catch { autoStart.Enabled = false; }
            personalFields.Controls.Add(autoStart); preferencesNote.AutoSize = true; preferencesNote.MaximumSize = new Size(820, 0); preferencesNote.Margin = new Padding(3, 12, 3, 3); personalFields.Controls.Add(preferencesNote);
            autoStart.CheckedChanged += (s, e) =>
            {
                if (changingPreferences) return;
                try { startup.SetEnabled(autoStart.Checked); Announce(L.T(autoStart.Checked ? "startup.enabled" : "startup.disabled")); }
                catch (Exception ex) { changingPreferences = true; autoStart.Checked = !autoStart.Checked; changingPreferences = false; Failed(ex); }
            };
            language.SelectedIndexChanged += async (s, e) => { if (!changingPreferences) await ChangeLanguage(); };
            theme.SelectedIndexChanged += (s, e) =>
            {
                if (changingPreferences) return; var next = settings.Copy(); next.Theme = theme.SelectedIndex == 2 ? "dark" : theme.SelectedIndex == 1 ? "light" : "system";
                if (Persist(next)) ApplyAppearance(); else RestorePreferenceChoices();
            };
            textScale.SelectedIndexChanged += (s, e) =>
            {
                if (changingPreferences) return; var next = settings.Copy(); next.TextScale = 100 + 25 * textScale.SelectedIndex;
                if (Persist(next)) ApplyAppearance(); else RestorePreferenceChoices();
            };
        }
        private bool Persist(AppSettings next)
        { try { next.Save(settingsPath); settings = next; return true; } catch (Exception ex) { Failed(new InvalidOperationException(L.T("prefs.saveError", ex.Message), ex)); return false; } }
        private void RestorePreferenceChoices()
        {
            changingPreferences = true; language.SelectedIndex = settings.Language == "en" ? 1 : 0; theme.SelectedIndex = settings.Theme == "dark" ? 2 : settings.Theme == "light" ? 1 : 0;
            textScale.SelectedIndex = (settings.TextScale - 100) / 25; monitoring.Checked = settings.RefreshSensors; changingPreferences = false;
        }
        private void ChangeMonitoring()
        {
            if (changingPreferences) return; var next = settings.Copy(); next.RefreshSensors = monitoring.Checked;
            if (!Persist(next)) RestorePreferenceChoices(); timer.Enabled = monitoring.Checked && started; UpdateTray(!monitoring.Checked);
        }
        private async Task ChangeLanguage()
        {
            string nextLanguage = language.SelectedIndex == 1 ? "en" : "pl"; if (nextLanguage == settings.Language) return;
            SetBusy(true);
            try
            {
                await FinishSensorRead(); var next = settings.Copy(); next.Language = nextLanguage;
                if (!Persist(next)) { RestorePreferenceChoices(); return; }
                var draft = CaptureDraft(); bool wasDirty = dirty; L.Load(nextLanguage); ApplyLanguage();
                if (snapshot != null && backend != null) snapshot = await Task.Run(() => backend.Read(snapshot.Gpu.Index));
                if (snapshot != null) { Render(); RestoreDraft(draft); dirty = wasDirty; }
                ApplyAppearance(); Announce(L.T("prefs.languageChanged"));
            }
            catch (Exception ex) { Failed(ex); } finally { SetBusy(false); }
        }
        private void ApplyLanguage()
        {
            Text = "Radeoner " + L.Version; AccessibleName = L.T("ui.windowName"); gpuLabel.Text = L.T("ui.gpuLabel"); gpu.AccessibleName = L.T("ui.gpu");
            overview.AccessibleName = L.T("ui.overviewName"); report.AccessibleName = L.T("ui.reportName"); status.AccessibleName = L.T("ui.statusName");
            SetButton(refresh, "ui.refresh", "ui.refreshName"); SetButton(apply, "ui.apply", "ui.applyName"); SetButton(undo, "ui.undo", "ui.undoName");
            SetButton(factory, "ui.factory", "ui.factoryName"); SetButton(save, "ui.saveReport", "ui.saveReportName"); SetButton(export, "ui.export", "ui.exportName");
            SetButton(import, "ui.import", "ui.importName"); SetButton(help, "ui.help", "ui.helpName"); SetButton(exit, "ui.exit", "ui.exitName");
            monitoring.Text = L.T("ui.monitoring"); monitoring.AccessibleName = L.T("ui.monitoringName"); tabs.AccessibleName = L.T("ui.tabs");
            string[] keys = { "ui.readingsTab", "ui.tuningTab", "ui.fanTab", "ui.reportTab", "settings.tab" };
            for (int i = 0; i < keys.Length; i++) { tabs.TabPages[i].Text = L.T(keys[i]); tabs.TabPages[i].AccessibleName = L.T(keys[i]); }
            languageLabel.Text = L.T("prefs.language"); language.AccessibleName = L.T("prefs.languageName"); themeLabel.Text = L.T("prefs.theme"); theme.AccessibleName = L.T("prefs.themeName");
            textLabel.Text = L.T("prefs.text"); textScale.AccessibleName = L.T("prefs.textName");
            changingPreferences = true; int themeIndex = theme.SelectedIndex;
            theme.Items[0] = L.T("prefs.system"); theme.Items[1] = L.T("prefs.light"); theme.Items[2] = L.T("prefs.dark"); theme.SelectedIndex = themeIndex; changingPreferences = false;
            autoStart.Text = L.T("prefs.autostart"); autoStart.AccessibleName = L.T("prefs.autostartName"); preferencesNote.Text = L.T("prefs.note");
            trayShow.Text = L.T("tray.show"); trayRead.Text = L.T("tray.refresh"); trayPersonalize.Text = L.T("settings.tab"); trayExit.Text = L.T("ui.exit");
            foreach (ToolStripItem item in trayMenu.Items) item.AccessibleName = item.Text.Replace("&", ""); gpu.Refresh(); UpdateTray(false);
            LocalizeUpdates(); LocalizeInformation();
        }
        private static void SetButton(Button button, string textKey, string nameKey) { button.Text = L.T(textKey); button.AccessibleName = L.T(nameKey); }
        private void SystemPreferencesChanged(object sender, UserPreferenceChangedEventArgs e)
        { if (!disposed && IsHandleCreated) try { BeginInvoke((Action)ApplyAppearance); } catch (InvalidOperationException) { } }
        private void ApplyAppearance()
        {
            if (!fonts.TryGetValue(settings.TextScale, out applicationFont))
            { applicationFont = new Font(SystemFonts.MessageBoxFont.FontFamily, SystemFonts.MessageBoxFont.Size * settings.TextScale / 100f); fonts.Add(settings.TextScale, applicationFont); }
            Font = applicationFont; trayMenu.Font = applicationFont; status.Height = Math.Max(60, Font.Height * 3 + 12); tabs.ItemSize = new Size(0, Font.Height + 14);
            Appearance.Apply(this, settings.Theme); trayMenu.RenderMode = ToolStripRenderMode.System;
            trayMenu.BackColor = Appearance.Dark(settings.Theme) ? Color.FromArgb(43, 43, 46) : SystemColors.Menu;
            trayMenu.ForeColor = Appearance.Dark(settings.Theme) ? Color.White : SystemColors.MenuText;
            foreach (ToolStripItem item in trayMenu.Items) { item.BackColor = trayMenu.BackColor; item.ForeColor = trayMenu.ForeColor; }
        }
        private void Announce(string text) { if (disposed) return; status.Text = text; if (status.IsHandleCreated && Visible) NotifyWinEvent(0x800E, status.Handle, -4, 0); }
        private void UpdateActions()
        {
            bool selected = snapshot != null; gpu.Enabled = !busy; refresh.Enabled = !busy && selected; monitoring.Enabled = !busy && selected;
            tuningFields.Enabled = !busy; fanFields.Enabled = !busy; personalFields.Enabled = !busy;
            apply.Enabled = !busy && selected && dirty; undo.Enabled = !busy && selected && lastApply != null && lastApply.After.Gpu.Id == snapshot.Gpu.Id;
            factory.Enabled = !busy && selected && snapshot.CanReset; save.Enabled = !busy && selected;
            export.Enabled = import.Enabled = !busy && selected; trayRead.Enabled = !busy && selected; exit.Enabled = trayExit.Enabled = !busy;
            UpdateUpdateActions();
        }
        private void SetBusy(bool value)
        {
            if (disposed) return;
            if (value && !busy)
            {
                savedFocus = Accessibility.Focused(this); savedEditor = null;
                var text = savedFocus as TextBoxBase; savedSelectionStart = text == null ? 0 : text.SelectionStart; savedSelectionLength = text == null ? 0 : text.SelectionLength;
                foreach (var pair in editors) if (pair.Value.ContainsFocus) savedEditor = pair.Key;
                for (int i = 0; i < temperatures.Count; i++) { if (temperatures[i].ContainsFocus) savedEditor = "fan.t." + i; if (speeds[i].ContainsFocus) savedEditor = "fan.s." + i; }
                if (zeroRpm != null && zeroRpm.ContainsFocus) savedEditor = "fan.zero";
            }
            bool wasBusy = busy; busy = value; UseWaitCursor = value; UpdateActions();
            if (wasBusy && !value && Visible)
            {
                Control target = savedFocus;
                if (savedEditor != null)
                {
                    if (editors.ContainsKey(savedEditor)) target = editors[savedEditor];
                    else if (savedEditor == "fan.zero") target = zeroRpm;
                    else if (savedEditor.StartsWith("fan.t.")) { int i; if (int.TryParse(savedEditor.Substring(6), out i) && i < temperatures.Count) target = temperatures[i]; }
                    else if (savedEditor.StartsWith("fan.s.")) { int i; if (int.TryParse(savedEditor.Substring(6), out i) && i < speeds.Count) target = speeds[i]; }
                }
                if (target != null && !target.IsDisposed && target.CanFocus)
                {
                    target.Focus(); var text = target as TextBoxBase;
                    if (text == null && target is NumericUpDown) text = target.Controls.OfType<TextBoxBase>().FirstOrDefault();
                    if (text != null) text.Select(Math.Min(savedSelectionStart, text.TextLength), Math.Min(savedSelectionLength, Math.Max(0, text.TextLength - savedSelectionStart)));
                }
                else if (savedFocus != null) tabs.Focus();
            }
            if (!value) QueueUpdateOffer();
        }
        private async Task FinishSensorRead() { if (sensorTask != null) try { await sensorTask; } catch { } }
        private void Failed(Exception ex) { if (disposed) return; if (TestFailure != null) { TestFailure(ex); return; } ShowFromTray(); Announce(ex.Message); MessageBox.Show(this, ex.Message, L.T("ui.messageTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information); }
        private async Task Initialize()
        {
            SetBusy(true);
            try
            {
                backend = await Task.Run<IBackend>(() => { try { return new AmdBackend(); } catch (Exception ex) { return new UnavailableBackend(ex.Message); } });
                if (disposed) { backend.Dispose(); return; }
                loadingSelection = true; foreach (var info in backend.Gpus) gpu.Items.Add(info); if (gpu.Items.Count > 0) gpu.SelectedIndex = 0; loadingSelection = false;
                if (gpu.Items.Count == 0) { report.Text = backend.Status; overview.Text = L.T("ui.noGpu", backend.Status); Announce(overview.Text); initializedSuccessfully = true; return; }
                snapshot = await Task.Run(() => backend.Read(0)); if (disposed) return; Render(); Announce(L.T("ui.ready")); initializedSuccessfully = true; if (Visible) gpu.Focus();
            }
            catch (Exception ex) { Failed(ex); } finally { SetBusy(false); }
        }
        private bool Discard(string key) { return !dirty || MessageBox.Show(this, L.T(key), "Radeoner", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes; }
        private async Task SelectGpu()
        {
            if (gpu.SelectedItem == null) return;
            if (!Discard("ui.discardSwitch")) { loadingSelection = true; gpu.SelectedIndex = snapshot.Gpu.Index; loadingSelection = false; return; }
            int index = ((GpuInfo)gpu.SelectedItem).Index; SetBusy(true); Announce(L.T("ui.reading"));
            try { await FinishSensorRead(); snapshot = await Task.Run(() => backend.Read(index)); Render(); Announce(L.T("ui.gpuReady", snapshot.Gpu.Name)); }
            catch (Exception ex) { loadingSelection = true; gpu.SelectedIndex = snapshot == null ? -1 : snapshot.Gpu.Index; loadingSelection = false; Failed(ex); } finally { SetBusy(false); }
        }
        private async Task RefreshAll(bool askDiscard)
        {
            if (busy || snapshot == null || askDiscard && !Discard("ui.discardRefresh")) return;
            int index = snapshot.Gpu.Index; SetBusy(true); Announce(L.T("ui.reading"));
            try { await FinishSensorRead(); snapshot = await Task.Run(() => backend.Read(index)); Render(); timer.Enabled = monitoring.Checked && started; Announce(L.T("ui.readDone", snapshot.Settings.Count, snapshot.Fan.Count)); }
            catch (Exception ex) { Failed(ex); } finally { SetBusy(false); }
        }
        private async Task RefreshSensors(bool force)
        {
            if (busy || sensorReading || snapshot == null || disposed || !force && !monitoring.Checked) return;
            int index = snapshot.Gpu.Index; sensorReading = true; sensorTask = Task.Run(() => backend.Read(index));
            try { var data = await sensorTask; if (!disposed && !busy && snapshot.Gpu.Index == index) { sensors = data; if (!overview.Focused) overview.Text = Overview(data); UpdateTray(false); } }
            catch { timer.Stop(); if (!disposed) { UpdateTray(true); Announce(L.T("ui.autoReadFailed")); } } finally { sensorReading = false; }
        }
        private void UpdateTray(bool stale)
        {
            if (tray == null || disposed) return; var data = sensors ?? snapshot;
            tray.Update(TrayText.Format(data, stale || !monitoring.Checked || data != null && (DateTimeOffset.Now - data.CapturedAt).TotalSeconds > 10));
        }
        private static string Overview(Snapshot data)
        {
            var b = new StringBuilder(data.Gpu + "\r\n" + L.T("report.driver") + data.Gpu.DriverDescription + "\r\n" + L.T("report.memory") + data.Gpu.MemoryDescription + "\r\n\r\n");
            b.AppendLine(L.T("ui.sensorTime", data.CapturedAt.ToLocalTime().ToString("HH:mm:ss"))); foreach (var m in data.Metrics) b.AppendLine(m);
            b.AppendLine(); b.AppendLine(L.T("ui.fieldCount", data.Settings.Count, data.Fan.Count));
            if (data.AtFactory.HasValue) b.AppendLine(L.T("report.factory") + L.T(data.AtFactory.Value ? "common.yes" : "common.no")); foreach (var n in data.Notes) b.AppendLine(n); return b.ToString();
        }
        private static void ClearFields(FlowLayoutPanel panel) { while (panel.Controls.Count > 0) { var child = panel.Controls[0]; panel.Controls.RemoveAt(0); child.Dispose(); } }
        private NumericUpDown Number(string name, IntRange range, int value)
        {
            var number = new NumericUpDown { AccessibleName = name, AccessibleDescription = L.T("ui.range", range), Minimum = range.Min, Maximum = range.Max, Increment = range.Step, Value = value, Width = 160, DecimalPlaces = 0, ThousandsSeparator = false };
            Accessibility.Number(number);
            number.TextChanged += (s, e) => { dirty = true; UpdateActions(); }; number.ValueChanged += (s, e) => { dirty = true; UpdateActions(); }; return number;
        }
        private void Render()
        {
            sensors = snapshot; overview.Text = Overview(snapshot); report.Text = snapshot.Report(); ClearFields(tuningFields); ClearFields(fanFields); editors.Clear(); temperatures.Clear(); speeds.Clear(); zeroRpm = null;
            tuningFields.Controls.Add(Note(L.T("ui.tuningNote"))); if (snapshot.Settings.Count == 0) tuningFields.Controls.Add(Note(L.T("ui.noTuning")));
            foreach (var setting in snapshot.Settings)
            {
                var row = new FlowLayoutPanel { AutoSize = true, WrapContents = true, MaximumSize = new Size(1200, 0) };
                row.Controls.Add(new Label { Text = setting.Name + " (" + setting.Unit + "):", AutoSize = true, Margin = new Padding(3, 6, 8, 3) });
                var number = Number(setting.Name + ", " + setting.Unit, setting.Range, setting.Value); row.Controls.Add(number);
                row.Controls.Add(new Label { Text = L.T("ui.range", setting.Range), AutoSize = true, Margin = new Padding(8, 6, 3, 3) });
                editors.Add(setting.Id, number); tuningFields.Controls.Add(row); if (!string.IsNullOrEmpty(setting.Note)) tuningFields.Controls.Add(Note(setting.Note));
            }
            fanFields.Controls.Add(Note(L.T("ui.fanNote"))); if (snapshot.Fan.Count == 0) fanFields.Controls.Add(Note(L.T("ui.noFan")));
            for (int i = 0; i < snapshot.Fan.Count; i++)
            {
                var row = new FlowLayoutPanel { AutoSize = true, WrapContents = true };
                row.Controls.Add(new Label { Text = L.T("ui.pointTemperature", i + 1), AutoSize = true, Margin = new Padding(3, 6, 3, 3) });
                var t = Number(L.T("ui.pointTemperatureName", i + 1), snapshot.FanTemperature, snapshot.Fan[i].Temperature); row.Controls.Add(t); temperatures.Add(t);
                row.Controls.Add(new Label { Text = L.T("ui.speed"), AutoSize = true, Margin = new Padding(8, 6, 3, 3) });
                var s = Number(L.T("ui.pointSpeedName", i + 1), snapshot.FanSpeed, snapshot.Fan[i].Speed); row.Controls.Add(s); speeds.Add(s); fanFields.Controls.Add(row);
            }
            if (snapshot.Fan.Count > 0) fanFields.Controls.Add(Note(L.T("ui.fanRanges", snapshot.FanTemperature, snapshot.FanSpeed)));
            if (snapshot.ZeroRpm.HasValue)
            {
                zeroRpm = new ThemedCheckBox { Text = L.T("ui.zeroRpm"), AccessibleName = L.T("ui.zeroRpmName"), Checked = snapshot.ZeroRpm.Value, AutoSize = true };
                zeroRpm.CheckedChanged += (s, e) => { dirty = true; UpdateActions(); }; fanFields.Controls.Add(zeroRpm);
            }
            dirty = false; Appearance.Apply(tuningFields, settings.Theme); Appearance.Apply(fanFields, settings.Theme); Accessibility.Order(this); UpdateActions(); UpdateTray(false);
        }
        private Dictionary<string, string> CaptureDraft()
        {
            var result = editors.ToDictionary(p => p.Key, p => p.Value.Text);
            for (int i = 0; i < temperatures.Count; i++) { result["fan.t." + i] = temperatures[i].Text; result["fan.s." + i] = speeds[i].Text; }
            if (zeroRpm != null) result["fan.zero"] = zeroRpm.Checked ? "1" : "0"; return result;
        }
        private void RestoreDraft(Dictionary<string, string> draft)
        {
            foreach (var p in editors) if (draft.ContainsKey(p.Key)) p.Value.Text = draft[p.Key];
            for (int i = 0; i < temperatures.Count; i++) { if (draft.ContainsKey("fan.t." + i)) temperatures[i].Text = draft["fan.t." + i]; if (draft.ContainsKey("fan.s." + i)) speeds[i].Text = draft["fan.s." + i]; }
            if (zeroRpm != null && draft.ContainsKey("fan.zero")) zeroRpm.Checked = draft["fan.zero"] == "1";
        }
        private Changes Collect()
        {
            var changes = new Changes();
            foreach (var setting in snapshot.Settings)
            { int value; if (!int.TryParse(editors[setting.Id].Text, out value) || !setting.Range.Contains(value)) throw new InvalidOperationException(L.T("ui.invalidNumber", setting.Name, setting.Range)); if (value != setting.Value) changes.Values.Add(setting.Id, value); }
            if (snapshot.Fan.Count > 0)
            {
                var points = new List<FanPoint>();
                for (int i = 0; i < temperatures.Count; i++) { int t, s; if (!int.TryParse(temperatures[i].Text, out t) || !int.TryParse(speeds[i].Text, out s)) throw new InvalidOperationException(L.T("ui.invalidFanNumber")); points.Add(new FanPoint(t, s)); }
                if (!Validation.SameFan(snapshot.Fan, points)) changes.Fan = points;
            }
            if (zeroRpm != null && zeroRpm.Checked != snapshot.ZeroRpm.Value) changes.ZeroRpm = zeroRpm.Checked; Validation.Validate(snapshot, changes); return changes;
        }
        internal void Stage(Changes changes)
        {
            Validation.Validate(snapshot, changes); foreach (var p in changes.Values) editors[p.Key].Value = p.Value;
            if (changes.Fan != null) for (int i = 0; i < changes.Fan.Count; i++) { temperatures[i].Value = changes.Fan[i].Temperature; speeds[i].Value = changes.Fan[i].Speed; }
            if (changes.ZeroRpm.HasValue) zeroRpm.Checked = changes.ZeroRpm.Value; dirty = !changes.Empty; UpdateActions();
        }
        private void ExportProfile()
        {
            if (busy || snapshot == null) return;
            try
            {
                var profile = Profiles.Capture(snapshot, Collect());
                using (var dialog = new SaveFileDialog { Title = L.T("profile.exportTitle"), Filter = L.T("profile.filter"), FileName = "Radeoner-profile.json", DefaultExt = "json" })
                { if (dialog.ShowDialog(this) != DialogResult.OK) return; Profiles.Save(dialog.FileName, profile); Announce(L.T("profile.saved", dialog.FileName)); }
            }
            catch (Exception ex) { Failed(ex); }
        }
        private async Task ImportProfile()
        {
            if (busy || snapshot == null) return;
            using (var dialog = new OpenFileDialog { Title = L.T("profile.importTitle"), Filter = L.T("profile.filter"), CheckFileExists = true })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK || !Discard("profile.discard")) return; SetBusy(true);
                try
                {
                    var profile = Profiles.Load(dialog.FileName); await FinishSensorRead(); var current = await Task.Run(() => backend.Read(snapshot.Gpu.Index));
                    var changes = Profiles.Prepare(current, profile); snapshot = current; Render(); Stage(changes);
                    tabs.SelectedIndex = snapshot.Settings.Count > 0 ? 1 : snapshot.Fan.Count > 0 ? 2 : 0; Announce(L.T(changes.Empty ? "profile.same" : "profile.loaded"));
                }
                catch (Exception ex) { Failed(ex); } finally { SetBusy(false); }
            }
        }
        private async Task ApplyChanges()
        {
            if (busy || snapshot == null) return;
            try
            {
                var changes = Collect(); if (changes.Empty) { dirty = false; UpdateActions(); Announce(L.T("ui.noChanges")); return; }
                if (MessageBox.Show(this, Validation.Describe(snapshot, changes) + "\r\n" + L.T("ui.confirmApply"), L.T("ui.applyTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                SetBusy(true); Announce(L.T("ui.applying")); await FinishSensorRead(); lastApply = await Task.Run(() => TuningTransaction.Apply(backend, snapshot, changes));
                if (disposed) return; snapshot = lastApply.After; Render(); Announce(L.T("ui.applied"));
            }
            catch (Exception ex) { lastApply = null; Failed(ex); } finally { SetBusy(false); }
        }
        private async Task UndoChanges()
        {
            if (busy || lastApply == null) return; if (dirty) { Failed(new InvalidOperationException(L.T("ui.undoDirty"))); return; } var previous = lastApply;
            if (MessageBox.Show(this, Validation.Describe(snapshot, previous.Undo) + "\r\n" + L.T("ui.confirmUndo"), "Radeoner", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            SetBusy(true); Announce(L.T("ui.undoing"));
            try { await FinishSensorRead(); var result = await Task.Run(() => TuningTransaction.Apply(backend, previous.After, previous.Undo)); snapshot = result.After; lastApply = null; Render(); Announce(L.T("ui.undone")); }
            catch (Exception ex) { lastApply = null; Failed(ex); } finally { SetBusy(false); }
        }
        private async Task ResetFactory()
        {
            if (busy || snapshot == null || !snapshot.CanReset) return;
            if (MessageBox.Show(this, L.T("ui.confirmFactory", snapshot.Gpu), L.T("ui.factoryTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            SetBusy(true); Announce(L.T("ui.resetting"));
            try { await FinishSensorRead(); int index = snapshot.Gpu.Index; snapshot = await Task.Run(() => { backend.Reset(index); return backend.Read(index); }); lastApply = null; Render(); Announce(L.T(snapshot.AtFactory == true ? "ui.factoryConfirmed" : "ui.factoryUnconfirmed")); }
            catch (Exception ex) { Failed(ex); } finally { SetBusy(false); }
        }
        private void SaveReport()
        {
            if (snapshot == null) return;
            using (var dialog = new SaveFileDialog { Title = L.T("ui.reportTitle"), Filter = L.T("ui.textFilter"), FileName = "Radeoner-report.txt", DefaultExt = "txt" })
            { if (dialog.ShowDialog(this) != DialogResult.OK) return; try { File.WriteAllText(dialog.FileName, report.Text, new UTF8Encoding(true)); Announce(L.T("ui.reportSaved", dialog.FileName)); } catch (Exception ex) { Failed(ex); } }
        }
        private bool HideToTray()
        {
            if (tray == null || !tray.EnsureVisible()) { ShowInTaskbar = true; Opacity = 1; Show(); Announce(L.T("tray.failed")); return false; }
            Hide(); ShowInTaskbar = false; return true;
        }
        private void ShowFromTray()
        {
            if (disposed) return; Opacity = 1; ShowInTaskbar = true; Show(); if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal; Activate(); BringToFront();
        }
        private async Task RequestExit()
        {
            if (busy) { ShowFromTray(); Announce(L.T("ui.waitExit")); return; }
            ShowFromTray(); if (!Discard("ui.discardExit")) return; SetBusy(true); timer.Stop(); await FinishSensorRead(); SetBusy(false); exitRequested = true; Close();
        }
        private void HandleClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !exitRequested) { e.Cancel = true; HideToTray(); return; }
            timer.Stop(); if (tray != null) { tray.Dispose(); tray = null; }
            // Never race a native read/write during Windows shutdown.
            if (!busy && !sensorReading && backend != null) { backend.Dispose(); backend = null; }
        }
        private void ShowHelp() { SystemDialogs.Details(this, L.T("ui.helpTitle"), L.T("ui.helpText", L.Version)); }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((keyData & Keys.Modifiers) == Keys.Control && (keyData & Keys.KeyCode) >= Keys.D1 && (keyData & Keys.KeyCode) <= Keys.D5)
            { tabs.SelectedIndex = (int)(keyData & Keys.KeyCode) - (int)Keys.D1; tabs.Focus(); return true; }
            if (keyData == Keys.F5) { refresh.PerformClick(); return true; }
            if (keyData == (Keys.Control | Keys.Enter)) { apply.PerformClick(); return true; }
            if (keyData == (Keys.Control | Keys.E)) { export.PerformClick(); return true; }
            if (keyData == (Keys.Control | Keys.I)) { import.PerformClick(); return true; }
            if (keyData == (Keys.Control | Keys.Shift | Keys.S)) { save.PerformClick(); return true; }
            if (keyData == Keys.F6) { status.Focus(); status.SelectAll(); return true; }
            if (keyData == Keys.F1) { ShowHelp(); return true; } return base.ProcessCmdKey(ref msg, keyData);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed) { disposed = true; SystemEvents.UserPreferenceChanged -= SystemPreferencesChanged; timer.Dispose(); DisposeUpdates(); if (tray != null) tray.Dispose(); trayMenu.Dispose(); if (!busy && !sensorReading && backend != null) { backend.Dispose(); backend = null; } }
            base.Dispose(disposing); if (disposing) { foreach (var font in fonts.Values) font.Dispose(); fonts.Clear(); applicationFont = null; }
        }
    }
}
