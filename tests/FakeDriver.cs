using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Radeoner;

// In-memory implementations of the documented ADLX C vtables. These tests
// exercise marshaling and backend calls without loading an AMD driver DLL.
internal sealed class FakeDriver : IDisposable
{
    internal sealed class Obj
    {
        internal IntPtr Pointer, Table;
        internal int References;
        internal List<Delegate> Delegates = new List<Delegate>();
        internal Obj(int slots)
        {
            Table = Marshal.AllocHGlobal(slots * IntPtr.Size); Pointer = Marshal.AllocHGlobal(IntPtr.Size);
            for (int i = 0; i < slots; i++) Marshal.WriteIntPtr(Table, i * IntPtr.Size, IntPtr.Zero);
            Marshal.WriteIntPtr(Pointer, Table);
        }
        internal void Set(int slot, Delegate fn) { Delegates.Add(fn); Marshal.WriteIntPtr(Table, slot * IntPtr.Size, Marshal.GetFunctionPointerForDelegate(fn)); }
    }
    internal Obj System;
    internal int Writes;
    internal bool Unsupported, Legacy, RejectWrites, NoSensors, Factory = true;
    internal string RejectOnceId, PartialFailOnceId, IgnoreOnceId;
    internal Dictionary<string, int> Values = new Dictionary<string, int> { { "gfx.min", 1000 }, { "gfx.max", 2000 }, { "gfx.voltage", 1000 }, { "vram.max", 1500 }, { "power", 0 }, { "fan.min", 1000 }, { "fan.target", 2000 } };
    internal List<FanPoint> Fan = new List<FanPoint> { new FanPoint(50, 20), new FanPoint(80, 80) };
    internal int[][] GfxStates = { new[] { 800, 900 }, new[] { 1200, 1000 } };
    internal int[][] VramStates = { new[] { 800, 900 }, new[] { 1400, 1000 } };
    internal bool ZeroRpm = true;
    private readonly List<Obj> objects = new List<Obj>();
    private readonly List<IntPtr> strings = new List<IntPtr>();

    private Obj Object(int slots, params string[] interfaces)
    {
        var o = new Obj(slots); objects.Add(o);
        o.Set(0, new Native.Ref(p => ++o.References)); o.Set(1, new Native.Ref(p => --o.References));
        o.Set(2, new Native.QueryInterface((IntPtr p, string name, out IntPtr result) =>
        {
            bool matches = interfaces.Contains(name);
            if (name.StartsWith("IADLXManualGraphicsTuning")) matches = name == (Legacy ? "IADLXManualGraphicsTuning1" : "IADLXManualGraphicsTuning2") && interfaces.Any(n => n.StartsWith("IADLXManualGraphicsTuning"));
            if (name.StartsWith("IADLXManualVRAMTuning")) matches = name == (Legacy ? "IADLXManualVRAMTuning1" : "IADLXManualVRAMTuning2") && interfaces.Any(n => n.StartsWith("IADLXManualVRAMTuning"));
            result = matches ? Ret(o) : IntPtr.Zero; return matches ? 0 : 6;
        }));
        return o;
    }
    private IntPtr Ret(Obj o) { o.References++; return o.Pointer; }
    private IntPtr Text(string s) { var p = Marshal.StringToHGlobalAnsi(s); strings.Add(p); return p; }
    private void Ptr(Obj owner, int slot, Obj target) { owner.Set(slot, new Native.GetPtr((IntPtr p, out IntPtr v) => { v = Ret(target); return 0; })); }
    private void Integer(Obj o, int slot, Func<int> get) { o.Set(slot, new Native.GetInt((IntPtr p, out int v) => { v = get(); return 0; })); }
    private void Bool(Obj o, int slot, Func<bool> get) { o.Set(slot, new Native.GetBool((IntPtr p, out byte v) => { v = get() ? (byte)1 : (byte)0; return 0; })); }
    private void String(Obj o, int slot, string value)
    {
        var str = Text(value); o.Set(slot, new Native.GetPtr((IntPtr p, out IntPtr v) => { v = str; return 0; }));
    }
    private void Scalar(Obj o, int start, string id, IntRange range)
    {
        o.Set(start, new Native.GetRange((IntPtr p, out IntRange r) => { r = range; return 0; }));
        Integer(o, start + 1, () => Values[id]);
        o.Set(start + 2, new Native.SetInt((p, value) =>
        {
            Writes++; if (RejectWrites) return 3;
            if (RejectOnceId == id) { RejectOnceId = null; return 3; }
            if (IgnoreOnceId == id) { IgnoreOnceId = null; return 0; }
            if (id == "gfx.min" && value > Values["gfx.max"] || id == "gfx.max" && value < Values["gfx.min"]) return 4;
            Values[id] = value; Factory = false;
            if (PartialFailOnceId == id) { PartialFailOnceId = null; return 3; }
            return 0;
        }));
    }
    private Obj List<T>(IList<T> items, Func<T, Obj> at)
    {
        var list = Object(13);
        list.Set(3, new Native.Size(p => (uint)items.Count));
        // The generic At intentionally fails: production runtimes can do the same.
        list.Set(7, new Native.At((IntPtr p, uint index, out IntPtr v) => { v = IntPtr.Zero; return 6; }));
        list.Set(11, new Native.At((IntPtr p, uint index, out IntPtr v) => { v = Ret(at(items[(int)index])); return 0; }));
        return list;
    }
    internal FakeDriver()
    {
        var gpu = Object(19, "IADLXGPU");
        String(gpu, 7, "Testowy Radeon"); String(gpu, 9, "PCI\\VEN_1002&DEV_TEST"); Integer(gpu, 5, () => Unsupported ? 1 : 2);
        Integer(gpu, 11, () => 8192); String(gpu, 12, "GDDR6"); String(gpu, 8, "test-driver-key");
        var list = List(new[] { gpu }, x => x);
        var gfx = Object(12, "IADLXManualGraphicsTuning1", "IADLXManualGraphicsTuning2");
        var vram = Object(12, "IADLXManualVRAMTuning1", "IADLXManualVRAMTuning2");
        var power = Object(10, "IADLXManualPowerTuning");
        var fan = Object(23, "IADLXManualFanTuning");
        Scalar(gfx, 3, "gfx.min", new IntRange(500, 3000, 10)); Scalar(gfx, 6, "gfx.max", new IntRange(500, 3000, 10)); Scalar(gfx, 9, "gfx.voltage", new IntRange(700, 1200, 10));
        Scalar(vram, 7, "vram.max", new IntRange(500, 2500, 10)); Scalar(power, 3, "power", new IntRange(-20, 20, 1));
        Scalar(fan, 16, "fan.min", new IntRange(500, 4000, 100)); Scalar(fan, 20, "fan.target", new IntRange(500, 4000, 100));
        Bool(fan, 15, () => true); Bool(fan, 19, () => true); Bool(fan, 8, () => true); Bool(fan, 9, () => ZeroRpm);
        fan.Set(10, new Native.SetBool((p, v) => { Writes++; ZeroRpm = v != 0; Factory = false; return 0; }));
        fan.Set(3, new Native.GetRanges((IntPtr p, out IntRange speed, out IntRange temp) => { speed = new IntRange(0, 100, 1); temp = new IntRange(30, 100, 1); return 0; }));
        var fanLists = new Dictionary<IntPtr, List<FanPoint>>();
        fan.Set(4, new Native.GetPtr((IntPtr p, out IntPtr v) =>
        {
            var copy = Fan.Select(x => new FanPoint(x.Temperature, x.Speed)).ToList();
            var result = List(copy, point =>
            {
                var state = Object(7);
                Integer(state, 3, () => point.Speed); Integer(state, 5, () => point.Temperature);
                state.Set(4, new Native.SetInt((s, n) => { point.Speed = n; return 0; }));
                state.Set(6, new Native.SetInt((s, n) => { point.Temperature = n; return 0; }));
                return state;
            });
            fanLists[result.Pointer] = copy; v = Ret(result); return 0;
        }));
        fan.Set(6, new Native.ValidateList((IntPtr p, IntPtr l, out int error) => { error = -1; return 0; }));
        fan.Set(7, new Native.SetList((p, l) => { Writes++; Fan = fanLists[l].Select(x => new FanPoint(x.Temperature, x.Speed)).ToList(); Factory = false; return 0; }));
        SetupLegacy(gfx, 3, 4, true); SetupLegacy(vram, 7, 8, false);
        var tuning = Object(18, "IADLXGPUTuningServices");
        tuning.Set(4, new Native.GpuBool((IntPtr p, IntPtr g, out byte v) => { v = Factory ? (byte)1 : (byte)0; return 0; }));
        tuning.Set(5, new Native.GpuAction((p, g) => { Writes++; Factory = true; return 0; }));
        for (int i = 8; i <= 11; i++) tuning.Set(i, new Native.GpuBool((IntPtr p, IntPtr g, out byte v) => { v = Unsupported ? (byte)0 : (byte)1; return 0; }));
        Obj[] features = { gfx, vram, fan, power };
        for (int i = 0; i < features.Length; i++)
        {
            Obj feature = features[i];
            tuning.Set(14 + i, new Native.GpuPtr((IntPtr p, IntPtr g, out IntPtr v) => { v = Ret(feature); return 0; }));
        }
        var monitor = Object(23); var support = Object(26); var metrics = Object(15);
        double[] doubles = { 12.5, 0, 0, 42.5, 50.5, 10.5, 12.5, 0, 0, 0 };
        int[] integers = { 0, 800, 900, 0, 0, 0, 0, 1200, 256, 900 };
        for (int i = 0; i < 10; i++)
        {
            int index = i; Bool(support, 3 + i, () => !NoSensors && index != 4);
            if (new[] { 0, 3, 4, 5, 6 }.Contains(i)) metrics.Set(4 + i, new Native.GetDouble((IntPtr p, out double v) => { v = doubles[index]; return 0; }));
            else Integer(metrics, 4 + i, () => integers[index]);
        }
        monitor.Set(18, new Native.GpuPtr((IntPtr p, IntPtr g, out IntPtr v) => { v = Ret(metrics); return 0; }));
        monitor.Set(21, new Native.GpuPtr((IntPtr p, IntPtr g, out IntPtr v) => { v = Ret(support); return 0; }));
        System = new Obj(12); objects.Add(System);
        Ptr(System, 1, list); Ptr(System, 8, tuning); Ptr(System, 9, monitor);
    }
    private void SetupLegacy(Obj obj, int rangesSlot, int statesSlot, bool graphics)
    {
        // Select the legacy table by QueryInterface before this helper is enabled.
        // The returned object has its own vtable since its layout differs from v2.
        var legacy = Object(12);
        var tables = new Dictionary<IntPtr, int[][]>();
        legacy.Set(rangesSlot, new Native.GetRanges((IntPtr p, out IntRange frequency, out IntRange voltage) => { frequency = new IntRange(500, 2500, 10); voltage = new IntRange(700, 1200, 10); return 0; }));
        legacy.Set(statesSlot, new Native.GetPtr((IntPtr p, out IntPtr v) =>
        {
            var data = (graphics ? GfxStates : VramStates).Select(a => a.ToArray()).ToArray();
            var list = List(data, row =>
            {
                var state = Object(7);
                Integer(state, 3, () => row[0]); Integer(state, 5, () => row[1]);
                state.Set(4, new Native.SetInt((s, n) => { row[0] = n; return 0; }));
                state.Set(6, new Native.SetInt((s, n) => { row[1] = n; return 0; }));
                return state;
            });
            tables[list.Pointer] = data; v = Ret(list); return 0;
        }));
        legacy.Set(statesSlot + 2, new Native.ValidateList((IntPtr p, IntPtr l, out int error) => { error = -1; return 0; }));
        legacy.Set(statesSlot + 3, new Native.SetList((p, l) => { Writes++; if (graphics) GfxStates = tables[l]; else VramStates = tables[l]; Factory = false; return 0; }));
        obj.Set(2, new Native.QueryInterface((IntPtr p, string name, out IntPtr v) =>
        {
            string prefix = graphics ? "IADLXManualGraphicsTuning" : "IADLXManualVRAMTuning";
            bool matches = name == prefix + (Legacy ? "1" : "2");
            v = matches ? Ret(Legacy ? legacy : obj) : IntPtr.Zero; return matches ? 0 : 6;
        }));
    }
    internal void CheckReleased()
    {
        foreach (var o in objects) if (o != System && o.References != 0) throw new Exception("Native reference leak or double release: " + o.References);
    }
    public void Dispose()
    {
        foreach (var p in strings) Marshal.FreeHGlobal(p);
        foreach (var o in objects) { Marshal.FreeHGlobal(o.Pointer); Marshal.FreeHGlobal(o.Table); }
    }
}
