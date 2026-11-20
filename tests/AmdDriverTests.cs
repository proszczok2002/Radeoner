using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Radeoner;

internal static partial class Tests
{
    private const string TestInstanceId = @"PCI\VEN_1002&DEV_150E&SUBSYS_3A191043&REV_E3\4&35FE04F8&0&0041";
    private const string TestAmdPage = "https://www.amd.com/en/support/downloads/drivers.html/graphics/radeon-rx/radeon-rx-9000-series/amd-radeon-rx-9070-xt.html";

    private static void RunAmdDriverTests()
    {
        Test("AMD product lookup selects the newest full AMD package in the matching Windows section", () =>
        {
            var address = new Uri(TestAmdPage);
            Check(AmdDriverPackage.SuggestedPage(new GpuInfo { Name = "AMD Radeon RX 9070 XT" }) == TestAmdPage, "RX 9070 XT page was not recognized");
            Check(AmdDriverPackage.SuggestedPage(new GpuInfo { Name = "AMD Radeon RX 9060 XT", Memory = "8192 MB, GDDR6" })
                .EndsWith("amd-radeon-rx-9060-xt-8gb.html", StringComparison.Ordinal), "RX 9060 XT 8GB page was not recognized");
            const string marker = "<h2><button>Windows 11 - 64-Bit Edition</button></h2>";
            const string note = "/en/resources/support-articles/release-notes/RN-RAD-WIN-26-11-2.html";
            Func<string, string> entry = url => "<article><a href='" + url + "'>Download</a><a href='" + note + "'>Notes</a></article>";
            const string prefix = "https://drivers.amd.com/drivers/";
            const string older = "whql-amd-software-adrenalin-edition-26.9.2-win11.exe";
            const string newer = "whql-amd-software-adrenalin-edition-26.11.2-win11.exe";
            string bad = entry("https://drivers.amd.com.example/drivers/" + newer)
                + entry(prefix + "whql-amd-software-adrenalin-edition-27.1.0-minimalsetup_web.exe");
            var offer = AmdDriverPackage.ParseProductPage(address, marker + bad + entry(prefix + older) + entry(prefix + newer)
                + "<h2><button>Windows 10 - 64-Bit Edition</button></h2>" + entry(prefix + newer.Replace("26.11.2", "29.10.0")), 11);
            Check(offer.Package.AbsoluteUri == prefix + newer && offer.Notes.AbsolutePath == note && offer.Release == new Version(26, 11, 2), "Incorrect AMD package");
            try { AmdDriverPackage.ParseProductPage(address, marker + bad, 11); throw new InvalidOperationException("Unexpected package was accepted"); }
            catch (IOException) { }
            Check(!AmdDriverPackage.IsProductPage(new Uri("https://www.amd.com.example" + address.AbsolutePath)), "Foreign product page accepted");
            Check(AmdDriverPackage.ContainsOnlyDisplaySoftware("SoftwareType=1\nSoftwareBinary=regamdcomp.exe"), "Driver runtime registration rejected");
            Check(!AmdDriverPackage.ContainsOnlyDisplaySoftware("SoftwareType=2\nSoftwareID=pfn://Radeon"), "Store app accepted");
            Check(!AmdDriverPackage.ContainsOnlyDisplaySoftware("SoftwareType=1\nSoftwareBinary=setup.exe"), "Additional installer accepted");
        });
        Test("Elevated AMD worker rejects malformed inputs before touching drivers", () =>
        {
            Check(AmdDisplayInf.DecodeVersion((32UL << 48) | (32029UL << 16) | 2010UL) == new Version(32, 0, 32029, 2010), "Windows PnP driver version ABI is wrong");
            Check(AmdDisplayInf.IsAmdInstance(TestInstanceId), "Valid GPU instance rejected");
            string hash = new string('a', 64);
            Check(AmdDriverPackage.Install(TestInstanceId, @"C:\unrelated.exe", hash, hash, "32.0.32029.2010") == AmdDisplayInf.Changed,
                "Unexpected input path reached privileged work");
            Check(AmdDriverPackage.Install(TestInstanceId + "\" extra", "", hash, hash, "32.0.32029.2010") == AmdDisplayInf.Changed,
                "Invalid GPU instance advanced");
        });
        Test("AMD package dialog keeps keyboard controls visible and installation locked in all six languages", () =>
        {
            foreach (string language in L.Languages)
            {
                L.Load(language);
                using (var font = new Font(SystemFonts.MessageBoxFont.FontFamily, SystemFonts.MessageBoxFont.Size * 2))
                using (var dialog = new DriverUpdateDialog(new GpuInfo { Name = "Radeon test GPU", Id = TestInstanceId, Driver = "32.0.12345.67" }, font, "system"))
                {
                    dialog.Size = new Size(600, 500);
                    dialog.Opacity = 0;
                    dialog.Show(); Application.DoEvents();
                    var controls = Descendants(dialog).ToList();
                    var details = controls.OfType<TextBox>().Single(t => t.Multiline);
                    var prepare = controls.OfType<Button>().Single(b => b.AccessibleName == L.T("amd.prepare"));
                    var install = controls.OfType<Button>().Single(b => b.AccessibleName == L.T("amd.install"));
                    Check(details.ReadOnly && details.Text.Contains("32.0.12345.67") && details.Focused, "Installed driver is not readable at first focus");
                    Check(!prepare.Enabled && !install.Enabled, "Unverified package could be installed");
                    foreach (Control control in controls.Where(c => c.Enabled && (c is Button || c is TextBox)))
                        CheckFocusVisible(control, "AMD packages/" + language);
                    dialog.Close();
                }
            }
            L.Load("pl");
        });
    }
}
