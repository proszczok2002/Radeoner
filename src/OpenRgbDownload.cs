using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Radeoner
{
    // Pinned official portable release. Updating it requires reviewing the source and hash.
    internal static class OpenRgbDownload
    {
        internal const string FileName = "OpenRGB_1.1_Windows_64_8e16de2.zip";
        internal const string Url = "https://codeberg.org/OpenRGB/OpenRGB/releases/download/release_1.1/" + FileName;
        internal const string Sha256 = "63291726c6fc84eb479ef2ee752e97e1187f6ce9c49fd27eee40ded588a8172a";
        internal const long Size = 22019605;
        internal static async Task DownloadAsync(string destination, IProgress<long> progress, CancellationToken cancellation)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
            using (var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }))
            {
                timeout.CancelAfter(TimeSpan.FromMinutes(5));
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Radeoner/" + L.Version);
                await DownloadAsync(client, destination, progress, timeout.Token, Size, Sha256).ConfigureAwait(false);
            }
        }
        internal static async Task DownloadAsync(HttpClient client, string destination, IProgress<long> progress, CancellationToken cancellation, long expectedSize, string expectedHash)
        {
            destination = Path.GetFullPath(destination);
            string temporary = Path.Combine(Path.GetDirectoryName(destination), ".radeoner-openrgb-" + Guid.NewGuid().ToString("N") + ".part");
            try
            {
                // Redirects are deliberately disabled; a moved release needs a reviewed URL.
                using (var response = await client.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false))
                {
                    if (response.StatusCode != HttpStatusCode.OK) throw new IOException(L.T("rgb.downloadHttp", (int)response.StatusCode));
                    if (response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength.Value != expectedSize) throw new IOException(L.T("rgb.downloadInvalid"));
                    using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var abort = cancellation.Register(() => input.Dispose()))
                    using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                    using (var hash = SHA256.Create())
                    {
                        byte[] buffer = new byte[81920]; int count; long total = 0;
                        while ((count = await input.ReadAsync(buffer, 0, buffer.Length, cancellation).ConfigureAwait(false)) != 0)
                        {
                            total += count;
                            if (total > expectedSize) throw new IOException(L.T("rgb.downloadInvalid"));
                            await output.WriteAsync(buffer, 0, count, cancellation).ConfigureAwait(false);
                            hash.TransformBlock(buffer, 0, count, buffer, 0);
                            if (progress != null) progress.Report(total);
                        }
                        hash.TransformFinalBlock(new byte[0], 0, 0);
                        if (total != expectedSize || !string.Equals(BitConverter.ToString(hash.Hash).Replace("-", ""), expectedHash, StringComparison.OrdinalIgnoreCase)) throw new IOException(L.T("rgb.downloadInvalid"));
                    }
                }
                cancellation.ThrowIfCancellationRequested();
                // Existing user files survive every failed/cancelled download.
                if (File.Exists(destination)) File.Replace(temporary, destination, null);
                else File.Move(temporary, destination);
            }
            catch (Exception) { cancellation.ThrowIfCancellationRequested(); throw; }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
