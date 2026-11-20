using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Radeoner;

internal static partial class Tests
{
    private const string TestHardwareId = @"PCI\VEN_1002&DEV_150E";
    private const string TestInstanceId = TestHardwareId + @"&SUBSYS_3A191043&REV_E3\4&35FE04F8&0&0041";

    private static DriverOffer TestDriverOffer(bool accepted = true)
    {
        return new DriverOffer
        {
            Id = Guid.NewGuid(), Revision = 2, Title = "Advanced Micro Devices, Inc. - Display - 32.0.32015.12012",
            Class = "Display", Provider = "Advanced Micro Devices, Inc.", HardwareId = TestHardwareId,
            LicenseAccepted = accepted, License = accepted ? null : "Example driver terms.", Date = new DateTime(2026, 11, 20)
        };
    }

    private static void RunWindowsDriverUpdateTests()
    {
        Test("Windows Update driver selection is specific to the selected AMD GPU", () =>
        {
            var selected = new DriverTarget { InstanceId = TestInstanceId, Version = "32.0.32015.10000" };
            selected.HardwareIds.Add(TestHardwareId);
            var other = new DriverTarget { InstanceId = @"PCI\VEN_1002&DEV_7550\4&11&0&0042" };
            other.HardwareIds.Add(@"PCI\VEN_1002&DEV_7550");
            var present = new[] { selected, other };
            var offer = TestDriverOffer();
            Check(WindowsDriverUpdates.Matches(offer, selected, present), "Newer AMD display driver was filtered");
            Check(!WindowsDriverUpdates.Matches(offer, other, present), "Offer appeared for the wrong GPU");
            other.HardwareIds.Add(TestHardwareId);
            Check(!WindowsDriverUpdates.Matches(offer, selected, present), "Driver matching two display devices was accepted");
            other.HardwareIds.Remove(TestHardwareId);
            offer.ApplicableDisplayIds.UnionWith(other.HardwareIds);
            Check(!WindowsDriverUpdates.Matches(offer, selected, present), "Secondary device match was ignored");
            foreach (Action<DriverOffer> invalidate in new Action<DriverOffer>[]
            {
                o => o.Class = "System", o => o.Provider = "Unrelated Vendor", o => o.MayPrompt = true,
                o => o.Bundled = true, o => o.Title = "Display - 32.0.32015.10000",
                o => o.Title = "Display - 32.0.31900.1", o => o.Id = Guid.Empty, o => o.Revision = 0,
                o => { o.LicenseAccepted = false; o.License = ""; }
            })
            {
                offer = TestDriverOffer();
                invalidate(offer);
                Check(!WindowsDriverUpdates.Matches(offer, selected, present), "Invalid or unsafe driver offer was accepted");
            }
        });
        Test("The elevated driver worker validates device, update identity, revision and consent", () =>
        {
            string valid = Guid.NewGuid().ToString("D");
            Check(WindowsDriverUpdates.IsAmdInstance(TestInstanceId), "Valid PnP instance was rejected");
            foreach (string invalid in new[] { TestInstanceId + "\" anything", TestHardwareId, "PCI/NVIDIA", null })
                Check(WindowsDriverUpdates.RunInstall(invalid, valid, "2", "reviewed") == WindowsDriverUpdates.OfferChanged,
                    "Malformed device argument reached the worker");
            Check(WindowsDriverUpdates.RunInstall(TestInstanceId, "missing", "2", "reviewed") == WindowsDriverUpdates.OfferChanged
                && WindowsDriverUpdates.RunInstall(TestInstanceId, valid, "-1", "accepted") == WindowsDriverUpdates.OfferChanged
                && WindowsDriverUpdates.RunInstall(TestInstanceId, valid, "2", "missing") == WindowsDriverUpdates.OfferChanged,
                "Invalid update argument reached the worker");
        });
        Test("AMD driver update dialog has localized keyboard controls and prevents unconfirmed installation", () =>
        {
            foreach (string language in L.Languages)
            {
                L.Load(language);
                using (var font = new Font(SystemFonts.MessageBoxFont.FontFamily, SystemFonts.MessageBoxFont.Size * 2))
                using (var dialog = new DriverUpdateDialog(new GpuInfo { Name = "Radeon test GPU", Id = TestInstanceId, Driver = "32.0.12345.67" }, font, "system"))
                {
                    dialog.Size = new Size(600, 500);
                    dialog.Opacity = 0;
                    dialog.Show();
                    Application.DoEvents();
                    var fields = Descendants(dialog).ToList();
                    var details = fields.OfType<TextBox>().Single();
                    var offers = fields.OfType<ComboBox>().Single();
                    var install = fields.OfType<Button>().Single(b => b.AccessibleName == L.T("wud.install"));
                    var license = fields.OfType<Button>().Single(b => b.AccessibleName == L.T("wud.license"));
                    Check(details.ReadOnly && details.Text.Contains("32.0.12345.67") && details.Focused, "Installed driver was not reachable");
                    Check(!install.Enabled && !offers.Enabled, "Installation was available before selecting an offer");
                    offers.Items.Add(TestDriverOffer(false));
                    offers.Items.Add(TestDriverOffer());
                    offers.SelectedIndex = 0;
                    Check(license.Enabled && !install.Enabled, "Installation was enabled before the required license could be read");
                    offers.SelectedIndex = 1;
                    Check(install.Enabled && !license.Enabled && details.Text.Contains(offers.SelectedItem.ToString()), "Selected offer was not presented");
                    foreach (var control in fields.Where(c => c.Enabled && (c is Button || c is TextBox || c is ComboBox)))
                        CheckFocusVisible(control, "AMD updates/" + language);
                    bool asked = false;
                    SystemDialogs.TestShow = (instruction, body, question) =>
                    {
                        asked = question && body.Contains(offers.SelectedItem.ToString());
                        return 7;
                    };
                    try { install.PerformClick(); }
                    finally { SystemDialogs.TestShow = null; }
                    Check(asked && !dialog.RefreshDriver && install.Enabled, "Declining the system dialog must not install the driver");
                    dialog.Close();
                }
            }
            L.Load("pl");
        });
    }
}
