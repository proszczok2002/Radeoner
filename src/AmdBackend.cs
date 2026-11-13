using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using Microsoft.Win32;

namespace Radeoner
{
    internal sealed class Binding
    {
        internal AdlxObject Object;
        internal int RangeSlot, GetSlot, SetSlot;
        internal string Id, Name, Unit, Note;
        internal Setting Read()
        {
            IntRange range;
            Native.Check(Object.Method<Native.GetRange>(RangeSlot)(Object.Pointer, out range), L.T("driver.001") + Name);
            int value = Object.Int(GetSlot);
            if (!range.Contains(value))
                throw new InvalidOperationException(L.T("driver.002") + Name);
            return new Setting { Id = Id, Name = Name, Unit = Unit, Note = Note, Range = range, Value = value };
        }
        internal void Write(int value) { Native.Check(Object.Method<Native.SetInt>(SetSlot)(Object.Pointer, value), L.T("driver.003") + Name); }
    }
    internal sealed class GpuContext : IDisposable
    {
        internal AdlxObject Gpu, Gfx, Vram, Power, Fan, GfxStates, VramStates;
        internal GpuInfo Info;
        internal List<Binding> Bindings = new List<Binding>();
        internal List<string> Notes = new List<string>();
        internal bool Ready, AnyTuning;
        internal string Language;
        internal void ClearTuning()
        {
            foreach (var o in new[] { Gfx, Vram, Power, Fan, GfxStates, VramStates }) if (o != null) o.Dispose();
            Gfx = Vram = Power = Fan = GfxStates = VramStates = null;
            Bindings.Clear(); Notes.Clear(); Ready = AnyTuning = false;
        }
        public void Dispose()
        {
            foreach (var o in new[] { Gfx, Vram, Power, Fan, GfxStates, VramStates, Gpu }) if (o != null) o.Dispose();
        }
    }
    public sealed class AmdBackend : IBackend
    {
        private AdlxLibrary library;
        private IntPtr system;
        private AdlxObject tuning, monitoring;
        private readonly List<GpuContext> contexts = new List<GpuContext>();
        private readonly List<GpuInfo> gpus = new List<GpuInfo>();
        public List<GpuInfo> Gpus { get { return gpus; } }
        public string Status { get { return contexts.Count == 0 ? L.T("driver.011") : L.T("driver.012") + contexts.Count + "."; } }
        public AmdBackend()
        {
            try
            {
                library = new AdlxLibrary(); system = library.System;
                Discover();
            }
            catch { Dispose(); throw; }
        }
        // Native contract tests inject an in-memory AMD interface without loading a driver.
        internal AmdBackend(IntPtr testSystem)
        {
            system = testSystem;
            try { Discover(); } catch { Dispose(); throw; }
        }
        private void Discover()
        {
            IntPtr pointer;
            Native.Check(Native.Method<Native.GetPtr>(system, Slots.IADLXSystem.GetGPUs)(system, out pointer), L.T("driver.006"));
            using (var list = new AdlxObject(pointer))
            {
                uint count = list.Count;
                if (count > 64) throw new InvalidOperationException(L.T("driver.007"));
                for (uint i = 0; i < count; i++)
                {
                    var gpu = list.At(i);
                    var ctx = new GpuContext { Gpu = gpu };
                    contexts.Add(ctx);
                    ctx.Info = new GpuInfo { Index = (int)i, Name = gpu.Text(Slots.IADLXGPU.Name), Type = gpu.Int(Slots.IADLXGPU.Type), Id = gpu.Text(Slots.IADLXGPU.PNPString) };
                    try { ctx.Info.Memory = gpu.Int(Slots.IADLXGPU.TotalVRAM) + " MB, " + gpu.Text(Slots.IADLXGPU.VRAMType); } catch { ctx.Info.Memory = null; }
                    try
                    {
                        string path = gpu.Text(Slots.IADLXGPU.DriverPath);
                        using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\" + path))
                            ctx.Info.Driver = key == null ? null : Convert.ToString(key.GetValue("DriverVersion", ""));
                    }
                    catch { ctx.Info.Driver = null; }
                    gpus.Add(ctx.Info);
                }
            }
            tuning = SystemService(Slots.IADLXSystem.GetGPUTuningServices);
            monitoring = SystemService(Slots.IADLXSystem.GetPerformanceMonitoringServices);
        }
        private AdlxObject SystemService(int slot)
        {
            IntPtr p; int code = Native.Method<Native.GetPtr>(system, slot)(system, out p);
            if (code != 0 || p == IntPtr.Zero) { if (p != IntPtr.Zero) new AdlxObject(p).Dispose(); return null; }
            return new AdlxObject(p);
        }
        private AdlxObject GpuService(AdlxObject service, int slot, GpuContext ctx)
        {
            IntPtr p;
            Native.Check(service.Method<Native.GpuPtr>(slot)(service.Pointer, ctx.Gpu.Pointer, out p), L.T("driver.013"));
            if (p == IntPtr.Zero) throw new InvalidOperationException(L.T("driver.014"));
            return new AdlxObject(p);
        }
        private AdlxObject Feature(GpuContext ctx, int supportSlot, int getSlot, string title)
        {
            if (tuning == null) { ctx.Notes.Add(title + L.T("driver.015")); return null; }
            byte supported;
            int code = tuning.Method<Native.GpuBool>(supportSlot)(tuning.Pointer, ctx.Gpu.Pointer, out supported);
            if (code != 0) { ctx.Notes.Add(title + L.T("driver.016") + Native.Error(code) + "."); return null; }
            if (supported == 0) { ctx.Notes.Add(title + L.T("driver.017")); return null; }
            ctx.AnyTuning = true;
            try { return GpuService(tuning, getSlot, ctx); }
            catch (Exception ex) { ctx.Notes.Add(title + ": " + ex.Message); return null; }
        }
        private void Bind(GpuContext ctx, AdlxObject obj, string id, string name, string unit, int range, int get, int set, string note)
        {
            ctx.Bindings.Add(new Binding { Object = obj, Id = id, Name = name, Unit = unit, RangeSlot = range, GetSlot = get, SetSlot = set, Note = note });
        }
        private GpuContext Context(int index)
        {
            if (index < 0 || index >= contexts.Count) throw new InvalidOperationException(L.T("driver.018"));
            var c = contexts[index];
            if (c.Ready && c.Language != L.Language) c.ClearTuning();
            if (c.Ready) return c;
            c.Ready = true; c.Language = L.Language;
            using (var feature = Feature(c, Slots.IADLXGPUTuningServices.IsSupportedManualGFXTuning, Slots.IADLXGPUTuningServices.GetManualGFXTuning, L.T("driver.019")))
            {
                if (feature != null)
                {
                    c.Gfx = feature.Query("IADLXManualGraphicsTuning2");
                    if (c.Gfx != null)
                    {
                        Bind(c, c.Gfx, "gfx.min", L.T("driver.020"), "MHz", 3, 4, 5, "");
                        Bind(c, c.Gfx, "gfx.max", L.T("driver.021"), "MHz", 6, 7, 8, L.T("driver.022"));
                        Bind(c, c.Gfx, "gfx.voltage", L.T("driver.023"), "mV", 9, 10, 11, L.T("driver.024"));
                    }
                    else { c.GfxStates = feature.Query("IADLXManualGraphicsTuning1"); if (c.GfxStates == null) c.Notes.Add(L.T("driver.025")); }
                }
            }
            using (var feature = Feature(c, Slots.IADLXGPUTuningServices.IsSupportedManualVRAMTuning, Slots.IADLXGPUTuningServices.GetManualVRAMTuning, L.T("driver.026")))
            {
                if (feature != null)
                {
                    c.Vram = feature.Query("IADLXManualVRAMTuning2");
                    if (c.Vram != null) Bind(c, c.Vram, "vram.max", L.T("driver.027"), "MHz", 7, 8, 9, "");
                    else { c.VramStates = feature.Query("IADLXManualVRAMTuning1"); if (c.VramStates == null) c.Notes.Add(L.T("driver.028")); }
                }
            }
            using (var feature = Feature(c, Slots.IADLXGPUTuningServices.IsSupportedManualPowerTuning, Slots.IADLXGPUTuningServices.GetManualPowerTuning, L.T("driver.029")))
                if (feature != null) { c.Power = feature.Query("IADLXManualPowerTuning"); if (c.Power == null) c.Notes.Add(L.T("driver.030")); }
            if (c.Power != null) Bind(c, c.Power, "power", L.T("driver.031"), "%", 3, 4, 5, L.T("driver.032"));
            using (var feature = Feature(c, Slots.IADLXGPUTuningServices.IsSupportedManualFanTuning, Slots.IADLXGPUTuningServices.GetManualFanTuning, L.T("driver.033")))
                if (feature != null) { c.Fan = feature.Query("IADLXManualFanTuning"); if (c.Fan == null) c.Notes.Add(L.T("driver.034")); }
            if (c.Fan != null)
            {
                try { if (c.Fan.Bool(Slots.IADLXManualFanTuning.IsSupportedMinFanSpeed)) Bind(c, c.Fan, "fan.min", L.T("driver.035"), "RPM", 16, 17, 18, ""); } catch (Exception ex) { c.Notes.Add(ex.Message); }
                try { if (c.Fan.Bool(Slots.IADLXManualFanTuning.IsSupportedTargetFanSpeed)) Bind(c, c.Fan, "fan.target", L.T("driver.036"), "RPM", 20, 21, 22, ""); } catch (Exception ex) { c.Notes.Add(ex.Message); }
            }
            return c;
        }
        public Snapshot Read(int gpuIndex)
        {
            var c = Context(gpuIndex);
            var s = new Snapshot { Gpu = c.Info, EnvironmentInfo = library == null ? L.T("driver.005") : "ADLX " + library.Version + L.T("driver.004") + library.Path, CanReset = tuning != null && c.AnyTuning };
            s.Notes.AddRange(c.Notes);
            s.Notes.Add(L.T("driver.037"));
            if (c.Info.Type == 1) s.Notes.Add(L.T("driver.038"));
            foreach (var binding in c.Bindings)
            {
                try { s.Settings.Add(binding.Read()); } catch (Exception ex) { s.Notes.Add(binding.Name + ": " + ex.Message); }
            }
            ReadStates(c.GfxStates, "gfx.state.", 3, 4, s);
            ReadStates(c.VramStates, "vram.state.", 7, 8, s);
            if (tuning != null)
            {
                byte atFactory;
                if (tuning.Method<Native.GpuBool>(Slots.IADLXGPUTuningServices.IsAtFactory)(tuning.Pointer, c.Gpu.Pointer, out atFactory) == 0) s.AtFactory = atFactory != 0;
            }
            if (c.Fan != null)
            {
                try
                {
                    Native.Check(c.Fan.Method<Native.GetRanges>(Slots.IADLXManualFanTuning.GetFanTuningRanges)(c.Fan.Pointer, out s.FanSpeed, out s.FanTemperature), L.T("driver.039"));
                    if (s.FanSpeed.Step <= 0 || s.FanTemperature.Step <= 0 || s.FanSpeed.Min > s.FanSpeed.Max || s.FanTemperature.Min > s.FanTemperature.Max) throw new InvalidOperationException(L.T("driver.040"));
                    using (var list = c.Fan.Get(Slots.IADLXManualFanTuning.GetFanTuningStates))
                    {
                        if (list.Count > 32) throw new InvalidOperationException(L.T("driver.041"));
                        for (uint i = 0; i < list.Count; i++) using (var state = list.At(i))
                        {
                            int temperature = state.Int(Slots.IADLXManualFanTuningState.GetTemperature), speed = state.Int(Slots.IADLXManualFanTuningState.GetFanSpeed);
                            if (!s.FanTemperature.Contains(temperature) || !s.FanSpeed.Contains(speed)) throw new InvalidOperationException(L.T("driver.042"));
                            s.Fan.Add(new FanPoint(temperature, speed));
                        }
                    }
                }
                catch (Exception ex) { s.Fan.Clear(); s.Notes.Add(L.T("driver.043") + ex.Message); }
                try { if (c.Fan.Bool(Slots.IADLXManualFanTuning.IsSupportedZeroRPM)) s.ZeroRpm = c.Fan.Bool(Slots.IADLXManualFanTuning.GetZeroRPMState); }
                catch (Exception ex) { s.Notes.Add("Zero RPM: " + ex.Message); }
            }
            ReadMetrics(c, s);
            return s;
        }
        private void ReadStates(AdlxObject obj, string prefix, int rangeSlot, int statesSlot, Snapshot snapshot)
        {
            if (obj == null) return;
            var settings = new List<Setting>();
            try
            {
                IntRange freq, volt;
                Native.Check(obj.Method<Native.GetRanges>(rangeSlot)(obj.Pointer, out freq, out volt), L.T("driver.044"));
                using (var list = obj.Get(statesSlot))
                {
                    if (list.Count > 32) throw new InvalidOperationException(L.T("driver.045"));
                    for (uint i = 0; i < list.Count; i++) using (var state = list.At(i))
                    {
                        foreach (bool voltage in new[] { false, true })
                        {
                            var range = voltage ? volt : freq;
                            if (range.Step <= 0 || range.Min > range.Max) continue;
                            int value = state.Int(voltage ? Slots.IADLXManualTuningState.GetVoltage : Slots.IADLXManualTuningState.GetFrequency);
                            if (!range.Contains(value)) continue;
                            settings.Add(new Setting { Id = prefix + i + (voltage ? ".voltage" : ".frequency"), Name = (prefix.StartsWith("gfx") ? "GPU" : L.T("driver.046")) + L.T("driver.047") + i + (voltage ? L.T("driver.048") : L.T("driver.049")), Unit = voltage ? "mV" : "MHz", Range = range, Value = value, Note = L.T("driver.050") });
                        }
                    }
                }
                snapshot.Settings.AddRange(settings);
            }
            catch (Exception ex) { snapshot.Notes.Add(L.T("driver.051") + ex.Message); }
        }
        private void ReadMetrics(GpuContext c, Snapshot snapshot)
        {
            if (monitoring == null) { snapshot.Metrics.Add(L.T("driver.052")); return; }
            try
            {
                using (var support = GpuService(monitoring, Slots.IADLXPerformanceMonitoringServices.GetSupportedGPUMetrics, c))
                using (var metrics = GpuService(monitoring, Slots.IADLXPerformanceMonitoringServices.GetCurrentGPUMetrics, c))
                {
                    string[] names = { L.T("driver.053"), L.T("driver.054"), L.T("driver.026"), L.T("driver.055"), L.T("driver.056"), L.T("driver.057"), L.T("driver.058"), L.T("driver.059"), L.T("driver.060"), L.T("driver.061") };
                    string[] units = { "%", "MHz", "MHz", "°C", "°C", "W", "W", "RPM", "MB", "mV" };
                    string[] ids = { "usage", "clock", "memoryClock", "temperature", "hotspot", "power", "boardPower", "fan", "vram", "voltage" };
                    bool[] doubles = { true, false, false, true, true, true, true, false, false, false };
                    for (int i = 0; i < names.Length; i++)
                    {
                        try
                        {
                            if (!support.Bool(3 + i)) { snapshot.Metrics.Add(names[i] + L.T("driver.062")); continue; }
                            double value;
                            if (doubles[i]) Native.Check(metrics.Method<Native.GetDouble>(4 + i)(metrics.Pointer, out value), names[i]);
                            else value = metrics.Int(4 + i);
                            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new InvalidOperationException(L.T("driver.063"));
                            snapshot.MetricValues[ids[i]] = value;
                            snapshot.Metrics.Add(names[i] + ": " + value.ToString("0.#", CultureInfo.CurrentCulture) + " " + units[i]);
                        }
                        catch (Exception ex) { snapshot.Metrics.Add(names[i] + ": " + ex.Message); }
                    }
                }
            }
            catch (Exception ex) { snapshot.Metrics.Add(L.T("driver.064") + ex.Message); }
        }
        public void WriteSetting(int gpuIndex, string id, int value)
        {
            var binding = Context(gpuIndex).Bindings.SingleOrDefault(x => x.Id == id);
            if (binding == null) throw new InvalidOperationException(L.T("driver.065") + id);
            if (!binding.Read().Range.Contains(value)) throw new InvalidOperationException(L.T("driver.066"));
            binding.Write(value);
        }
        public void WriteStates(int gpuIndex, string prefix, IDictionary<string, int> values)
        {
            var c = Context(gpuIndex);
            var obj = prefix == "gfx.state." ? c.GfxStates : prefix == "vram.state." ? c.VramStates : null;
            if (obj == null) throw new InvalidOperationException(L.T("driver.067"));
            int getSlot = prefix == "gfx.state." ? 4 : 8;
            using (var list = obj.Get(getSlot))
            {
                if (list.Count > 32) throw new InvalidOperationException(L.T("driver.068"));
                for (uint i = 0; i < list.Count; i++) using (var state = list.At(i))
                {
                    int value;
                    if (values.TryGetValue(prefix + i + ".frequency", out value)) Native.Check(state.Method<Native.SetInt>(4)(state.Pointer, value), L.T("driver.069") + i);
                    if (values.TryGetValue(prefix + i + ".voltage", out value)) Native.Check(state.Method<Native.SetInt>(6)(state.Pointer, value), L.T("driver.070") + i);
                }
                int errorIndex;
                Native.Check(obj.Method<Native.ValidateList>(getSlot + 2)(obj.Pointer, list.Pointer, out errorIndex), L.T("driver.071") + errorIndex);
                Native.Check(obj.Method<Native.SetList>(getSlot + 3)(obj.Pointer, list.Pointer), L.T("driver.072"));
            }
        }
        public void WriteFan(int gpuIndex, IList<FanPoint> points)
        {
            var obj = Context(gpuIndex).Fan;
            if (obj == null) throw new InvalidOperationException(L.T("driver.073"));
            using (var list = obj.Get(Slots.IADLXManualFanTuning.GetFanTuningStates))
            {
                if (list.Count != points.Count) throw new InvalidOperationException(L.T("driver.074"));
                for (uint i = 0; i < list.Count; i++) using (var state = list.At(i))
                {
                    Native.Check(state.Method<Native.SetInt>(Slots.IADLXManualFanTuningState.SetTemperature)(state.Pointer, points[(int)i].Temperature), L.T("driver.075") + (i + 1));
                    Native.Check(state.Method<Native.SetInt>(Slots.IADLXManualFanTuningState.SetFanSpeed)(state.Pointer, points[(int)i].Speed), L.T("driver.076") + (i + 1));
                }
                int errorIndex;
                int code = obj.Method<Native.ValidateList>(Slots.IADLXManualFanTuning.IsValidFanTuningStates)(obj.Pointer, list.Pointer, out errorIndex);
                Native.Check(code, L.T("driver.077") + errorIndex);
                Native.Check(obj.Method<Native.SetList>(Slots.IADLXManualFanTuning.SetFanTuningStates)(obj.Pointer, list.Pointer), L.T("driver.078"));
            }
        }
        public void WriteZeroRpm(int gpuIndex, bool value)
        {
            var obj = Context(gpuIndex).Fan;
            if (obj == null || !obj.Bool(Slots.IADLXManualFanTuning.IsSupportedZeroRPM)) throw new InvalidOperationException(L.T("driver.079"));
            Native.Check(obj.Method<Native.SetBool>(Slots.IADLXManualFanTuning.SetZeroRPMState)(obj.Pointer, value ? (byte)1 : (byte)0), L.T("driver.080"));
        }
        public void Reset(int gpuIndex)
        {
            var c = Context(gpuIndex);
            if (tuning == null || !c.AnyTuning) throw new InvalidOperationException(L.T("driver.081"));
            Native.Check(tuning.Method<Native.GpuAction>(Slots.IADLXGPUTuningServices.ResetToFactory)(tuning.Pointer, c.Gpu.Pointer), L.T("driver.082"));
        }
        public void Dispose()
        {
            foreach (var c in contexts) c.Dispose(); contexts.Clear();
            if (monitoring != null) { monitoring.Dispose(); monitoring = null; }
            if (tuning != null) { tuning.Dispose(); tuning = null; }
            if (library != null) { library.Dispose(); library = null; }
            system = IntPtr.Zero;
        }
    }

    // The Windows inventory is available even if ADLX is absent or rejects the driver.
    public sealed class UnavailableBackend : IBackend
    {
        public List<GpuInfo> Gpus { get; private set; }
        public string Status { get; private set; }
        public UnavailableBackend(string reason)
        {
            Status = reason; Gpus = new List<GpuInfo>();
            try
            {
                using (var query = new ManagementObjectSearcher("SELECT Name,PNPDeviceID,DriverVersion FROM Win32_VideoController"))
                using (var results = query.Get()) foreach (ManagementObject row in results) using (row)
                {
                    string id = Convert.ToString(row["PNPDeviceID"]);
                    if (id.IndexOf("VEN_1002", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    Gpus.Add(new GpuInfo { Index = Gpus.Count, Name = Convert.ToString(row["Name"]), Driver = Convert.ToString(row["DriverVersion"]), Id = id });
                }
            }
            catch (Exception ex) { Status += L.T("driver.083") + ex.Message; }
        }
        public Snapshot Read(int index)
        {
            var s = new Snapshot { Gpu = Gpus[index], EnvironmentInfo = L.T("driver.084") };
            s.Notes.Add(Status); s.Notes.Add(L.T("driver.085"));
            return s;
        }
        public void WriteSetting(int i, string id, int v) { throw new InvalidOperationException(Status); }
        public void WriteStates(int i, string p, IDictionary<string, int> v) { throw new InvalidOperationException(Status); }
        public void WriteFan(int i, IList<FanPoint> v) { throw new InvalidOperationException(Status); }
        public void WriteZeroRpm(int i, bool v) { throw new InvalidOperationException(Status); }
        public void Reset(int i) { throw new InvalidOperationException(Status); }
        public void Dispose() { }
    }
}
