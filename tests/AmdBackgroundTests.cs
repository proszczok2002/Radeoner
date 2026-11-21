using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Radeoner;

internal static partial class Tests
{
    private static void RunAmdBackgroundTests()
    {
        Test("Only optional AMD background task actions and installer services are eligible", () =>
        {
            string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "AMD");
            string manager = Path.Combine(root, @"AMDInstallManager\AMDInstallManager.exe");
            string legacy = Path.Combine(root, @"CIM\Bin64\InstallManagerApp.exe");
            string adrenalin = Path.Combine(root, @"CNext\CNext\RadeonSoftware.exe");
            string crashDefender = Path.Combine(Environment.SystemDirectory, "amdfendrsr.exe");
            Check(AmdBackgroundWindows.KnownExecutable(manager, "-CheckForUpdates", true), "AMD Install Manager not detected");
            Check(AmdBackgroundWindows.KnownExecutable(legacy, "/AUTOUPDATEIN", true), "Legacy AMD Updater not detected");
            Check(!AmdBackgroundWindows.KnownExecutable(legacy, "/INSTALL", true), "Manual AMD installer was included");
            Check(!AmdBackgroundWindows.KnownExecutable(crashDefender, null, false), "Crash Defender was included");
            Check(!AmdBackgroundWindows.KnownExecutable(Path.Combine(root, @"AMDInstallManager\..\amdfendrsr.exe"), null, false), "Traversal was included");
            Check(AmdBackgroundWindows.KnownExecutable(adrenalin, "-runtime", true) &&
                !AmdBackgroundWindows.KnownExecutable(adrenalin, "-runtime", false), "Adrenalin task and driver services are not separated");

            Func<string, string> action = path => "<Exec><Command>" + path + "</Command></Exec>";
            Func<string, string> parse = actions => AmdBackgroundWindows.TaskCommand(
                "<Task xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\"><Actions>" + actions + "</Actions></Task>",
                AmdBackgroundWindows.KnownExecutable);
            Check(parse(action(manager)) != null, "Modern Install Manager task was not recognized");
            Check(parse(action(manager) + action(crashDefender)) == null, "Mixed task was included");
            Check(parse(action(manager) + "<ComHandler/>") == null, "Unrecognized task action was included");
        });

        Test("AMD pause saves original settings before changes and restores only matching components", () =>
        {
            var saved = new List<AmdBackgroundState>();
            var persisted = new List<AmdBackgroundState>();
            var calls = new List<string>();
            Action<List<AmdBackgroundState>> save = state => persisted = state.ToList();
            var task = BackgroundFixture("task", "Enabled");
            var service = BackgroundFixture("service", "Auto");
            service.State.Running = service.State.Delayed = true;
            var unchanged = BackgroundFixture("disabled", "Disabled");
            task.Change = previous =>
            {
                Check(previous == null && persisted.Contains(task.State), "Task was not backed up before editing");
                calls.Add("pause task");
            };
            service.Change = previous =>
            {
                Check(persisted.Contains(service.State), "Service was not backed up before editing");
                if (previous != null) Check(previous.Mode == "Auto" && previous.Running && previous.Delayed, "Service state was lost");
                calls.Add(previous == null ? "pause service" : "restore service");
            };
            var items = new List<AmdBackgroundItem> { task, service, unchanged };
            Check(AmdBackground.Apply(true, items, saved, save) == AmdBackground.Success, "Pause failed");
            items[0] = BackgroundFixture("task", "Disabled");
            items[0].State.Command = "replacement";
            Check(AmdBackground.Apply(false, items, saved, save) == AmdBackground.Success, "Restore failed");
            Check(saved.Count == 0 && persisted.Count == 0, "Restore left a backup");
            Check(calls.SequenceEqual(new[] { "pause task", "pause service", "restore service" }), "Target selection or restore was incorrect");
        });

        Test("AMD pause rolls back changes and keeps a durable retry when a restore fails", () =>
        {
            var saved = new List<AmdBackgroundState>();
            var persisted = new List<AmdBackgroundState>();
            Action<List<AmdBackgroundState>> save = state => persisted = state.ToList();
            var task = BackgroundFixture("task", "Enabled");
            var service = BackgroundFixture("service", "Manual");
            bool taskPaused = false, failTaskRestore = true;
            task.Change = previous =>
            {
                if (previous != null && failTaskRestore) throw new IOException();
                taskPaused = previous == null;
            };
            service.Change = previous => { if (previous == null) throw new IOException(); };
            var items = new List<AmdBackgroundItem> { task, service };
            service.Busy = true;
            Check(AmdBackground.Apply(true, items, saved, save) == AmdBackground.Active && saved.Count == 0 && !taskPaused,
                "An active task or transitional service caused a partial change");
            service.Busy = false;
            Check(AmdBackground.Apply(true, items, saved, save) == AmdBackground.RestorePending && taskPaused, "Failed rollback was hidden");
            Check(persisted.SequenceEqual(new[] { task.State }), "Retry state was lost");
            failTaskRestore = false;
            task.Verified = false;
            Check(AmdBackground.Apply(false, items, persisted, save) == AmdBackground.RestorePending && taskPaused && persisted.Count == 1,
                "A temporary signature failure discarded the backup or restored an unverified task");
            task.Verified = true;
            Check(AmdBackground.Apply(false, items, persisted, save) == AmdBackground.Success && !taskPaused, "Retry did not restore the task");
        });

        Test("The AMD settings check box exposes its name and cancellation returns without requesting elevation", () =>
        {
            using (var form = TestForm("amd-background"))
            {
                SetField(form, "started", true);
                form.Opacity = 0;
                form.ShowInTaskbar = false;
                form.Show();
                Call(form, "SelectPage", 4);
                var checkBox = Field<CheckBox>(form, "pauseAmd");
                bool checkedBefore = checkBox.Checked;
                int confirmations = 0;
                SystemDialogs.TestShow = (title, body, question) =>
                {
                    confirmations++;
                    Check(title == L.T("amdBackground.group") && question && body == L.T(checkedBefore ?
                        "amdBackground.confirmRestore" : "amdBackground.confirmPause"), "Wrong confirmation");
                    return 7; // No
                };
                try
                {
                    Check(checkBox.AccessibilityObject.Role == AccessibleRole.CheckButton && checkBox.TabStop &&
                        checkBox.AccessibilityObject.Name == L.T("amdBackground.check"), "Check box lost keyboard or MSAA semantics");
                    Check(checkBox.Focus(), "AMD check box could not be focused");
                    checkBox.AccessibilityObject.DoDefaultAction();
                    Check(confirmations == 1 && checkBox.Checked == checkedBefore && checkBox.Enabled, "Cancellation changed the check box");
                }
                finally { SystemDialogs.TestShow = null; SetField(form, "exitRequested", true); form.Close(); }
            }
        });
    }

    private static AmdBackgroundItem BackgroundFixture(string id, string mode)
    {
        return new AmdBackgroundItem
        {
            State = new AmdBackgroundState { Id = id, Command = id, Mode = mode },
            Verified = true,
            Change = previous => { throw new Exception("Unexpected change for " + id); }
        };
    }
}
