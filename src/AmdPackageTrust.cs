using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace Radeoner
{
    internal static class AmdPackageTrust
    {
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
                    if (name != "Advanced Micro Devices" && name != "Advanced Micro Devices, Inc." && name != "Advanced Micro Devices, Inc")
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
                return BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "").ToLowerInvariant();
        }
    }
}
