using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Radeoner
{
    [DataContract] public sealed class GpuInfo
    {
        [DataMember] public int Index, Type;
        [DataMember] public string Name, Id, Memory, Driver;
        public string MemoryDescription { get { return string.IsNullOrEmpty(Memory) ? L.T("common.unavailable") : Memory; } }
        public string DriverDescription { get { return string.IsNullOrEmpty(Driver) ? L.T("common.unavailable") : Driver; } }
        public override string ToString() { return Name + " (" + L.T(Type == 1 ? "gpu.integrated" : Type == 2 ? "gpu.discrete" : "gpu.unknown") + ")"; }
    }
    [DataContract] public sealed class Setting
    {
        [DataMember] public string Id, Name, Unit, Note;
        [DataMember] public IntRange Range;
        [DataMember] public int Value;
    }
    [DataContract] public sealed class FanPoint
    {
        [DataMember] public int Temperature, Speed;
        public FanPoint(int temperature, int speed) { Temperature = temperature; Speed = speed; }
    }
    [DataContract] public sealed class Snapshot
    {
        [DataMember] public GpuInfo Gpu;
        [DataMember] public List<Setting> Settings = new List<Setting>();
        [DataMember] public List<FanPoint> Fan = new List<FanPoint>();
        [DataMember] public IntRange FanTemperature, FanSpeed;
        [DataMember] public bool? ZeroRpm, AtFactory;
        [DataMember] public bool CanReset;
        [DataMember] public List<string> Notes = new List<string>();
        [DataMember] public List<string> Metrics = new List<string>();
        [DataMember] public Dictionary<string, double> MetricValues = new Dictionary<string, double>();
        [DataMember] public DateTimeOffset CapturedAt = DateTimeOffset.Now;
        [DataMember] public string EnvironmentInfo;
        public string Report()
        {
            var b = new StringBuilder("Radeoner " + L.Version + "\r\n");
            b.AppendLine(EnvironmentInfo);
            if (Gpu != null) { b.AppendLine(Gpu.ToString()); b.AppendLine(L.T("report.id") + Gpu.Id); b.AppendLine(L.T("report.memory") + Gpu.MemoryDescription); b.AppendLine(L.T("report.driver") + Gpu.DriverDescription); }
            b.AppendLine(L.T("report.time") + CapturedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            foreach (string m in Metrics) b.AppendLine(m);
            b.AppendLine(); b.AppendLine(L.T("report.settings"));
            foreach (var s in Settings) b.AppendLine(L.T("report.setting", s.Name, s.Value, s.Unit, s.Range, s.Note));
            for (int i = 0; i < Fan.Count; i++) b.AppendLine(L.T("report.fanPoint", i + 1, Fan[i].Temperature, Fan[i].Speed));
            if (ZeroRpm.HasValue) b.AppendLine("Zero RPM: " + L.T(ZeroRpm.Value ? "common.on" : "common.off"));
            if (AtFactory.HasValue) b.AppendLine(L.T("report.factory") + L.T(AtFactory.Value ? "common.yes" : "common.no"));
            foreach (string n in Notes) b.AppendLine(n);
            return b.ToString();
        }
    }
    public sealed class Changes
    {
        public Dictionary<string, int> Values = new Dictionary<string, int>();
        public List<FanPoint> Fan; // Null means leave the fan curve unchanged.
        public bool? ZeroRpm;
        public bool Empty { get { return Values.Count == 0 && Fan == null && !ZeroRpm.HasValue; } }
    }
    public interface ISensorBackend
    {
        Snapshot ReadSensors(int gpuIndex);
    }
    public interface IBackend : IDisposable
    {
        List<GpuInfo> Gpus { get; }
        string Status { get; }
        Snapshot Read(int gpuIndex);
        void WriteSetting(int gpuIndex, string id, int value);
        void WriteStates(int gpuIndex, string prefix, IDictionary<string, int> values);
        void WriteFan(int gpuIndex, IList<FanPoint> points);
        void WriteZeroRpm(int gpuIndex, bool value);
        void Reset(int gpuIndex);
    }
    public static class Validation
    {
        public static void Validate(Snapshot snapshot, Changes changes)
        {
            foreach (var pair in changes.Values)
            {
                var s = snapshot.Settings.SingleOrDefault(x => x.Id == pair.Key);
                if (s == null) throw new InvalidOperationException(L.T("driver.101") + pair.Key);
                if (!s.Range.Contains(pair.Value)) throw new InvalidOperationException(s.Name + L.T("driver.102") + s.Range + " " + s.Unit + ".");
            }
            var min = snapshot.Settings.FirstOrDefault(x => x.Id == "gfx.min");
            var max = snapshot.Settings.FirstOrDefault(x => x.Id == "gfx.max");
            // A negative max-frequency range denotes offset-based controls. Do not compare
            // an absolute minimum frequency to an offset maximum frequency.
            if (min != null && max != null && max.Range.Min > 0)
            {
                int lo = changes.Values.ContainsKey(min.Id) ? changes.Values[min.Id] : min.Value;
                int hi = changes.Values.ContainsKey(max.Id) ? changes.Values[max.Id] : max.Value;
                if (lo > hi) throw new InvalidOperationException(L.T("driver.103"));
            }
            if (changes.ZeroRpm.HasValue && !snapshot.ZeroRpm.HasValue) throw new InvalidOperationException(L.T("driver.104"));
            if (changes.Fan != null)
            {
                if (snapshot.Fan.Count == 0 || changes.Fan.Count != snapshot.Fan.Count) throw new InvalidOperationException(L.T("driver.105"));
                for (int i = 0; i < changes.Fan.Count; i++)
                {
                    var p = changes.Fan[i];
                    if (!snapshot.FanTemperature.Contains(p.Temperature) || !snapshot.FanSpeed.Contains(p.Speed)) throw new InvalidOperationException(L.T("driver.106") + (i + 1) + L.T("driver.107"));
                    if (i > 0 && (p.Temperature <= changes.Fan[i - 1].Temperature || p.Speed < changes.Fan[i - 1].Speed)) throw new InvalidOperationException(L.T("driver.108"));
                }
            }
        }
        public static bool SameFan(IList<FanPoint> first, IList<FanPoint> second)
        {
            return first.Count == second.Count && first.Zip(second, (a, b) => a.Temperature == b.Temperature && a.Speed == b.Speed).All(x => x);
        }
        public static string Describe(Snapshot snapshot, Changes changes)
        {
            var b = new StringBuilder(snapshot.Gpu.ToString() + "\r\n\r\n");
            foreach (var s in snapshot.Settings.Where(x => changes.Values.ContainsKey(x.Id))) b.AppendLine(s.Name + ": " + s.Value + " → " + changes.Values[s.Id] + " " + s.Unit);
            if (changes.Fan != null) for (int i = 0; i < changes.Fan.Count; i++) b.AppendLine(L.T("report.fanPoint", i + 1, changes.Fan[i].Temperature, changes.Fan[i].Speed));
            if (changes.ZeroRpm.HasValue) b.AppendLine("Zero RPM: " + L.T(changes.ZeroRpm.Value ? "common.on" : "common.off"));
            return b.ToString();
        }
    }
}
