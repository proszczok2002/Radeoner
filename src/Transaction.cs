using System;
using System.Collections.Generic;
using System.Linq;

namespace Radeoner
{
    public sealed class ApplyResult
    {
        public Snapshot After;
        public Changes Undo;
    }
    public static class TuningTransaction
    {
        private sealed class Operation { internal Action Apply, Undo; }
        public static ApplyResult Apply(IBackend backend, Snapshot reviewed, Changes changes)
        {
            if (changes.Empty) throw new InvalidOperationException(L.T("driver.109"));
            int gpu = reviewed.Gpu.Index;
            var before = backend.Read(gpu);
            if (before.Gpu.Id != reviewed.Gpu.Id) throw new InvalidOperationException(L.T("driver.110"));
            Validation.Validate(before, changes);
            var undo = new Changes();
            foreach (var pair in changes.Values)
            {
                var old = reviewed.Settings.Single(x => x.Id == pair.Key);
                var current = before.Settings.Single(x => x.Id == pair.Key);
                if (old.Value != current.Value || old.Range.Min != current.Range.Min || old.Range.Max != current.Range.Max || old.Range.Step != current.Range.Step)
                    throw new InvalidOperationException(L.T("driver.111") + current.Name + L.T("driver.112"));
                undo.Values.Add(pair.Key, current.Value);
            }
            if (changes.Fan != null)
            {
                if (!Validation.SameFan(reviewed.Fan, before.Fan)) throw new InvalidOperationException(L.T("driver.113"));
                undo.Fan = before.Fan.Select(p => new FanPoint(p.Temperature, p.Speed)).ToList();
            }
            if (changes.ZeroRpm.HasValue)
            {
                if (before.ZeroRpm != reviewed.ZeroRpm) throw new InvalidOperationException(L.T("driver.114"));
                undo.ZeroRpm = before.ZeroRpm;
            }
            var operations = new List<Operation>();
            var scalarIds = changes.Values.Keys.Where(x => !x.Contains(".state.")).ToList();
            // If the new minimum exceeds the old maximum, expand the maximum first.
            if (scalarIds.Contains("gfx.min") && scalarIds.Contains("gfx.max"))
            {
                var max = before.Settings.Single(x => x.Id == "gfx.max");
                if (max.Range.Min > 0 && changes.Values["gfx.min"] > max.Value)
                { scalarIds.Remove("gfx.max"); scalarIds.Insert(0, "gfx.max"); }
                else { scalarIds.Remove("gfx.min"); scalarIds.Insert(0, "gfx.min"); }
            }
            foreach (string id in scalarIds)
            {
                string key = id;
                operations.Add(new Operation { Apply = () => backend.WriteSetting(gpu, key, changes.Values[key]), Undo = () => backend.WriteSetting(gpu, key, undo.Values[key]) });
            }
            foreach (string group in new[] { "gfx.state.", "vram.state." })
            {
                string prefix = group;
                var next = changes.Values.Where(x => x.Key.StartsWith(prefix, StringComparison.Ordinal)).ToDictionary(x => x.Key, x => x.Value);
                if (next.Count == 0) continue;
                var previous = undo.Values.Where(x => x.Key.StartsWith(prefix, StringComparison.Ordinal)).ToDictionary(x => x.Key, x => x.Value);
                operations.Add(new Operation { Apply = () => backend.WriteStates(gpu, prefix, next), Undo = () => backend.WriteStates(gpu, prefix, previous) });
            }
            if (changes.Fan != null) operations.Add(new Operation { Apply = () => backend.WriteFan(gpu, changes.Fan), Undo = () => backend.WriteFan(gpu, undo.Fan) });
            if (changes.ZeroRpm.HasValue) operations.Add(new Operation { Apply = () => backend.WriteZeroRpm(gpu, changes.ZeroRpm.Value), Undo = () => backend.WriteZeroRpm(gpu, undo.ZeroRpm.Value) });
            var attempted = new List<Operation>();
            try
            {
                foreach (var op in operations) { attempted.Add(op); op.Apply(); }
                var after = backend.Read(gpu);
                Verify(after, changes);
                return new ApplyResult { After = after, Undo = undo };
            }
            catch (Exception ex)
            {
                var failures = new List<string>();
                for (int i = attempted.Count - 1; i >= 0; i--) try { attempted[i].Undo(); } catch (Exception error) { failures.Add(error.Message); }
                try { Verify(backend.Read(gpu), undo); } catch (Exception error) { failures.Add(error.Message); }
                throw new InvalidOperationException(L.T("driver.115") + ex.Message + "\r\n" +
                    (failures.Count == 0 ? L.T("driver.116") : L.T("driver.117") + string.Join("; ", failures) + L.T("driver.118")), ex);
            }
        }
        public static void Verify(Snapshot current, Changes expected)
        {
            foreach (var pair in expected.Values)
            {
                var setting = current.Settings.SingleOrDefault(x => x.Id == pair.Key);
                if (setting == null || setting.Value != pair.Value) throw new InvalidOperationException(L.T("driver.119") + pair.Key + ".");
            }
            if (expected.Fan != null && !Validation.SameFan(current.Fan, expected.Fan)) throw new InvalidOperationException(L.T("driver.120"));
            if (expected.ZeroRpm.HasValue && expected.ZeroRpm != current.ZeroRpm) throw new InvalidOperationException(L.T("driver.121"));
        }
    }
}
