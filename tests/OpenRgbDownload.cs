using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Radeoner;

internal static partial class Tests
{
    private sealed class RgbDownloadHandler : HttpMessageHandler
    {
        internal byte[] Data = { 1, 2, 3, 4 }; internal int Status = 200;
        internal CancellationTokenSource Cancel;
        internal bool UnknownLength;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
        {
            Check(request.RequestUri.AbsoluteUri == OpenRgbDownload.Url, "Unexpected download source");
            if (Cancel != null) Cancel.Cancel();
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)Status) { Content = UnknownLength ? new UnknownLengthContent(Data) : new ByteArrayContent(Data) });
        }
    }
    private sealed class UnknownLengthContent : ByteArrayContent
    {
        internal UnknownLengthContent(byte[] data) : base(data) { }
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
    }
    private static void RunRgbDownloadTests()
    {
        Test("OpenRGB download verifies bytes, replaces only on success and preserves files after failure/cancel", () =>
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rgb-download-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
            string target = Path.Combine(folder, "OpenRGB test.zip"); var handler = new RgbDownloadHandler();
            string hash; using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(handler.Data)).Replace("-", "").ToLowerInvariant();
            using (var client = new HttpClient(handler))
            {
                for (int scenario = 0; scenario < 7; scenario++)
                {
                    File.WriteAllText(target, "previous user file"); handler.Status = scenario == 3 ? 503 : scenario == 4 ? 302 : 200;
                    handler.UnknownLength = scenario == 2 || scenario == 6;
                    using (var cancel = new CancellationTokenSource())
                    {
                        handler.Cancel = scenario == 5 ? cancel : null;
                        string expectedHash = scenario == 1 ? new string('0', 64) : hash;
                        long size = scenario == 2 ? 5 : scenario == 6 ? 3 : 4;
                        bool success = false;
                        try { OpenRgbDownload.DownloadAsync(client, target, null, cancel.Token, size, expectedHash).GetAwaiter().GetResult(); success = true; }
                        catch (Exception ex) { Check(ex is IOException || ex is OperationCanceledException, "Unexpected download failure: " + ex); }
                        Check(success == (scenario == 0), "Bad transfer was accepted: " + scenario);
                        Check(success ? File.ReadAllBytes(target).SequenceEqual(handler.Data) : File.ReadAllText(target) == "previous user file", "Existing file was damaged: " + scenario);
                        Check(Directory.GetFiles(folder).Length == 1, "Partial file left behind: " + scenario);
                    }
                }
                // Also cover a previously absent destination.
                target = Path.Combine(folder, "new.zip"); handler.Status = 200; handler.Cancel = null;
                OpenRgbDownload.DownloadAsync(client, target, null, CancellationToken.None, 4, hash).GetAwaiter().GetResult();
                Check(File.ReadAllBytes(target).SequenceEqual(handler.Data), "New download not saved");
            }
        });
    }
}
