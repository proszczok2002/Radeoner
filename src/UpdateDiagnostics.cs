using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Radeoner
{
    internal static class UpdateDiagnostics
    {
        // Explicit developer command. The actual new WinForms UI confirms startup,
        // removes the backup/helper and then exits, without changing GPU settings.
        internal static async Task<int> RunAsync(string[] args)
        {
            try
            {
                AvailableUpdate update; byte[] archive;
                if (args.Length == 3)
                {
                    var manifest = JsonFile.Read<UpdateManifest>(Path.GetFullPath(args[1]), 65536);
                    update = new AvailableUpdate { Manifest = manifest, Tag = "v" + manifest.DisplayVersion };
                    archive = File.ReadAllBytes(Path.GetFullPath(args[2]));
                }
                else
                {
                    using (var transport = new GithubTransport())
                    {
                        var feed = new UpdateFeed(transport); update = await feed.CheckAsync(new Version(BuildInfo.AssemblyVersion), CancellationToken.None).ConfigureAwait(false);
                        if (update == null) return 2;
                        archive = await feed.DownloadAsync(update, CancellationToken.None).ConfigureAwait(false);
                    }
                }
                using (var parent = Process.GetCurrentProcess())
                {
                    var plan = UpdateInstall.Stage(update, archive, AppDomain.CurrentDomain.BaseDirectory, parent, true, true);
                    using (var worker = UpdateInstall.StartWorker(plan)) { if (worker == null) throw new IOException("No update worker."); }
                }
                return 0;
            }
            catch (Exception ex)
            { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update-smoke-error.txt"), ex.ToString()); return 1; }
        }
    }
}
