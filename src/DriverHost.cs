using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Text;

namespace Radeoner
{
    [DataContract] internal sealed class DriverRequest
    {
        [DataMember] internal string Operation, Language, Id;
        [DataMember] internal int Gpu, Value;
        [DataMember] internal bool Flag;
        [DataMember] internal Dictionary<string, int> Values;
        [DataMember] internal List<FanPoint> Fan;
    }
    [DataContract] internal sealed class DriverResponse
    {
        [DataMember] internal string Status, Error;
        [DataMember] internal List<GpuInfo> Gpus;
        [DataMember] internal Snapshot Snapshot;
    }
    internal static class DriverWire
    {
        internal static string Encode<T>(T value)
        { using (var stream = new MemoryStream()) { new DataContractJsonSerializer(typeof(T)).WriteObject(stream, value); return Convert.ToBase64String(stream.ToArray()); } }
        internal static T Decode<T>(string value)
        {
            if (value == null) throw new IOException(L.T("driver.workerEnded"));
            if (value.Length > 2 * 1024 * 1024) throw new IOException(L.T("driver.workerProtocol"));
            return UpdateFeed.Parse<T>(Convert.FromBase64String(value));
        }
    }
    internal static class DriverHost
    {
        [DllImport("kernel32.dll")] private static extern uint SetErrorMode(uint mode);
        internal static IBackend CreateBackend()
        { try { return new AmdBackend(); } catch (Exception ex) { Diagnostics.Log("driver initialization", ex); return new UnavailableBackend(ex.Message); } }
        internal static int Run(int parentId, Func<IBackend> factory)
        {
            SetErrorMode(3); // A failing driver must not leave a Windows crash dialog blocking the helper.
            try
            {
                using (var parent = Process.GetProcessById(parentId))
                using (var watcher = new Timer(s => { try { if (parent.HasExited) Environment.Exit(0); } catch { Environment.Exit(0); } }, null, 1000, 1000))
                using (var input = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false, true), true))
                using (var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true })
                using (var backend = factory())
                {
                    string line;
                    while ((line = input.ReadLine()) != null)
                    {
                        var request = DriverWire.Decode<DriverRequest>(line);
                        if (request.Operation == "exit") break;
                        var result = new DriverResponse();
                        try
                        {
                            if (request.Language != L.Language) L.Load(request.Language);
                            switch (request.Operation)
                            {
                                case "hello": result.Gpus = backend.Gpus; result.Status = backend.Status; break;
                                case "read": result.Snapshot = backend.Read(request.Gpu); break;
                                case "sensors": result.Snapshot = backend is ISensorBackend ? ((ISensorBackend)backend).ReadSensors(request.Gpu) : backend.Read(request.Gpu); break;
                                case "setting": backend.WriteSetting(request.Gpu, request.Id, request.Value); break;
                                case "states": backend.WriteStates(request.Gpu, request.Id, request.Values); break;
                                case "fan": backend.WriteFan(request.Gpu, request.Fan); break;
                                case "zero": backend.WriteZeroRpm(request.Gpu, request.Flag); break;
                                case "reset": backend.Reset(request.Gpu); break;
                                default: throw new InvalidOperationException(L.T("driver.workerProtocol"));
                            }
                        }
                        catch (Exception ex) { result.Error = ex.Message; Diagnostics.Log(request.Operation, ex); }
                        output.WriteLine(DriverWire.Encode(result));
                    }
                }
                return 0;
            }
            catch (Exception ex) { Diagnostics.Log("driver host", ex); return 1; }
        }
    }
    // ADLX lives entirely in this owned child process. Native crashes and stalled
    // calls therefore cannot take down the UI or block its message loop.
    internal sealed class IsolatedBackend : IBackend, ISensorBackend
    {
        private readonly object gate = new object();
        private readonly string executable, arguments;
        private readonly int timeout;
        private Process worker;
        private bool disposed, writeBlocked;
        private DateTime retryAfter;
        public List<GpuInfo> Gpus { get; private set; }
        public string Status { get; private set; }
        internal IsolatedBackend() : this(typeof(IsolatedBackend).Assembly.Location, "--driver-worker", 6000) { }
        internal IsolatedBackend(string path, string command, int timeoutMs)
        { executable = path; arguments = command; timeout = timeoutMs; try { Start(); } catch { Stop(); throw; } }
        private DriverResponse Exchange(DriverRequest request, int milliseconds)
        {
            request.Language = L.Language;
            worker.StandardInput.WriteLine(DriverWire.Encode(request)); worker.StandardInput.Flush();
            var response = worker.StandardOutput.ReadLineAsync();
            response.ContinueWith(t => { var ignored = t.Exception; }, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
            if (!response.Wait(milliseconds)) throw new TimeoutException(L.T("driver.workerTimeout"));
            return DriverWire.Decode<DriverResponse>(response.GetAwaiter().GetResult());
        }
        private void Start()
        {
            if (disposed) throw new ObjectDisposedException("IsolatedBackend");
            worker = Process.Start(new ProcessStartInfo(executable, arguments + " " + Process.GetCurrentProcess().Id)
            { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardInput = true, RedirectStandardOutput = true, WorkingDirectory = Path.GetDirectoryName(executable) });
            var hello = Exchange(new DriverRequest { Operation = "hello" }, Math.Max(timeout, 20000));
            if (hello.Error != null || hello.Gpus == null || hello.Gpus.Count > 64) throw new IOException(hello.Error ?? L.T("driver.workerProtocol"));
            if (Gpus != null && (Gpus.Count != hello.Gpus.Count || !SameDevices(Gpus, hello.Gpus))) throw new IOException(L.T("driver.workerDevices"));
            Gpus = hello.Gpus; Status = hello.Status;
        }
        private static bool SameDevices(List<GpuInfo> before, List<GpuInfo> after)
        { for (int i = 0; i < before.Count; i++) if (!string.Equals(before[i].Id, after[i].Id, StringComparison.OrdinalIgnoreCase)) return false; return true; }
        private DriverResponse Call(DriverRequest request, bool read)
        {
            lock (gate)
            {
                if (disposed) throw new ObjectDisposedException("IsolatedBackend");
                if (!read && writeBlocked) throw new InvalidOperationException(L.T("driver.workerReadFirst"));
                DriverResponse result;
                try
                {
                    if (worker == null)
                    {
                        if (!read || DateTime.UtcNow < retryAfter) throw new IOException(L.T("driver.workerRetry"));
                        Start();
                    }
                    result = Exchange(request, timeout);
                }
                catch (Exception ex)
                {
                    writeBlocked = true; retryAfter = DateTime.UtcNow.AddSeconds(2); Stop(); Diagnostics.Log("driver IPC " + request.Operation, ex);
                    throw new InvalidOperationException(L.T("driver.workerFailure", ex.Message), ex);
                }
                if (result.Error != null) throw new InvalidOperationException(result.Error);
                return result;
            }
        }
        public Snapshot Read(int index)
        {
            lock (gate)
            {
                var data = Call(new DriverRequest { Operation = "read", Gpu = index }, true).Snapshot;
                if (data == null || data.Gpu == null) throw new IOException(L.T("driver.workerProtocol"));
                writeBlocked = false; return data;
            }
        }
        public Snapshot ReadSensors(int index)
        {
            var data = Call(new DriverRequest { Operation = "sensors", Gpu = index }, true).Snapshot;
            if (data == null || data.Gpu == null) throw new IOException(L.T("driver.workerProtocol")); return data;
        }
        public void WriteSetting(int gpu, string id, int value) { Call(new DriverRequest { Operation = "setting", Gpu = gpu, Id = id, Value = value }, false); }
        public void WriteStates(int gpu, string prefix, IDictionary<string, int> values) { Call(new DriverRequest { Operation = "states", Gpu = gpu, Id = prefix, Values = new Dictionary<string, int>(values) }, false); }
        public void WriteFan(int gpu, IList<FanPoint> points) { Call(new DriverRequest { Operation = "fan", Gpu = gpu, Fan = new List<FanPoint>(points) }, false); }
        public void WriteZeroRpm(int gpu, bool value) { Call(new DriverRequest { Operation = "zero", Gpu = gpu, Flag = value }, false); }
        public void Reset(int gpu) { Call(new DriverRequest { Operation = "reset", Gpu = gpu }, false); }
        private void Stop()
        {
            var owned = worker; worker = null; if (owned == null) return;
            try { owned.StandardInput.Close(); if (!owned.WaitForExit(250)) { owned.Kill(); owned.WaitForExit(1000); } } catch (InvalidOperationException) { } catch (System.ComponentModel.Win32Exception) { } catch (IOException) { }
            finally { owned.Dispose(); }
        }
        public void Dispose() { lock (gate) { disposed = true; Stop(); } }
    }
}
