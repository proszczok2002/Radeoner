using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Microsoft.Win32.SafeHandles;

namespace Radeoner
{
    internal sealed class AmdBackgroundWindows
    {
        private static readonly string[] AmdFolders = new[] { Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86 }
            .Select(folder => Path.Combine(Environment.GetFolderPath(folder), "AMD") + "\\").ToArray();
        private readonly Dictionary<string, bool> trusted = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        internal static bool KnownExecutable(string path, string arguments, bool task)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            path = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
            try
            {
                if (!Path.IsPathRooted(path)) return false;
                path = Path.GetFullPath(path);
            }
            catch (ArgumentException) { return false; }
            catch (NotSupportedException) { return false; }
            catch (PathTooLongException) { return false; }
            string root = AmdFolders.FirstOrDefault(folder => path.StartsWith(folder, StringComparison.OrdinalIgnoreCase));
            if (root == null) return false;
            string relative = path.Substring(root.Length).ToUpperInvariant();
            if (relative.StartsWith(@"AMDINSTALLMANAGER\", StringComparison.Ordinal) && relative.EndsWith(".EXE", StringComparison.Ordinal))
                return true;
            if (relative == @"CIM\BIN64\INSTALLMANAGERAPP.EXE")
                return Regex.IsMatch(arguments ?? "", @"(?:^|\s)/AUTOUPDATEIN(?:\s|$)", RegexOptions.IgnoreCase);
            return task && (relative == @"CNEXT\CNEXT\RADEONSOFTWARE.EXE" || relative == @"CNEXT\CNEXT\AMDRSSERV.EXE");
        }

        private bool TrustedExecutable(string path, string arguments, bool task)
        {
            if (!KnownExecutable(path, arguments, task)) return false;
            path = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim().Trim('"')));
            bool verified;
            if (trusted.TryGetValue(path, out verified)) return verified;
            try
            {
                using (var file = File.OpenRead(path)) AmdPackageTrust.VerifyAndHash(file);
                verified = true;
            }
            catch (IOException) { verified = false; }
            catch (UnauthorizedAccessException) { verified = false; }
            trusted[path] = verified;
            return verified;
        }

        internal static bool InstallManagerIsRunning()
        {
            foreach (var process in Process.GetProcessesByName("AMDInstallManager"))
                using (process)
                {
                    try { if (KnownExecutable(process.MainModule.FileName, null, false)) return true; }
                    catch (InvalidOperationException) { } // Process exited during the check.
                }
            return false;
        }

        internal List<AmdBackgroundItem> ReadItems()
        {
            var items = new List<AmdBackgroundItem>();
            dynamic scheduler = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service", true));
            scheduler.Connect();
            foreach (string folderPath in new[] { @"\", @"\AMD", @"\AMD\AMD Install Manager" })
            {
                dynamic folder;
                try { folder = scheduler.GetFolder(folderPath); }
                catch (FileNotFoundException) { continue; }
                catch (DirectoryNotFoundException) { continue; }
                dynamic tasks = folder.GetTasks(1); // TASK_ENUM_HIDDEN
                for (int index = 1; index <= (int)tasks.Count; index++)
                {
                    dynamic task = tasks[index];
                    string xml = task.Xml;
                    string command = TaskCommand(xml, KnownExecutable);
                    if (command == null) continue;
                    string name = task.Name, path = task.Path;
                    int status = task.State;
                    var state = new AmdBackgroundState { Id = "task:" + path, Command = command, Mode = task.Enabled ? "Enabled" : "Disabled" };
                    items.Add(new AmdBackgroundItem
                    {
                        State = state,
                        Verified = TaskCommand(xml, TrustedExecutable) != null,
                        Busy = status == 2 || status == 4, // Queued or running.
                        Change = previous =>
                        {
                            dynamic current = folder.GetTask(name);
                            if (!string.Equals(command, TaskCommand((string)current.Xml, TrustedExecutable), StringComparison.OrdinalIgnoreCase))
                                throw new InvalidOperationException(path);
                            if (previous == null && ((int)current.State == 2 || (int)current.State == 4))
                                throw new InvalidOperationException(path);
                            current.Enabled = previous != null;
                        }
                    });
                }
            }
            using (var query = new ManagementObjectSearcher("SELECT * FROM Win32_Service"))
            using (var services = query.Get())
                foreach (ManagementObject service in services)
                    using (service)
                    {
                        string command = service["PathName"] as string;
                        if (!IsInstallerService(command, KnownExecutable) || (string)service["ServiceType"] != "Own Process") continue;
                        string mode = (string)service["StartMode"], status = (string)service["State"], name = (string)service["Name"];
                        if (mode != "Auto" && mode != "Manual" && mode != "Disabled") continue;
                        var state = new AmdBackgroundState { Id = "service:" + name, Command = command, Mode = mode,
                            Running = status == "Running", Delayed = (bool?)service["DelayedAutoStart"] == true };
                        string servicePath = service.Path.Path;
                        items.Add(new AmdBackgroundItem
                        {
                            State = state,
                            Verified = IsInstallerService(command, TrustedExecutable),
                            Busy = status != "Running" && status != "Stopped",
                            Change = previous => ChangeService(servicePath, state, previous)
                        });
                    }
            return items;
        }

        internal static string TaskCommand(string xml, Func<string, string, bool, bool> approve)
        {
            var document = new XmlDocument { XmlResolver = null };
            document.LoadXml(xml);
            var namespaces = new XmlNamespaceManager(document.NameTable);
            namespaces.AddNamespace("t", "http://schemas.microsoft.com/windows/2004/02/mit/task");
            var commands = new List<string>();
            foreach (XmlNode action in document.SelectNodes("/t:Task/t:Actions/*", namespaces))
            {
                if (action.LocalName != "Exec") return null;
                var commandNode = action.SelectSingleNode("t:Command", namespaces);
                var argumentsNode = action.SelectSingleNode("t:Arguments", namespaces);
                string command = commandNode == null ? null : commandNode.InnerText;
                string arguments = argumentsNode == null ? null : argumentsNode.InnerText;
                if (!approve(command, arguments, true)) return null;
                commands.Add(command + "\n" + arguments);
            }
            return commands.Count == 0 ? null : string.Join("\n\n", commands);
        }

        private static bool IsInstallerService(string command, Func<string, string, bool, bool> approve)
        {
            // SCM image paths may be quoted or contain spaces and trailing arguments.
            var match = Regex.Match(command ?? "", @"^\s*(?:""([^""]+\.exe)""|([^""]+?\.exe))(?=\s|$)", RegexOptions.IgnoreCase);
            string path = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            return match.Success && approve(path, command.Substring(match.Length), false);
        }

        private static void ChangeService(string path, AmdBackgroundState state, AmdBackgroundState previous)
        {
            using (var service = new ManagementObject(path))
            {
                service.Get();
                if (!string.Equals(state.Command, (string)service["PathName"], StringComparison.OrdinalIgnoreCase) ||
                    (string)service["ServiceType"] != "Own Process")
                    throw new InvalidOperationException(state.Id);
                string name = (string)service["Name"], status = (string)service["State"];
                if (previous == null)
                {
                    if (status != "Running" && status != "Stopped") throw new InvalidOperationException(name);
                    SetStartMode(name, 4, null);
                    if (status == "Running")
                    {
                        InvokeService(service, "StopService", 6); // No cascading stops of dependent services.
                        WaitForState(service, "Stopped");
                    }
                }
                else
                {
                    bool automatic = previous.Mode == "Auto";
                    SetStartMode(name, automatic ? 2u : 3u, automatic ? (bool?)previous.Delayed : null);
                    if (!previous.Running || status == "Running") return;
                    if (status == "Stop Pending") WaitForState(service, "Stopped");
                    InvokeService(service, "StartService", 10);
                    WaitForState(service, "Running");
                }
            }
        }

        private static void InvokeService(ManagementObject service, string method, uint expected)
        {
            var result = (uint)service.InvokeMethod(method, (object[])null);
            if (result != 0 && result != expected) throw new InvalidOperationException(method + " " + result);
        }

        private static void WaitForState(ManagementObject service, string expected)
        {
            var watch = Stopwatch.StartNew();
            do
            {
                service.Get();
                if ((string)service["State"] == expected) return;
                Thread.Sleep(150);
            } while (watch.Elapsed < TimeSpan.FromSeconds(8));
            throw new TimeoutException(expected);
        }

        private static void SetStartMode(string name, uint mode, bool? delayed)
        {
            const uint noChange = 0xffffffff;
            using (var manager = OpenSCManager(null, null, 1)) // SC_MANAGER_CONNECT
            {
                if (manager.IsInvalid) throw new Win32Exception();
                using (var service = OpenService(manager, name, 2)) // SERVICE_CHANGE_CONFIG
                {
                    if (service.IsInvalid || !ChangeServiceConfig(service, noChange, mode, noChange, null, IntPtr.Zero,
                        IntPtr.Zero, IntPtr.Zero, null, null, null)) throw new Win32Exception();
                    if (delayed.HasValue)
                    {
                        int flag = delayed.Value ? 1 : 0;
                        if (!ChangeServiceConfig2(service, 3, ref flag)) throw new Win32Exception(); // SERVICE_CONFIG_DELAYED_AUTO_START_INFO
                    }
                }
            }
        }

        private sealed class ServiceHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private ServiceHandle() : base(true) { }
            protected override bool ReleaseHandle() { return CloseServiceHandle(handle); }
        }
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern ServiceHandle OpenSCManager(string machine, string database, uint access);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern ServiceHandle OpenService(ServiceHandle manager, string name, uint access);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool ChangeServiceConfig(ServiceHandle service, uint type, uint start, uint error,
            string image, IntPtr group, IntPtr tag, IntPtr dependencies, string account, string password, string display);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool ChangeServiceConfig2(ServiceHandle service, uint level, ref int info);
        [DllImport("advapi32.dll")]
        private static extern bool CloseServiceHandle(IntPtr handle);
    }
}
