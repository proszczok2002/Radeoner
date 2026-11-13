using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Radeoner;

internal static partial class Tests
{
    private sealed class FakeUpdateTransport : IUpdateTransport
    {
        internal readonly Dictionary<string, byte[]> Responses = new Dictionary<string, byte[]>();
        public Task<byte[]> GetAsync(Uri uri, int maximumBytes, CancellationToken cancellation)
        { cancellation.ThrowIfCancellationRequested(); byte[] data = Responses[uri.AbsoluteUri]; if (data.Length > maximumBytes) throw new InvalidOperationException("oversized"); return Task.FromResult(data); }
        public void Dispose() { }
    }
    private static byte[] Json<T>(T value)
    { using (var output = new MemoryStream()) { new DataContractJsonSerializer(typeof(T)).WriteObject(output, value); return output.ToArray(); } }
    private static string UpdateTestDirectory(string name)
    { string folder = TestFile("updates-" + name + "-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder); return folder; }
    private static Dictionary<string, byte[]> PackageContent()
    {
        var data = UpdateFeed.PackagePaths.ToDictionary(p => p, p => Encoding.UTF8.GetBytes("test file " + p));
        data["Radeoner.exe"] = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Radeoner.UpdateFixture.exe"));
        data["Radeoner.exe.config"] = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Radeoner.exe.config"));
        return data;
    }
    private static AvailableUpdate TestUpdate(Dictionary<string, byte[]> files, string substituteEntry, out byte[] bytes)
    {
        using (var output = new MemoryStream())
        {
            using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true)) foreach (var file in files)
            {
                var entry = zip.CreateEntry(file.Key == "Czytaj.txt" && substituteEntry != null ? substituteEntry : file.Key);
                using (var stream = entry.Open()) stream.Write(file.Value, 0, file.Value.Length);
            }
            bytes = output.ToArray();
        }
        var manifest = new UpdateManifest { Format = "Radeoner.Update", Schema = 1, Version = "99.0.0.0", DisplayVersion = "99.0", Asset = "Radeoner-99.0-portable.zip",
            Size = bytes.Length, Sha256 = UpdateFeed.Hash(bytes), Files = files.Select(p => new UpdateFile { Path = p.Key, Size = p.Value.Length, Sha256 = UpdateFeed.Hash(p.Value) }).ToList() };
        return new AvailableUpdate { Manifest = manifest, Tag = "v99.0", DownloadUrl = "https://github.com/proszczok2002/Radeoner/releases/download/v99.0/" + manifest.Asset };
    }
    private static FakeUpdateTransport TestFeed(AvailableUpdate update, byte[] archive)
    {
        string metadata = "https://github.com/proszczok2002/Radeoner/releases/download/" + update.Tag + "/update.json";
        var transport = new FakeUpdateTransport();
        transport.Responses[UpdateFeed.LatestUrl] = Json(new GithubRelease { Tag = update.Tag, Assets = new List<GithubAsset> {
            new GithubAsset { Name = "update.json", Url = metadata }, new GithubAsset { Name = update.Manifest.Asset, Url = update.DownloadUrl, Size = update.Manifest.Size } } });
        transport.Responses[metadata] = Json(update.Manifest); transport.Responses[update.DownloadUrl] = archive; return transport;
    }
    private static string InstallTarget()
    {
        string path = UpdateTestDirectory("target");
        foreach (string relative in UpdateFeed.PackagePaths)
        {
            string file = UpdateInstall.FilePath(path, relative); Directory.CreateDirectory(Path.GetDirectoryName(file));
            if (relative == "Radeoner.exe") File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Radeoner.exe"), file);
            else if (relative == "Radeoner.exe.config") File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Radeoner.exe.config"), file);
            else File.WriteAllText(file, "old " + relative);
        }
        File.WriteAllText(Path.Combine(path, "Radeoner.settings.json"), "preferences preserved");
        File.WriteAllText(Path.Combine(path, "my-profile.json"), "profile preserved"); return path;
    }
    private static void RunUpdateTests()
    {
        Test("Update feed validates public GitHub release, newer version, manifest and archive checksum", () =>
        {
            byte[] archive; var update = TestUpdate(PackageContent(), null, out archive);
            using (var transport = TestFeed(update, archive))
            {
                var feed = new UpdateFeed(transport); var found = feed.CheckAsync(new Version(1, 0, 0, 0), CancellationToken.None).GetAwaiter().GetResult();
                Check(found.Manifest.Version == "99.0.0.0" && found.Tag == "v99.0", "Newer release not found");
                Check(feed.DownloadAsync(found, CancellationToken.None).GetAwaiter().GetResult().SequenceEqual(archive), "Archive verification failed");
                Check(feed.CheckAsync(new Version(99, 0, 0, 0), CancellationToken.None).GetAwaiter().GetResult() == null, "Same-version update offered");
                transport.Responses[found.DownloadUrl] = Encoding.UTF8.GetBytes("corrupt");
                Expect(() => feed.DownloadAsync(found, CancellationToken.None).GetAwaiter().GetResult(), "SHA-256");
            }
        });
        Test("Update feed rejects foreign URLs, missing manifests, unknown files and duplicate files", () =>
        {
            byte[] archive; var update = TestUpdate(PackageContent(), null, out archive);
            Check(!UpdateFeed.IsReleaseAsset("https://github.com/other/Radeoner/releases/download/v99.0/update.json", update.Tag, "update.json"), "Foreign repository trusted");
            Check(!GithubTransport.TrustedDownload(new Uri("http://github.com/")) && !GithubTransport.TrustedDownload(new Uri("https://github.com.attacker.invalid/")), "Unsafe redirect accepted");
            update.Manifest.Files[0].Path = "../Radeoner.settings.json"; Expect(() => UpdateFeed.ValidateManifest(update.Manifest), "Manifest");
            update = TestUpdate(PackageContent(), null, out archive); update.Manifest.Files[1].Path = update.Manifest.Files[0].Path; Expect(() => UpdateFeed.ValidateManifest(update.Manifest), "Manifest");
            update = TestUpdate(PackageContent(), null, out archive);
            using (var transport = TestFeed(update, archive))
            {
                transport.Responses[UpdateFeed.LatestUrl] = Json(new GithubRelease { Tag = "v99.0", Assets = new List<GithubAsset>() });
                Expect(() => new UpdateFeed(transport).CheckAsync(new Version(1, 0), CancellationToken.None).GetAwaiter().GetResult(), "Wydanie");
            }
        });
        Test("Update staging rejects path traversal and bad file hashes without changing target files", () =>
        {
            byte[] archive; var update = TestUpdate(PackageContent(), "../escape.txt", out archive); string target = InstallTarget();
            string original = UpdateFeed.Hash(Path.Combine(target, "Radeoner.exe"));
            using (var parent = Process.GetCurrentProcess()) Expect(() => UpdateInstall.Stage(update, archive, target, parent, true, true), "Archiwum");
            Check(Directory.GetDirectories(target, ".radeoner-update-*").Length == 0 && !File.Exists(Path.Combine(target, "escape.txt")), "Bad archive escaped staging or left data");
            update = TestUpdate(PackageContent(), null, out archive); update.Manifest.Files.First(f => f.Path == "Czytaj.txt").Sha256 = new string('0', 64);
            using (var parent = Process.GetCurrentProcess()) Expect(() => UpdateInstall.Stage(update, archive, target, parent, true, true), "sprawdzenia");
            Check(UpdateFeed.Hash(Path.Combine(target, "Radeoner.exe")) == original, "Staging modified installed executable");
        });
        Test("Install keeps backup until confirmation, rollback restores all files and preserves user data", () =>
        {
            byte[] archive; var update = TestUpdate(PackageContent(), null, out archive); string target = InstallTarget();
            var before = UpdateFeed.PackagePaths.ToDictionary(p => p, p => UpdateFeed.Hash(UpdateInstall.FilePath(target, p)));
            UpdatePlan plan; using (var parent = Process.GetCurrentProcess()) plan = UpdateInstall.Stage(update, archive, target, parent, true, true);
            UpdateInstall.InstallFiles(plan);
            Check(File.Exists(Path.Combine(plan.StagingDirectory, "backup", "Radeoner.exe")), "Old EXE removed before readiness confirmation");
            Check(UpdateFeed.Hash(Path.Combine(target, "Radeoner.exe")) == update.Manifest.Files.Single(f => f.Path == "Radeoner.exe").Sha256, "Candidate EXE not installed");
            Check(File.ReadAllText(Path.Combine(target, "Radeoner.settings.json")) == "preferences preserved" && File.ReadAllText(Path.Combine(target, "my-profile.json")) == "profile preserved", "User settings overwritten");
            UpdateInstall.Rollback(plan);
            foreach (var p in before) Check(UpdateFeed.Hash(UpdateInstall.FilePath(target, p.Key)) == p.Value, "Rollback mismatch: " + p.Key);
            UpdateInstall.CleanStaging(plan); Check(!Directory.Exists(plan.StagingDirectory), "Temporary files not removed");
        });
        Test("Update staging cleanup refuses directories outside the exact owned staging path", () =>
        {
            byte[] archive; var update = TestUpdate(PackageContent(), null, out archive); string target = InstallTarget();
            UpdatePlan plan; using (var parent = Process.GetCurrentProcess()) plan = UpdateInstall.Stage(update, archive, target, parent, true, true);
            string safe = plan.StagingDirectory; plan.StagingDirectory = target;
            Expect(() => UpdateInstall.CleanStaging(plan), "plan"); Check(File.Exists(Path.Combine(target, "my-profile.json")), "Unsafe cleanup deleted user files");
            plan.StagingDirectory = safe; UpdateInstall.CleanStaging(plan);
        });
        Test("Updater worker restores original executable when the candidate exits before confirming startup", () =>
        {
            byte[] archive; var update = TestUpdate(PackageContent(), null, out archive); string target = InstallTarget();
            string before = UpdateFeed.Hash(Path.Combine(target, "Radeoner.exe")); UpdatePlan plan;
            using (var parent = Process.GetCurrentProcess()) plan = UpdateInstall.Stage(update, archive, target, parent, true, true);
            // A short-lived process supplies a completed parent identity; no unrelated process is stopped.
            using (var ended = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"), "/d /c exit 0") { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden }))
            {
                plan.ParentPid = ended.Id; plan.ParentStartTicks = ended.StartTime.ToUniversalTime().Ticks; ended.WaitForExit();
            }
            JsonFile.Write(Path.Combine(plan.StagingDirectory, UpdateInstall.PlanName), plan);
            using (var worker = UpdateInstall.StartWorker(plan)) { Check(worker.WaitForExit(15000), "Rollback worker timed out"); Check(worker.ExitCode == 1, "Failing candidate was accepted"); }
            Check(UpdateFeed.Hash(Path.Combine(target, "Radeoner.exe")) == before, "Original executable not restored");
            var outcome = JsonFile.Read<UpdateOutcome>(Path.Combine(plan.StagingDirectory, "outcome.json"), 65536); Check(!outcome.Success, "Rollback outcome incorrect");
            UpdateInstall.CleanStaging(plan);
        });
        Test("Update preferences migrate existing settings and retain interval and startup selection", () =>
        {
            string file = TestFile("update-preferences.json"); File.WriteAllText(file, "{\"Version\":1,\"Language\":\"pl\",\"Theme\":\"system\",\"TextScale\":100,\"RefreshSensors\":false}");
            var settings = AppSettings.Load(file); Check(settings.CheckUpdates && settings.CheckUpdatesOnStartup && settings.UpdateIntervalMinutes == 60, "Old settings migration failed");
            settings.CheckUpdates = false; settings.CheckUpdatesOnStartup = false; settings.UpdateIntervalMinutes = 120; settings.Save(file);
            settings = AppSettings.Load(file); Check(!settings.CheckUpdates && !settings.CheckUpdatesOnStartup && settings.UpdateIntervalMinutes == 120, "Update preferences not saved");
            settings.UpdateIntervalMinutes = int.MaxValue; settings.Save(file); Check(AppSettings.Load(file).UpdateIntervalMinutes == 60, "Invalid interval accepted");
            using (var form = TestForm("update-ui"))
            {
                var controls = Descendants(form).ToList();
                Check(controls.OfType<System.Windows.Forms.Button>().Any(b => b.AccessibleName == L.T("update.checkName")), "Manual check unavailable");
                Check(controls.OfType<System.Windows.Forms.NumericUpDown>().Single(n => n.AccessibleName == L.T("update.intervalName")).Minimum == 5, "Interval accessibility/range missing");
                SetField(form, "started", true); form.SuppressAutomaticUpdateChecks = false;
                Field<System.Windows.Forms.NumericUpDown>(form, "updateMinutes").Value = 120;
                var timer = Field<System.Windows.Forms.Timer>(form, "updateTimer");
                Check(timer.Enabled && timer.Interval == 120 * 60000, "Periodic update timer not configured");
                Field<System.Windows.Forms.CheckBox>(form, "automaticUpdates").Checked = false;
                Check(!timer.Enabled && Field<System.Windows.Forms.Button>(form, "checkUpdate").Enabled, "Disabling automatic checks disabled manual check or kept the timer running");
            }
        });
    }
}
