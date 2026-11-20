using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace Radeoner
{
    internal sealed class DriverTarget
    {
        internal string InstanceId, Version;
        internal readonly HashSet<string> HardwareIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    internal sealed class DriverOffer
    {
        internal string Title, Provider, Class, HardwareId, License;
        internal Guid Id;
        internal int Revision;
        internal DateTime Date;
        internal bool LicenseAccepted, MayPrompt, Bundled;
        internal readonly HashSet<string> ApplicableDisplayIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public override string ToString() { return Title; }
    }

    internal static class WindowsDriverUpdates
    {
        internal const string SupportUrl = "https://www.amd.com/en/support/download/drivers.html";
        internal const int Installed = 0, RestartRequired = 10, OfferChanged = 11, WindowsBusy = 12, Failed = 13;
        private const string SearchFilter = "IsInstalled=0 and IsHidden=0 and Type='Driver'";
        private static readonly Regex InstancePattern = new Regex(@"\APCI\\VEN_1002&DEV_[0-9A-F]{4}[A-Z0-9&_.-]*\\[A-Z0-9&_.-]+\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex VersionAtEnd = new Regex(@"\b(\d+\.\d+\.\d+\.\d+)\s*\z", RegexOptions.CultureInvariant);

        internal static bool IsAmdInstance(string id)
        {
            return !string.IsNullOrEmpty(id) && id.Length <= 256 && InstancePattern.IsMatch(id);
        }

        internal static bool Matches(DriverOffer offer, DriverTarget selected, IList<DriverTarget> present)
        {
            if (selected == null || offer.Id == Guid.Empty || offer.Revision < 1 || offer.Bundled || offer.MayPrompt
                || string.IsNullOrWhiteSpace(offer.Title) || (!offer.LicenseAccepted && string.IsNullOrWhiteSpace(offer.License))
                || !string.Equals(offer.Class, "Display", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrEmpty(offer.HardwareId) || !offer.HardwareId.StartsWith(@"PCI\VEN_1002&DEV_", StringComparison.OrdinalIgnoreCase))
                return false;
            string provider = (offer.Provider ?? "").Trim().TrimEnd('.');
            if (!string.Equals(provider, "Advanced Micro Devices, Inc", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(provider, "Advanced Micro Devices", StringComparison.OrdinalIgnoreCase)) return false;
            if (!selected.HardwareIds.Contains(offer.HardwareId)
                || present.Any(card => card != selected && (card.HardwareIds.Contains(offer.HardwareId)
                    || card.HardwareIds.Overlaps(offer.ApplicableDisplayIds)))) return false;

            // Windows ranks packages by applicability, which is not always a version upgrade.
            // Reject a known older or equal version when the catalog includes it in the title.
            Version installed, offered;
            var version = VersionAtEnd.Match(offer.Title);
            return !version.Success || !Version.TryParse(selected.Version, out installed)
                || !Version.TryParse(version.Groups[1].Value, out offered) || offered > installed;
        }

        private static List<DriverTarget> ReadTargets(string instanceId)
        {
            var targets = new List<DriverTarget>();
            // Count matching IDs across all present display adapters, including identical cards.
            using (var query = new ManagementObjectSearcher("SELECT DeviceID,HardwareID,CompatibleID FROM Win32_PnPEntity WHERE PNPClass='Display' AND Present=True"))
            using (var rows = query.Get())
                foreach (ManagementObject row in rows) using (row)
                {
                    var target = new DriverTarget { InstanceId = row["DeviceID"] as string };
                    target.HardwareIds.UnionWith(row["HardwareID"] as string[] ?? new string[0]);
                    target.HardwareIds.UnionWith(row["CompatibleID"] as string[] ?? new string[0]);
                    targets.Add(target);
                }
            var selected = FindTarget(targets, instanceId);
            if (selected != null)
                using (var query = new ManagementObjectSearcher("SELECT DeviceID,DriverVersion FROM Win32_PnPSignedDriver WHERE DeviceClass='DISPLAY'"))
                using (var rows = query.Get())
                    foreach (ManagementObject row in rows) using (row)
                        if (string.Equals(row["DeviceID"] as string, instanceId, StringComparison.OrdinalIgnoreCase))
                            selected.Version = row["DriverVersion"] as string;
            return targets;
        }

        private static DriverTarget FindTarget(IEnumerable<DriverTarget> targets, string instanceId)
        {
            return targets.SingleOrDefault(card => string.Equals(card.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
        }

        // WUA automation objects stay on the thread that owns this scope. Only data reaches WinForms.
        private sealed class ComScope : IDisposable
        {
            private readonly List<object> objects = new List<object>();
            internal dynamic Keep(object item) { objects.Add(item); return item; }
            internal dynamic Create(string name) { return Keep(Activator.CreateInstance(Type.GetTypeFromProgID(name, true))); }
            public void Dispose()
            {
                for (int i = objects.Count - 1; i >= 0; i--) Marshal.ReleaseComObject(objects[i]);
            }
        }

        private static dynamic Search(ComScope scope, dynamic session, string criteria)
        {
            dynamic searcher = scope.Keep(session.CreateUpdateSearcher());
            dynamic result = scope.Keep(searcher.Search(criteria));
            int code = result.ResultCode;
            if (code != 2) throw new IOException(L.T("wud.scanResult", code));
            return scope.Keep(result.Updates);
        }

        private static DriverOffer ReadOffer(ComScope scope, dynamic update)
        {
            dynamic identity = scope.Keep(update.Identity);
            dynamic behavior = scope.Keep(update.InstallationBehavior);
            dynamic bundled = scope.Keep(update.BundledUpdates);
            bool accepted = update.EulaAccepted;
            var offer = new DriverOffer
            {
                Id = Guid.Parse((string)identity.UpdateID), Revision = identity.RevisionNumber,
                Title = update.Title, Provider = update.DriverProvider, Class = update.DriverClass,
                HardwareId = update.DriverHardwareID, Date = update.DriverVerDate,
                LicenseAccepted = accepted, License = accepted ? null : (string)update.EulaText,
                MayPrompt = behavior.CanRequestUserInput, Bundled = bundled.Count != 0
            };
            dynamic entries = scope.Keep(update.WindowsDriverUpdateEntries);
            int count = entries.Count;
            for (int i = 0; i < count; i++)
            {
                dynamic entry = scope.Keep(entries.Item[i]);
                if (string.Equals((string)entry.DriverClass, "Display", StringComparison.OrdinalIgnoreCase))
                    offer.ApplicableDisplayIds.Add((string)entry.DriverHardwareID);
            }
            return offer;
        }

        internal static List<DriverOffer> Check(string instanceId)
        {
            if (!IsAmdInstance(instanceId)) throw new InvalidOperationException(L.T("wud.noGpu"));
            var targets = ReadTargets(instanceId);
            var selected = FindTarget(targets, instanceId);
            if (selected == null) throw new InvalidOperationException(L.T("wud.noGpu"));
            using (var scope = new ComScope())
            {
                dynamic session = scope.Create("Microsoft.Update.Session");
                session.ClientApplicationID = "Radeoner";
                dynamic updates = Search(scope, session, SearchFilter);
                var offers = new List<DriverOffer>();
                int count = updates.Count;
                for (int i = 0; i < count; i++)
                {
                    dynamic update = scope.Keep(updates.Item[i]);
                    if (!string.Equals((string)update.DriverClass, "Display", StringComparison.OrdinalIgnoreCase)) continue;
                    DriverOffer offer = ReadOffer(scope, update);
                    if (Matches(offer, selected, targets)) offers.Add(offer);
                }
                return offers.OrderByDescending(offer => offer.Date).ThenBy(offer => offer.Title).ToList();
            }
        }

        internal static int RequestInstall(string instanceId, DriverOffer offer)
        {
            if (!IsAmdInstance(instanceId) || offer.Id == Guid.Empty || offer.Revision < 1) return OfferChanged;
            string arguments = "--install-windows-amd \"" + instanceId + "\" " + offer.Id.ToString("D")
                + " " + offer.Revision.ToString(CultureInfo.InvariantCulture) + (offer.LicenseAccepted ? " accepted" : " reviewed");
            using (var process = Process.Start(new ProcessStartInfo(typeof(WindowsDriverUpdates).Assembly.Location, arguments)
            {
                UseShellExecute = true, Verb = "runas", WorkingDirectory = Environment.SystemDirectory
            }))
            {
                if (process == null) return Failed;
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        internal static int RunInstall(string instanceId, string updateId, string revision, string license)
        {
            Guid id;
            int number;
            if (!IsAmdInstance(instanceId) || !Guid.TryParseExact(updateId, "D", out id) || id == Guid.Empty
                || !int.TryParse(revision, NumberStyles.None, CultureInfo.InvariantCulture, out number) || number < 1
                || (license != "accepted" && license != "reviewed"))
                return OfferChanged;
            try
            {
                using (var identity = WindowsIdentity.GetCurrent())
                    if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator)) return unchecked((int)0x80070005);

                var targets = ReadTargets(instanceId);
                using (var scope = new ComScope())
                {
                    dynamic session = scope.Create("Microsoft.Update.Session");
                    session.ClientApplicationID = "Radeoner";
                    string criteria = SearchFilter + " and UpdateID='" + id.ToString("D") + "' and RevisionNumber=" + number.ToString(CultureInfo.InvariantCulture);
                    dynamic updates = Search(scope, session, criteria);
                    if (updates.Count != 1) return OfferChanged;
                    dynamic update = scope.Keep(updates.Item[0]);
                    DriverOffer offer = ReadOffer(scope, update);
                    if (offer.Id != id || offer.Revision != number || !Matches(offer, FindTarget(targets, instanceId), targets))
                        return OfferChanged;
                    if (!offer.LicenseAccepted && license != "reviewed") return OfferChanged;

                    dynamic installer = scope.Keep(session.CreateUpdateInstaller());
                    if (installer.IsBusy || installer.RebootRequiredBeforeInstallation) return WindowsBusy;
                    dynamic chosen = scope.Create("Microsoft.Update.UpdateColl");
                    chosen.Add(update);
                    // The calling dialog presents the catalog terms and asks for explicit consent.
                    if (!offer.LicenseAccepted) update.AcceptEula();
                    if (!update.IsDownloaded)
                    {
                        dynamic downloader = scope.Keep(session.CreateUpdateDownloader());
                        downloader.Updates = chosen;
                        dynamic download = scope.Keep(downloader.Download());
                        if (download.ResultCode != 2 || !update.IsDownloaded)
                            return FailureCode(scope, download);
                    }

                    targets = ReadTargets(instanceId);
                    if (!Matches(offer, FindTarget(targets, instanceId), targets)) return OfferChanged;
                    if (installer.IsBusy || installer.RebootRequiredBeforeInstallation) return WindowsBusy;
                    installer.Updates = chosen;
                    installer.AllowSourcePrompts = false;
                    dynamic result = scope.Keep(installer.Install());
                    dynamic single = scope.Keep(result.GetUpdateResult(0));
                    if (result.ResultCode != 2 || single.ResultCode != 2)
                    {
                        int error = single.HResult;
                        return error == 0 ? Failed : error;
                    }
                    return result.RebootRequired ? RestartRequired : Installed;
                }
            }
            catch (COMException ex) { return ex.ErrorCode; }
            catch (Exception) { return Failed; }
        }

        private static int FailureCode(ComScope scope, dynamic result)
        {
            dynamic failure = scope.Keep(result.GetUpdateResult(0));
            int error = failure.HResult;
            return error == 0 ? Failed : error;
        }
    }
}
