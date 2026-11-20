using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Radeoner;

internal static partial class Tests
{
    private const string AmdTestUrl = "https://drivers.amd.com/drivers/installer/26.20/whql/amd-software-adrenalin-edition-26.11.2-minimalsetup-261118_web.exe";

    private sealed class AmdDownloadHandler : HttpMessageHandler
    {
        internal HttpStatusCode InstallerStatus = HttpStatusCode.OK;
        internal int InstallerRequests;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri.AbsoluteUri == AmdInstaller.SupportUrl)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("<a href=\"" + AmdTestUrl + "\">Download</a>", Encoding.UTF8, "text/html")
                });
            Check(request.RequestUri.AbsoluteUri == AmdTestUrl && request.Headers.Referrer.AbsoluteUri == AmdInstaller.SupportUrl,
                "Unexpected AMD request or missing official referrer");
            InstallerRequests++;
            return Task.FromResult(new HttpResponseMessage(InstallerStatus) { Content = new ByteArrayContent(new byte[] { 77, 90, 2, 3 }) });
        }
    }

    private static void RunAmdInstallerTests()
    {
        Test("AMD installer lookup only accepts unambiguous Auto-Detect links on the vendor host", () =>
        {
            string link = "<a href=\"" + AmdTestUrl + "\">Download</a>";
            Check(AmdInstaller.FindInstaller(link + link).AbsoluteUri == AmdTestUrl, "Repeated copies of the one AMD link were not accepted");
            foreach (string badPage in new[] { "", link.Replace("drivers.amd.com", "drivers.amd.com.example"), link.Replace("https:", "http:"),
                link.Replace("_web.exe", ".exe"), link + link.Replace("261118", "261119") })
            {
                bool rejected = false;
                try { AmdInstaller.FindInstaller(badPage); }
                catch (IOException ex) { rejected = ex.Message == L.T("amd.downloadLink"); }
                Check(rejected, "An unexpected or ambiguous download link was accepted");
            }
        });
        Test("Unsigned and failed AMD downloads preserve the selected file and never launch it", () =>
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "amd-download-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "installer.exe");
            File.WriteAllText(path, "existing file");
            var handler = new AmdDownloadHandler();
            using (var client = new HttpClient(handler))
            {
                foreach (var status in new[] { HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.ServiceUnavailable })
                {
                    handler.InstallerStatus = status;
                    string error = null;
                    try { AmdInstaller.DownloadAsync(client, path, null, CancellationToken.None).GetAwaiter().GetResult(); }
                    catch (IOException ex) { error = ex.Message; }
                    Check(error == (status == HttpStatusCode.OK ? L.T("amd.signature") : L.T("amd.httpError", (int)status)), "AMD download was not rejected safely");
                    Check(File.ReadAllText(path) == "existing file" && Directory.GetFiles(folder).Length == 1, "AMD download damaged the existing file or left a partial file");
                }
                Check(handler.InstallerRequests == 3, "Unexpected AMD request count");
            }
        });
        Test("AMD updates dialog exposes installed driver and keeps native controls visible in each language", () =>
        {
            foreach (string language in L.Languages)
            {
                L.Load(language);
                using (var font = new Font(SystemFonts.MessageBoxFont.FontFamily, SystemFonts.MessageBoxFont.Size * 2))
                using (var dialog = new DriverUpdateDialog(new GpuInfo { Name = "Radeon test GPU", Driver = "32.0.12345.67" }, font, "system"))
                {
                    dialog.Size = new Size(600, 500);
                    dialog.Opacity = 0;
                    dialog.Show();
                    Application.DoEvents();
                    var fields = Descendants(dialog).ToList();
                    var details = fields.OfType<TextBox>().Single();
                    Check(details.ReadOnly && details.Text.Contains("32.0.12345.67") && details.Focused, "Installed driver was not reachable");
                    Check(!fields.OfType<Button>().Single(b => b.AccessibleName == L.T("amd.run")).Enabled, "Installer could run before verification");
                    foreach (var control in fields.Where(c => c.Enabled && (c is Button || c is TextBox)))
                        CheckFocusVisible(control, "AMD updates/" + language);
                    dialog.Close();
                }
            }
            L.Load("pl");
        });
    }
}
