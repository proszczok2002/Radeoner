using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Radeoner
{
    public sealed partial class MainForm
    {
        private readonly GroupBox amdBackgroundGroup = new GroupBox();
        private readonly Label amdBackgroundNote = Note("");
        private readonly CheckBox pauseAmd = new ThemedCheckBox { AutoCheck = false, AutoSize = true };

        private void BuildAmdBackground()
        {
            var fields = SettingsGroup(amdBackgroundGroup);
            fields.Controls.Add(pauseAmd);
            fields.Controls.Add(amdBackgroundNote);
            RefreshAmdBackgroundChoice();
            pauseAmd.Click += async (s, e) => await ChangeAmdBackground();
        }

        private void LocalizeAmdBackground()
        {
            amdBackgroundGroup.Text = amdBackgroundGroup.AccessibleName = L.T("amdBackground.group");
            pauseAmd.Text = pauseAmd.AccessibleName = L.T("amdBackground.check");
            amdBackgroundNote.Text = pauseAmd.AccessibleDescription = L.T("amdBackground.note");
        }

        private void RefreshAmdBackgroundChoice()
        {
            try { pauseAmd.Checked = AmdBackground.HasBackup(); }
            catch (Exception ex) { Diagnostics.Log("read AMD background state", ex); }
        }

        private async Task ChangeAmdBackground()
        {
            if (busy || updateInstalling) return;
            RefreshAmdBackgroundChoice();
            bool pause = !pauseAmd.Checked;
            string title = L.T("amdBackground.group"), message = null;
            SetBusy(true);
            try
            {
                if (!SystemDialogs.Confirm(this, L.T(pause ? "amdBackground.confirmPause" : "amdBackground.confirmRestore"), title))
                    return;
                Announce(L.T("amdBackground.working"));
                int result = await Task.Run(() => AmdBackground.Request(pause));
                switch (result)
                {
                    case AmdBackground.Success:
                        Announce(L.T(pause ? "amdBackground.paused" : "amdBackground.restored"));
                        break;
                    case AmdBackground.Nothing:
                        message = L.T(pause ? "amdBackground.nothing" : "amdBackground.restored");
                        break;
                    case AmdBackground.Active:
                        message = L.T("amdBackground.active");
                        break;
                    case AmdBackground.RestorePending:
                        message = L.T("amdBackground.pending");
                        break;
                    default:
                        message = L.T("amdBackground.failed");
                        break;
                }
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == 1223) Announce(L.T("amdBackground.cancelled"));
                else { Diagnostics.Log("change AMD background", ex); message = L.T("amdBackground.failed"); }
            }
            catch (Exception ex)
            {
                Diagnostics.Log("change AMD background", ex);
                message = L.T("amdBackground.failed");
            }
            finally
            {
                if (!disposed) RefreshAmdBackgroundChoice();
                SetBusy(false);
            }
            if (message != null && !disposed)
            {
                Announce(message);
                SystemDialogs.Info(this, message, title);
            }
        }
    }
}
