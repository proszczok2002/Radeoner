using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Radeoner;

internal static partial class Tests
{
    private sealed class RgbServer : IDisposable
    {
        private readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        private readonly Task task;
        private TcpClient accepted;
        private volatile bool disposed;
        internal volatile bool ChangeIdentity, BadMagic;
        internal readonly bool NativeBrightness, ModeSpecific, IncludeOff;
        internal int Port, Updates, ActiveMode;
        internal uint Brightness = 110;
        internal uint[] Colors = { 0xFFFFFF, 0xFFFFFF };
        internal uint ModeColor = 0xFFFFFF;
        internal Exception Failure;
        internal System.Threading.ManualResetEventSlim ListRequested, ListRelease;
        internal RgbServer(bool nativeBrightness = false, bool modeSpecific = false, bool includeOff = true)
        {
            NativeBrightness = nativeBrightness; ModeSpecific = modeSpecific; IncludeOff = includeOff; listener.Start(); Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            task = Task.Run(() =>
            {
                try
                {
                    accepted = listener.AcceptTcpClient();
                    using (var stream = accepted.GetStream())
                    using (var r = new BinaryReader(stream))
                    using (var w = new BinaryWriter(stream))
                    {
                        while (!disposed)
                        {
                            Check(r.ReadUInt32() == 0x4247524F, "Invalid OpenRGB request magic");
                            uint device = r.ReadUInt32(), command = r.ReadUInt32(); int length = checked((int)r.ReadUInt32());
                            Check(length < 100000, "Unbounded RGB request"); byte[] data = r.ReadBytes(length); Check(data.Length == length, "Truncated RGB request"); byte[] response = null;
                            if (command == 40) { Check(BitConverter.ToUInt32(data, 0) == 3, "Protocol 3 was not negotiated"); response = BitConverter.GetBytes(6u); }
                            else if (command == 50) Check(Encoding.UTF8.GetString(data).StartsWith("Radeoner v"), "RGB client version absent");
                            else if (command == 0)
                            {
                                if (ListRequested != null) { ListRequested.Set(); Check(ListRelease.Wait(3000), "RGB list gate timed out"); }
                                response = BitConverter.GetBytes(2u);
                            }
                            else if (command == 1)
                            {
                                Check(data.SequenceEqual(BitConverter.GetBytes(3u)), "Controller requested at wrong protocol");
                                response = device == 0 ? new byte[] { 8, 0, 0, 0, 0, 0, 0, 0 } : Description();
                            }
                            else
                            {
                                Check(device == 1, "RGB write targeted a non-GPU"); Updates++;
                                using (var payload = new BinaryReader(new MemoryStream(data)))
                                {
                                    Check(payload.ReadUInt32() == data.Length, "RGB payload size excludes its own size field");
                                    if (command == 1101)
                                    {
                                        ActiveMode = payload.ReadInt32(); Check(ActiveMode == 0 || ActiveMode == 1, "Invalid lighting mode");
                                        Check(ReadString(payload) == (ActiveMode == 0 ? "Direct" : "Off"), "Mode name changed");
                                        Check(payload.ReadUInt32() == (ActiveMode == 0 ? 0xFFFFu : 0), "Mode value changed");
                                        uint flags = payload.ReadUInt32(); Check(flags == (ActiveMode == 0 ? (ModeSpecific ? 64u : 32u) | (NativeBrightness ? 16u : 0) : 0), "Mode flags changed");
                                        Check(payload.ReadUInt32() == 0 && payload.ReadUInt32() == 0, "Speed range changed");
                                        uint min = payload.ReadUInt32(), max = payload.ReadUInt32();
                                        Check(min == (ActiveMode == 0 && NativeBrightness ? 10u : 0) && max == (ActiveMode == 0 && NativeBrightness ? 110u : 0), "Brightness range changed");
                                        uint colorCount = ActiveMode == 0 && ModeSpecific ? 1u : 0;
                                        Check(payload.ReadUInt32() == colorCount && payload.ReadUInt32() == colorCount && payload.ReadUInt32() == 0, "Colors/speed metadata changed");
                                        Brightness = payload.ReadUInt32(); Check(payload.ReadUInt32() == 0, "Direction changed");
                                        Check(payload.ReadUInt32() == (ActiveMode == 0 ? ModeSpecific ? 2u : 1u : 0), "Invalid color mode"); Check(payload.ReadUInt16() == colorCount, "Unexpected mode colors"); if (colorCount == 1) ModeColor = payload.ReadUInt32();
                                    }
                                    else if (command == 1050)
                                    {
                                        Check(payload.ReadUInt16() == 2, "LED count changed"); Colors[0] = payload.ReadUInt32(); Colors[1] = payload.ReadUInt32();
                                    }
                                    else throw new Exception("Unexpected RGB command " + command);
                                    Check(payload.BaseStream.Position == data.Length, "Trailing RGB request bytes");
                                }
                            }
                            if (response != null)
                            {
                                // Send header and payload separately to exercise TCP framing.
                                w.Write(BadMagic ? 0x00000000u : 0x4247524Fu); w.Write(device); w.Write(command); w.Write((uint)response.Length); w.Flush(); w.Write(response); w.Flush();
                            }
                        }
                    }
                }
                catch (Exception ex) { if (!disposed) Failure = ex; }
            });
        }
        private static void String(BinaryWriter w, string value) { byte[] text = Encoding.UTF8.GetBytes(value + "\0"); w.Write((ushort)text.Length); w.Write(text); }
        private static string ReadString(BinaryReader r) { return Encoding.UTF8.GetString(r.ReadBytes(r.ReadUInt16())).TrimEnd('\0'); }
        internal byte[] Description()
        {
            using (var memory = new MemoryStream())
            using (var w = new BinaryWriter(memory))
            {
                w.Write(0u); w.Write(2); foreach (string value in new[] { ChangeIdentity ? "Another GPU" : "ASUS TUF RX 9070 XT test fixture", "ASUS", "Synthetic SDK fixture", "1", "test-only", "test-bus" }) String(w, value);
                int modeCount = IncludeOff ? 2 : 1; w.Write((ushort)modeCount); w.Write(ActiveMode);
                for (int mode = 0; mode < modeCount; mode++)
                {
                    bool direct = mode == 0; String(w, direct ? "Direct" : "Off"); w.Write(direct ? 0xFFFFu : 0); w.Write(direct ? (ModeSpecific ? 64u : 32u) | (NativeBrightness ? 16u : 0) : 0);
                    w.Write(0u); w.Write(0u); w.Write(direct && NativeBrightness ? 10u : 0); w.Write(direct && NativeBrightness ? 110u : 0);
                    uint colors = direct && ModeSpecific ? 1u : 0;
                    w.Write(colors); w.Write(colors); w.Write(0u); w.Write(direct ? Brightness : 0); w.Write(0u); w.Write(direct ? ModeSpecific ? 2u : 1u : 0); w.Write((ushort)colors); if (colors == 1) w.Write(ModeColor);
                }
                w.Write((ushort)1); String(w, "GPU LEDs"); w.Write(1); w.Write(0u); w.Write(2u); w.Write(2u); w.Write((ushort)0);
                w.Write((ushort)2); String(w, "LED 1"); w.Write(0u); String(w, "LED 2"); w.Write(1u);
                w.Write((ushort)2); w.Write(Colors[0]); w.Write(Colors[1]); w.Flush(); long size = memory.Length; memory.Position = 0; w.Write((uint)size); w.Flush(); return memory.ToArray();
            }
        }
        internal void CheckHealthy() { if (Failure != null) throw new Exception("SDK server contract failed", Failure); }
        public void Dispose() { disposed = true; if (accepted != null) accepted.Close(); listener.Stop(); task.Wait(1500); }
    }
    private static void RunRgbTests()
    {
        Test("OpenRGB loopback protocol 3 discovers only GPUs and applies color, software dimming and Off", () =>
        {
            foreach (bool offMode in new[] { true, false })
            using (var server = new RgbServer(false, false, offMode))
            using (var client = new OpenRgbClient(server.Port))
            {
                var devices = client.Devices(); Check(devices.Count == 1 && devices[0].Index == 1 && server.Updates == 0, "Discovery modified lighting or included non-GPU devices");
                client.Apply(devices[0], 0, 200, 100, 40, 50, false); server.CheckHealthy();
                Check(server.Colors.All(c => c == 0x143264), "Software brightness/color byte order incorrect");
                client.Apply(devices[0], 0, 255, 255, 255, 100, true); server.CheckHealthy(); Check(offMode ? server.ActiveMode == 1 : server.Colors.All(c => c == 0), "Off mode/black fallback failed");
            }
        });
        Test("OpenRGB hardware brightness maps reported min/max and zero brightness selects Off", () =>
        {
            foreach (bool modeSpecific in new[] { false, true })
            using (var server = new RgbServer(true, modeSpecific))
            using (var client = new OpenRgbClient(server.Port))
            {
                var device = client.Devices().Single(); client.Apply(device, 0, 200, 100, 40, 25, false); server.CheckHealthy();
                Check(server.Brightness == 35 && (modeSpecific ? server.ModeColor : server.Colors[0]) == 0x2864C8, "Native brightness scaled RGB instead of hardware brightness");
                client.Apply(device, 0, 200, 100, 40, 0, false); server.CheckHealthy(); Check(server.ActiveMode == 1, "0 percent did not turn off");
            }
        });
        Test("OpenRGB rejects changed GPU identity and malformed frames before issuing lighting writes", () =>
        {
            using (var server = new RgbServer())
            using (var client = new OpenRgbClient(server.Port))
            {
                var device = client.Devices().Single(); server.ChangeIdentity = true; bool rejected = false;
                try { client.Apply(device, 0, 255, 0, 0, 100, false); } catch (IOException) { rejected = true; }
                Check(rejected && server.Updates == 0, "A replaced GPU received a write"); server.CheckHealthy();
            }
            using (var server = new RgbServer())
            {
                server.BadMagic = true; bool rejected = false; try { using (var client = new OpenRgbClient(server.Port)) { } } catch (IOException) { rejected = true; }
                Check(rejected, "Malformed SDK frame was accepted");
            }
        });
        Test("RGB dialog is local, lazy, keyboard accessible and labels all numeric edit children", () =>
        {
            var watch = Stopwatch.StartNew();
            using (var dialog = new RgbDialog(SystemFonts.MessageBoxFont, "system"))
            {
                Check(watch.ElapsedMilliseconds < 2000 && dialog.Text.Contains("v" + L.Version), "RGB dialog performed blocking startup work");
                var controls = Descendants(dialog).ToList();
                foreach (var control in controls.Where(c => c is Button || c is ComboBox || c is NumericUpDown || c is TextBox)) Check(!string.IsNullOrWhiteSpace(control.AccessibleName), "Unlabeled RGB control");
                foreach (var number in controls.OfType<NumericUpDown>()) Check(number.Controls.OfType<TextBox>().Single().AccessibleName == number.AccessibleName, "Unlabeled RGB numeric edit");
                Check(controls.OfType<TextBox>().Any(t => t.Text == L.T("rgb.ready")), "RGB connected automatically");
                Check(controls.OfType<Button>().Single(b => b.AccessibleName == L.T("rgb.download")).Enabled, "Download requires an RGB server");
            }
        });
    }
}
