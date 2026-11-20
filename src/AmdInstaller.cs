using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Radeoner
{
    internal static class AmdInstaller
    {
        internal const string SupportUrl = "https://www.amd.com/en/support/download/drivers.html";
        private const int MaximumPageSize = 4 * 1024 * 1024;
        private const int MaximumInstallerSize = 128 * 1024 * 1024;
        private static readonly Regex InstallerLink = new Regex(
            @"\bhref\s*=\s*[""'](https://drivers\.amd\.com/drivers/installer/[\d.]+/(?:whql|non-whql)/amd-software-adrenalin-edition-[a-z0-9.-]+-minimalsetup-\d+_web\.exe)[""']",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        internal static async Task<string> DownloadAsync(string destination, IProgress<long> progress, CancellationToken cancellation)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
            using (var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }))
            {
                timeout.CancelAfter(TimeSpan.FromMinutes(4));
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Radeoner/" + L.Version);
                return await DownloadAsync(client, destination, progress, timeout.Token).ConfigureAwait(false);
            }
        }

        internal static Uri FindInstaller(string page)
        {
            // Accept only the dedicated Auto-Detect executable on AMD's download host.
            var links = InstallerLink.Matches(page).Cast<Match>().Select(m => m.Groups[1].Value)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (links.Length != 1) throw new IOException(L.T("amd.downloadLink"));
            return new Uri(links[0], UriKind.Absolute);
        }

        internal static async Task<string> DownloadAsync(HttpClient client, string destination, IProgress<long> progress, CancellationToken cancellation)
        {
            destination = Path.GetFullPath(destination);
            if (!string.Equals(Path.GetExtension(destination), ".exe", StringComparison.OrdinalIgnoreCase))
                throw new IOException(L.T("amd.fileType"));
            string temporary = Path.Combine(Path.GetDirectoryName(destination), ".radeoner-amd-" + Guid.NewGuid().ToString("N") + ".part");
            try
            {
                Uri installer;
                using (var html = new HttpRequestMessage(HttpMethod.Get, SupportUrl))
                using (var page = new MemoryStream())
                {
                    await CopyAsync(client, html, page, MaximumPageSize, null, cancellation).ConfigureAwait(false);
                    installer = FindInstaller(Encoding.UTF8.GetString(page.ToArray()));
                }
                using (var request = new HttpRequestMessage(HttpMethod.Get, installer))
                using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                {
                    request.Headers.Referrer = new Uri(SupportUrl);
                    await CopyAsync(client, request, output, MaximumInstallerSize, progress, cancellation).ConfigureAwait(false);
                }
                cancellation.ThrowIfCancellationRequested();
                string expectedHash;
                using (var package = File.Open(temporary, FileMode.Open, FileAccess.Read, FileShare.Read))
                    expectedHash = VerifyAndHash(package);
                cancellation.ThrowIfCancellationRequested();
                if (File.Exists(destination)) File.Replace(temporary, destination, null);
                else File.Move(temporary, destination);
                return expectedHash;
            }
            catch (Exception) { cancellation.ThrowIfCancellationRequested(); throw; }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        private static async Task CopyAsync(HttpClient client, HttpRequestMessage request, Stream output, int maximumSize, IProgress<long> progress, CancellationToken cancellation)
        {
            using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new IOException(L.T("amd.httpError", (int)response.StatusCode));
                long? length = response.Content.Headers.ContentLength;
                if (length == 0 || length > maximumSize) throw new IOException(L.T("amd.invalidDownload"));
                using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (cancellation.Register(input.Dispose))
                {
                    var buffer = new byte[81920];
                    long bytes = 0;
                    int count;
                    while ((count = await input.ReadAsync(buffer, 0, buffer.Length, cancellation).ConfigureAwait(false)) != 0)
                    {
                        bytes += count;
                        if (bytes > maximumSize) throw new IOException(L.T("amd.invalidDownload"));
                        await output.WriteAsync(buffer, 0, count, cancellation).ConfigureAwait(false);
                        if (progress != null) progress.Report(bytes);
                    }
                    if (bytes == 0 || (length.HasValue && length.Value != bytes))
                        throw new IOException(L.T("amd.invalidDownload"));
                }
            }
        }

        internal static void Launch(string path, string hash, CancellationToken cancellation)
        {
            // Keep the path locked for reading from validation until Windows opens it.
            using (var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (!string.Equals(VerifyAndHash(file), hash, StringComparison.Ordinal))
                    throw new IOException(L.T("amd.invalidDownload"));
                cancellation.ThrowIfCancellationRequested();
                using (var process = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true, WorkingDirectory = Environment.SystemDirectory }))
                    if (process == null) throw new IOException(L.T("amd.launchFailed"));
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TrustFile
        {
            internal uint Size;
            internal IntPtr Path, Handle, KnownSubject;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TrustData
        {
            internal uint Size;
            internal IntPtr PolicyData, ClientData;
            internal uint UiChoice, Revocation, UnionChoice;
            internal IntPtr File;
            internal uint StateAction;
            internal IntPtr State, UrlReference;
            internal uint ProviderFlags, UiContext;
            internal IntPtr SignatureSettings;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProviderCertificate
        {
            internal uint Size;
            internal IntPtr Certificate;
        }

        [DllImport("wintrust.dll", ExactSpelling = true)]
        private static extern int WinVerifyTrust(IntPtr window, ref Guid action, ref TrustData data);
        [DllImport("wintrust.dll", ExactSpelling = true)]
        private static extern IntPtr WTHelperProvDataFromStateData(IntPtr state);
        [DllImport("wintrust.dll", ExactSpelling = true)]
        private static extern IntPtr WTHelperGetProvSignerFromChain(IntPtr data, uint index, [MarshalAs(UnmanagedType.Bool)] bool counterSigner, uint counterIndex);
        [DllImport("wintrust.dll", ExactSpelling = true)]
        private static extern IntPtr WTHelperGetProvCertFromChain(IntPtr signer, uint index);

        // Use the publisher Windows validated, including the file digest and certificate chain.
        internal static string VerifyAndHash(FileStream file)
        {
            Guid action = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");
            var data = new TrustData
            {
                Size = (uint)Marshal.SizeOf(typeof(TrustData)),
                UiChoice = 2, // WTD_UI_NONE
                Revocation = 1, // WTD_REVOKE_WHOLECHAIN
                UnionChoice = 1, // WTD_CHOICE_FILE
                StateAction = 1, // WTD_STATEACTION_VERIFY
                ProviderFlags = 0x80 // WTD_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT
            };
            IntPtr path = IntPtr.Zero;
            try
            {
                path = Marshal.StringToHGlobalUni(file.Name);
                data.File = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TrustFile)));
                Marshal.StructureToPtr(new TrustFile
                {
                    Size = (uint)Marshal.SizeOf(typeof(TrustFile)),
                    Path = path,
                    Handle = file.SafeFileHandle.DangerousGetHandle()
                }, data.File, false);
                if (WinVerifyTrust(new IntPtr(-1), ref action, ref data) != 0) throw new IOException(L.T("amd.signature"));
                var provider = WTHelperProvDataFromStateData(data.State);
                var signer = provider == IntPtr.Zero ? IntPtr.Zero : WTHelperGetProvSignerFromChain(provider, 0, false, 0);
                var certificate = signer == IntPtr.Zero ? IntPtr.Zero : WTHelperGetProvCertFromChain(signer, 0);
                if (certificate == IntPtr.Zero) throw new IOException(L.T("amd.signature"));
                var cert = (ProviderCertificate)Marshal.PtrToStructure(certificate, typeof(ProviderCertificate));
                using (var publisher = new X509Certificate2(cert.Certificate))
                {
                    string name = publisher.GetNameInfo(X509NameType.SimpleName, false);
                    if (name != "Advanced Micro Devices" && name != "Advanced Micro Devices, Inc.")
                        throw new IOException(L.T("amd.signature"));
                }
            }
            finally
            {
                if (data.State != IntPtr.Zero)
                {
                    data.StateAction = 2; // WTD_STATEACTION_CLOSE
                    WinVerifyTrust(new IntPtr(-1), ref action, ref data);
                }
                Marshal.FreeHGlobal(data.File);
                Marshal.FreeHGlobal(path);
            }
            GC.KeepAlive(file);
            file.Position = 0;
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(file));
        }
    }
}
