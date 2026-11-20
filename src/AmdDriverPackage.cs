using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Radeoner
{
    internal sealed class AmdDriverOffer
    {
        internal Uri Page, Package, Notes;
        internal Version Release;
        internal Version[] StoreVersions = new Version[0];
    }

    internal sealed class AmdDriverPackage : IDisposable
    {
        internal const string SupportUrl = "https://www.amd.com/en/support/download/drivers.html";
        internal const string LicenseUrl = "https://www.amd.com/en/legal/eula.html";
        private const string ProductPath = "/en/support/downloads/drivers.html/";
        private const long MaximumPackage = 2500L * 1024 * 1024;
        private static readonly Regex FullPackageName = new Regex(@"/(?:whql-)?amd-software-adrenalin-edition-(\d+\.\d+(?:\.\d+){0,2})-[a-z0-9.-]+\.exe\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex Links = new Regex(@"\bhref\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        private readonly string folder;
        private readonly bool elevated;
        internal string FilePath { get { return Path.Combine(folder, "amd-driver.exe"); } }
        internal string Hash { get; private set; }
        internal AmdDisplayInf Driver { get; private set; }

        internal AmdDriverPackage(bool elevated = false)
        {
            this.elevated = elevated;
            string root = elevated ? Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) : Path.GetTempPath();
            folder = Path.Combine(root, "Radeoner.AMD." + Guid.NewGuid().ToString("N"));
            if (Directory.Exists(folder)) throw new IOException();
            if (elevated)
            {
                var security = new DirectorySecurity();
                security.SetAccessRuleProtection(true, false);
                var inheritance = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
                var administrators = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
                security.SetOwner(administrators);
                security.AddAccessRule(new FileSystemAccessRule(administrators, FileSystemRights.FullControl, inheritance, PropagationFlags.None, AccessControlType.Allow));
                security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), FileSystemRights.FullControl, inheritance, PropagationFlags.None, AccessControlType.Allow));
                Directory.CreateDirectory(folder, security);
            }
            else Directory.CreateDirectory(folder);
            if ((File.GetAttributes(folder) & FileAttributes.ReparsePoint) != 0) throw new IOException();
        }

        internal static string SuggestedPage(GpuInfo gpu)
        {
            if (gpu == null) return "";
            var model = Regex.Match(gpu.Name ?? "", @"\bRX\s+([5679])([0-9]{3})(?:\s*(XTX|XT|GRE))?\s*\z", RegexOptions.IgnoreCase);
            if (model.Success)
            {
                string number = model.Groups[1].Value + model.Groups[2].Value;
                string variant = model.Groups[3].Success ? "-" + model.Groups[3].Value.ToLowerInvariant() : "";
                return "https://www.amd.com" + ProductPath + "graphics/radeon-rx/radeon-rx-" + number[0] + "000-series/amd-radeon-rx-" + number + variant + ".html";
            }
            if (gpu.Type != 1) return "";
            try
            {
                using (var query = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                using (var results = query.Get())
                    foreach (ManagementObject processor in results) using (processor)
                    {
                        var ai = Regex.Match(processor["Name"] as string ?? "", @"\bRyzen\s+AI\s+(\d)\s+(?:(HX)\s+)?([34]\d\d)\b", RegexOptions.IgnoreCase);
                        if (!ai.Success) continue;
                        string number = ai.Groups[3].Value;
                        string hx = ai.Groups[2].Success || number == "365" ? "-hx" : "";
                        return "https://www.amd.com" + ProductPath + "processors/ryzen/ryzen-ai-" + number[0] + "00-series/amd-ryzen-ai-" + ai.Groups[1].Value + hx + "-" + number + ".html";
                    }
            }
            catch (ManagementException) { }
            return "";
        }

        private static bool Official(Uri uri, string host)
        {
            return uri != null && uri.IsAbsoluteUri && uri.Scheme == Uri.UriSchemeHttps && uri.IsDefaultPort
                && uri.Host == host && uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0;
        }

        internal static bool IsProductPage(Uri page)
        {
            return Official(page, "www.amd.com") && Regex.IsMatch(page.AbsolutePath,
                @"\A/en/support/downloads/drivers\.html/(?:graphics|processors)/[a-z0-9/-]+\.html\z", RegexOptions.IgnoreCase);
        }

        private static Version PackageVersion(Uri package)
        {
            if (!Official(package, "drivers.amd.com") || !package.AbsolutePath.StartsWith("/drivers/", StringComparison.OrdinalIgnoreCase)) return null;
            string path = package.AbsolutePath;
            if (path.IndexOf("minimalsetup", StringComparison.OrdinalIgnoreCase) >= 0 || path.IndexOf("_web", StringComparison.OrdinalIgnoreCase) >= 0) return null;
            var match = FullPackageName.Match(path);
            Version number;
            return match.Success && Version.TryParse(match.Groups[1].Value, out number) ? number : null;
        }

        internal static AmdDriverOffer ParseProductPage(Uri page, string html, int windows)
        {
            if (!IsProductPage(page)) throw new IOException(L.T("amd.pageError"));
            var heading = Regex.Match(html, @"Windows\s+" + windows + @"\s*-\s*64-Bit Edition\s*</button>", RegexOptions.IgnoreCase);
            if (!heading.Success) throw new IOException(L.T("amd.noPackage"));
            string section = html.Substring(heading.Index + heading.Length);
            var next = Regex.Match(section, @"<h2(?:[\s>])", RegexOptions.IgnoreCase);
            if (next.Success) section = section.Substring(0, next.Index);
            AmdDriverOffer best = null;
            foreach (Match article in Regex.Matches(section, @"<article\b[^>]*>[\s\S]*?</article>", RegexOptions.IgnoreCase))
            {
                var offer = new AmdDriverOffer { Page = page };
                foreach (Match link in Links.Matches(article.Value))
                {
                    Uri uri;
                    if (!Uri.TryCreate(page, WebUtility.HtmlDecode(link.Groups[1].Value), out uri)) continue;
                    Version release = PackageVersion(uri);
                    if (release != null) { offer.Package = uri; offer.Release = release; }
                    if (Official(uri, "www.amd.com") && Regex.IsMatch(uri.AbsolutePath, @"\A/en/resources/support-articles/release-notes/RN-RAD-WIN-[a-z0-9-]+\.html\z", RegexOptions.IgnoreCase))
                        offer.Notes = uri;
                }
                if (offer.Package != null && (best == null || offer.Release > best.Release)) best = offer;
            }
            if (best == null) throw new IOException(L.T("amd.noPackage"));
            return best;
        }

        private static HttpClient Client()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
            client.Timeout = TimeSpan.FromMinutes(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/130.0.0.0 Safari/537.36");
            return client;
        }

        private static async Task<string> GetPage(HttpClient client, Uri page, CancellationToken token)
        {
            using (HttpResponseMessage response = await client.GetAsync(page, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                const int limit = 4 * 1024 * 1024;
                if (response.Content.Headers.ContentLength > limit) throw new IOException();
                using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var output = new MemoryStream())
                {
                    await Copy(input, output, limit, null, token).ConfigureAwait(false);
                    return System.Text.Encoding.UTF8.GetString(output.ToArray());
                }
            }
        }

        internal static async Task<AmdDriverOffer> Check(Uri page, CancellationToken token)
        {
            if (!IsProductPage(page)) throw new IOException(L.T("amd.pageError"));
            using (var client = Client())
            {
                var offer = ParseProductPage(page, await GetPage(client, page, token).ConfigureAwait(false), Environment.OSVersion.Version.Build >= 22000 ? 11 : 10);
                if (offer.Notes != null)
                {
                    try
                    {
                        string notes = await GetPage(client, offer.Notes, token).ConfigureAwait(false);
                        string plain = WebUtility.HtmlDecode(Regex.Replace(notes, "<[^>]*>", " "));
                        offer.StoreVersions = Regex.Matches(plain, @"Windows\s+Driver\s+Store\s+Version\s+(\d+\.\d+\.\d+\.\d+)", RegexOptions.IgnoreCase)
                            .Cast<Match>().Select(m => new Version(m.Groups[1].Value)).Distinct().OrderBy(v => v).ToArray();
                    }
                    catch (HttpRequestException) { }
                    catch (IOException) { }
                }
                return offer;
            }
        }

        internal async Task Download(AmdDriverOffer offer, IProgress<long> progress, CancellationToken token)
        {
            if (!IsProductPage(offer.Page) || PackageVersion(offer.Package) == null) throw new IOException(L.T("amd.noPackage"));
            using (var client = Client())
            using (var request = new HttpRequestMessage(HttpMethod.Get, offer.Package))
            {
                request.Headers.Referrer = offer.Page;
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    long? length = response.Content.Headers.ContentLength;
                    if (length < 32 * 1024 * 1024 || length > MaximumPackage) throw new IOException(L.T("amd.archive"));
                    using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var output = new FileStream(FilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        long bytes = await Copy(input, output, MaximumPackage, progress, token).ConfigureAwait(false);
                        if (bytes < 32 * 1024 * 1024 || (length.HasValue && bytes != length.Value))
                            throw new IOException(L.T("amd.archive"));
                    }
                }
            }
        }

        private static async Task<long> Copy(Stream input, Stream output, long limit, IProgress<long> progress, CancellationToken token)
        {
            long total = 0, last = 0;
            var buffer = new byte[128 * 1024];
            int read;
            while ((read = await input.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) > 0)
            {
                total += read;
                if (total > limit) throw new IOException(L.T("amd.archive"));
                await output.WriteAsync(buffer, 0, read, token).ConfigureAwait(false);
                if (progress != null && total - last >= 4 * 1024 * 1024) { progress.Report(total / (1024 * 1024)); last = total; }
            }
            return total;
        }

        internal void Prepare(string instanceId, string expectedHash, CancellationToken token)
        {
            string payload = Path.Combine(folder, "display-payload.7z");
            using (var input = File.OpenRead(FilePath))
            {
                if (input.Length > MaximumPackage) throw new IOException(L.T("amd.archive"));
                Hash = AmdPackageTrust.VerifyAndHash(input);
                if (expectedHash != null && !string.Equals(Hash, expectedHash, StringComparison.Ordinal))
                    throw new IOException(L.T("amd.changed"));
                token.ThrowIfCancellationRequested();
                WritePayload(input, payload, token);
            }
            using (var process = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "tar.exe"),
                "-xf \"" + payload + "\" -C \"" + folder + "\" Packages/Drivers/Display")
                { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Environment.SystemDirectory }))
            {
                while (!process.WaitForExit(250))
                    if (token.IsCancellationRequested) { process.Kill(); process.WaitForExit(); token.ThrowIfCancellationRequested(); }
                if (process.ExitCode != 0) throw new IOException(L.T("amd.archive"));
            }
            File.Delete(payload);
            string display = Path.Combine(folder, @"Packages\Drivers\Display");
            foreach (string inf in Directory.EnumerateFiles(display, "*.inf", SearchOption.AllDirectories))
                if (!ContainsOnlyDisplaySoftware(File.ReadAllText(inf))) throw new IOException(L.T("amd.additionalSoftware"));
            Driver = AmdDisplayInf.Find(display, instanceId);
        }

        internal static bool ContainsOnlyDisplaySoftware(string inf)
        {
            // Modern AMD OpenGL/OpenCL/Vulkan INFs register their own runtime DLLs with regamdcomp.
            // Reject unexpected application installers and Store app references.
            foreach (Match directive in Regex.Matches(inf, @"(?im)^\s*(SoftwareType|SoftwareBinary|SoftwareID|RunPreSetupCommands|RunPostSetupCommands)\s*=\s*([^;\r\n]*)"))
            {
                string key = directive.Groups[1].Value;
                string value = directive.Groups[2].Value.Trim().Trim('"');
                if (key.Equals("SoftwareType", StringComparison.OrdinalIgnoreCase) && value == "1") continue;
                if (key.Equals("SoftwareBinary", StringComparison.OrdinalIgnoreCase) && value.Equals("regamdcomp.exe", StringComparison.OrdinalIgnoreCase)) continue;
                return false;
            }
            return true;
        }

        private static void WritePayload(FileStream input, string outputFile, CancellationToken token)
        {
            using (var reader = new BinaryReader(input, System.Text.Encoding.UTF8, true))
            {
                input.Position = 0x3C;
                int pe = reader.ReadInt32();
                if (pe < 0x40 || pe > 1024 * 1024 || pe + 180 >= input.Length) throw new IOException(L.T("amd.archive"));
                input.Position = pe;
                if (reader.ReadUInt32() != 0x4550) throw new IOException(L.T("amd.archive"));
                input.Position = pe + 24;
                int magic = reader.ReadUInt16();
                if (magic != 0x10b && magic != 0x20b) throw new IOException(L.T("amd.archive"));
                input.Position = pe + 24 + (magic == 0x20b ? 112 : 96) + 32;
                uint signatureStart = reader.ReadUInt32();
                if (signatureStart == 0 || signatureStart >= input.Length) throw new IOException(L.T("amd.archive"));
                input.Position = 0;
                byte[] head = reader.ReadBytes((int)Math.Min(signatureStart, 16 * 1024 * 1024));
                byte[] sevenZip = { 0x37, 0x7a, 0xbc, 0xaf, 0x27, 0x1c, 0 };
                for (int i = 0; i <= head.Length - 32; i++)
                {
                    if (head[i] != sevenZip[0] || !sevenZip.SequenceEqual(head.Skip(i).Take(sevenZip.Length))) continue;
                    ulong room = (ulong)signatureStart - (ulong)i - 32;
                    ulong offset = BitConverter.ToUInt64(head, i + 12), header = BitConverter.ToUInt64(head, i + 20);
                    if (offset > room || header == 0 || header > room - offset) continue;
                    input.Position = i;
                    long left = (long)(offset + header + 32);
                    using (var output = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        byte[] buffer = new byte[256 * 1024];
                        while (left > 0)
                        {
                            token.ThrowIfCancellationRequested();
                            int size = input.Read(buffer, 0, (int)Math.Min(left, buffer.Length));
                            if (size == 0) throw new IOException(L.T("amd.archive"));
                            output.Write(buffer, 0, size);
                            left -= size;
                        }
                    }
                    return;
                }
                throw new IOException(L.T("amd.archive"));
            }
        }

        internal static int RequestInstall(string instanceId, AmdDriverPackage package)
        {
            string arguments = "--install-amd-driver \"" + instanceId + "\" \"" + package.FilePath + "\" "
                + package.Hash + " " + package.Driver.Hash + " " + package.Driver.Version;
            using (var process = Process.Start(new ProcessStartInfo(typeof(AmdDriverPackage).Assembly.Location, arguments)
                { UseShellExecute = true, Verb = "runas", WorkingDirectory = Environment.SystemDirectory }))
            {
                if (process == null) return AmdDisplayInf.Failed;
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        internal static int Install(string instanceId, string source, string packageHash, string infHash, string version)
        {
            Version expected;
            if (!AmdDisplayInf.IsAmdInstance(instanceId) || !UpdateFeed.ValidHash(packageHash)
                || !UpdateFeed.ValidHash(infHash) || !Version.TryParse(version, out expected)
                || !Regex.IsMatch(source ?? "", @"\A[A-Z]:\\[^""\r\n]+\\Radeoner\.AMD\.[0-9a-f]{32}\\amd-driver\.exe\z", RegexOptions.IgnoreCase))
                return AmdDisplayInf.Changed;
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                    if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator)) return 5;
                using (var stage = new AmdDriverPackage(true))
                {
                    using (var input = File.OpenRead(source))
                    using (var output = new FileStream(stage.FilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        if (input.Length > MaximumPackage) return AmdDisplayInf.Changed;
                        input.CopyTo(output);
                    }
                    stage.Prepare(instanceId, packageHash, CancellationToken.None);
                    if (stage.Driver == null || stage.Driver.Hash != infHash || stage.Driver.Version != expected) return AmdDisplayInf.Changed;
                    return stage.Driver.Install(instanceId);
                }
            }
            catch (Exception) { return AmdDisplayInf.Failed; }
        }

        public void Dispose()
        {
            try { Directory.Delete(folder, true); }
            catch (IOException ex) { if (!elevated) Diagnostics.Log("AMD temporary files", ex); }
            catch (UnauthorizedAccessException ex) { if (!elevated) Diagnostics.Log("AMD temporary files", ex); }
        }
    }
}
