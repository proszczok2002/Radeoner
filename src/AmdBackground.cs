using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Principal;
using System.Threading;
using Microsoft.Win32;

namespace Radeoner
{
    [DataContract]
    internal sealed class AmdBackgroundState
    {
        [DataMember] internal string Id, Command, Mode;
        [DataMember] internal bool Running, Delayed;
    }

    internal sealed class AmdBackgroundItem
    {
        internal AmdBackgroundState State;
        internal bool Busy, Verified;
        internal Action<AmdBackgroundState> Change;
    }

    internal static class AmdBackground
    {
        internal const int Success = 0, Nothing = 3, Active = 4, Failed = 5, RestorePending = 6;
        private const string RegistryPath = @"SOFTWARE\Radeoner\AmdBackground";
        private const string BackupValue = "Backup";

        internal static bool HasBackup()
        {
            using (var key = Registry.LocalMachine.OpenSubKey(RegistryPath))
                return key != null && key.GetValue(BackupValue) != null;
        }

        internal static int Request(bool pause)
        {
            using (var process = Process.Start(new ProcessStartInfo(typeof(AmdBackground).Assembly.Location,
                "--amd-background " + (pause ? "pause" : "restore"))
            { UseShellExecute = true, Verb = "runas", WorkingDirectory = Environment.SystemDirectory }))
            {
                if (process == null) return Failed;
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        internal static int Run(bool pause)
        {
            try
            {
                using (var identity = WindowsIdentity.GetCurrent())
                    if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator)) return Failed;
                using (var mutex = new Mutex(false, @"Global\Radeoner-AmdBackground"))
                {
                    try { if (!mutex.WaitOne(0)) return Active; }
                    catch (AbandonedMutexException) { }
                    try { return RunLocked(pause); }
                    finally { mutex.ReleaseMutex(); }
                }
            }
            catch (Exception) { return Failed; }
        }

        private static int RunLocked(bool pause)
        {
            var serializer = new DataContractJsonSerializer(typeof(List<AmdBackgroundState>));
            var saved = new List<AmdBackgroundState>();
            using (var key = Registry.LocalMachine.OpenSubKey(RegistryPath))
            {
                var data = key == null ? null : key.GetValue(BackupValue);
                if (data != null)
                    using (var input = new MemoryStream((byte[])data)) saved = (List<AmdBackgroundState>)serializer.ReadObject(input);
            }
            if (pause && saved.Count != 0) return RestorePending;
            if (!pause && saved.Count == 0) return Nothing;
            if (pause && AmdBackgroundWindows.InstallManagerIsRunning()) return Active;
            var items = new AmdBackgroundWindows().ReadItems();
            return Apply(pause, items, saved, states =>
            {
                using (var key = Registry.LocalMachine.CreateSubKey(RegistryPath))
                {
                    if (states.Count == 0) key.DeleteValue(BackupValue, false);
                    else
                        using (var output = new MemoryStream())
                        {
                            serializer.WriteObject(output, states);
                            key.SetValue(BackupValue, output.ToArray(), RegistryValueKind.Binary);
                        }
                    key.Flush();
                }
            });
        }

        internal static int Apply(bool pause, List<AmdBackgroundItem> items, List<AmdBackgroundState> saved,
            Action<List<AmdBackgroundState>> save)
        {
            if (!pause) return Restore(items, saved, save) ? Success : RestorePending;
            var enabled = items.Where(item => item.Verified && item.State.Mode != "Disabled").ToList();
            if (enabled.Count == 0) return Nothing;
            if (enabled.Any(item => item.Busy)) return Active;
            try
            {
                foreach (var item in enabled)
                {
                    saved.Add(item.State);
                    save(saved);
                    item.Change(null);
                }
                return Success;
            }
            catch (Exception)
            {
                return Restore(items, saved, save) ? Failed : RestorePending;
            }
        }

        private static bool Restore(List<AmdBackgroundItem> items, List<AmdBackgroundState> saved,
            Action<List<AmdBackgroundState>> save)
        {
            for (int index = saved.Count - 1; index >= 0; index--)
            {
                var previous = saved[index];
                var item = items.FirstOrDefault(current => current.State.Id.Equals(previous.Id, StringComparison.OrdinalIgnoreCase)
                    && current.State.Command.Equals(previous.Command, StringComparison.OrdinalIgnoreCase));
                try
                {
                    if (item != null)
                    {
                        if (!item.Verified) continue;
                        item.Change(previous);
                    }
                    var remaining = new List<AmdBackgroundState>(saved);
                    remaining.RemoveAt(index);
                    save(remaining);
                    saved.RemoveAt(index);
                }
                catch (Exception) { }
            }
            return saved.Count == 0;
        }
    }
}
