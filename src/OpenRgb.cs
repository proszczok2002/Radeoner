using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Radeoner
{
    internal sealed class RgbMode
    {
        internal string Name;
        internal uint Value, Flags, SpeedMin, SpeedMax, BrightnessMin, BrightnessMax, ColorsMin, ColorsMax, Speed, Brightness, Direction, ColorMode;
        internal List<uint> Colors = new List<uint>();
        internal bool PerLed { get { return (Flags & 32) != 0; } }
        internal bool ModeColor { get { return (Flags & 64) != 0 && ColorsMax > 0; } }
        internal bool CanColor { get { return PerLed || ModeColor; } }
        internal bool NativeBrightness { get { return (Flags & 16) != 0 && BrightnessMax > BrightnessMin; } }
        public override string ToString() { return Name; }
    }
    internal sealed class RgbDevice
    {
        internal uint Index;
        internal int Type, ActiveMode;
        internal string Name, Vendor, Description, Version, Serial, Location;
        internal List<RgbMode> Modes = new List<RgbMode>();
        internal List<uint> Colors = new List<uint>();
        internal string Identity { get { return Name + "\n" + Vendor + "\n" + Serial + "\n" + Location; } }
        public override string ToString() { return Name + (string.IsNullOrEmpty(Location) ? "" : " — " + Location); }
    }
    // Independent SDK protocol 3 client. No OpenRGB binary/library or SMBus
    // driver is bundled. Only an explicitly requested loopback connection is used.
    internal sealed class OpenRgbClient : IDisposable
    {
        private sealed class DeviceListChangedException : IOException
        {
            internal DeviceListChangedException() : base(L.T("rgb.devicesChanged")) { }
        }

        private readonly TcpClient socket = new TcpClient();
        private NetworkStream stream;
        private int disposed;
        internal OpenRgbClient(int port) : this(port, CancellationToken.None) { }

        private OpenRgbClient(int port, CancellationToken cancellation)
        {
            if (port < 1 || port > 65535) throw new ArgumentOutOfRangeException("port");
            try
            {
                using (cancellation.Register(Dispose))
                {
                    cancellation.ThrowIfCancellationRequested();
                    var connect = socket.ConnectAsync(IPAddress.Loopback, port);
                    if (!connect.Wait(1500)) throw new IOException(L.T("rgb.noServer"));
                    socket.NoDelay = true;
                    stream = socket.GetStream(); stream.ReadTimeout = stream.WriteTimeout = 3000;
                    byte[] version = Request(0, 40, BitConverter.GetBytes(3u));
                    if (version.Length != 4) throw new IOException(L.T("rgb.badPacket"));
                    if (BitConverter.ToUInt32(version, 0) < 3) throw new IOException(L.T("rgb.oldProtocol"));
                    Send(0, 50, Encoding.UTF8.GetBytes("Radeoner v" + L.Version + "\0"));
                }
                cancellation.ThrowIfCancellationRequested();
            }
            catch (Exception ex)
            {
                Dispose();
                cancellation.ThrowIfCancellationRequested();
                if (ex.GetBaseException() is SocketException) throw new IOException(L.T("rgb.noServer"), ex);
                throw;
            }
        }

        internal static OpenRgbClient Discover(int port, CancellationToken cancellation, out List<RgbDevice> devices)
        {
            for (int attempt = 0; ; attempt++)
            {
                OpenRgbClient connection = null;
                try
                {
                    connection = new OpenRgbClient(port, cancellation);
                    using (cancellation.Register(connection.Dispose)) devices = connection.Devices();
                    cancellation.ThrowIfCancellationRequested();
                    return connection;
                }
                catch (Exception ex)
                {
                    if (connection != null) connection.Dispose();
                    cancellation.ThrowIfCancellationRequested();
                    if (!(ex is DeviceListChangedException) || attempt == 2) throw;
                    // A fresh connection discards the outstanding reply and stale indices.
                    if (cancellation.WaitHandle.WaitOne(250)) cancellation.ThrowIfCancellationRequested();
                }
            }
        }
        private static byte[] Bytes(Action<BinaryWriter> write)
        { using (var memory = new MemoryStream()) { using (var output = new BinaryWriter(memory, Encoding.UTF8, true)) write(output); return memory.ToArray(); } }
        private void Send(uint device, uint command, byte[] payload)
        {
            if (Volatile.Read(ref disposed) != 0) throw new IOException(L.T("rgb.disconnected"));
            byte[] packet = Bytes(w => { w.Write(new byte[] { 79, 82, 71, 66 }); w.Write(device); w.Write(command); w.Write((uint)payload.Length); w.Write(payload); });
            try { stream.Write(packet, 0, packet.Length); }
            catch (IOException ex)
            {
                if (ex.InnerException is SocketException) throw new IOException(L.T("rgb.disconnected"), ex);
                throw;
            }
        }
        private byte[] Read(int count)
        {
            var bytes = new byte[count]; int offset = 0;
            try
            {
                while (offset < count)
                {
                    int read = stream.Read(bytes, offset, count - offset);
                    if (read == 0) throw new IOException(L.T("rgb.disconnected"));
                    offset += read;
                }
            }
            catch (IOException ex)
            {
                if (ex.InnerException is SocketException) throw new IOException(L.T("rgb.disconnected"), ex);
                throw;
            }
            return bytes;
        }
        private byte[] Request(uint device, uint command, byte[] payload)
        {
            Send(device, command, payload);
            byte[] header = Read(16);
            uint size = BitConverter.ToUInt32(header, 12), actual = BitConverter.ToUInt32(header, 8);
            if (header[0] != 79 || header[1] != 82 || header[2] != 71 || header[3] != 66 || size > 4 * 1024 * 1024) throw new IOException(L.T("rgb.badPacket"));
            if (actual == 100 && size == 0) throw new DeviceListChangedException();
            if (actual != command || BitConverter.ToUInt32(header, 4) != device) throw new IOException(L.T("rgb.badPacket"));
            return Read((int)size);
        }
        private static string String(BinaryReader reader)
        {
            int count = reader.ReadUInt16();
            if (count < 1 || count > 4096 || count > reader.BaseStream.Length - reader.BaseStream.Position) throw new IOException(L.T("rgb.badPacket"));
            byte[] data = reader.ReadBytes(count);
            if (data[count - 1] != 0) throw new IOException(L.T("rgb.badPacket"));
            return Encoding.UTF8.GetString(data, 0, count - 1).Replace('\r', ' ').Replace('\n', ' ');
        }
        private static int Count(BinaryReader reader)
        { int count = reader.ReadUInt16(); if (count > 4096) throw new IOException(L.T("rgb.badPacket")); return count; }
        internal static RgbDevice Parse(uint index, byte[] data)
        {
            using (var memory = new MemoryStream(data))
            using (var r = new BinaryReader(memory, Encoding.UTF8))
            {
                if (r.ReadUInt32() != data.Length) throw new IOException(L.T("rgb.badPacket"));
                var d = new RgbDevice { Index = index, Type = r.ReadInt32(), Name = String(r), Vendor = String(r), Description = String(r), Version = String(r), Serial = String(r), Location = String(r) };
                int modes = Count(r); d.ActiveMode = r.ReadInt32();
                for (int i = 0; i < modes; i++)
                {
                    var mode = new RgbMode { Name = String(r), Value = r.ReadUInt32(), Flags = r.ReadUInt32(), SpeedMin = r.ReadUInt32(), SpeedMax = r.ReadUInt32(), BrightnessMin = r.ReadUInt32(), BrightnessMax = r.ReadUInt32(), ColorsMin = r.ReadUInt32(), ColorsMax = r.ReadUInt32(), Speed = r.ReadUInt32(), Brightness = r.ReadUInt32(), Direction = r.ReadUInt32(), ColorMode = r.ReadUInt32() };
                    if (mode.BrightnessMin > mode.BrightnessMax || mode.ColorsMax > 4096 || mode.ColorsMin > mode.ColorsMax) throw new IOException(L.T("rgb.badPacket"));
                    int colors = Count(r); for (int j = 0; j < colors; j++) mode.Colors.Add(r.ReadUInt32()); d.Modes.Add(mode);
                }
                int zones = Count(r);
                for (int i = 0; i < zones; i++)
                {
                    String(r); r.ReadInt32(); r.ReadUInt32(); r.ReadUInt32(); r.ReadUInt32(); int matrix = r.ReadUInt16();
                    if (matrix > memory.Length - memory.Position) throw new IOException(L.T("rgb.badPacket")); r.ReadBytes(matrix);
                }
                int leds = Count(r); for (int i = 0; i < leds; i++) { String(r); r.ReadUInt32(); }
                int deviceColors = Count(r); for (int i = 0; i < deviceColors; i++) d.Colors.Add(r.ReadUInt32());
                if (modes == 0 && d.ActiveMode == 0) d.ActiveMode = -1;
                if (memory.Position != memory.Length || d.ActiveMode < -1 || (modes == 0 ? d.ActiveMode != -1 : d.ActiveMode >= modes)) throw new IOException(L.T("rgb.badPacket"));
                return d;
            }
        }
        internal List<RgbDevice> Devices()
        {
            byte[] count = Request(0, 0, new byte[0]);
            if (count.Length != 4 || BitConverter.ToUInt32(count, 0) > 256) throw new IOException(L.T("rgb.badPacket"));
            var devices = new List<RgbDevice>();
            for (uint i = 0; i < BitConverter.ToUInt32(count, 0); i++)
            {
                byte[] description = Request(i, 1, BitConverter.GetBytes(3u));
                if (description.Length < 8 || BitConverter.ToUInt32(description, 0) != description.Length) throw new IOException(L.T("rgb.badPacket"));
                if (BitConverter.ToInt32(description, 4) == 2) devices.Add(Parse(i, description)); // GPUs only.
            }
            return devices;
        }
        private static void WriteString(BinaryWriter w, string text)
        { byte[] bytes = Encoding.UTF8.GetBytes(text + "\0"); w.Write((ushort)bytes.Length); w.Write(bytes); }
        internal static uint Color(int red, int green, int blue, int percent)
        {
            if (red < 0 || red > 255 || green < 0 || green > 255 || blue < 0 || blue > 255 || percent < 0 || percent > 100) throw new ArgumentOutOfRangeException("color");
            return (uint)(red * percent / 100 | green * percent / 100 << 8 | blue * percent / 100 << 16);
        }
        internal void Apply(RgbDevice selected, int modeIndex, int red, int green, int blue, int percent, bool off)
        {
            off = off || percent == 0;
            uint raw = Color(red, green, blue, percent);
            var current = Parse(selected.Index, Request(selected.Index, 1, BitConverter.GetBytes(3u)));
            if (current.Type != 2 || current.Identity != selected.Identity) throw new IOException(L.T("rgb.devicesChanged"));
            if (!off)
            {
                if (modeIndex < 0 || modeIndex >= selected.Modes.Count) throw new InvalidOperationException(L.T("rgb.noColor"));
                var expectedMode = selected.Modes[modeIndex]; modeIndex = current.Modes.FindIndex(m => m.Name == expectedMode.Name && m.Value == expectedMode.Value);
                if (modeIndex < 0) throw new IOException(L.T("rgb.devicesChanged"));
            }
            if (off)
            {
                int offIndex = current.Modes.FindIndex(m => string.Equals(m.Name, "Off", StringComparison.OrdinalIgnoreCase));
                if (offIndex >= 0) modeIndex = offIndex;
                else { modeIndex = current.Modes.FindIndex(m => m.CanColor); if (modeIndex < 0) throw new InvalidOperationException(L.T("rgb.noColor")); }
            }
            if (modeIndex < 0 || modeIndex >= current.Modes.Count) throw new InvalidOperationException(L.T("rgb.noColor"));
            var mode = current.Modes[modeIndex];
            if (!off && !mode.CanColor) throw new InvalidOperationException(L.T("rgb.noColor"));
            if (mode.PerLed && current.Colors.Count == 0) throw new InvalidOperationException(L.T("rgb.noColor"));
            uint color = off ? 0 : mode.NativeBrightness ? Color(red, green, blue, 100) : raw;
            uint brightness = mode.NativeBrightness ? mode.BrightnessMin + (uint)Math.Round((mode.BrightnessMax - mode.BrightnessMin) * (off ? 0 : percent) / 100d) : mode.Brightness;
            int colors = mode.ModeColor ? (int)Math.Max(1, mode.ColorsMin) : mode.Colors.Count;
            byte[] modeData = Bytes(w =>
            {
                WriteString(w, mode.Name); w.Write(mode.Value); w.Write(mode.Flags); w.Write(mode.SpeedMin); w.Write(mode.SpeedMax); w.Write(mode.BrightnessMin); w.Write(mode.BrightnessMax);
                w.Write(mode.ColorsMin); w.Write(mode.ColorsMax); w.Write(mode.Speed); w.Write(brightness); w.Write(mode.Direction); w.Write(mode.PerLed ? 1u : mode.ModeColor ? 2u : mode.ColorMode);
                w.Write((ushort)colors); for (int i = 0; i < colors; i++) w.Write(color);
            });
            Send(current.Index, 1101, Bytes(w => { w.Write((uint)(8 + modeData.Length)); w.Write(modeIndex); w.Write(modeData); }));
            if (mode.PerLed) Send(current.Index, 1050, Bytes(w => { w.Write((uint)(6 + current.Colors.Count * 4)); w.Write((ushort)current.Colors.Count); for (int i = 0; i < current.Colors.Count; i++) w.Write(color); }));
            RgbDevice result = null;
            // Some servers expose the new state after their controller thread has
            // finished processing the write. Repeat the read, never the write.
            for (int attempt = 0; attempt < 3; attempt++)
            {
                if (attempt > 0) Thread.Sleep(150);
                result = Parse(current.Index, Request(current.Index, 1, BitConverter.GetBytes(3u)));
                if (result.Type != 2 || result.Identity != selected.Identity) throw new IOException(L.T("rgb.devicesChanged"));
                if (Matches(result, modeIndex, mode, brightness, current.Colors.Count, colors, color)) break;
                if (attempt == 2) throw new IOException(L.T("rgb.notConfirmed"));
            }
            foreach (var cached in selected.Modes)
            {
                var updated = result.Modes.FirstOrDefault(m => m.Name == cached.Name && m.Value == cached.Value);
                if (updated != null) { cached.Brightness = updated.Brightness; cached.Colors = updated.Colors; }
            }
            selected.Colors = result.Colors; selected.ActiveMode = result.ActiveMode;
        }
        private static bool Matches(RgbDevice result, int index, RgbMode mode, uint brightness, int ledCount, int colors, uint color)
        {
            if (result.ActiveMode != index) return false;
            var reported = result.Modes[index];
            if (reported.Name != mode.Name || reported.Value != mode.Value) return false;
            if (mode.NativeBrightness && reported.Brightness != brightness) return false;
            if (mode.PerLed && (result.Colors.Count != ledCount || result.Colors.Any(c => c != color))) return false;
            return !mode.ModeColor || (reported.Colors.Count == colors && reported.Colors.All(c => c == color));
        }
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0) socket.Close();
        }
    }
}
