using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Radeoner
{
    [DataContract]
    public sealed class UpdatePlan
    {
        [DataMember(IsRequired = true)] public int Schema = 1;
        [DataMember(IsRequired = true)] public string Nonce;
        [DataMember(IsRequired = true)] public string TargetDirectory;
        [DataMember(IsRequired = true)] public string StagingDirectory;
        [DataMember(IsRequired = true)] public string OldVersion;
        [DataMember(IsRequired = true)] public string NewVersion;
        [DataMember(IsRequired = true)] public string DisplayVersion;
        [DataMember(IsRequired = true)] public string OldExeSha256;
        [DataMember(IsRequired = true)] public List<UpdateFile> Files;
        [DataMember(IsRequired = true)] public int ParentPid;
        [DataMember(IsRequired = true)] public long ParentStartTicks;
        [DataMember] public int WorkerPid;
        [DataMember] public long WorkerStartTicks;
        [DataMember] public bool StartInTray;
        [DataMember] public bool SmokeExit;
        [DataMember] public string Language;
        [DataMember] public bool FilesInstalled;
        [DataMember] public List<string> OriginalPaths = new List<string>();
        [DataMember] public List<string> AttemptedPaths = new List<string>();
    }
    [DataContract]
    internal sealed class UpdateReady
    {
        [DataMember] internal string Nonce;
        [DataMember] internal int Pid;
        [DataMember] internal string Version;
    }
    [DataContract]
    internal sealed class UpdateOutcome
    {
        [DataMember] internal bool Success;
        [DataMember] internal string Message;
    }
    public static class UpdateInstall
    {
        internal const string PlanName = "plan.json", HelperName = "Radeoner.Update.exe";
        internal static string FilePath(string root, string relative) { return Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)); }
        private static bool EqualPath(string a, string b) { return string.Equals(Path.GetFullPath(a).TrimEnd('\\'), Path.GetFullPath(b).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase); }
        internal static void NoReparse(string path)
        {
            if ((File.Exists(path) || Directory.Exists(path)) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException(L.T("update.reparse"));
        }
        internal static void ValidatePlan(UpdatePlan plan, string planPath)
        {
            Guid nonce; Version oldVersion, newVersion;
            if (plan == null || plan.Schema != 1 || !Guid.TryParseExact(plan.Nonce, "N", out nonce) ||
                !Version.TryParse(plan.OldVersion, out oldVersion) || !Version.TryParse(plan.NewVersion, out newVersion) || newVersion <= oldVersion ||
                !UpdateFeed.ValidHash(plan.OldExeSha256) || plan.ParentPid <= 0 || plan.ParentStartTicks <= 0 ||
                string.IsNullOrEmpty(plan.TargetDirectory) || string.IsNullOrEmpty(plan.StagingDirectory) ||
                !Path.IsPathRooted(plan.TargetDirectory) || !Path.IsPathRooted(plan.StagingDirectory) ||
                !EqualPath(Path.GetDirectoryName(plan.StagingDirectory), plan.TargetDirectory) ||
                Path.GetFileName(plan.StagingDirectory) != ".radeoner-update-" + plan.Nonce ||
                !EqualPath(planPath, Path.Combine(plan.StagingDirectory, PlanName))) throw new InvalidOperationException(L.T("update.badPlan"));
            NoReparse(plan.TargetDirectory); NoReparse(plan.StagingDirectory);
            if (plan.Files == null || plan.Files.Count != UpdateFeed.PackagePaths.Length) throw new InvalidOperationException(L.T("update.badPlan"));
            if (plan.OriginalPaths == null || plan.AttemptedPaths == null || plan.OriginalPaths.Any(p => !UpdateFeed.PackagePaths.Contains(p)) || plan.AttemptedPaths.Any(p => !UpdateFeed.PackagePaths.Contains(p))) throw new InvalidOperationException(L.T("update.badPlan"));
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in plan.Files)
                if (f == null || !UpdateFeed.PackagePaths.Contains(f.Path, StringComparer.Ordinal) || !paths.Add(f.Path) || !UpdateFeed.ValidHash(f.Sha256) || f.Size <= 0 || f.Size > 16 * 1024 * 1024)
                    throw new InvalidOperationException(L.T("update.badPlan"));
            foreach (string folder in new[] { "payload", "backup" }) NoReparse(Path.Combine(plan.StagingDirectory, folder));
        }
        internal static void ValidateExecutable(string path, string expectedVersion)
        {
            var info = FileVersionInfo.GetVersionInfo(path); Version actual;
            if (info.ProductName != "Radeoner" || !Version.TryParse(info.FileVersion, out actual) || actual != new Version(expectedVersion))
                throw new InvalidOperationException(L.T("update.wrongExecutable"));
        }
        internal static void VerifyFile(string path, UpdateFile expected)
        {
            NoReparse(path);
            if (!File.Exists(path) || new FileInfo(path).Length != expected.Size || !string.Equals(UpdateFeed.Hash(path), expected.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(L.T("update.fileMismatch", expected.Path));
        }
        public static UpdatePlan Stage(AvailableUpdate release, byte[] archive, string targetDirectory, Process parent, bool startInTray, bool smokeExit)
        {
            UpdateFeed.ValidateManifest(release.Manifest);
            if (archive.LongLength != release.Manifest.Size || !string.Equals(UpdateFeed.Hash(archive), release.Manifest.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException(L.T("update.hashMismatch"));
            string target = Path.GetFullPath(targetDirectory).TrimEnd('\\');
            NoReparse(target); if (!Directory.Exists(target)) throw new InvalidOperationException(L.T("update.badPlan"));
            string currentExe = Path.Combine(target, "Radeoner.exe");
            var current = FileVersionInfo.GetVersionInfo(currentExe);
            if (current.ProductName != "Radeoner" || new Version(release.Manifest.Version) <= new Version(current.FileVersion)) throw new InvalidOperationException(L.T("update.notNewer"));
            var plan = new UpdatePlan { Nonce = Guid.NewGuid().ToString("N"), TargetDirectory = target, OldVersion = current.FileVersion, NewVersion = release.Manifest.Version,
                DisplayVersion = release.Manifest.DisplayVersion, OldExeSha256 = UpdateFeed.Hash(currentExe), Files = release.Manifest.Files,
                ParentPid = parent.Id, ParentStartTicks = parent.StartTime.ToUniversalTime().Ticks, StartInTray = startInTray, SmokeExit = smokeExit, Language = L.Language };
            plan.StagingDirectory = Path.Combine(target, ".radeoner-update-" + plan.Nonce);
            Directory.CreateDirectory(plan.StagingDirectory); File.SetAttributes(plan.StagingDirectory, File.GetAttributes(plan.StagingDirectory) | FileAttributes.Hidden);
            string planPath = Path.Combine(plan.StagingDirectory, PlanName);
            try
            {
                ValidatePlan(plan, planPath);
                string payload = Path.Combine(plan.StagingDirectory, "payload"); Directory.CreateDirectory(payload);
                using (var input = new MemoryStream(archive, false))
                using (var zip = new ZipArchive(input, ZipArchiveMode.Read))
                {
                    if (zip.Entries.Count != plan.Files.Count) throw new InvalidOperationException(L.T("update.badArchive"));
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var entry in zip.Entries)
                    {
                        // Only the exact eight release files can be extracted. No arbitrary paths,
                        // directory entries, symlinks, profiles or preferences from an archive.
                        var expected = plan.Files.SingleOrDefault(f => f.Path == entry.FullName);
                        if (expected == null || !seen.Add(entry.FullName) || entry.Length != expected.Size || ((entry.ExternalAttributes >> 16) & 0xf000) == 0xa000)
                            throw new InvalidOperationException(L.T("update.badArchive"));
                        string output = FilePath(payload, entry.FullName); Directory.CreateDirectory(Path.GetDirectoryName(output));
                        using (var stream = entry.Open()) using (var file = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        {
                            byte[] buffer = new byte[81920]; long written = 0; int count;
                            while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
                            { written += count; if (written > expected.Size) throw new InvalidOperationException(L.T("update.badArchive")); file.Write(buffer, 0, count); }
                            file.Flush(true);
                        }
                        VerifyFile(output, expected);
                    }
                }
                ValidateExecutable(Path.Combine(payload, "Radeoner.exe"), plan.NewVersion);
                File.Copy(currentExe, Path.Combine(plan.StagingDirectory, HelperName));
                if (File.Exists(currentExe + ".config")) File.Copy(currentExe + ".config", Path.Combine(plan.StagingDirectory, HelperName + ".config"));
                JsonFile.Write(planPath, plan); return plan;
            }
            catch { CleanStaging(plan); throw; }
        }
        internal static string Quote(string value)
        {
            var b = new StringBuilder("\""); int slashes = 0;
            foreach (char c in value)
            {
                if (c == '\\') { slashes++; continue; }
                if (c == '"') { b.Append('\\', slashes * 2 + 1); b.Append(c); slashes = 0; continue; }
                b.Append('\\', slashes); slashes = 0; b.Append(c);
            }
            b.Append('\\', slashes * 2); return b.Append('"').ToString();
        }
        public static Process StartWorker(UpdatePlan plan)
        {
            string path = Path.Combine(plan.StagingDirectory, PlanName); ValidatePlan(plan, path);
            return Process.Start(new ProcessStartInfo(Path.Combine(plan.StagingDirectory, HelperName), "--apply-update " + Quote(path))
            { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, WorkingDirectory = plan.StagingDirectory });
        }
        internal static UpdatePlan ReadPlan(string path)
        { var plan = JsonFile.Read<UpdatePlan>(path, 65536); ValidatePlan(plan, path); return plan; }
        internal static void CleanStaging(UpdatePlan plan)
        {
            string path = Path.GetFullPath(plan.StagingDirectory); ValidatePlan(plan, Path.Combine(path, PlanName));
            if (!Directory.Exists(path)) return;
            // Verify every entry before a recursive delete; reparse points are never followed.
            CheckTree(path); Directory.Delete(path, true);
        }
        private static void CheckTree(string directory)
        {
            NoReparse(directory);
            foreach (var path in Directory.GetFileSystemEntries(directory))
            { NoReparse(path); if (Directory.Exists(path)) CheckTree(path); }
        }
        internal static void InstallFiles(UpdatePlan plan)
        {
            string payload = Path.Combine(plan.StagingDirectory, "payload"), backup = Path.Combine(plan.StagingDirectory, "backup");
            if (!string.Equals(UpdateFeed.Hash(Path.Combine(plan.TargetDirectory, "Radeoner.exe")), plan.OldExeSha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException(L.T("update.originalChanged"));
            foreach (var file in plan.Files) VerifyFile(FilePath(payload, file.Path), file);
            Directory.CreateDirectory(backup);
            plan.OriginalPaths = plan.Files.Where(f => File.Exists(FilePath(plan.TargetDirectory, f.Path))).Select(f => f.Path).ToList();
            JsonFile.Write(Path.Combine(plan.StagingDirectory, PlanName), plan);
            foreach (var file in plan.Files)
            {
                string source = FilePath(payload, file.Path), target = FilePath(plan.TargetDirectory, file.Path), saved = FilePath(backup, file.Path);
                NoReparse(Path.GetDirectoryName(target)); NoReparse(target);
                Directory.CreateDirectory(Path.GetDirectoryName(target)); Directory.CreateDirectory(Path.GetDirectoryName(saved));
                plan.AttemptedPaths.Add(file.Path); JsonFile.Write(Path.Combine(plan.StagingDirectory, PlanName), plan);
                if (File.Exists(target)) File.Replace(source, target, saved); else File.Move(source, target);
            }
            plan.FilesInstalled = true; JsonFile.Write(Path.Combine(plan.StagingDirectory, PlanName), plan);
        }
        internal static void Rollback(UpdatePlan plan)
        {
            var errors = new List<string>();
            foreach (var file in plan.Files.AsEnumerable().Reverse())
            {
                string backup = FilePath(Path.Combine(plan.StagingDirectory, "backup"), file.Path), target = FilePath(plan.TargetDirectory, file.Path);
                try
                {
                    if (File.Exists(backup))
                    {
                        NoReparse(target); NoReparse(backup);
                        if (File.Exists(target)) File.Delete(target); File.Move(backup, target);
                    }
                    else if (!plan.OriginalPaths.Contains(file.Path) && plan.AttemptedPaths.Contains(file.Path) && File.Exists(target) && string.Equals(UpdateFeed.Hash(target), file.Sha256, StringComparison.OrdinalIgnoreCase)) File.Delete(target);
                }
                catch (Exception ex) { errors.Add(file.Path + ": " + ex.Message); }
            }
            if (errors.Count > 0) throw new IOException(string.Join("; ", errors));
        }
        internal static void DeleteBackup(UpdatePlan plan)
        {
            string backup = Path.Combine(plan.StagingDirectory, "backup");
            if (Directory.Exists(backup)) { CheckTree(backup); Directory.Delete(backup, true); }
        }
        internal static bool SameProcess(int id, long ticks)
        {
            try { using (var process = Process.GetProcessById(id)) return !process.HasExited && process.StartTime.ToUniversalTime().Ticks == ticks; }
            catch (ArgumentException) { return false; } catch (InvalidOperationException) { return false; }
        }
        internal static void WaitParent(UpdatePlan plan)
        {
            if (!SameProcess(plan.ParentPid, plan.ParentStartTicks)) return;
            using (var process = Process.GetProcessById(plan.ParentPid))
                if (!process.WaitForExit(45000)) throw new InvalidOperationException(L.T("update.parentTimeout"));
        }
        internal static int RunWorker(string planPath)
        {
            UpdatePlan plan = null; Process child = null; bool installed = false;
            try
            {
                plan = ReadPlan(planPath); L.Load(plan.Language);
                if (!EqualPath(Assembly.GetExecutingAssembly().Location, Path.Combine(plan.StagingDirectory, HelperName))) throw new InvalidOperationException(L.T("update.badPlan"));
                if (!string.Equals(UpdateFeed.Hash(Assembly.GetExecutingAssembly().Location), plan.OldExeSha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException(L.T("update.badPlan"));
                using (var self = Process.GetCurrentProcess()) { plan.WorkerPid = self.Id; plan.WorkerStartTicks = self.StartTime.ToUniversalTime().Ticks; }
                JsonFile.Write(planPath, plan); WaitParent(plan);
                installed = true; InstallFiles(plan);
                string arguments = "--complete-update " + Quote(planPath) + (plan.StartInTray ? " --tray" : "");
                child = Process.Start(new ProcessStartInfo(Path.Combine(plan.TargetDirectory, "Radeoner.exe"), arguments)
                { UseShellExecute = false, WorkingDirectory = plan.TargetDirectory, WindowStyle = plan.StartInTray ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal });
                string readyPath = Path.Combine(plan.StagingDirectory, "ready.json"); var watch = Stopwatch.StartNew(); bool ready = false;
                while (watch.Elapsed < TimeSpan.FromSeconds(60))
                {
                    if (File.Exists(readyPath))
                    {
                        var message = JsonFile.Read<UpdateReady>(readyPath, 4096);
                        if (message.Nonce != plan.Nonce || message.Pid != child.Id || message.Version != plan.NewVersion) throw new InvalidOperationException(L.T("update.badConfirmation"));
                        ready = true; break;
                    }
                    if (child.HasExited) break;
                    System.Threading.Thread.Sleep(150);
                }
                if (!ready) throw new InvalidOperationException(L.T("update.startFailed"));
                // Old executables are only removed after the new WinForms UI is initialized.
                // Cleanup can be retried by the new process after this helper exits.
                // Never roll a healthy app back after deleting part of its backup.
                try { DeleteBackup(plan); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                JsonFile.Write(Path.Combine(plan.StagingDirectory, "outcome.json"), new UpdateOutcome { Success = true }); return 0;
            }
            catch (Exception ex)
            {
                if (plan == null) return 1;
                if (child != null) try { if (!child.HasExited) { child.Kill(); child.WaitForExit(10000); } } catch { }
                string message = ex.Message;
                bool restored = true;
                if (installed) try { Rollback(plan); } catch (Exception rollback) { restored = false; message += "\r\n" + rollback.Message; }
                try { JsonFile.Write(Path.Combine(plan.StagingDirectory, "outcome.json"), new UpdateOutcome { Success = false, Message = message }); File.WriteAllText(Path.Combine(plan.StagingDirectory, "error.txt"), message, Encoding.UTF8); } catch { }
                if (!plan.SmokeExit && restored) try { Process.Start(new ProcessStartInfo(Path.Combine(plan.TargetDirectory, "Radeoner.exe"), "--update-rolled-back") { UseShellExecute = false, WorkingDirectory = plan.TargetDirectory }); } catch { }
                return 1;
            }
            finally { if (child != null) child.Dispose(); }
        }
        internal static async Task<UpdatePlan> ConfirmAsync(string planPath)
        {
            var plan = ReadPlan(planPath);
            if (!EqualPath(AppDomain.CurrentDomain.BaseDirectory, plan.TargetDirectory) || Assembly.GetExecutingAssembly().GetName().Version != new Version(plan.NewVersion) || !plan.FilesInstalled || !SameProcess(plan.WorkerPid, plan.WorkerStartTicks))
                throw new InvalidOperationException(L.T("update.badConfirmation"));
            VerifyFile(Path.Combine(plan.TargetDirectory, "Radeoner.exe"), plan.Files.Single(f => f.Path == "Radeoner.exe"));
            using (var self = Process.GetCurrentProcess()) JsonFile.Write(Path.Combine(plan.StagingDirectory, "ready.json"), new UpdateReady { Nonce = plan.Nonce, Pid = self.Id, Version = plan.NewVersion });
            await Task.Run(() =>
            {
                if (SameProcess(plan.WorkerPid, plan.WorkerStartTicks)) using (var worker = Process.GetProcessById(plan.WorkerPid))
                    if (!worker.WaitForExit(30000)) throw new InvalidOperationException(L.T("update.cleanupTimeout"));
            });
            var outcome = JsonFile.Read<UpdateOutcome>(Path.Combine(plan.StagingDirectory, "outcome.json"), 65536);
            if (!outcome.Success) throw new InvalidOperationException(outcome.Message);
            CleanStaging(plan); return plan;
        }
    }
}
