using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Radeoner
{
    internal sealed class AmdDisplayInf
    {
        internal const int Installed = 0, RestartRequired = 3010, Changed = 11, Failed = 13, NotBetter = 259;
        private const uint EnumerateSingleInf = 0x00010000, QuietInstall = 0x00800000, InstallNonInteractive = 0x00000004;
        private static readonly Guid DisplayClass = new Guid("4D36E968-E325-11CE-BFC1-08002BE10318");
        private static readonly Regex InstancePattern = new Regex(@"\APCI\\VEN_1002&DEV_[0-9A-F]{4}[A-Z0-9&_.-]*\\[A-Z0-9&_.-]+\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        internal string Path, Hash, HardwareId;
        internal Version Version, CurrentVersion;
        internal bool Newer { get { return CurrentVersion != null && Version > CurrentVersion; } }

        internal static bool IsAmdInstance(string value)
        {
            return !string.IsNullOrEmpty(value) && value.Length <= 200 && InstancePattern.IsMatch(value);
        }

        private static string UniqueHardwareId(string instanceId)
        {
            string preferred = null;
            using (var query = new ManagementObjectSearcher("SELECT DeviceID,PNPClass,HardwareID,CompatibleID FROM Win32_PnPEntity WHERE Present=True"))
            using (var rows = query.Get())
            {
                var otherIds = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (ManagementObject row in rows) using (row)
                {
                    var hardwareIds = row["HardwareID"] as string[] ?? new string[0];
                    if (string.Equals(instanceId, row["DeviceID"] as string, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(row["PNPClass"] as string, "Display", StringComparison.OrdinalIgnoreCase))
                        preferred = hardwareIds.FirstOrDefault();
                    else
                    {
                        otherIds.UnionWith(hardwareIds);
                        otherIds.UnionWith(row["CompatibleID"] as string[] ?? new string[0]);
                    }
                }
                if (preferred == null || otherIds.Contains(preferred) || !Regex.IsMatch(preferred, @"\APCI\\VEN_1002&DEV_[0-9A-F]{4}&[A-Z0-9&_.-]+\z", RegexOptions.IgnoreCase))
                    return null;
            }
            return preferred;
        }

        private static Version ReadInstalled(string instanceId)
        {
            using (var query = new ManagementObjectSearcher("SELECT DeviceID,DriverVersion FROM Win32_PnPSignedDriver WHERE DeviceClass='DISPLAY'"))
            using (var rows = query.Get())
                foreach (ManagementObject row in rows) using (row)
                    if (string.Equals(instanceId, row["DeviceID"] as string, StringComparison.OrdinalIgnoreCase))
                    {
                        Version version;
                        return Version.TryParse(row["DriverVersion"] as string, out version) ? version : null;
                    }
            return null;
        }

        internal static AmdDisplayInf Find(string displayFolder, string instanceId)
        {
            if (!IsAmdInstance(instanceId) || !Directory.Exists(displayFolder)) return null;
            string hardwareId = UniqueHardwareId(instanceId);
            if (hardwareId == null) return null;
            Version current = ReadInstalled(instanceId);
            if (current == null) return null;
            var display = DisplayClass;
            IntPtr devices = SetupDiGetClassDevs(ref display, null, IntPtr.Zero, 2); // DIGCF_PRESENT
            if (devices == new IntPtr(-1)) throw new Win32Exception();
            try
            {
                var device = new Device { Size = (uint)Marshal.SizeOf(typeof(Device)) };
                if (!SetupDiOpenDeviceInfo(devices, instanceId, IntPtr.Zero, 0, ref device)) throw new Win32Exception();
                var parameters = new InstallParams { Size = (uint)Marshal.SizeOf(typeof(InstallParams)) };
                if (!SetupDiGetDeviceInstallParams(devices, ref device, ref parameters)) throw new Win32Exception();
                parameters.Flags |= EnumerateSingleInf | QuietInstall;
                string bestPath = null;
                Version bestVersion = null;
                uint bestRank = uint.MaxValue;
                foreach (string path in Directory.EnumerateFiles(displayFolder, "*.inf", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                {
                    Guid cls;
                    uint required;
                    if (path.Length >= 260 || !SetupDiGetINFClass(path, out cls, new StringBuilder(64), 64, out required) || cls != DisplayClass) continue;
                    parameters.DriverPath = path;
                    if (!SetupDiSetDeviceInstallParams(devices, ref device, ref parameters)) throw new Win32Exception();
                    const uint compatibleDrivers = 2;
                    if (!SetupDiBuildDriverInfoList(devices, ref device, compatibleDrivers)) throw new Win32Exception();
                    try
                    {
                        var info = new DriverInfo { Size = (uint)Marshal.SizeOf(typeof(DriverInfo)) };
                        if (!SetupDiEnumDriverInfo(devices, ref device, compatibleDrivers, 0, ref info))
                        {
                            if (Marshal.GetLastWin32Error() != 259) throw new Win32Exception();
                            continue;
                        }
                        string provider = (info.Provider ?? "").Trim().TrimEnd('.');
                        if (!provider.Equals("Advanced Micro Devices, Inc", StringComparison.OrdinalIgnoreCase)
                            && !provider.Equals("Advanced Micro Devices", StringComparison.OrdinalIgnoreCase)
                            && !provider.Equals("ATI Technologies Inc", StringComparison.OrdinalIgnoreCase)) continue;
                        var rank = new DriverParams { Size = (uint)Marshal.SizeOf(typeof(DriverParams)) };
                        if (!SetupDiGetDriverInstallParams(devices, ref device, ref info, ref rank)) throw new Win32Exception();
                        Version version = DecodeVersion(info.Version);
                        if (bestVersion == null || rank.Rank < bestRank || (rank.Rank == bestRank && version > bestVersion))
                        {
                            bestVersion = version;
                            bestPath = path;
                            bestRank = rank.Rank;
                        }
                    }
                    finally { SetupDiDestroyDriverInfoList(devices, ref device, compatibleDrivers); }
                }
                if (bestPath == null) return null;
                var signer = new InfSigner { Size = (uint)Marshal.SizeOf(typeof(InfSigner)) };
                if (!SetupVerifyInfFile(bestPath, IntPtr.Zero, ref signer)) throw new Win32Exception();
                return new AmdDisplayInf
                {
                    Path = bestPath, Hash = UpdateFeed.Hash(bestPath), HardwareId = hardwareId, Version = bestVersion, CurrentVersion = current
                };
            }
            finally { SetupDiDestroyDeviceInfoList(devices); }
        }

        internal static Version DecodeVersion(ulong number)
        {
            return new Version((ushort)(number >> 48), (ushort)(number >> 32), (ushort)(number >> 16), (ushort)number);
        }

        internal int Install(string instanceId)
        {
            Version currentlyInstalled = ReadInstalled(instanceId);
            if (currentlyInstalled == null || currentlyInstalled >= Version
                || !string.Equals(HardwareId, UniqueHardwareId(instanceId), StringComparison.OrdinalIgnoreCase))
                return Changed;
            bool reboot;
            // Windows checks the signed catalog and the current rank. No force or interactive flags.
            if (!UpdateDriverForPlugAndPlayDevices(IntPtr.Zero, HardwareId, Path, InstallNonInteractive, out reboot))
            {
                int error = Marshal.GetLastWin32Error();
                return error == 0 ? Failed : error;
            }
            return reboot ? RestartRequired : Installed;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Device
        {
            internal uint Size;
            internal Guid Class;
            internal uint Instance;
            internal IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct InstallParams
        {
            internal uint Size, Flags, FlagsEx;
            internal IntPtr Parent, Handler, Context, Queue, ClassReserved;
            internal uint Reserved;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] internal string DriverPath;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DriverInfo
        {
            internal uint Size, Type;
            internal IntPtr Reserved;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] internal string Description;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] internal string Manufacturer;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] internal string Provider;
            internal System.Runtime.InteropServices.ComTypes.FILETIME Date;
            internal ulong Version;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DriverParams
        {
            internal uint Size, Rank, Flags;
            internal IntPtr PrivateData;
            internal uint Reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct InfSigner
        {
            internal uint Size;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] internal string Catalog;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] internal string Signer;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] internal string SignerVersion;
        }

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid cls, string enumerator, IntPtr parent, uint flags);
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetupDiOpenDeviceInfo(IntPtr devices, string instance, IntPtr parent, uint flags, ref Device device);
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetupDiGetDeviceInstallParams(IntPtr devices, ref Device device, ref InstallParams data);
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetupDiSetDeviceInstallParams(IntPtr devices, ref Device device, ref InstallParams data);
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetupDiGetINFClass(string path, out Guid cls, StringBuilder name, uint length, out uint required);
        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiBuildDriverInfoList(IntPtr devices, ref Device device, uint type);
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetupDiEnumDriverInfo(IntPtr devices, ref Device device, uint type, uint index, ref DriverInfo driver);
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetupDiGetDriverInstallParams(IntPtr devices, ref Device device, ref DriverInfo driver, ref DriverParams data);
        [DllImport("setupapi.dll")]
        private static extern bool SetupDiDestroyDriverInfoList(IntPtr devices, ref Device device, uint type);
        [DllImport("setupapi.dll")]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr devices);
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetupVerifyInfFile(string inf, IntPtr alternatePlatform, ref InfSigner signer);
        [DllImport("newdev.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool UpdateDriverForPlugAndPlayDevices(IntPtr parent, string hardwareId, string inf, uint flags, out bool reboot);
    }
}
