using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Radeoner
{
    [DataContract]
    public sealed class ProfileRange
    {
        [DataMember(IsRequired = true, Order = 0)] public int Min;
        [DataMember(IsRequired = true, Order = 1)] public int Max;
        [DataMember(IsRequired = true, Order = 2)] public int Step;
        internal ProfileRange(IntRange range) { Min = range.Min; Max = range.Max; Step = range.Step; }
        internal bool Matches(IntRange range) { return Min == range.Min && Max == range.Max && Step == range.Step; }
        internal bool Contains(int value) { return new IntRange(Min, Max, Step).Contains(value); }
    }
    [DataContract]
    public sealed class ProfileSetting
    {
        [DataMember(IsRequired = true, Order = 0)] public string Id;
        [DataMember(IsRequired = true, Order = 1)] public string Unit;
        [DataMember(IsRequired = true, Order = 2)] public ProfileRange Range;
        [DataMember(IsRequired = true, Order = 3)] public int Value;
    }
    [DataContract]
    public sealed class ProfilePoint
    {
        [DataMember(IsRequired = true, Order = 0)] public int Temperature;
        [DataMember(IsRequired = true, Order = 1)] public int Speed;
    }
    [DataContract]
    public sealed class GpuProfile
    {
        [DataMember(IsRequired = true, Order = 0)] public string Format = "Radeoner.GpuProfile";
        [DataMember(IsRequired = true, Order = 1)] public int Version = 1;
        [DataMember(Order = 2)] public string CreatedUtc;
        [DataMember(IsRequired = true, Order = 3)] public string GpuName;
        [DataMember(IsRequired = true, Order = 4)] public string HardwareId;
        [DataMember(IsRequired = true, Order = 5)] public int GpuType;
        [DataMember(IsRequired = true, Order = 6)] public List<ProfileSetting> Settings;
        [DataMember(Order = 7)] public List<ProfilePoint> Fan;
        [DataMember(Order = 8)] public ProfileRange FanTemperature;
        [DataMember(Order = 9)] public ProfileRange FanSpeed;
        [DataMember(Order = 10)] public bool? ZeroRpm;
    }
    public static class Profiles
    {
        internal static string HardwareKey(GpuInfo gpu)
        {
            string id = (gpu.Id ?? "").ToUpperInvariant();
            var vendor = Regex.Match(id, @"VEN_([0-9A-F]{4})(?=&|\\|$)");
            var device = Regex.Match(id, @"DEV_([0-9A-F]{4})(?=&|\\|$)");
            var subsystem = Regex.Match(id, @"SUBSYS_([0-9A-F]{8})(?=&|\\|$)");
            if (!vendor.Success || !device.Success) return "PNP:" + id;
            return "VEN_" + vendor.Groups[1].Value + "&DEV_" + device.Groups[1].Value + (subsystem.Success ? "&SUBSYS_" + subsystem.Groups[1].Value : "");
        }
        public static GpuProfile Capture(Snapshot snapshot, Changes edits)
        {
            Validation.Validate(snapshot, edits);
            var p = new GpuProfile { CreatedUtc = DateTime.UtcNow.ToString("o"), GpuName = snapshot.Gpu.Name, HardwareId = HardwareKey(snapshot.Gpu), GpuType = snapshot.Gpu.Type };
            p.Settings = snapshot.Settings.Select(s => new ProfileSetting { Id = s.Id, Unit = s.Unit, Range = new ProfileRange(s.Range), Value = edits.Values.ContainsKey(s.Id) ? edits.Values[s.Id] : s.Value }).ToList();
            var fan = edits.Fan ?? snapshot.Fan;
            if (fan.Count > 0)
            {
                p.Fan = fan.Select(f => new ProfilePoint { Temperature = f.Temperature, Speed = f.Speed }).ToList();
                p.FanTemperature = new ProfileRange(snapshot.FanTemperature); p.FanSpeed = new ProfileRange(snapshot.FanSpeed);
            }
            p.ZeroRpm = edits.ZeroRpm ?? snapshot.ZeroRpm;
            return p;
        }
        public static void Save(string path, GpuProfile profile) { JsonFile.Write(path, profile); }
        public static GpuProfile Load(string path)
        {
            try { return JsonFile.Read<GpuProfile>(path, 1024 * 1024); }
            catch (SerializationException ex) { throw new InvalidOperationException(L.T("profile.invalid"), ex); }
            catch (System.Xml.XmlException ex) { throw new InvalidOperationException(L.T("profile.invalid"), ex); }
        }
        // Only constructs editor changes. It never calls any backend method or setter.
        public static Changes Prepare(Snapshot current, GpuProfile profile)
        {
            if (profile == null || profile.Format != "Radeoner.GpuProfile" || profile.Version != 1 || profile.Settings == null || profile.Settings.Count > 256 || string.IsNullOrWhiteSpace(profile.GpuName))
                throw new InvalidOperationException(L.T("profile.invalid"));
            if (string.IsNullOrWhiteSpace(current.Gpu.Id) || !string.Equals(profile.HardwareId, HardwareKey(current.Gpu), StringComparison.OrdinalIgnoreCase) || profile.GpuType != current.Gpu.Type)
                throw new InvalidOperationException(L.T("profile.wrongGpu", profile.GpuName, current.Gpu.Name));
            var changes = new Changes(); var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in profile.Settings)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Id) || entry.Id.Length > 100 || !seen.Add(entry.Id) || entry.Range == null)
                    throw new InvalidOperationException(L.T("profile.invalid"));
                var target = current.Settings.SingleOrDefault(s => s.Id == entry.Id);
                if (target == null) throw new InvalidOperationException(L.T("profile.unavailable", entry.Id));
                if (target.Unit != entry.Unit || !entry.Range.Matches(target.Range)) throw new InvalidOperationException(L.T("profile.rangeChanged", target.Name));
                if (!entry.Range.Contains(entry.Value)) throw new InvalidOperationException(L.T("profile.invalidValue", target.Name));
                if (target.Value != entry.Value) changes.Values.Add(entry.Id, entry.Value);
            }
            if (profile.Fan != null)
            {
                if (profile.Fan.Count == 0 || profile.Fan.Count > 32 || profile.Fan.Any(p => p == null) || profile.FanTemperature == null || profile.FanSpeed == null ||
                    !profile.FanTemperature.Matches(current.FanTemperature) || !profile.FanSpeed.Matches(current.FanSpeed)) throw new InvalidOperationException(L.T("profile.fanChanged"));
                var points = profile.Fan.Select(f => new FanPoint(f.Temperature, f.Speed)).ToList();
                // Validate even when all points equal the current values.
                Validation.Validate(current, new Changes { Fan = points });
                if (!Validation.SameFan(points, current.Fan)) changes.Fan = points;
            }
            if (profile.ZeroRpm.HasValue)
            {
                if (!current.ZeroRpm.HasValue) throw new InvalidOperationException(L.T("profile.zeroUnavailable"));
                if (profile.ZeroRpm != current.ZeroRpm) changes.ZeroRpm = profile.ZeroRpm;
            }
            Validation.Validate(current, changes); return changes;
        }
    }
}
