using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Radeoner
{
    public sealed partial class MainForm
    {
        internal string UpdateCompletionPlan;
        internal bool UpdateWasRolledBack;
        private readonly FlowLayoutPanel updateFields = new FlowLayoutPanel();
        private readonly CheckBox automaticUpdates = new ThemedCheckBox { AutoSize = true }, updatesAtStartup = new ThemedCheckBox { AutoSize = true };
        private readonly NumericUpDown updateMinutes = new NumericUpDown { Minimum = 5, Maximum = 10080, Width = 130 };
        private readonly Label updateIntervalLabel = new Label { AutoSize = true }, updateVersionLabel = new Label { AutoSize = true }, updateNote = new Label { AutoSize = true, MaximumSize = new Size(820, 0) };
        private readonly Button checkUpdate = Button(), installUpdate = Button();
        private readonly TextBox updateStatus = ReadOnlyText();
        private readonly System.Windows.Forms.Timer updateTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer offerTimer = new System.Windows.Forms.Timer { Interval = 500 };
        private readonly CancellationTokenSource updateCancellation = new CancellationTokenSource();
        private AvailableUpdate availableUpdate;
        private bool updateChecking, updateInstalling;
        private string notifiedVersion;
        private bool pendingUpdateOffer, showingUpdateOffer;

        private void BuildUpdates()
        {
            ConfigureFields(updateFields); updateFields.Dock = DockStyle.None; updateFields.AutoSize = true; updateFields.AutoScroll = false;
            personalFields.Controls.Add(updateFields);
            automaticUpdates.Checked = settings.CheckUpdates; updatesAtStartup.Checked = settings.CheckUpdatesOnStartup; updateMinutes.Value = settings.UpdateIntervalMinutes;
            updateFields.Controls.Add(updateVersionLabel); updateFields.Controls.Add(automaticUpdates); updateFields.Controls.Add(updatesAtStartup);
            var interval = new FlowLayoutPanel { AutoSize = true, WrapContents = true }; interval.Controls.Add(updateIntervalLabel); interval.Controls.Add(updateMinutes); updateFields.Controls.Add(interval);
            var actions = new FlowLayoutPanel { AutoSize = true, WrapContents = true }; actions.Controls.Add(checkUpdate); actions.Controls.Add(installUpdate); updateFields.Controls.Add(actions);
            updateStatus.Dock = DockStyle.None; updateStatus.Size = new Size(800, 110); updateFields.Controls.Add(updateStatus); updateFields.Controls.Add(updateNote);
            automaticUpdates.CheckedChanged += (s, e) => SaveUpdatePreferences(); updatesAtStartup.CheckedChanged += (s, e) => SaveUpdatePreferences(); updateMinutes.ValueChanged += (s, e) => SaveUpdatePreferences();
            checkUpdate.Click += async (s, e) => await CheckUpdates(true); installUpdate.Click += async (s, e) => await DownloadAndInstallUpdate();
            updateTimer.Tick += async (s, e) => await CheckUpdates(false);
            offerTimer.Tick += (s, e) => QueueUpdateOffer();
        }
        private void LocalizeUpdates()
        {
            automaticUpdates.Text = L.T("update.automatic"); automaticUpdates.AccessibleName = L.T("update.automaticName");
            updatesAtStartup.Text = L.T("update.onStartup"); updatesAtStartup.AccessibleName = L.T("update.onStartupName");
            updateIntervalLabel.Text = L.T("update.interval"); updateMinutes.AccessibleName = L.T("update.intervalName"); updateMinutes.AccessibleDescription = L.T("update.intervalRange");
            Accessibility.Number(updateMinutes);
            updateVersionLabel.Text = L.T("update.current", L.Version); updateNote.Text = L.T("update.note");
            SetButton(checkUpdate, "update.check", "update.checkName"); SetButton(installUpdate, "update.install", "update.installName");
            updateStatus.AccessibleName = L.T("update.statusName");
            updateStatus.Text = availableUpdate == null ? L.T("update.source") : L.T("update.available", availableUpdate.Manifest.DisplayVersion);
        }
        private void UpdateUpdateActions()
        {
            updateFields.Enabled = !busy && !updateInstalling;
            checkUpdate.Enabled = !busy && !updateChecking && !updateInstalling;
            installUpdate.Enabled = !busy && !updateChecking && !updateInstalling && availableUpdate != null;
            updatesAtStartup.Enabled = automaticUpdates.Checked; updateMinutes.Enabled = automaticUpdates.Checked;
        }
        private void SaveUpdatePreferences()
        {
            if (changingPreferences) return;
            var next = settings.Copy(); next.CheckUpdates = automaticUpdates.Checked; next.CheckUpdatesOnStartup = updatesAtStartup.Checked; next.UpdateIntervalMinutes = (int)updateMinutes.Value;
            if (!Persist(next))
            {
                changingPreferences = true; automaticUpdates.Checked = settings.CheckUpdates; updatesAtStartup.Checked = settings.CheckUpdatesOnStartup;
                updateMinutes.Value = settings.UpdateIntervalMinutes; changingPreferences = false;
            }
            ConfigureUpdateTimer(); UpdateUpdateActions();
        }
        private void ConfigureUpdateTimer()
        {
            updateTimer.Stop(); updateTimer.Interval = settings.UpdateIntervalMinutes * 60000;
            updateTimer.Enabled = started && settings.CheckUpdates && !SuppressAutomaticUpdateChecks && !disposed;
        }
        private async void StartUpdateChecks()
        {
            ConfigureUpdateTimer();
            if (settings.CheckUpdates && settings.CheckUpdatesOnStartup && !SuppressAutomaticUpdateChecks && !disposed) await CheckUpdates(false);
        }
        private async Task CheckUpdates(bool manual)
        {
            if (updateChecking || updateInstalling || showingUpdateOffer || disposed) return;
            updateChecking = true; UpdateUpdateActions(); updateStatus.Text = L.T("update.checking");
            if (manual) { tabs.SelectedIndex = 4; Announce(updateStatus.Text); }
            try
            {
                using (var transport = new GithubTransport()) availableUpdate = await new UpdateFeed(transport).CheckAsync(new Version(BuildInfo.AssemblyVersion), updateCancellation.Token);
                if (disposed) return;
                updateStatus.Text = availableUpdate == null ? L.T("update.upToDate", L.Version, DateTime.Now.ToString("HH:mm:ss")) : L.T("update.available", availableUpdate.Manifest.DisplayVersion);
                if (manual || availableUpdate != null) Announce(updateStatus.Text);
                if (availableUpdate != null && (manual || notifiedVersion != availableUpdate.Manifest.Version)) pendingUpdateOffer = true;
                else if (manual && availableUpdate == null) SystemDialogs.Info(this, updateStatus.Text, "Radeoner");
            }
            catch (OperationCanceledException) { if (!disposed) updateStatus.Text = L.T("update.cancelled"); }
            catch (Exception ex) { if (!disposed) { updateStatus.Text = L.T("update.checkFailed", ex.Message); if (manual) Announce(updateStatus.Text); } }
            finally { updateChecking = false; if (!disposed) { UpdateUpdateActions(); QueueUpdateOffer(); } }
        }
        private void QueueUpdateOffer()
        {
            if (disposed) return;
            offerTimer.Enabled = pendingUpdateOffer;
            if (!pendingUpdateOffer || disposed || busy || updateChecking || updateInstalling || showingUpdateOffer || !IsHandleCreated) return;
            BeginInvoke((Action)(async () => { if (!pendingUpdateOffer || disposed || busy || updateChecking || updateInstalling || showingUpdateOffer || !Enabled) return; pendingUpdateOffer = false; offerTimer.Stop(); await DownloadAndInstallUpdate(); }));
        }
        private async Task DownloadAndInstallUpdate()
        {
            if (availableUpdate == null || busy || updateChecking || updateInstalling || disposed || showingUpdateOffer) return;
            ShowFromTray();
            bool accepted; showingUpdateOffer = true; notifiedVersion = availableUpdate.Manifest.Version;
            string notes = UpdateFeed.ReadableNotes(availableUpdate.ReleaseNotes);
            string preview = notes.Length > 1800 ? notes.Substring(0, 1800) + "\r\n" + L.T("update.notesTruncated") : notes;
            try { accepted = SystemDialogs.Show(this, "Radeoner", L.T("update.availableTitle", availableUpdate.Manifest.DisplayVersion),
                L.T("update.confirm", L.Version, availableUpdate.Manifest.DisplayVersion) + (dirty ? "\r\n" + L.T("update.unsaved") : "") + "\r\n\r\n" +
                L.T("update.changes") + "\r\n" + preview, true,
                new DialogAction { Id = 101, Text = L.T("update.fullNotes"), Invoke = window => SystemDialogs.Details(new SystemDialogs.WindowOwner(window), L.T("update.changes"), notes) }) == 6; }
            finally { showingUpdateOffer = false; }
            if (!accepted) return;
            updateInstalling = true; SetBusy(true); UpdatePlan plan = null; bool workerStarted = false;
            try
            {
                await FinishSensorRead(); Announce(L.T("update.downloading")); updateStatus.Text = status.Text;
                byte[] archive;
                using (var transport = new GithubTransport()) archive = await new UpdateFeed(transport).DownloadAsync(availableUpdate, updateCancellation.Token);
                using (var parent = Process.GetCurrentProcess())
                    plan = await Task.Run(() => UpdateInstall.Stage(availableUpdate, archive, AppDomain.CurrentDomain.BaseDirectory, parent, false, false));
                Announce(L.T("update.restarting"));
                using (var worker = UpdateInstall.StartWorker(plan)) { if (worker == null) throw new IOException(L.T("update.workerFailed")); }
                workerStarted = true; exitRequested = true; timer.Stop(); updateTimer.Stop(); busy = false; Close();
            }
            catch (Exception ex)
            {
                if (plan != null && !workerStarted) try { UpdateInstall.CleanStaging(plan); } catch { }
                if (!disposed) { updateStatus.Text = L.T("update.installFailed", ex.Message); Announce(updateStatus.Text); }
            }
            finally { updateInstalling = false; SetBusy(false); }
        }
        private async Task CompleteUpdate()
        {
            SetBusy(true);
            try
            {
                var plan = await UpdateInstall.ConfirmAsync(UpdateCompletionPlan);
                Announce(L.T("update.completed", plan.DisplayVersion));
                SetBusy(false);
                if (plan.SmokeExit) await RequestExit();
            }
            catch (Exception ex) { if (!disposed) Announce(L.T("update.installFailed", ex.Message)); }
            finally { SetBusy(false); }
        }
        private void DisposeUpdates() { updateTimer.Dispose(); offerTimer.Dispose(); updateCancellation.Cancel(); updateCancellation.Dispose(); }
    }
}
